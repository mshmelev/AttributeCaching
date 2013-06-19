using System;
using System.Collections.Generic;

namespace AttributeCaching
{
	public class CacheScope
	{
		[ThreadStatic]
		private static Stack<CacheContext> contexts;


		internal static CacheContext AddContext (string cacheKey)
		{
			var ctx = new CacheContext (cacheKey);
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