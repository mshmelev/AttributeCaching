using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using PostSharp.Aspects;

namespace AttributeCaching
{
	[Serializable]
	public class CacheableAttribute : OnMethodBoundaryAspect
	{
		private int[] cacheArgIndexes;
		private string methodDeclaration;
		private string propertyGetMethodDeclaration;
		private bool isPropertySetMethod;

		public CacheableAttribute()
		{
		}


		/// <summary>
		/// Precalculate some parameters, method is invoked once during the compilation
		/// </summary>
		/// <param name="method"></param>
		/// <param name="aspectInfo"></param>
		public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
		{
			BuildCacheableArgIndexes(method);
			methodDeclaration = GetMethodDeclaration(method);

			if (method.IsSpecialName)
			{
				if (method.Name.StartsWith ("set_"))
				{
					isPropertySetMethod = true;
					propertyGetMethodDeclaration = GetMethodDeclaration (method, "get_" + method.Name.Substring (4));
				}
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="method"></param>
		private void BuildCacheableArgIndexes (MethodBase method)
		{
			var pars = method.GetParameters();
			var indexes = new List<int>();
			for (int i = 0; i < pars.Length; i++)
			{
				var parameterInfo = pars[i];
				if (!IsDefined (parameterInfo, typeof (CacheIgnoreAttribute)))
					indexes.Add (i);
			}
			cacheArgIndexes = indexes.ToArray();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnEntry(MethodExecutionArgs args)
		{
			string key = KeyBuilder.BuildKey(args.Arguments, methodDeclaration, cacheArgIndexes);
			args.MethodExecutionTag = key;

			object value = CacheFactory.Cache.Get (key);
			if (value != null)
			{
				args.ReturnValue = value;
				args.FlowBehavior = FlowBehavior.Return;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnSuccess(MethodExecutionArgs args)
		{
			string key = (string)args.MethodExecutionTag;
			if (args.ReturnValue!= null)
				CacheFactory.Cache.Add (key, args.ReturnValue, DateTimeOffset.Now.AddDays (2));
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnExit(MethodExecutionArgs args)
		{
			// clear property Get cache
			if (isPropertySetMethod)
			{
				string key = KeyBuilder.BuildKey(null, propertyGetMethodDeclaration, new int[0]);
				CacheFactory.Cache.Remove(key);
			}
		}


		/// <summary>
		/// Generates method full signature including parameter types
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodName"></param>
		/// <returns></returns>
		private static string GetMethodDeclaration(MethodBase method, string methodName= null)
		{
			var res = new StringBuilder();
			res.Append (method.ReflectedType.FullName);
			res.Append ('.');
			res.Append(methodName ?? method.Name);

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