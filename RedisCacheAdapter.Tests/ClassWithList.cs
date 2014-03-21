using System.Collections.Generic;

namespace RedisCacheAdapter.Tests
{
	public class ClassWithList
	{
		public ClassWithList()
		{
			Items = new List<int> {7, 5, 9};
		}

		public List<int> Items { get; set; }
	}
}