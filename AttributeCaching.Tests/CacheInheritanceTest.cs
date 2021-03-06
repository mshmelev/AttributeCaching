﻿using System;
using AttributeCaching.CacheAdapters;
using AttributeCaching.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace AttributeCaching.Tests
{
	[TestClass]
	public class CacheInheritanceTest
	{
		private IVisitor visitor;
		private TestCachingOnClass testClass;


		[TestInitialize]
		public void Init()
		{
			visitor = MockRepository.GenerateStrictMockWithRemoting<IVisitor>();
			testClass = new TestCachingOnClass(visitor);
		}


		[TestCleanup]
		public void Cleanup()
		{
			visitor.VerifyAllExpectations();
			((IDisposable)CacheFactory.Cache).Dispose();
			CacheFactory.Cache = new MemoryCacheAdapter();
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

			System.Threading.Thread.Sleep(20);		// check the overriden life time is in effect
			visitor.Expect(m => m.Visit()).Repeat.Once();
			Assert.AreEqual("cached", testClass.CalcCacheOverride());
			Assert.AreEqual("cached", testClass.CalcCacheOverride());
		}


	}
}