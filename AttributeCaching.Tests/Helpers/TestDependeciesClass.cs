using System;
using System.Linq;
using System.Runtime.Caching;

namespace AttributeCaching.Tests.Helpers
{
	public class TestDependeciesClass
	{
		private string[] cars = {"carA", "carB", "carC"};

		[Cacheable("aa", "bb")]
		public string GetCars()
		{
			return String.Join(",", cars);
		}


		public void Update (int car, string newName)
		{
			cars[car] = newName;
			//CacheFactory.Cache.DefaultCacheCapabilities== 
		}
	}
}