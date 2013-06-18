using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheableTest
	{
		private IVisitorTest visitor;
		private TestClass testClass;


		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateDynamicMockWithRemoting<IVisitorTest>();
			testClass = new TestClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();
			((IDisposable)CacheFactory.Cache).Dispose();
		}

		

		[TestMethod]
		public void TestGeneralCaching()
		{
			visitor.Expect(m => m.Visit("a", "b")).Repeat.Once();
			Assert.AreEqual("a_b", testClass.Calc("a", "b"));
			Assert.AreEqual ("a_b", testClass.Calc("a", "b"));

			visitor.Expect(m => m.Visit("c", "d")).Repeat.Once();
			Assert.AreEqual("c_d", testClass.Calc("c", "d"));
			Assert.AreEqual ("c_d", testClass.Calc("c", "d"));
		}

		[TestMethod]
		public void TestNoParameter()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("noparam", testClass.Calc());
			Assert.AreEqual("noparam", testClass.Calc());
			Assert.AreEqual("noparam2", testClass.Calc2());
			Assert.AreEqual("noparam2", testClass.Calc2());
		}

		[TestMethod]
		public void TestOverloaded()
		{
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("int_1", testClass.CalcOverloaded(1));
			Assert.AreEqual("string_1", testClass.CalcOverloaded("1"));
		}


		[TestMethod]
		public void TestNullParams()
		{
			visitor.Expect (m => m.Visit()).IgnoreArguments().Repeat.Times(5);
			Assert.AreEqual ("_", testClass.Calc(null, null));
			Assert.AreEqual ("_", testClass.Calc("", null));
			Assert.AreEqual ("_", testClass.Calc(null, ""));
			Assert.AreEqual("null_null", testClass.Calc("null", "null"));
			Assert.AreEqual("noparam", testClass.Calc());
		}


		/*
		 * TODO:
		 * - Ignore parameter
		 * - Properties
		 * - Caching time
		 * - Dependencies
		 * 
		 * */


	}
}
