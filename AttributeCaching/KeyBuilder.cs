using System.Reflection;
using System.Text;
using PostSharp.Aspects;

namespace AttributeCaching
{
	internal static class KeyBuilder
	{
		private const char ParamSeparator = '\u5678';
		private const string EmptyStringReplacer = "\u0000";

		public static string BuildKey (MethodExecutionArgs args, string methodDeclaration, int[] cacheArgIndexes)
		{
			var key = new StringBuilder(methodDeclaration);

			foreach (int cacheArgIndex in cacheArgIndexes)
			{
				object val= args.Arguments[cacheArgIndex];
				string strVal= "";
				if (val != null)
				{
					strVal = val.ToString();
					if (strVal.Length == 0)
						strVal = EmptyStringReplacer;
				}

				key.Append (strVal);
				key.Append (ParamSeparator);
			}

			return key.ToString();
		}




		/// <summary>
		/// Generates method full signature including parameter types
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static string GetMethodDeclaration(MethodBase method)
		{
			var res = new StringBuilder();
			res.Append (method.ReflectedType.FullName);
			res.Append ('.');
			res.Append (method.Name);

			res.Append('(');
			var pars = method.GetParameters();
			foreach (var parameterInfo in pars)
			{
				res.Append (parameterInfo.ParameterType.FullName);
				res.Append (',');
			}
			res.Append (')');

			return res.ToString();
		}
		

	}
}