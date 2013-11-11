using System;
using System.Runtime.Caching;
using System.Threading.Tasks;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheableTest
	{
		private IVisitor visitor;
		private TestClass testClass;


		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateStrictMockWithRemoting<IVisitor>();
			testClass = new TestClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();
			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache= new MemoryCache ("test");
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


		[TestMethod]
		public void TestArrays()
		{
			visitor.Expect (m => m.Visit()).IgnoreArguments().Repeat.Times(4);

			Assert.AreEqual ("a_b_c,d_e", testClass.CalcArray(new[] {"a","b","c"}, new[] {"d","e"}));
			Assert.AreEqual ("a_b_c,d_e", testClass.CalcArray(new[] {"a","b","c"}, new[] {"d","e"}));
			
			Assert.AreEqual ("a_bb_c,d_e", testClass.CalcArray(new[] {"a","bb","c"}, new[] {"d","e"}));
			Assert.AreEqual ("a_bb_c,d_e", testClass.CalcArray(new[] {"a","bb","c"}, new[] {"d","e"}));
			
			Assert.AreEqual("a_b_c,", testClass.CalcArray(new[] { "a", "b", "c" }, new string[0]));
			Assert.AreEqual("a_b_c,", testClass.CalcArray(new[] { "a", "b", "c" }, new string[0]));

			Assert.AreEqual(",", testClass.CalcArray(new string[0], new string[0]));
			Assert.AreEqual(",", testClass.CalcArray(new string[0], new string[0]));
		}



		[TestMethod]
		public void TestVariedParams()
		{
			visitor.Expect (m => m.Visit()).IgnoreArguments().Repeat.Times(3);
			Assert.AreEqual ("params_1", testClass.CalcParams("par1"));
			Assert.AreEqual ("params_1", testClass.CalcParams("par1"));
			Assert.AreEqual ("params_2", testClass.CalcParams("par1", "par2"));
			Assert.AreEqual ("params_2", testClass.CalcParams("par1", "par2"));
			Assert.AreEqual ("params_0", testClass.CalcParams());
			Assert.AreEqual ("params_0", testClass.CalcParams());
		}


		[TestMethod]
		public void TestProperties()
		{
			visitor.Expect (m => m.Visit()).Repeat.Once();
			Assert.AreEqual ("prop", testClass.CalcProp);
			Assert.AreEqual ("prop", testClass.CalcProp);
		}


		[TestMethod]
		public void TestPropertySetNotCacheable()
		{
			visitor.Expect (m => m.Visit("prop2")).Repeat.Twice();
			testClass.CalcProp = "prop2";
			testClass.CalcProp = "prop2";
		}

		[TestMethod]
		public void TestPropertyCacheReset()
		{
			visitor.Expect (m => m.Visit()).IgnoreArguments().Repeat.Times(3);
			Assert.AreEqual ("prop", testClass.CalcProp);
			Assert.AreEqual ("prop", testClass.CalcProp);

			testClass.CalcProp = "prop2";
			Assert.AreEqual("prop2", testClass.CalcProp);
			Assert.AreEqual("prop2", testClass.CalcProp);
		}

		
		[TestMethod]
		public void TestIndexedProperty()
		{
			visitor.Expect (m => m.Visit("a1")).Repeat.Once();
			Assert.AreEqual ("ind_a1", testClass["a1"]);
			Assert.AreEqual ("ind_a1", testClass["a1"]);
		}


		[TestMethod]
		public void TestIgnoreParam()
		{
			visitor.Expect(m => m.Visit("a1", "b1", "c1")).Repeat.Once();
			Assert.AreEqual("a1_c1", testClass.CalcIgnoreParam ("a1", "b1", "c1"));
			Assert.AreEqual("a1_c1", testClass.CalcIgnoreParam("a1", "b2", "c1"));
			Assert.AreEqual("a1_c1", testClass.CalcIgnoreParam("a1", "", "c1"));
			Assert.AreEqual("a1_c1", testClass.CalcIgnoreParam("a1", null, "c1"));

			visitor.Expect(m => m.Visit("a2", "b1", "c1")).Repeat.Once();
			Assert.AreEqual("a2_c1", testClass.CalcIgnoreParam("a2", "b1", "c1"));
			Assert.AreEqual("a2_c1", testClass.CalcIgnoreParam("a2", "b2", "c1"));

			visitor.Expect(m => m.Visit("a2", "b1", "c2")).Repeat.Once();
			Assert.AreEqual("a2_c2", testClass.CalcIgnoreParam("a2", "b1", "c2"));
			Assert.AreEqual("a2_c2", testClass.CalcIgnoreParam("a2", "b2", "c2"));
		}


		[TestMethod]
		public void TestExpiration()
		{
			visitor.Expect(m => m.Visit("a1")).Repeat.Once();
			Assert.AreEqual("a1", testClass.CalcExpiring ("a1"));
			Assert.AreEqual("a1", testClass.CalcExpiring ("a1"));

			System.Threading.Thread.Sleep (21);
			visitor.Expect(m => m.Visit("a1")).Repeat.Once();
			Assert.AreEqual("a1", testClass.CalcExpiring("a1"));
			Assert.AreEqual("a1", testClass.CalcExpiring("a1"));
		}



		/// <summary>
		/// Ensures that caching value is calculated only once by a function when multiple streams access the function simultaneously
		/// </summary>
		[TestMethod]
		public void TestCacheLocks()
		{
			visitor.Expect (m => m.Visit ("a1")).Repeat.Once();

			var t1= Task.Run (() => testClass.CalcLongProcess ("a1"));
			var t2= Task.Run (() => testClass.CalcLongProcess ("a1"));
			var t3= Task.Run (() => testClass.CalcLongProcess ("a1"));
			Task.WaitAll (t1, t2, t3);
		}

		[TestMethod]
		public void TestCacheLocksWithExceptions()
		{
			visitor.Expect(m => m.Visit("a1")).Repeat.Times(3);

			var t1 = Task.Run(() => {try { testClass.CalcLongProcessExceptions ("a1"); } catch { } });
			var t2 = Task.Run(() => {try { testClass.CalcLongProcessExceptions ("a1"); } catch { } });
			var t3 = Task.Run(() => {try { testClass.CalcLongProcessExceptions ("a1"); } catch { } });
			Task.WaitAll(t1, t2, t3);
		}


		[TestMethod]
		public void TestCacheNoLocks()
		{
			visitor.Expect (m => m.Visit ("a1")).Repeat.Times(3);

			var t1 = Task.Run(() => testClass.CalcLongProcessUnsynced("a1"));
			var t2 = Task.Run(() => testClass.CalcLongProcessUnsynced("a1"));
			var t3 = Task.Run(() => testClass.CalcLongProcessUnsynced("a1"));
			Task.WaitAll (t1, t2, t3);
		}
	}
}
