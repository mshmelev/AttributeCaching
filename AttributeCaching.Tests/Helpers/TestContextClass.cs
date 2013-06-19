using System;
using System.Threading;
using System.Threading.Tasks;

namespace AttributeCaching.Tests.Helpers
{
	public class TestContextClass
	{
		private readonly IVisitor visitor;



		public TestContextClass(IVisitor visitor)
		{
			this.visitor = visitor;
		}


		[Cacheable]
		public string Calc()
		{
			visitor.Visit(CacheScope.CurrentContext.CacheKey);
			return "cached";
		}

		[Cacheable]
		public string CalcParent()
		{
			visitor.Visit(CacheScope.CurrentContext.CacheKey);
			CalcChild();
			return "cached_parent";
		}

		[Cacheable]
		public string CalcChild()
		{
			visitor.Visit(CacheScope.CurrentContext.CacheKey);
			return "cached_child";
		}


		readonly AutoResetEvent syncEvent= new AutoResetEvent (false);

		[Cacheable]
		public string CalcThread1()
		{
			syncEvent.Reset();
			visitor.Visit(CacheScope.CurrentContext.CacheKey);

			var task= Task.Run(() => CalcThread2());
			syncEvent.WaitOne();

			visitor.Visit(CacheScope.CurrentContext.CacheKey);
			
			syncEvent.Set();
			task.Wait();

			return "cached_thread1";
		}


		[Cacheable]
		public string CalcThread2()
		{
			visitor.Visit(CacheScope.CurrentContext.CacheKey);
			syncEvent.Set();

			syncEvent.WaitOne();

			visitor.Visit(CacheScope.CurrentContext.CacheKey);
			return "cached_thread1";
		}


		[Cacheable (1000)]
		public string CalcExpiring()
		{
			visitor.Visit();

			CacheScope.CurrentContext.LifeSpan = TimeSpan.FromMilliseconds (10);

			return "cached";
		}

	}
}