using System.Reflection;
using System.Text;

namespace AttributeCaching.Tools
{
	internal static class Reflection
	{
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