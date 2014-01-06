using System;
using AttributeCaching.CacheAdapters;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheNamesTest
	{
		private IVisitor visitor;
		private TestCacheNamesClass testClass;


		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateStrictMockWithRemoting<IVisitor>();
			testClass = new TestCacheNamesClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();
			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache = new MemoryCacheAdapter();
		}


		[TestMethod]
		public void TestCacheNamesHaveDifferentStorages()
		{
			visitor.Expect(m => m.Visit()).Repeat.Times(3);
			testClass.GetCarCacheDefault (0);
			testClass.GetCarCache1 (0);
			testClass.GetCarCache2 (0);

			testClass.UpdateCarCacheDefault();
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarCacheDefault(0);
			testClass.GetCarCache1(0);
			testClass.GetCarCache2(0);

			testClass.UpdateCarCache1();
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarCacheDefault(0);
			testClass.GetCarCache1(0);
			testClass.GetCarCache2(0);
		}



	}
}