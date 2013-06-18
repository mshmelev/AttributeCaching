using System;
using System.Runtime.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class NotCacheableTest
	{

		[TestMethod]
		public void TestNotCacheableMethod()
		{
			var visitor = MockRepository.GenerateDynamicMockWithRemoting<IVisitor>();
			var testClass = new TestNotCacheableClass (visitor);

			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("cached", testClass.CalcCache());
			Assert.AreEqual("cached", testClass.CalcCache());

			visitor.Expect (m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("notcached", testClass.CalcNotCache());
			Assert.AreEqual("notcached", testClass.CalcNotCache());
			
			visitor.Expect(m => m.Visit()).Repeat.Twice();
			Assert.AreEqual("prop", testClass.CalcPropNotCache);
			Assert.AreEqual("prop", testClass.CalcPropNotCache);

			visitor.VerifyAllExpectations();
		}


	}
}