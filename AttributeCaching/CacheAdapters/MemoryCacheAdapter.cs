using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace AttributeCaching.CacheAdapters
{
	internal class MemoryCacheAdapter : ObjectCacheAdapter, IDisposable
	{
		private readonly Dictionary<string, MemoryCache> caches= new Dictionary<string, MemoryCache>();

		public void Dispose()
		{
			foreach (var cache in caches.Values)
				cache.Dispose();
			caches.Clear();
		}


		protected override ObjectCache GetCache (string cacheName)
		{
			cacheName = cacheName ?? "\u1234";

			MemoryCache cache;
			if (!caches.TryGetValue (cacheName, out cache))
			{
				lock (caches)
				{
					if (!caches.TryGetValue (cacheName, out cache))
					{
						cache = new MemoryCache (cacheName);
						caches.Add(cacheName, cache);
					}
				}
			}

			return cache;
		}
	}
}