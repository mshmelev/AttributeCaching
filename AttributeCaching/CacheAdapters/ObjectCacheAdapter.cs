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
		protected ObjectCache cache;
		protected readonly Dictionary<string,HashSet<string>> tagKeysDependencies = new Dictionary<string, HashSet<string>>();


		protected ObjectCacheAdapter(ObjectCache cache)
		{
			this.cache = cache;
		}


		public override object Get (string key)
		{
			return cache.Get (key);
		}


		public override void Set(string key, object value, DateTimeOffset absoluteExpiration, params string[] dependencyTags)
		{
			cache.Set (key, value, absoluteExpiration);
			AddDependencyTags(key, dependencyTags);
		}


		protected void AddDependencyTags (string key, IEnumerable<string> dependencyTags)
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


		public override object Remove (string key)
		{
			return cache.Remove (key);
		}


		/// <summary>
		/// Evicts all objects in cache which have ALL passed tags
		/// </summary>
		/// <param name="dependencyTags"></param>
		public override void EvictAll (params string[] dependencyTags)
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
					cache.Remove (key);
			}
		}
	}
}