﻿using System;

namespace AttributeCaching.Tests
{
	public class TestClass
	{
		private readonly IVisitorTest visitorTest;

		public TestClass(IVisitorTest visitorTest)
		{
			this.visitorTest = visitorTest;
		}


		[Cacheable]
		public string Calc(string prop1, string prop2)
		{
			visitorTest.Visit (prop1, prop2);

			return prop1 + "_" + prop2;
		}

		[Cacheable]
		public string Calc()
		{
			visitorTest.Visit();
			return "noparam";
		}

		[Cacheable]
		public string Calc2()
		{
			visitorTest.Visit();
			return "noparam2";
		}

		[Cacheable]
		public string CalcOverloaded(int id)
		{
			visitorTest.Visit();
			return "int_"+id;
		}

		[Cacheable]
		public string CalcOverloaded(string id)
		{
			visitorTest.Visit();
			return "string_"+id;
		}


		[Cacheable]
		public string CalcArray(string[] arr1, string[] arr2)
		{
			visitorTest.Visit(arr1, arr2);
			return String.Join("_", arr1) + "," + String.Join("_", arr2);
		}


		[Cacheable]
		public string CalcParams(params string[] props)
		{
			visitorTest.Visit(props);
			return "params_"+props.Length;
		}


		public string Calc2 (
			string prop1,
			[CacheIgnore] string prop2)
		{
			return prop1 + "_" + prop2;
		}




	}
}