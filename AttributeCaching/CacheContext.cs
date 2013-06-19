namespace AttributeCaching
{
	public class CacheContext
	{
		internal CacheContext(string cacheKey)
		{
			CacheKey = cacheKey;
		}


		public string CacheKey
		{
			get;
			private set;
		}
	}
}