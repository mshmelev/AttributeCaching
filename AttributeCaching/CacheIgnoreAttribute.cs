using System;

namespace AttributeCaching
{
	/// <summary>
	/// Allows to ignore a method parameter value for the caching mechanism.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public class CacheIgnoreAttribute : Attribute
	{
		 
	}
}