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
	public class RedisCacheAdapter : CacheAdapter
	{
		private const int CacheDb = 0;
		private const int TagsDb = 1;
		private readonly TimeSpan RecentHistoryLifetime = TimeSpan.FromSeconds (1);

		private readonly RedisServer[] servers;
		private int curServer = -1;
		private bool isInited;

		private RedisConnection redis;
		private RedisSubscriberConnection subChannel;
		private MemoryCache memoryCache;
		private MemoryCache recentKeys;

		private readonly static object sync= new object();

		public event EventHandler<Exception> OnError;		// TODO: handle



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


		private bool OpenDb()
		{
			try
			{
				lock (sync)
				{
					if (memoryCache != null)
						memoryCache.Dispose();
					memoryCache = new MemoryCache("RedisCacheAdapter.InternalCache");

					if (recentKeys != null)
						recentKeys.Dispose();
					recentKeys = new MemoryCache("RedisCacheAdapter.RecentKeys");

					++curServer;
					if (curServer >= servers.Length)
						curServer = 0;

					redis = new RedisConnection(servers[curServer].Host, servers[curServer].Port);
					redis.Error += OnRedisError;
					redis.Closed += OnRedisClosed;
					redis.Open().Wait();

					subChannel = redis.GetOpenSubscriberChannel();
					subChannel.Error += OnRedisError;
					subChannel.Subscribe(String.Format("__keyevent@{0}__:del", CacheDb), OnRemoteKeyDeleted);
					subChannel.Subscribe(String.Format("__keyevent@{0}__:expire", CacheDb), OnRemoteKeyExpirationSet);		// expire event is enough, as far as SETEX redis command is only used to set values

					isInited = true;
					return true;
				}
			}
			catch (Exception ex)
			{
				isInited = false;

				if (OnError != null)
					OnError (this, ex);
			}
			return isInited;
		}


		private bool ValidateDb()
		{
			if (isInited)
				return true;
			return OpenDb();
		}


		public override object Get (string key, string cacheName)
		{
			if (!ValidateDb())
				return null;

			try
			{
				object obj = memoryCache.Get(key);

				if (obj == null)
				{
					var getTask = redis.Strings.Get(CacheDb, key);
					var ttlTask = redis.Keys.TimeToLive(CacheDb, key);

					obj = ProtoBufHelper.Deserialize(getTask.Result);
					if (obj != null)
						memoryCache.Set(key, obj, DateTimeOffset.Now.AddSeconds(ttlTask.Result));
				}

				return obj;
			}
			catch (Exception ex)
			{
				if (OnError != null)
					OnError(this, ex);

				return null;
			}
		}


		public override void Set (string key, object value, TimeSpan lifeSpan, string cacheName, IEnumerable<string> dependencyTags)
		{
			if (!ValidateDb())
				return;

			try
			{
				memoryCache.Set(key, value, DateTimeOffset.Now.Add(lifeSpan));

				Task.Run(() =>
				{
					try
					{
						redis.Strings.Set(CacheDb, key, ProtoBufHelper.Serialize(value), (long)lifeSpan.TotalSeconds);
						recentKeys.Set(key, true, DateTimeOffset.Now.Add(RecentHistoryLifetime));
						AddDependencyTags(key, dependencyTags);
					}
					catch (Exception ex)
					{
						if (OnError != null)
							OnError(this, ex);
					}
				});
			}
			catch (Exception ex)
			{
				if (OnError != null)
					OnError(this, ex);
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
				if (OnError != null)
					OnError(this, ex);
			}
		}


		public override void EvictAll (string cacheName, params string[] dependencyTags)
		{
			if (!ValidateDb())
				return;

			try
			{
				redis.Sets.IntersectString(TagsDb, dependencyTags)
					.ContinueWith(task => Remove(task.Result))
					.Wait();
			}
			catch (Exception ex)
			{
				if (OnError != null)
					OnError(this, ex);
			}
		}


		public override void EvictAny (string cacheName, params string[] dependencyTags)
		{
			if (!ValidateDb())
				return;

			try
			{
				redis.Sets.UnionString(TagsDb, dependencyTags)
					.ContinueWith(task => Remove(task.Result))
					.Wait();
			}
			catch (Exception ex)
			{
				if (OnError != null)
					OnError(this, ex);
			}
		}


		private void AddDependencyTags(string key, IEnumerable<string> dependencyTags)
		{
			foreach (var tag in dependencyTags)
				redis.Sets.Add (TagsDb, tag, key);
		}


		private void OnRemoteKeyExpirationSet(string ev, byte[] keyBytes)
		{
			try
			{
				string key = GetKey(keyBytes);

				if (memoryCache.Contains(key))
				{
					if (recentKeys.Contains(key))						// ignore if just added by this process
					{
						recentKeys.Remove(key);							// next notification will be real for sure
					}
					else
					{
						var getTask = redis.Strings.Get(CacheDb, key);
						var ttlTask = redis.Keys.TimeToLive(CacheDb, key);

						var obj = ProtoBufHelper.Deserialize(getTask.Result);
						if (obj != null)
							memoryCache.Set(key, obj, DateTimeOffset.Now.AddSeconds(ttlTask.Result));
						else
							memoryCache.Remove(key);
					}
				}
			}
			catch (Exception ex)
			{
				if (OnError != null)
					OnError(this, ex);
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
				if (OnError != null)
					OnError(this, ex);
			}
		}


		private void OnRedisError(object sender, ErrorEventArgs errorEventArgs)
		{
			if (OnError != null)
				OnError(this, errorEventArgs.Exception);
		}


		private void OnRedisClosed(object sender, EventArgs eventArgs)
		{
			isInited = false;
			OpenDb();
		}


		private string GetKey (byte[] keyBytes)
		{
			return Encoding.UTF8.GetString(keyBytes);
		}


	}
}