namespace AttributeCaching.Tests
{
	[Cacheable]
	public class TestNotCacheableClass
	{
		private readonly IVisitor visitor;

		[Cacheable(AttributeExclude = true)]
		public TestNotCacheableClass(IVisitor visitor)
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

	}
}