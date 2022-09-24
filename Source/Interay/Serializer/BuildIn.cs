using System;

namespace Interay.Serializer
{
	public sealed partial class BuildIn : NetworkSerializer
	{
		private bool _initialized = false;
		public override bool IsInitialized => _initialized;

		public override bool Initialize()
		{
			 _initialized = true;
			 return true;
		}

		public override NetworkMessage Deserialize(INetworkPacket packet, Type type)
		{
			throw new NotImplementedException();
		}

		public override INetworkPacket Serialize(object data, Type type)
		{
			throw new NotImplementedException();
		}


		public override void Dispose()
		{
			_initialized = false;
		}
	}
}