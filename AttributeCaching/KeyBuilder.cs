﻿using System;
using System.Collections;
using System.Reflection;
using System.Text;
using PostSharp.Aspects;

namespace AttributeCaching
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
		public static string BuildKey (MethodExecutionArgs args, string methodDeclaration, int[] cacheArgIndexes)
		{
			var key = new StringBuilder(methodDeclaration);

			foreach (int cacheArgIndex in cacheArgIndexes)
			{
				key.Append (GetParamValue(args.Arguments[cacheArgIndex]));
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





		private static string GetParamValue (object val)
		{
			if (val == null)
				return "";

			// strings
			string strVal = val as string;
			if (strVal != null)
			{
				if (strVal.Length == 0)
					strVal = EmptyStringReplacer;
				return strVal;
			}

			// collections
			IEnumerable coll = val as IEnumerable;
			if (coll!= null)
			{
				var sb = new StringBuilder();
				sb.Append ('[');
				foreach (object item in coll)
				{
					sb.Append (GetParamValue (item));
					sb.Append (ParamSeparator);
				}
				sb.Append (']');
				return sb.ToString();
			}

			// all others
			strVal = val .ToString();
			if (strVal.Length == 0)
				strVal = EmptyStringReplacer;

			return strVal;
		}
	}
}
