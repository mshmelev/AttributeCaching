using System;
using System.Collections;
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
		/// Dependency tags for the currently executing method. Can be changed but affects only the currently executing method.
		/// </summary>
		public List<string> DependencyTags
		{
			get;
			private set;
		}
		

		/// <summary>
		/// Updates an existing dependency tag. Can be useful to append an ID value to tag
		/// </summary>
		/// <param name="oldTag"></param>
		/// <param name="newTag"></param>
		/// <returns>true, if old tag was found</returns>
		/// <example>cacheContext.ChangeDependecyTag ("item_", "item_"+123);</example>
		public bool ChangeDependecyTag (string oldTag, string newTag)
		{
			int i = DependencyTags.IndexOf (oldTag);
			if (i == -1)
				return false;

			DependencyTags[i] = newTag;

			return true;
		}
	}
}