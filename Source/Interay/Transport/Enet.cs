using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Interay.Transport
{
	/// <summary>
	/// Enet transport layer.
	/// </summary>
	public sealed class Enet : NetworkTransport
	{
		private const PacketFlags DefaultSendFlags = PacketFlags.Reliable;

		#region Fields
		private Dictionary<uint, IntPtr> _peers;
		private IntPtr _host;
		private IntPtr _clientPeer;
		private bool _isInitialized;
		private bool _isRunning;
		#endregion

		#region Properties
		/// <inheritdoc />
		public override bool IsInitialized => _isInitialized;

		/// <inheritdoc />
		public override bool IsRunning => _isRunning;
		#endregion

		#region Methods
		/// <inheritdoc />
		public override bool Initialize()
		{
			if (IsInitialized)
			{
				LogInfo("ENet driver is already initialized!");
				return IsInitialized;
			}
			/*var callbacks = new ENetCallbacks
			{
				malloc = Marshal.AllocHGlobal,
				free = Marshal.FreeHGlobal,
				no_memory = () => throw new OutOfMemoryException("ENet: Out of memory!")
			};
			if (Native.Initialize(Native.Version, ref callbacks) < 0)
   			{
				LogInfo("Failed to initialize ENet driver!");
				return false;
			}*/
			if (Native.Initialize() < 0)
			{
				LogInfo("Failed to initialize ENet driver!");
				return false;
			}
			_isInitialized = true;
			return true;
		}

		/// <inheritdoc />
		public override bool Start(HostType hostType, string address, ushort port)
		{
			if (_isRunning || !_isInitialized)
				return false;
			var nativeAddress = new ENetAddress() { Port = port };
			var isServer = (hostType & HostType.Server) == HostType.Server;
			if (!isServer && Native.SetHost(ref nativeAddress, address) < 0)
			{
				LogWarning("Setting host address on transport layer failed!");
				return false;
			}
			_host = isServer
				? Native.CreateHost(ref nativeAddress, (IntPtr)Convert.ToInt32(Settings.MaxConnections), (IntPtr)1, 0, 0)
				: Native.CreateHost(IntPtr.Zero, (IntPtr)1, (IntPtr)1, 0, 0);
			if (_host == IntPtr.Zero)
			{
				LogWarning("Creating host on transport layer failed!");
				return false;
			}
			if(isServer)
			{
				_peers = new Dictionary<uint, IntPtr>();
			}
			else
			{
				_clientPeer = Native.Connect(_host, ref nativeAddress, 1, 0);
				if (_clientPeer == IntPtr.Zero)
				{
					Native.DestroyHost(_host);
					_host = IntPtr.Zero;
					return false;
				}
			}
			_isRunning = true;
			return true;
		}

		/// <inheritdoc />
		public override void Stop()
		{
			if(!_isRunning)
				return;
			if(_clientPeer != IntPtr.Zero)
			{
				Native.DisconnectNow(_clientPeer, 0);
        		_clientPeer = IntPtr.Zero;
			}
			Native.Flush(_host);
			Native.DestroyHost(_host);
			_host = IntPtr.Zero;
			_isRunning = false;
		}

		/// <inheritdoc />
		public override void Disconnect(ulong id)
		{
			var intID = (uint)id;
			if(_peers.ContainsKey(intID))
			{
				Native.DisconnectNow(_peers[intID], 0);
				_peers.Remove(intID);
				LogInfo($"Disconnected client with id {id} from transport.");
			}
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			if(_isRunning)
				Stop();
			Native.Deinitialize();
			_isInitialized = false;
		}

		/// <inheritdoc />
		public override void Tick(float deltaTime)
		{
			if(!_isRunning)
				return;
			var result = 0;
			while (true)
			{
				result = Native.Service(_host, out var networkEvent, 0);
				if(result < 0)
				{
					LogInfo("ENet fetching network events failed!");
					return;
				}
				else if (result == 0)
					return;
				var peerId  = Native.GetPeerId(networkEvent.Peer);
				switch (networkEvent.Type)
				{
					case EventType.Connect:
						if (_clientPeer != IntPtr.Zero)
							break;
						LogInfo($"Client connected! Peer ID: {peerId}");
						_peers.Add(peerId, networkEvent.Peer);
						InvokeOnClientConnected(peerId);
						break;
					case EventType.Disconnect:
					case EventType.Timeout:
						if (_clientPeer != IntPtr.Zero)
							break;
						LogInfo($"Client disconnected! Peer ID: {peerId}");
						_peers.Remove(peerId);
						InvokeOnClientDisconnected(peerId);
						break;
					case EventType.Receive:
						using (var packet = new EnetNetworkPacket(networkEvent))
						{
							InvokeOnPacketReceived(peerId, packet);
						}
						break;
				}
			}
		}

		/// <inheritdoc />
		public override bool Send(INetworkPacket packet)
		{
			if(!_isRunning)
				return false;
			if (packet is EnetNetworkPacket enetPacket)
			{
				if(enetPacket.Packet == IntPtr.Zero)
					return false;

				/*unsafe
				{
					var pointer = (uint*)enetPacket.Packet;
					pointer++;
					*pointer = (uint)enetPacket.Positon;
				}*/

				if(_clientPeer == IntPtr.Zero)
				{
					Native.Broadcast(_host, 0, enetPacket.Packet);
					return true;
				}
				return Native.Send(_clientPeer, 0, enetPacket.Packet) == 0;
			}
			LogWarning("Packet is not an ENet packet! Sending failed");
			return false;
		}

		/// <inheritdoc />
		public override bool SendTo(ulong id, INetworkPacket packet)
		{
			if(!_isRunning ||_peers is null)
				return false;
			if(!_peers.ContainsKey((uint)id))
			{
				LogWarning($"Client with id {id} is not connected!");
				return false;
			}
			if (packet is EnetNetworkPacket enetPacket)
			{
				if(enetPacket.Packet == IntPtr.Zero)
					return false;
				return Native.Send(_peers[(uint)id], 0, enetPacket.Packet) == 0;
			}
			LogWarning("Packet is not an ENet packet! Sending failed");
			return false;
		}

		/// <inheritdoc />
		public override INetworkPacket CreatePacket(int size) => new EnetNetworkPacket(size, DefaultSendFlags);

		/// <inheritdoc />
		protected override void OnSettingsChanged(ref NetworkSettings newSettings, out bool valid)
		{
			valid = true;
			if (_isRunning)
			{
				if(newSettings.MaxConnections != Settings.MaxConnections 
				|| newSettings.MessageMaxSize != Settings.MessageMaxSize)
				{
					LogWarning("In ENet, only \"TickRate\" and \"MaxNetworkScripts\" can be changed!");
					valid = false;
				}
				return;
			}
			newSettings.MaxConnections = Math.Min(newSettings.MaxConnections, Native.MaximumPeers);
		}

		#endregion

		#region Enums
		internal enum PacketFlags  : uint
		{
			None = 0,
			Reliable = 1,
			Unsequenced = 2,
			NoAllocate = 4,
			UnreliableFragmented = 8,
			Sent =  256
		}

		internal enum EventType 
		{
			None = 0,
			Connect = 1,
			Disconnect = 2,
			Receive = 3,
			Timeout = 4
		}
		#endregion

		#region Structs
		[StructLayout(LayoutKind.Sequential)]
		internal struct ENetAddress 
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] IP;
			public ushort Port;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct ENetEvent 
		{
			public EventType Type;
			public IntPtr Peer;
			public byte ChannelID;
			public uint Data;
			public IntPtr Packet;
		}

		[StructLayout(LayoutKind.Sequential, Size = 11)]
		internal struct ENetPacket
		{
			public uint Flags;
			public uint DataLength;
			IntPtr Data;
			FreeCallback FreeCallback;
			IntPtr UserData;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct ENetCallbacks 
		{
			public AllocCallback malloc;
			public FreeCallback free;
			public NoMemoryCallback no_memory;
		}
		#endregion

		#region Delegates
		internal delegate IntPtr AllocCallback(IntPtr size);
		internal delegate void FreeCallback(IntPtr memory);
		internal delegate void NoMemoryCallback();
		#endregion

		internal static class Native
		{
			#region Constants
			public const int MaximumPeers = 0xFFF;
			public const int MaximumPacketSize  = 32 * 1024 * 1024;
			public const uint Version = (2<<16)|(4<<8)|(8);
			private const string DllName = "enet";
			private const CallingConvention Convention = CallingConvention.Cdecl;
			#endregion

			#region Native Methods
			// int enet_initialize()
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_initialize")]
			internal static extern int Initialize();

			// int enet_initialize_with_callbacks(ENetVersion version, const ENetCallbacks * inits)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_initialize_with_callbacks")]
			internal static extern int Initialize(uint version, ref ENetCallbacks inits);

			// void enet_deinitialize(void);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_deinitialize")]
			internal static extern void Deinitialize();

			// ENetHost * enet_host_create(const ENetAddress *, size_t, size_t, enet_uint32, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_create")]
			internal static extern IntPtr CreateHost(ref ENetAddress address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth);

			// ENetHost * enet_host_create(const ENetAddress *, size_t, size_t, enet_uint32, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_create")]
			internal static extern IntPtr CreateHost(IntPtr address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth);
			
			// void enet_host_destroy(ENetHost *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_destroy")]
			internal static extern void DestroyHost(IntPtr host);

			// ENetPeer * enet_host_connect(ENetHost *, const ENetAddress *, enet_uint32, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_connect")]
			internal static extern IntPtr Connect(IntPtr host, ref ENetAddress address, uint channelCount, uint data);

			// void enet_peer_disconnect(ENetPeer *, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_peer_disconnect")]
			internal static extern void Disconnect(IntPtr peer, uint data);

			// void enet_peer_disconnect_now(ENetPeer *, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_peer_disconnect_now")]
			internal static extern void DisconnectNow(IntPtr peer, uint data);

			// int enet_host_service(ENetHost *, ENetEvent *, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_service")]
			internal static extern int Service(IntPtr host, out ENetEvent @event, uint timeout);
			
			// void enet_host_flush(ENetHost *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_flush")]
			internal static extern void Flush(IntPtr host);

			// int enet_peer_send(ENetPeer *, enet_uint8, ENetPacket *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_peer_send")]
			internal static extern int Send(IntPtr peer, byte channelID, IntPtr packet);

			// void enet_host_broadcast(ENetHost *host, enet_uint8 channelID, ENetPacket *packet)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_broadcast")]
			internal static extern void Broadcast(IntPtr host, byte channelID, IntPtr packet);

			// uint enet_peer_get_id(ENetPeer *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_peer_get_id")]
			internal static extern uint GetPeerId(IntPtr peer);

			// ENetPacket* enet_packet_create(const void*,size_t,enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_create")]
			internal static extern IntPtr CreatePacket(IntPtr data, IntPtr dataLength, PacketFlags flags);

			// void enet_packet_destroy(ENetPacket *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_destroy")]
			internal static extern void DestroyPacket(IntPtr packet);

			// void* enet_packet_get_data(ENetPacket *)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_get_data")]
			internal static extern IntPtr GetPacketData(IntPtr packet);

			// enet_uint32 enet_packet_get_length(ENetPacket *)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_get_length")]
			internal static extern uint GetPacketSize(IntPtr packet);

			// int enet_address_set_host(ENetAddress * address, const char * hostName);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_address_set_hostname")]
			internal static extern int SetHost(ref ENetAddress address, string hostName);
			#endregion
		}
	}
}