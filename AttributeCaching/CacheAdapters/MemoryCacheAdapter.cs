using System;
using System.Runtime.Caching;

namespace AttributeCaching.CacheAdapters
{
	internal class MemoryCacheAdapter : ObjectCacheAdapter, IDisposable
	{
		public MemoryCacheAdapter(MemoryCache cache) : base (cache)
		{
		}


		public void Dispose()
		{
			((MemoryCache)cache).Dispose();
		}
	}
}