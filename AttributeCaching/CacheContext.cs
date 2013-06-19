using System;

namespace AttributeCaching
{
	public class CacheContext
	{
		internal CacheContext(string cacheKey, TimeSpan lifeSpan)
		{
			CacheKey = cacheKey;
			LifeSpan = lifeSpan;
		}


		public string CacheKey
		{
			get;
			private set;
		}


		public TimeSpan LifeSpan
		{
			get;
			set;
		}
	}
}