using System;

namespace Interay
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class NetworkMethodAttribute : Attribute
	{
		public readonly bool AllowClient, AllowServer;
		
		// This is a positional argument
		public NetworkMethodAttribute()
		{
			AllowClient = true;
			AllowServer = true;
		}
	}
}