using System.Runtime.Caching;
using AttributeCaching.CacheAdapters;

namespace AttributeCaching
{
	/// <summary>
	/// Factory for cache adapters
	/// </summary>
	public static class CacheFactory
	{
		private static CacheAdapter cache;
		private static readonly object sync = new object();


		/// <summary>
		/// Current cache adapter instance
		/// </summary>
		public static CacheAdapter Cache
		{
			get
			{
				if (cache == null)
				{
					lock (sync)
					{
						if (cache == null)
							cache = new MemoryCacheAdapter();
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