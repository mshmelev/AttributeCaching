using System;
using System.Collections.Generic;

namespace AttributeCaching.CacheAdapters
{
	public abstract class CacheAdapter
	{
		/// <summary>
		/// Returns object from cache by key
		/// </summary>
		/// <param name="key"></param>
		/// <returns>null, if object is not found</returns>
		public abstract object Get (string key);


		/// <summary>
		/// Adds object to cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="absoluteExpiration"></param>
		/// <param name="dependencyTags"></param>
		public void Set (string key, object value, DateTimeOffset absoluteExpiration, params string[] dependencyTags)
		{
			Set (key, value, absoluteExpiration, (IEnumerable<string>)dependencyTags);
		}


		/// <summary>
		/// Adds object to cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="absoluteExpiration"></param>
		/// <param name="dependencyTags"></param>
		public abstract void Set(string key, object value, DateTimeOffset absoluteExpiration, IEnumerable<string> dependencyTags);


		/// <summary>
		/// Removes object from cache by key
		/// </summary>
		/// <param name="key"></param>
		/// <returns>An object that represents the value of the removed cache entry that was specified by the key, or null if the specified entry was not found</returns>
		public abstract object Remove (string key);



		/// <summary>
		/// Evicts all objects in cache which have ALL passed tags
		/// </summary>
		/// <param name="dependencyTags"></param>
		public abstract void EvictAll (params string[] dependencyTags);

	}
}