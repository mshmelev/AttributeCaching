using System.Runtime.Serialization;

namespace RedisCacheAdapter.Tests.Helpers
{
	[DataContract]
	public class DataContractClass
	{
		[DataMember]
		public string P1 { get; set; }

		public int f1;
	}
}