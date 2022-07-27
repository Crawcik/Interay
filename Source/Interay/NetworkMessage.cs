using System;
using System.Reflection;

namespace Interay
{
	public readonly struct NetworkMessage
	{
		public readonly NetworkScript Instance;
		public readonly MethodInfo Method;
		public readonly ulong? TargetID;
		public readonly Type DataType;
		public readonly object Data;

		public NetworkMessage(NetworkScript instance, MethodInfo method, object data = null, ulong? targetID = null)
		{
			Instance = instance;
			Method = method;
			TargetID = targetID;
			DataType = data?.GetType();
			Data = data;
		}
	}
	
}