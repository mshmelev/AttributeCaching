using System;
using System.Collections.Generic;

namespace AttributeCaching
{
	/// <summary>
	/// Represents a caching context for currently executing method. Accessible from CacheScope class.
	/// </summary>
	public class CacheContext
	{
		internal CacheContext(string cacheKey, TimeSpan lifeSpan, IEnumerable<string> dependencyTags)
		{
			CacheKey = cacheKey;
			LifeSpan = lifeSpan;
			DependencyTags = new List<string>(dependencyTags);
		}


		/// <summary>
		/// Caching key for the currently executing method
		/// </summary>
		public string CacheKey
		{
			get;
			private set;
		}


		/// <summary>
		/// Lifetime of the caching value for the currently executing method
		/// </summary>
		public TimeSpan LifeSpan
		{
			get;
			set;
		}


		/// <summary>
		/// Disable caching for the currently executing method
		/// </summary>
		public void DisableCaching()
		{
			LifeSpan = TimeSpan.Zero;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool IsCachingDisabled()
		{
			return (LifeSpan == TimeSpan.Zero);
		}


		/// <summary>
		/// Dependency tags for the currently executing method. Can be changed.
		/// </summary>
		public List<string> DependencyTags
		{
			get;
			private set;
		}
	}
}