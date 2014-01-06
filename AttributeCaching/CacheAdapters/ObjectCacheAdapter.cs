using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace AttributeCaching.CacheAdapters
{
	/// <summary>
	/// Adapter for .NET Framork native ObjectCache provider
	/// </summary>
	internal abstract class ObjectCacheAdapter : CacheAdapter
	{
		protected readonly Dictionary<string,HashSet<string>> tagKeysDependencies = new Dictionary<string, HashSet<string>>();


		protected abstract ObjectCache GetCache (string cacheName);


		public override object Get(string key, string cacheName)
		{
			return GetCache (cacheName).Get(key);
		}


		public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string cacheName, IEnumerable<string> dependencyTags)
		{
			GetCache(cacheName).Set(key, value, absoluteExpiration);
			AddDependencyTags(key, cacheName, dependencyTags);
		}


		protected void AddDependencyTags(string key, string cacheName, IEnumerable<string> dependencyTags)
		{
			foreach (var tag in dependencyTags)
			{
				HashSet<string> keys;
				if (!tagKeysDependencies.TryGetValue (tag, out keys))
				{
					lock (tagKeysDependencies)
					{
						if (!tagKeysDependencies.TryGetValue (tag, out keys))
						{
							keys = new HashSet<string>();
							tagKeysDependencies[tag] = keys;
						}
					}
				}
				keys.Add (key);
			}
		}


		public override bool Remove(string key, string cacheName)
		{
			return GetCache(cacheName).Remove(key)!= null;
		}


		/// <summary>
		/// Evicts all objects in cache which have ALL passed tags
		/// </summary>
		/// <param name="cacheName"></param>
		/// <param name="dependencyTags"></param>
		public override void EvictAll (string cacheName, params string[] dependencyTags)
		{
			HashSet<string> resultingSet = null;

			foreach (var tag in dependencyTags)
			{
				HashSet<string> keys;
				if (!tagKeysDependencies.TryGetValue (tag, out keys))
					return;		// intersection will be empty anyway

				if (resultingSet == null)
					resultingSet = new HashSet<string> (keys);
				else
					resultingSet.IntersectWith (keys);
			}

			if (resultingSet != null)
			{
				foreach (string key in resultingSet)
					GetCache(cacheName).Remove(key);
			}
		}


		/// <summary>
		/// Evicts all objects in cache which have ANY of passed tags
		/// </summary>
		/// <param name="cacheName"></param>
		/// <param name="dependencyTags"></param>
		public override void EvictAny (string cacheName, params string[] dependencyTags)
		{
			HashSet<string> resultingSet = null;

			foreach (var tag in dependencyTags)
			{
				HashSet<string> keys;
				if (tagKeysDependencies.TryGetValue (tag, out keys))
				{
					if (resultingSet == null)
						resultingSet = new HashSet<string> (keys);
					else
						resultingSet.UnionWith (keys);
				}
			}

			if (resultingSet != null)
			{
				foreach (string key in resultingSet)
					GetCache(cacheName).Remove(key);
			}
		}
	}
}