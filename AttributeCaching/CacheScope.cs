using System;
using System.Collections.Generic;

namespace AttributeCaching
{
	/// <summary>
	/// Allows to manage caching properties from within a cacheable method.
	/// </summary>
	/// <example>
	/// object SomeCacheableMethod()
	/// {
	///		// ...
	///		CacheScope.CurrentContext.DisableCaching();
	/// }
	/// </example>
	public class CacheScope
	{
		[ThreadStatic]
		private static Stack<CacheContext> contexts;


		internal static CacheContext AddContext(string cacheKey, TimeSpan lifeSpan, IEnumerable<string> dependencyTags)
		{
			var ctx = new CacheContext(cacheKey, lifeSpan, dependencyTags);
			Contexts.Push(ctx);

			return ctx;
		}


		internal static void RemoveContext()
		{
			Contexts.Pop();
		}


		public static CacheContext CurrentContext
		{
			get
			{
				return Contexts.Peek();
			}
		}


		private static Stack<CacheContext> Contexts
		{
			get
			{
				return contexts ?? (contexts = new Stack<CacheContext>());
			}
		}

	}
}