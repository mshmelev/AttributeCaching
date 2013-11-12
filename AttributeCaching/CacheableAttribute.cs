using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using AttributeCaching.Tools;
using PostSharp.Aspects;

namespace AttributeCaching
{
	/// <summary>
	/// Allows caching of a property, method, or a whole class.
	/// </summary>
	[Serializable]
	public class CacheableAttribute : OnMethodBoundaryAspect
	{
		private int[] cacheArgIndexes;
		private string methodDeclaration;
		private string propertyGetMethodDeclaration;
		private bool isPropertySetMethod;
		private TimeSpan lifeSpan;
		private object syncMethodCall = new object();


		/// <summary>
		/// Default, specifies infinit cache lifetime
		/// </summary>
		/// <param name="dependencyTags"></param>
		public CacheableAttribute(params string[] dependencyTags)
		{
			lifeSpan = TimeSpan.FromDays(365 * 1000);		// can't use TimeSpan.MaxValue because it will exceed DateTime.MaxValue
			DependencyTags = dependencyTags;
		}


		/// <summary>
		/// Specifies cache lifetime in seconds
		/// </summary>
		/// <param name="lifeSpanSeconds">Cache lifetime in seconds</param>
		/// <param name="dependencyTags"></param>
		public CacheableAttribute(double lifeSpanSeconds, params string[] dependencyTags)
		{
			lifeSpan = TimeSpan.FromSeconds(lifeSpanSeconds);
			DependencyTags = dependencyTags;
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
		/// Allows multiple simultaneous calls to the same cacheable method with the same parameters. Usually it's not needed to calculate the same cacheable value several times simultaneously.
		/// Default value: false.
		/// </summary>
		public bool AllowSimultenousCalls
		{
			get
			{
				return (syncMethodCall == null);
			}
			set
			{
				if (value)
				{
					syncMethodCall = null;
				}
				else
				{
					if (syncMethodCall == null)
						syncMethodCall = new object();
				}
			}
		}


		/// <summary>
		/// List of tags the caching value is dependent on.
		/// </summary>
		public string[] DependencyTags
		{
			get;
			set;
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
			// trye to get from cache first
			string key = KeyBuilder.BuildKey(args.Arguments, methodDeclaration, cacheArgIndexes);
			object value = CacheFactory.Cache.Get (key);

			if (value == null && syncMethodCall != null)
			{
				Monitor.Enter (syncMethodCall);
				value = CacheFactory.Cache.Get(key);			// value could have been alread put to the cache by other thread at this point
				if (value!= null)
					Monitor.Exit (syncMethodCall);
			}

			if (value != null)
			{
				args.ReturnValue = value;
				args.FlowBehavior = FlowBehavior.Return;
				return;
			}

			// get value from the method itself
			var context= CacheScope.AddContext (key, lifeSpan, DependencyTags);
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
				CacheFactory.Cache.Set(cacheContext.CacheKey, args.ReturnValue, DateTimeOffset.Now.Add(cacheContext.LifeSpan), cacheContext.DependencyTags);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnExit(MethodExecutionArgs args)
		{
			// clear cache for the property's Get method if Set was called
			if (isPropertySetMethod)
			{
				string key = KeyBuilder.BuildKey(null, propertyGetMethodDeclaration, new int[0]);
				CacheFactory.Cache.Remove(key);
			}

			if (syncMethodCall!= null && Monitor.IsEntered(syncMethodCall))
				Monitor.Exit (syncMethodCall);

			if (args.MethodExecutionTag!= null)
				CacheScope.RemoveContext();
		}
	}
}