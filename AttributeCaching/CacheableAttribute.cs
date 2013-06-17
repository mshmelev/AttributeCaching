using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Caching;
using PostSharp.Aspects;

namespace AttributeCaching
{
	[Serializable]
	public class CacheableAttribute : OnMethodBoundaryAspect
	{
		private int[] cacheArgIndexes;
		private string methodDeclaration;

		public CacheableAttribute()
		{
		}


		/// <summary>
		/// Precalculate some parameters, method is invoked once during the compilation
		/// </summary>
		/// <param name="method"></param>
		/// <param name="aspectInfo"></param>
		public override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
		{
			BuildCacheableArgIndexes(method);
			methodDeclaration = KeyBuilder.GetMethodDeclaration(method);
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
			string key = KeyBuilder.BuildKey(args, methodDeclaration, cacheArgIndexes);
			args.MethodExecutionTag = key;

			object value = CacheFactory.Cache.Get (key);
			if (value != null)
			{
				args.ReturnValue = value;
				args.FlowBehavior = FlowBehavior.Return;
			}
		}


		public override void OnSuccess(MethodExecutionArgs args)
		{
			string key = (string)args.MethodExecutionTag;
			if (args.ReturnValue!= null)
				CacheFactory.Cache.Add (key, args.ReturnValue, DateTimeOffset.Now.AddDays (2));
		}
	}
}