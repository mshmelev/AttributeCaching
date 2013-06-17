using System.Runtime.Caching;

namespace AttributeCaching
{
	public static class CacheFactory
	{
		private static ObjectCache cache;
		private static readonly object sync = new object();




		public static ObjectCache Cache
		{
			get
			{
				if (cache == null)
				{
					lock (sync)
					{
						if (cache == null)
							cache = MemoryCache.Default;
					}
				}

				return cache;
			}
			set
			{
				lock (sync)
				{
					cache = value;
				}
			}
		}


	}
}