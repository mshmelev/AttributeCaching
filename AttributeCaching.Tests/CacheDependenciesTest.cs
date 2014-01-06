using System;
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
			CacheFactory.Cache = new MemoryCacheAdapter ();
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

			CacheFactory.Cache.EvictAll(null, "car_1");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(1);
			testClass.GetCar(1);

			visitor.Expect(m => m.Visit()).Repeat.Never();
			testClass.GetCars();
		}


		[TestMethod]
		public void TestEvictAllEmpty()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.GetCars();
			testClass.GetCar(0);

			CacheFactory.Cache.EvictAll(null, "cars", "car_0");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(0);

			visitor.Expect(m => m.Visit()).Repeat.Never();
			testClass.GetCars();
		}


		[TestMethod]
		public void TestEvictAllNone()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.GetCars();
			testClass.GetCar(0);

			CacheFactory.Cache.EvictAll(null, "junk");

			visitor.Expect(m => m.Visit()).Repeat.Never();
			testClass.GetCar(0);
			testClass.GetCars();
		}


		[TestMethod]
		public void TestChangeDependencies()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarChangingDependency(0);
			testClass.GetCarChangingDependency(0);

			CacheFactory.Cache.EvictAll(null, "car_0");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarChangingDependency(0);
			testClass.GetCarChangingDependency(0);
		}

		[TestMethod]
		public void TestUpdateExistingDependencies()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarUpdatingDependency(0);
			testClass.GetCarUpdatingDependency(0);

			CacheFactory.Cache.EvictAll(null, "car_0");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarUpdatingDependency(0);
			testClass.GetCarUpdatingDependency(0);
		}

		[TestMethod]
		public void TestUpdateNonExistingDependencies()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCarUpdatingNonExistingDependency (0);
			testClass.GetCarUpdatingNonExistingDependency (0);

			CacheFactory.Cache.EvictAny(null, "car_0", "carM_0");

			visitor.Expect (m => m.Visit()).Repeat.Never();
			testClass.GetCarUpdatingNonExistingDependency(0);
		}


		[TestMethod]
		public void TestEvictAny()
		{
			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
			testClass.GetCars();

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(0);
			testClass.GetCar(0);

			CacheFactory.Cache.EvictAny(null, "cars", "car_0", "car_1", "car_2", "junk");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
			testClass.GetCars();

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(0);
			testClass.GetCar(0);
		}

		[TestMethod]
		public void TestEvictAnyNone()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.GetCars();
			testClass.GetCar(0);

			CacheFactory.Cache.EvictAny(null, "car_5", "car_6");

			visitor.Expect (m => m.Visit()).Repeat.Never();
			testClass.GetCars();
			testClass.GetCar(0);
		}


		[TestMethod]
		public void TestEvictAttrAny()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.GetCars();
			testClass.GetCar(0);

			testClass.UpdateWithAttrAny (0, "carAAA");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(0);

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCars();
		}


		[TestMethod]
		public void TestEvictAttrAll()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			testClass.GetCars();
			testClass.GetCar(0);

			testClass.UpdateWithAttrAll (0, "carAAA");

			visitor.Expect(m => m.Visit()).Repeat.Once();
			testClass.GetCar(0);

			visitor.Expect(m => m.Visit()).Repeat.Never();
			testClass.GetCars();
		}



	}
}