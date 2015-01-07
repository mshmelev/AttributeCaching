using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using AttributeCaching.CacheAdapters.ProtoBuf;
using BookSleeve;

namespace AttributeCaching.CacheAdapters
{
	/// <summary>
	/// Uses Redis as a central storage with local in-memory cache.
	/// Supports automatic failover between enlisted Redis servers (it's ok to be empty for the Redis server after switching over).
	/// Doesn't support named caches (cacheName parameter is ignored everywhere).
	/// Enabling of notifications on server is required with: CONFIG SET notify-keyspace-events Eg
	/// </summary>
	public class RedisCacheAdapter : CacheAdapter, IDisposable
	{
		private const int CacheDb = 0;
		private const int TagsDb = 1;
		private readonly TimeSpan RecentHistoryLifetime = TimeSpan.FromSeconds (2);

		private readonly RedisServer[] servers;
		private int curServer = -1;
		private const int ConnectRetryAttempts = 3;
		private bool isInited;
		private bool isDisposed;

		private RedisConnection redis;
		private RedisSubscriberConnection subChannel;
		private MemoryCache memoryCache;
		private MemoryCache recentKeys;

		private readonly static object sync= new object();

		/// <summary>
		/// Raised on any error occured: in background and in foreground
		/// </summary>
		public event EventHandler<Exception> OnError;

		/// <summary>
		/// Event name that should be sent when Redis DB is flushed (FLUSH command doesn't send any notifications by itself)
		/// </summary>
		public const string FlushedEventName = "__flushed";




		/// <summary>
		/// 
		/// </summary>
		/// <param name="connectionString">Example: Server=host1:port1,host2:port2</param>
		public RedisCacheAdapter(string connectionString)
		{
			var conBuilder = new DbConnectionStringBuilder();
			conBuilder.ConnectionString = connectionString;
			servers = ((string)conBuilder["Server"])
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => new RedisServer(s))
				.ToArray();

			OpenDb();
		}


		public void Dispose()
		{
			isDisposed = true;
			isInited = false;
			CloseRedisChannels();
			if (memoryCache!= null)
				memoryCache.Dispose();
			if (recentKeys!= null)
				recentKeys.Dispose();
		}


		private bool OpenDb()
		{
			try
			{
				lock (sync)
				{
					if (isDisposed)
						return false;

					CloseRedisChannels();
					InitMemoryCache();

					++curServer;
					if ((curServer / ConnectRetryAttempts) >= servers.Length)
						curServer = 0;

					redis = new RedisConnection(servers[curServer / ConnectRetryAttempts].Host, servers[curServer / ConnectRetryAttempts].Port);
					redis.Error += OnRedisError;
					redis.Closed += OnRedisClosed;
					redis.Open().Wait();

					subChannel = redis.GetOpenSubscriberChannel();
					subChannel.Error += OnRedisError;
					subChannel.Closed += OnRedisClosed;
					Task.WaitAll (
						subChannel.Subscribe (String.Format ("__keyevent@{0}__:del", CacheDb), OnRemoteKeyDeleted),
						subChannel.Subscribe (String.Format ("__keyevent@{0}__:expire", CacheDb), OnRemoteKeyExpirationSet), // expire event is enough, as far as SETEX redis command is only used to set values
						subChannel.Subscribe (FlushedEventName, OnRemoteFlushed)  // not a system message, should be published manually
					);
					isInited = true;
					return true;
				}
			}
			catch (Exception ex)
			{
				isInited = false;
				RaiseError (ex);
			}
			return isInited;
		}


		private void CloseRedisChannels()
		{
			if (subChannel != null)
			{
				subChannel.Error -= OnRedisError;
				subChannel.Closed -= OnRedisClosed;
				subChannel.Dispose();
			}
			if (redis != null)
			{
				redis.Error -= OnRedisError;
				redis.Closed -= OnRedisClosed;
				redis.Dispose();
			}
		}


		private void InitMemoryCache()
		{
			if (memoryCache != null)
				memoryCache.Dispose();
			memoryCache = new MemoryCache("RedisCacheAdapter.InternalCache");

			if (recentKeys != null)
				recentKeys.Dispose();
			recentKeys = new MemoryCache("RedisCacheAdapter.RecentKeys");
		}


		/// <summary>
		/// Returns current local in-memory cache
		/// </summary>
		internal MemoryCache MemoryCache
		{
			get
			{
				return memoryCache;
			}
		}

		internal RedisConnection RedisConnection
		{
			get
			{
				return redis;
			}
		}


		private bool ValidateDb()
		{
			if (isInited)
				return true;
			return OpenDb();
		}


		public override CacheItemWrapper Get (string key, string cacheName)
		{
			if (!ValidateDb())
				return null;

			try
			{
				var cacheItem = (CacheItemWrapper)memoryCache.Get(key);

				if (cacheItem == null)
				{
					var getTask = redis.Strings.Get(CacheDb, key);
					var ttlTask = redis.Keys.TimeToLive(CacheDb, key);

					object value= ProtoBufHelper.Deserialize(getTask.Result);
					if (value != null)
					{
						cacheItem = new CacheItemWrapper { Value = value.Equals(NullValue.Value) ? null : value };
						memoryCache.Set(key, cacheItem, (ttlTask.Result == -1) ? DateTimeOffset.Now.AddYears(100) : DateTimeOffset.Now.AddSeconds(ttlTask.Result));
					}
				}

				return cacheItem;
			}
			catch (Exception ex)
			{
				RaiseError(ex);
				return null;
			}
		}


		public override void Set (string key, object value, TimeSpan lifeSpan, string cacheName, IEnumerable<string> dependencyTags)
		{
			SetAsync(key, value, lifeSpan, dependencyTags);
		}


		internal Task<bool> SetAsync (string key, object value, TimeSpan lifeSpan, IEnumerable<string> dependencyTags)
		{
			if (!ValidateDb())
				return Task.FromResult (false);

			try
			{
				var cacheItem = new CacheItemWrapper {Value = value};
				memoryCache.Set(key, cacheItem, DateTimeOffset.Now.Add(lifeSpan));

				return Task.Run (() =>
				{
					try
					{
						recentKeys.Set(key, true, DateTimeOffset.Now.Add(RecentHistoryLifetime));
						redis.Strings.Set(CacheDb, key, ProtoBufHelper.Serialize (value ?? NullValue.Value), (long) lifeSpan.TotalSeconds).Wait();
						AddDependencyTags (key, dependencyTags);
						return true;
					}
					catch (Exception ex)
					{
						try
						{
							memoryCache.Remove (key); // if not saved in remote cache, not needed in the local one
						}
						catch (Exception ex2)
						{
							RaiseError (ex2);
						}
						RaiseError (ex);
					}
					return false;
				});
			}
			catch (Exception ex)
			{
				RaiseError (ex);
				return Task.FromResult(false);
			}
		}


		public override bool Remove (string key, string cacheName)
		{
			if (!ValidateDb())
				return false;

			Remove (new[] {key});
			return true;
		}


		private void Remove (string[] keys)
		{
			if (keys== null || keys.Length== 0)
				return;

			try
			{
				redis.Keys.Remove(CacheDb, keys);

				foreach (var key in keys)
				{
					memoryCache.Remove(key);
					recentKeys.Remove(key);
				}
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}


		public override void EvictAll (string cacheName, params string[] dependencyTags)
		{
			if (!ValidateDb())
				return;

			if (dependencyTags.Length== 0)
				return;

			try
			{
				redis.Sets.IntersectString(TagsDb, dependencyTags)
					.ContinueWith(task => Remove(task.Result))
					.Wait();
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}


		public override void EvictAny (string cacheName, params string[] dependencyTags)
		{
			if (!ValidateDb())
				return;

			if (dependencyTags.Length == 0)
				return;

			try
			{
				redis.Sets.UnionString(TagsDb, dependencyTags)
					.ContinueWith(task => Remove(task.Result))
					.Wait();
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}


		private void AddDependencyTags(string key, IEnumerable<string> dependencyTags)
		{
			if (dependencyTags == null)
				return;

			var tasks = new List<Task>();
			foreach (var tag in dependencyTags)
				tasks.Add (redis.Sets.Add (TagsDb, tag, key));

			Task.WaitAll (tasks.ToArray());
		}


		private void OnRemoteKeyExpirationSet(string ev, byte[] keyBytes)
		{
			try
			{
				string key = GetKey(keyBytes);

				if (memoryCache.Contains(key))
				{
					if (recentKeys.Remove(key)== null)						// ignore if key is just added by this process
					{
						var getTask = redis.Strings.Get(CacheDb, key);
						var ttlTask = redis.Keys.TimeToLive(CacheDb, key);

						var obj = ProtoBufHelper.Deserialize(getTask.Result);
						if (obj != null)
							memoryCache.Set(key, new CacheItemWrapper {Value = obj.Equals (NullValue.Value) ? null : obj}, (ttlTask.Result == -1) ? DateTimeOffset.Now.AddYears(100) : DateTimeOffset.Now.AddSeconds(ttlTask.Result));
						else
							memoryCache.Remove(key);
					}
				}
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}


		private void OnRemoteKeyDeleted(string ev, byte[] keyBytes)
		{
			try
			{
				string key = GetKey(keyBytes);
				memoryCache.Remove(key);
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}


		private void OnRemoteFlushed(string ev, byte[] data)
		{
			try
			{
				lock (sync)
					InitMemoryCache();
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}


		private void OnRedisError(object sender, ErrorEventArgs errorEventArgs)
		{
			RaiseError (errorEventArgs.Exception);
		}


		private void OnRedisClosed(object sender, EventArgs eventArgs)
		{
			isInited = false;
		}


		private string GetKey (byte[] keyBytes)
		{
			return Encoding.UTF8.GetString(keyBytes);
		}


		private void RaiseError(Exception ex)
		{
			if (OnError != null)
				OnError(this, ex);
		}


	}
}