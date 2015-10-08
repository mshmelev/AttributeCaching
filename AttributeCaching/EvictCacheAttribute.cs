using System;
using Shaspect;

namespace AttributeCaching
{
	/// <summary>
	/// Evicts cache by dependency tags.
	/// Eviction is done after a method is executed (successfully or with exception).
	/// </summary>
	[Serializable]
	public class EvictCacheAttribute : BaseAspectAttribute
	{
		private readonly string[] dependencyTags;
		

		/// <summary>
		/// Evicts cache by passed dependency tags
		/// </summary>
		/// <param name="dependencyTags"></param>
		public EvictCacheAttribute(params string[] dependencyTags)
		{
			this.dependencyTags = dependencyTags;
		}



		/// <summary>
		/// Specifies wether to evict using all tags or any tag. Default: false.
		/// </summary>
		public bool UseAllTags
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
	    /// 
	    /// </summary>
	    /// <param name="methodExecInfo"></param>
	    public override void OnExit (MethodExecInfo methodExecInfo)
		{
			if (UseAllTags)
				CacheFactory.Cache.EvictAll (CacheName, dependencyTags);
			else
				CacheFactory.Cache.EvictAny (CacheName, dependencyTags);
		}
	}
}