using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Interay.Serializer
{
	public sealed partial class BuildIn : General
	{
		private readonly Type _baseType = typeof(NetworkEntity);
		private bool _initialized = false;
		public override bool IsInitialized => _initialized;

		public override bool Initialize()
		{
			_initialized = true;
			return true;
		}

		public override bool Serialize(INetworkPacket packet, Type type, object data)
		{
			if (base.Serialize(packet, type, data))
				return true;

			if(type.BaseType != _baseType)
				return false;

			var entity = (NetworkEntity)data;
			entity.Serialize(packet);
			return true;
		}

		public override bool Deserialize(INetworkPacket packet, out object data)
		{
			if (base.Deserialize(packet, out data))
				return true;
			return true;
		}

		public override void Dispose()
		{
			_initialized = false;
		}

		public abstract class NetworkEntity
		{
			internal abstract void Serialize(INetworkPacket packet);
			internal abstract void Deserialize(INetworkPacket packet);
		}
	}
}