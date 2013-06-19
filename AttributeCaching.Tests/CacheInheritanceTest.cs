using System;
using System.Runtime.Caching;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheInheritanceTest
	{
		private IVisitor visitor;
		private TestTopCacheClass testClass;


		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateDynamicMockWithRemoting<IVisitor>();
			testClass = new TestTopCacheClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();
			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache = new MemoryCache("test");
		}


		[TestMethod]
		public void TestNotCacheableMethod()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("cached", testClass.CalcCache());
			Assert.AreEqual("cached", testClass.CalcCache());

			visitor.Expect (m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("notcached", testClass.CalcNotCache());
			Assert.AreEqual("notcached", testClass.CalcNotCache());
			
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("prop", testClass.CalcPropNotCache);
			Assert.AreEqual("prop", testClass.CalcPropNotCache);
		}


		[TestMethod]
		public void TestCacheOverride()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("cached", testClass.CalcCacheOverride());
			Assert.AreEqual("cached", testClass.CalcCacheOverride());

			System.Threading.Thread.Sleep (20);
			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("cached", testClass.CalcCacheOverride());
			Assert.AreEqual("cached", testClass.CalcCacheOverride());
		}


	}
}