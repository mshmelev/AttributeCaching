namespace AttributeCaching.CacheAdapters
{
	internal sealed class NullValue
	{
		private NullValue()
		{
		}


		public static readonly NullValue Value = new NullValue();


		public override bool Equals(object obj)
		{
			return (obj is NullValue);
		}


		public override int GetHashCode()
		{
			return 1234;
		}


		public static bool operator ==(NullValue v1, NullValue v2)
		{
			if ((object)v1 == null || (object)v2 == null)
				return false;

			return true;
		}

		public static bool operator !=(NullValue v1, NullValue v2)
		{
			return !(v1 == v2);
		}
	}
}