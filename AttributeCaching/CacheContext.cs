﻿using System;

namespace AttributeCaching
{
	/// <summary>
	/// Represents a caching context for currently executing method. Accessible from CacheScope class.
	/// </summary>
	public class CacheContext
	{
		internal CacheContext(string cacheKey, TimeSpan lifeSpan)
		{
			CacheKey = cacheKey;
			LifeSpan = lifeSpan;
		}


		public string CacheKey
		{
			get;
			private set;
		}


		/// <summary>
		/// Lifetime of the caching value
		/// </summary>
		public TimeSpan LifeSpan
		{
			get;
			set;
		}


		/// <summary>
		/// Disable caching of the value
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
	}
}