using System;
using System.Runtime.Caching;
using AttributeCaching.CacheAdapters;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheDependenciesTest
	{
		private IVisitor visitor;
		private TestDependeciesClass testClass;


		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateStrictMockWithRemoting<IVisitor>();
			testClass = new TestDependeciesClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();
			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache = new MemoryCacheAdapter (new MemoryCache("test"));
		}


		[TestMethod]
		public void TestEvict()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("carA,carB,carC", testClass.GetCars());
			Assert.AreEqual("carA,carB,carC", testClass.GetCars());
			
			testClass.Update (0, "carAAA");
			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("carAAA,carB,carC", testClass.GetCars());
			Assert.AreEqual("carAAA,carB,carC", testClass.GetCars());
		}

		[TestMethod]
		public void TestEvictManyTimes()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
			testClass.GetCars();
			
			testClass.Update (0, "carAAA");
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
			testClass.GetCars();

			testClass.Update (0, "carAAA2");
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
			testClass.GetCars();
		}


		[TestMethod]
		public void TestEvictMultipleFunctions()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("carA,carB,carC", testClass.GetCars());
			Assert.AreEqual("carB", testClass.GetCar(1));

			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.Update(1, "carBBB");
			Assert.AreEqual("carA,carBBB,carC", testClass.GetCars());
			Assert.AreEqual("carBBB", testClass.GetCar(1));
		}


		[TestMethod]
		public void TestEvictOneFunction()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
			testClass.GetCars();
			
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(1);
			testClass.GetCar(1);

			CacheFactory.Cache.EvictAll ("car_1");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(1);
			testClass.GetCar(1);

			visitor.Expect(m => m.Visit()).Repeat.Never();
			testClass.GetCars();
		}


		[TestMethod]
		public void TestEvictNone()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.GetCars();
			testClass.GetCar(0);

			CacheFactory.Cache.EvictAll("cars", "car_0");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(0);

			visitor.Expect(m => m.Visit()).Repeat.Never();
			testClass.GetCars();
		}


		[TestMethod]
		public void TestChangeDependencies()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarChangingDependency(0);
			testClass.GetCarChangingDependency(0);

			CacheFactory.Cache.EvictAll("car_0");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarChangingDependency(0);
			testClass.GetCarChangingDependency(0);
		}

	}
}