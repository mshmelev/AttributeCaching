using System;
using System.Collections.Generic;

namespace AttributeCaching.CacheAdapters
{
	/// <summary>
	/// Base adapter for different cache implementations
	/// </summary>
	public abstract class CacheAdapter
	{
		/// <summary>
		/// Returns object from cache by key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="cacheName">pass null for default cache</param>
		/// <returns>null, if object is not found</returns>
		public abstract object Get (string key, string cacheName);


		/// <summary>
		/// Adds object to cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="absoluteExpiration"></param>
		/// <param name="cacheName">pass null for default cache</param>
		/// <param name="dependencyTags"></param>
		public void Set (string key, object value, DateTimeOffset absoluteExpiration, string cacheName, params string[] dependencyTags)
		{
			Set (key, value, absoluteExpiration, cacheName, (IEnumerable<string>)dependencyTags);
		}


		/// <summary>
		/// Adds object to cache
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="absoluteExpiration"></param>
		/// <param name="cacheName">pass null for default cache</param>
		/// <param name="dependencyTags"></param>
		public abstract void Set(string key, object value, DateTimeOffset absoluteExpiration, string cacheName, IEnumerable<string> dependencyTags);


		/// <summary>
		/// Removes object from cache by key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="cacheName">pass null for default cache</param>
		/// <returns>An object that represents the value of the removed cache entry that was specified by the key, or null if the specified entry was not found</returns>
		public abstract object Remove(string key, string cacheName);


		/// <summary>
		/// Evicts all objects in cache which have ALL passed tags
		/// </summary>
		/// <param name="cacheName"></param>
		/// <param name="dependencyTags">pass null for default cache</param>
		public abstract void EvictAll (string cacheName, params string[] dependencyTags);


		/// <summary>
		/// Evicts all objects in cache which have ANY of passed tags
		/// </summary>
		/// <param name="cacheName">pass null for default cache</param>
		/// <param name="dependencyTags"></param>
		public abstract void EvictAny (string cacheName, params string[] dependencyTags);
	}
}