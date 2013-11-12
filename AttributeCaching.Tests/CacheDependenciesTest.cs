using System;
using System.Runtime.Caching;
using AttributeCaching.CacheAdapters;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheDependenciesTest
	{
		private TestDependeciesClass testClass;

		[TestInitialize]
		public void Init()
		{
			testClass = new TestDependeciesClass();
		}


		[TestCleanup]
		public void Cleanup()
		{
			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache = new MemoryCacheAdapter (new MemoryCache("test"));
		}


		//[TestMethod]
		public void TestDependency()
		{
			Assert.AreEqual ("carA,carB,carC", testClass.GetCars());
			testClass.Update (0, "carAAA");
			Assert.AreEqual("carAAA,carB,carC", testClass.GetCars());
		}



	}
}