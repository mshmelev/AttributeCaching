using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using AttributeCaching.Tools;
using Shaspect;


namespace AttributeCaching
{
	/// <summary>
	/// Allows caching of a property, method, or a whole class.
	/// </summary>
	[Serializable]
	public class CacheableAttribute : BaseAspectAttribute
	{
		private int[] cacheArgIndexes;
		private string methodDeclaration;
		private string propertyGetMethodDeclaration;
		private bool isPropertySetMethod;
		private TimeSpan lifeSpan;
		private bool syncMethodCall = true;


		/// <summary>
		/// Default constructor, specifies infinit cache lifetime
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
		public bool AllowConcurrentCalls
		{
			get
			{
				return !syncMethodCall;
			}
			set
			{
				syncMethodCall = !value;
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
		/// Specifies cache name/region/area. Can be used to store values in different cache storages.
		/// Default value: null.
		/// </summary>
		public string CacheName
		{
			get;
			set;
		}


	    /// <summary>
		/// Precalculate some parameters, method is invoked once during the assembly start
		/// </summary>
		/// <param name="method"></param>
	    public override void Initialize (MethodBase method)
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
        /// <param name="methodExecInfo"></param>
	    public override void OnEntry (MethodExecInfo methodExecInfo)
		{
			// try to get from cache first
			string key = KeyBuilder.BuildKey (methodExecInfo.Arguments, methodDeclaration, cacheArgIndexes);
			var cacheItem = CacheFactory.Cache.Get (key, CacheName);

			if (cacheItem == null && syncMethodCall)
			{
				string lockKey = String.Intern (key);
				Monitor.Enter (lockKey);

				cacheItem = CacheFactory.Cache.Get(key, CacheName);			// value could have been already put to the cache by other thread at this point
				if (cacheItem != null)
					Monitor.Exit (lockKey);
			}

			if (cacheItem != null)
			{
				methodExecInfo.ReturnValue = cacheItem.Value;
				methodExecInfo.ExecFlow = ExecFlow.Return;
				return;
			}

			// get value from the method itself
			var context= CacheScope.AddContext (key, lifeSpan, DependencyTags);
			methodExecInfo.Data = context;
		}


	    /// <summary>
	    /// 
	    /// </summary>
	    /// <param name="methodExecInfo"></param>
	    public override void OnSuccess (MethodExecInfo methodExecInfo)
		{
			var cacheContext = (CacheContext) methodExecInfo.Data;
			if (!isPropertySetMethod && !cacheContext.IsCachingDisabled())
				CacheFactory.Cache.Set(cacheContext.CacheKey, methodExecInfo.ReturnValue, cacheContext.LifeSpan, CacheName, cacheContext.DependencyTags);
		}


	    /// <summary>
	    /// 
	    /// </summary>
	    /// <param name="methodExecInfo"></param>
	    public override void OnExit (MethodExecInfo methodExecInfo)
		{
			// clear cache for the property's Get method if Set was called
			if (isPropertySetMethod)
			{
				string key = KeyBuilder.BuildKey(null, propertyGetMethodDeclaration, new int[0]);
				CacheFactory.Cache.Remove(key, CacheName);
			}

			if (methodExecInfo.Data != null)
			{
				var context = (CacheContext) methodExecInfo.Data;

				string lockKey = String.Intern (context.CacheKey);
				if (syncMethodCall && Monitor.IsEntered (lockKey))
					Monitor.Exit (lockKey);

				CacheScope.RemoveContext();
			}
		}
	}
}