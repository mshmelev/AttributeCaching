using System.Reflection;
using System.Text;
using PostSharp.Aspects;

namespace AttributeCaching
{
	internal static class KeyBuilder
	{
		private const char ParamSeparator = '\u5678';

		public static string BuildKey (MethodExecutionArgs args, string methodDeclaration, int[] cacheArgIndexes)
		{
			var key = new StringBuilder(methodDeclaration);

			foreach (int cacheArgIndex in cacheArgIndexes)
			{
				key.Append (args.Arguments[cacheArgIndex]);
				key.Append (ParamSeparator);
			}

			return key.ToString();
		}


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