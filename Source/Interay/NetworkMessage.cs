using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Interay
{
	internal struct NetworkMessage
	{
		public readonly NetworkScript Instance;
		public readonly MethodInfo Method;
		public readonly byte MessageType; // 1 bit = instance, 2 bit = data, 3 bit = targetID
		public ulong TargetID;
		public Type DataType;
		public object Data;

		public bool IsData => (MessageType & 0b0000_0010) == 0b0000_0010;
		public bool IsTarget => (MessageType & 0b0000_0100) == 0b0000_0100;

		public NetworkMessage(NetworkScript instance, MethodInfo method, byte messageType)
		{
			Instance = instance;
			Method = method;
			MessageType = messageType;
			TargetID = 0;
			DataType = null;
			Data = null;
		}

		public void Invoke()
		{
			if (IsData && IsTarget)
			{
				Method.Invoke(Instance, new object[] { Data, TargetID });
			}
			else if (IsData)
			{
				Method.Invoke(Instance, new object[] { Data });
			}
			else if (IsTarget)
			{
				var @delegate = (Action<ulong>)Delegate.CreateDelegate(typeof(Action<ulong>), Instance, Method);
				@delegate.Invoke(TargetID);
			}
			else
			{
				var @delegate = (Action)Delegate.CreateDelegate(typeof(Action), Instance, Method);
				@delegate.Invoke();
			}
		}
	}
	
}