using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using AttributeCaching.Tools;
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
		private readonly TimeSpan lifeSpan;


		public CacheableAttribute()
		{
			lifeSpan = TimeSpan.FromDays (36500);
		}

		public CacheableAttribute(double lifeSpanSeconds)
		{
			lifeSpan = TimeSpan.FromSeconds(lifeSpanSeconds);
		}



		/// <summary>
		/// Precalculate some parameters, method is invoked once during the compilation
		/// </summary>
		/// <param name="method"></param>
		/// <param name="aspectInfo"></param>
		public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
		{
			BuildCacheableArgIndexes(method);
			methodDeclaration = Reflection.GetMethodDeclaration(method);

			if (method.IsSpecialName && method.Name.StartsWith("set_"))
			{
				isPropertySetMethod = true;
				propertyGetMethodDeclaration = Reflection.GetMethodDeclaration (method.ReflectedType.GetMethod("get_" + method.Name.Substring(4)));
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

			object value = CacheFactory.Cache.Get (key);
			if (value != null)
			{
				args.ReturnValue = value;
				args.FlowBehavior = FlowBehavior.Return;
				return;
			}

			var context= CacheScope.AddContext (key, lifeSpan);
			args.MethodExecutionTag = context;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnSuccess(MethodExecutionArgs args)
		{
			var cacheContext = (CacheContext) args.MethodExecutionTag;
			if (args.ReturnValue!= null)
				CacheFactory.Cache.Add (cacheContext.CacheKey, args.ReturnValue, DateTimeOffset.Now.Add (cacheContext.LifeSpan));
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnExit(MethodExecutionArgs args)
		{
			// clear cache for the property Get method
			if (isPropertySetMethod)
			{
				string key = KeyBuilder.BuildKey(null, propertyGetMethodDeclaration, new int[0]);
				CacheFactory.Cache.Remove(key);
			}

			if (args.MethodExecutionTag!= null)
				CacheScope.RemoveContext();
		}
	}
}