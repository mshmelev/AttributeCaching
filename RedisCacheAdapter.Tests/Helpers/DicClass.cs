namespace RedisCacheAdapter.Tests.Helpers
{
	public class DicClass
	{
		protected bool Equals (DicClass other)
		{
			return string.Equals (Prop, other.Prop);
		}


		public override int GetHashCode()
		{
			return (Prop != null ? Prop.GetHashCode() : 0);
		}


		public string Prop { get; set; }

		public override bool Equals (object obj)
		{
			return Equals ((DicClass)obj);
		}
	}
}