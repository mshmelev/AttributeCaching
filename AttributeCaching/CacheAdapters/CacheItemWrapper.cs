namespace AttributeCaching.CacheAdapters
{
	/// <summary>
	/// Class is needed to store null values in cache.
	/// </summary>
	public sealed class CacheItemWrapper
	{
        /// <summary>
        /// Cached value
        /// </summary>
		public object Value { get; set; }
	}
}