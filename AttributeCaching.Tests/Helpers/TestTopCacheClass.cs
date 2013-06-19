namespace AttributeCaching.Tests.Helpers
{
	[Cacheable]
	public class TestTopCacheClass
	{
		private readonly IVisitor visitor;

		[Cacheable(AttributeExclude = true)]
		public TestTopCacheClass(IVisitor visitor)
		{
			this.visitor = visitor;
		}



		public string CalcCache()
		{
			visitor.Visit ();
			return "cached";
		}



		[Cacheable(AttributeExclude = true)]
		public string CalcNotCache()
		{
			visitor.Visit ();
			return "notcached";
		}

		[Cacheable(AttributeExclude = true)]
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

		[Cacheable(0.01, AspectPriority = 1)]
		public string CalcCacheOverride()
		{
			visitor.Visit();
			return "cached";
		}

	}
}