using System;

namespace AttributeCaching.Tests
{
	public class TestClass
	{
		private readonly IVisitor visitor;
		private string propValue = "prop";


		public TestClass(IVisitor visitor)
		{
			this.visitor = visitor;
		}


		[Cacheable]
		public string Calc(string prop1, string prop2)
		{
			visitor.Visit (prop1, prop2);

			return prop1 + "_" + prop2;
		}

		[Cacheable]
		public string Calc()
		{
			visitor.Visit();
			return "noparam";
		}

		[Cacheable]
		public string Calc2()
		{
			visitor.Visit();
			return "noparam2";
		}

		[Cacheable]
		public string CalcOverloaded(int id)
		{
			visitor.Visit();
			return "int_"+id;
		}

		[Cacheable]
		public string CalcOverloaded(string id)
		{
			visitor.Visit();
			return "string_"+id;
		}


		[Cacheable]
		public string CalcArray(string[] arr1, string[] arr2)
		{
			visitor.Visit(arr1, arr2);
			return String.Join("_", arr1) + "," + String.Join("_", arr2);
		}


		[Cacheable]
		public string CalcParams(params string[] props)
		{
			visitor.Visit(props);
			return "params_"+props.Length;
		}


		[Cacheable]
		public string CalcProp
		{
			get
			{
				visitor.Visit ();
				return propValue;
			}
			set
			{
				visitor.Visit(value);
				propValue = value;
			}
		}


		[Cacheable]
		public string this [string index]
		{
			get
			{
				visitor.Visit(index);
				return "ind_" + index;
			}

		}




		public string Calc2 (
			string prop1,
			[CacheIgnore] string prop2)
		{
			return prop1 + "_" + prop2;
		}




	}
}