using System;

namespace RedisCacheAdapter.Tests.Helpers
{
	public class ClassWithGetProp
	{
		public string PublicProp
		{
			get;
			set;
		}


		public string ReadonlyProp
		{
			get { return PublicProp + "_"; }
		}


		public string PrivateSetProp
		{
			get { return PublicProp + "_"; }
			private set
			{
				throw new Exception("This should never be called");
			}
		}

		 
	}
}