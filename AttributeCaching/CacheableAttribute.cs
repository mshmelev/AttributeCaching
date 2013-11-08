using System;
using System.Collections.Generic;
using System.Reflection;
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
		private TimeSpan lifeSpan;


		/// <summary>
		/// Default, specifies infinit cache lifetime
		/// </summary>
		public CacheableAttribute()
		{
			lifeSpan = TimeSpan.MaxValue;
		}

		/// <summary>
		/// Specifies cache lifetime in seconds
		/// </summary>
		/// <param name="lifeSpanSeconds">Cache lifetime in seconds</param>
		public CacheableAttribute(double lifeSpanSeconds)
		{
			lifeSpan = TimeSpan.FromSeconds(lifeSpanSeconds);
		}



		/// <summary>
		/// Gets/Sets cache lifetime in seconds.
		/// Allows syntax: [Cacheable (Seconds = 30)]
		/// </summary>
		public double Seconds
		{
			get
			{
				return lifeSpan.TotalSeconds;
			}
			set
			{
				lifeSpan = TimeSpan.FromSeconds (value);
			}
		}


		/// <summary>
		/// Gets/Sets cache lifetime in minutes.
		/// Allows syntax: [Cacheable (Minutes = 5)]
		/// </summary>
		public double Minutes
		{
			get
			{
				return lifeSpan.TotalMinutes;
			}
			set
			{
				lifeSpan = TimeSpan.FromMinutes (value);
			}
		}


		/// <summary>
		/// Gets/Sets cache lifetime in hours.
		/// Allows syntax: [Cacheable (Hours = 5)]
		/// </summary>
		public double Hours
		{
			get
			{
				return lifeSpan.TotalHours;
			}
			set
			{
				lifeSpan = TimeSpan.FromHours (value);
			}
		}


		/// <summary>
		/// Gets/Sets cache lifetime in days.
		/// Allows syntax: [Cacheable (Days = 2)]
		/// </summary>
		public double Days
		{
			get
			{
				return lifeSpan.TotalDays;
			}
			set
			{
				lifeSpan = TimeSpan.FromDays (value);
			}
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
			if (args.ReturnValue != null && !cacheContext.IsCachingDisabled())
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