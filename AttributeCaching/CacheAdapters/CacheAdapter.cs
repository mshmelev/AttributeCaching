using System;

namespace AttributeCaching.CacheAdapters
{
	public abstract class CacheAdapter
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public abstract object Get (string key);
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <param name="absoluteExpiration"></param>
		public abstract void Set (string key, object value, DateTimeOffset absoluteExpiration);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <returns>An object that represents the value of the removed cache entry that was specified by the key, or null if the specified entry was not found</returns>
		public abstract object Remove (string key);
	}
}