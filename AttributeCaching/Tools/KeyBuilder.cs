using System.Collections;
using System.Text;
using PostSharp.Aspects;

namespace AttributeCaching.Tools
{
	internal static class KeyBuilder
	{
		private const char ParamSeparator = '\u201a';				// looks like comma
		private const string EmptyStringReplacer = "\u0000";




		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <param name="methodDeclaration"></param>
		/// <param name="cacheArgIndexes"></param>
		/// <returns></returns>
		public static string BuildKey (Arguments args, string methodDeclaration, int[] cacheArgIndexes)
		{
			var key = new StringBuilder(methodDeclaration);

			foreach (int cacheArgIndex in cacheArgIndexes)
			{
				AddParamValue (key, args[cacheArgIndex]);
				key.Append (ParamSeparator);
			}

			return key.ToString();
		}


		private static void AddParamValue (StringBuilder res, object val)
		{
			if (val == null)
				return;

			// strings
			string strVal = val as string;
			if (strVal != null)
			{
				if (strVal.Length == 0)
					strVal = EmptyStringReplacer;
				res.Append (strVal);
				return;
			}

			// collections
			IEnumerable coll = val as IEnumerable;
			if (coll!= null)
			{
				res.Append ('[');
				foreach (object item in coll)
				{
					AddParamValue (res, item);
					res.Append (ParamSeparator);
				}
				res.Append (']');
				return;
			}

			// all others
			int n = res.Length;
			res.Append (val);
			if (res.Length== n)
				res.Append (EmptyStringReplacer);
		}
	}
}
