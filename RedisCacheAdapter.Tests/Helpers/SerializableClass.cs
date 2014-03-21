using System;
using System.Xml.Serialization;

namespace RedisCacheAdapter.Tests.Helpers
{
	[XmlType("aaa")]
	public class SerializableClass
	{
		public string P1 { get; set; }
		public int f1;
	}
}