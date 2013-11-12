using System.Runtime.Caching;
using AttributeCaching.CacheAdapters;

namespace AttributeCaching
{
	public static class CacheFactory
	{
		private static CacheAdapter cache;
		private static readonly object sync = new object();


		public static CacheAdapter Cache
		{
			get
			{
				if (cache == null)
				{
					lock (sync)
					{
						if (cache == null)
							cache = new MemoryCacheAdapter (MemoryCache.Default);
					}
				}
				return cache;
			}
			set
			{
				lock (sync)
					cache = value;
			}
		}


	}
}