using System;

namespace Interay
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class NetworkMethodAttribute : Attribute
	{
		public bool AccessFromClient, AccessFromServer;
		
		// This is a positional argument
		public NetworkMethodAttribute()
		{
			AccessFromClient = true;
			AccessFromServer = true;
		}
	}
}