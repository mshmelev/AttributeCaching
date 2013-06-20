using System;
using System.Runtime.Caching;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheContextTest
	{
		private IVisitor visitor;
		private TestContextClass testClass;

		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateDynamicMockWithRemoting<IVisitor>();
			testClass = new TestContextClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();

			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache = new MemoryCache("test");
		}


		[TestMethod]
		public void CacheKeyIsAccessible()
		{
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains (".Calc("))));
			testClass.Calc();
		}


		[TestMethod]
		public void NestedContexts()
		{
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains(".CalcParent("))));
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains(".CalcChild("))));
			testClass.CalcParent();
		}


		[TestMethod]
		public void ContextsAcrossThreads()
		{
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains("Thread1")))).Repeat.Once();
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains("Thread2")))).Repeat.Once();
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains("Thread1")))).Repeat.Once();
			visitor.Expect(m => m.Visit(Arg<object[]>.Matches(oo => ((string)oo[0]).Contains("Thread2")))).Repeat.Once();

			testClass.CalcThread1();
		}


		[TestMethod]
		public void LifeTimeChange()
		{
			visitor.Expect (m => m.Visit()).Repeat.Twice();
			testClass.CalcExpiring();
			System.Threading.Thread.Sleep (20);
			testClass.CalcExpiring();
		}




	}
}