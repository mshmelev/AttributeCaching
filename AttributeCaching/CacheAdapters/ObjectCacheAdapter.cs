using System;
using System.Runtime.Caching;

namespace AttributeCaching.CacheAdapters
{
	/// <summary>
	/// Adapter for .NET Framork native ObjectCache provider
	/// </summary>
	internal abstract class ObjectCacheAdapter : CacheAdapter
	{
		protected ObjectCache cache;

		protected ObjectCacheAdapter(ObjectCache cache)
		{
			this.cache = cache;
		}


		public override object Get (string key)
		{
			return cache.Get (key);
		}


		public override void Set (string key, object value, DateTimeOffset absoluteExpiration)
		{
			cache.Set (key, value, absoluteExpiration);
		}


		public override object Remove (string key)
		{
			return cache.Remove (key);
		}
	}
}