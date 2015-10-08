namespace AttributeCaching.Tests.Helpers
{
	[Cacheable]
	public class TestCachingOnClass
	{
		private readonly IVisitor visitor;

		[Cacheable(Exclude = true)]
		public TestCachingOnClass(IVisitor visitor)
		{
			this.visitor = visitor;
		}



		public string CalcCache()
		{
			visitor.Visit ();
			return "cached";
		}



		[Cacheable(Exclude = true)]
		public string CalcNotCache()
		{
			visitor.Visit ();
			return "notcached";
		}

		[Cacheable(Exclude = true)]
		public string CalcPropNotCache
		{
			get
			{
				visitor.Visit();
				return "prop";
			}
			set
			{
				visitor.Visit (value);
			}
		}

		[Cacheable(0.01, Replace = true)]
		public string CalcCacheOverride()
		{
			visitor.Visit();
			return "cached";
		}

	}
}