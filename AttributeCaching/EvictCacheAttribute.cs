using System;
using PostSharp.Aspects;

namespace AttributeCaching
{
	/// <summary>
	/// Evicts cache by dependency tags.
	/// Eviction is done after a method is executed (successfully or with exception).
	/// </summary>
	[Serializable]
	public class EvictCacheAttribute : OnMethodBoundaryAspect
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
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void OnExit (MethodExecutionArgs args)
		{
			if (UseAllTags)
				CacheFactory.Cache.EvictAll (dependencyTags);
			else
				CacheFactory.Cache.EvictAny (dependencyTags);
		}
	}
}