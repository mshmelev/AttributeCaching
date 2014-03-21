namespace RedisCacheAdapter.Tests
{
	public class ComplexClass : BaseClass
	{
		public int f1;
		public int[] arr;
		public string P1 { get; set; }
		public string[] P2 { get; set; }
		public ComplexClass[] subs;
		public int? f2;
		public int? f3;
	}

	public abstract class BaseClass
	{
		public string fb;
	}
}