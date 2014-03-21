using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace RedisCacheAdapter.Tests.Helpers
{
	[ProtoContract]
	[DataContract]
	[XmlType("aaa")]
	public class ProtoContractClass
	{
		[ProtoIgnore]
		public string P1 { get; set; }

		[ProtoMember(1)]
		public int f1;
	}
}