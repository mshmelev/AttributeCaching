namespace AttributeCaching.Tests.Helpers
{
	public class TestCacheNamesClass
	{
		private readonly IVisitor visitor;


		public TestCacheNamesClass(IVisitor visitor)
		{
			this.visitor = visitor;
		}


		[Cacheable("cars")]
		public string GetCarCacheDefault (int car)
		{
			visitor.Visit();
			return "car_" + car;
		}

		[Cacheable("cars", CacheName = "n1")]
		public string GetCarCache1(int car)
		{
			visitor.Visit();
			return "car_" + car;
		}


		[Cacheable ("cars", CacheName = "n2")]
		public string GetCarCache2 (int car)
		{
			visitor.Visit();
			return "car_" + car;
		}


		[EvictCache("cars")]
		public void UpdateCarCacheDefault()
		{
		}

		[EvictCache("cars", CacheName = "n1")]
		public void UpdateCarCache1()
		{
		}

		[EvictCache("cars", CacheName = "n2")]
		public void UpdateCarCache2()
		{
		}
	}
}