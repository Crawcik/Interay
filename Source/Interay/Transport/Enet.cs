using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace Interay.Transport
{
	/// <summary>
	/// Enet transport layer.
	/// </summary>
	public sealed class Enet: NetworkTransport
	{
		private const PacketFlags DefaultSendFlags = PacketFlags.NoAllocate | PacketFlags.Unsequenced;

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
				NetworkManager.LogInfo("ENet driver is already initialized!");
				return IsInitialized;
			}
			if (Native.Initialize() == 0)
   			{
				NetworkManager.LogInfo("Failed to initialize ENet driver!");
				return false;
			}
			NetworkManager.LogFatal("Failed to initialize ENet driver!");
			_isInitialized = true;
			return true;
		}

		/// <inheritdoc />
		public override bool Start(HostType hostType, IPAddress address, ushort port)
		{
			if (_isRunning || !_isInitialized)
				return false;
			var nativeAddress = new ENetAddress() { Port = port };
			var isServer = (hostType & HostType.Server) == HostType.Server;
			if (isServer && Native.SetHost(ref nativeAddress, address.ToString()) < 0)
			{
				NetworkManager.LogWarning("Setting host address on transport layer failed!");
				return false;
			}
			_host = isServer ? Native.CreateHost(ref nativeAddress, (int)Settings.MaxConnections, 1, 0, 0) : Native.CreateHost(IntPtr.Zero, 1, 1, 0, 0);
			if (_host == IntPtr.Zero)
				return false;
			if(!isServer)
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
				NetworkManager.LogInfo($"Disconnected client with id {id} from transport.");
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
					NetworkManager.LogWarning("ENet fetching network events failed!");
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
						_peers.Add(peerId, networkEvent.Peer);
						InvokeOnClientConnected(peerId);
						NetworkManager.LogInfo($"Client connected! Peer ID: {peerId}");
						break;
					case EventType.Disconnect:
					case EventType.Timeout:
						if (_clientPeer != IntPtr.Zero)
							break;
						NetworkManager.LogInfo($"Client disconnected! Peer ID: {peerId}");
						_peers.Remove(peerId);
						InvokeOnClientDisconnected(peerId);
						break;
					case EventType.Receive:
						using (var packet = new NetworkPacket(Native.GetPacketData(networkEvent.Packet), (int)Native.GetPacketLength(networkEvent.Packet)))
						{
							try
							{
								InvokeOnPacketReceived(peerId, packet);
							} catch { /* Just in case */ }
						}
						Native.DestroyPacket(networkEvent.Packet);
						break;
				}
			}
		}

		/// <inheritdoc />
		public override bool Send(NetworkPacket packet)
		{
			if(!_isRunning)
				return false;
			var error = false;
			var enetPacket = Native.CreatePacket(packet.GetPointer(), packet.Size, DefaultSendFlags);
			if(enetPacket == IntPtr.Zero)
				return false;
			if(_clientPeer == IntPtr.Zero)
			{
				Native.Broadcast(_host, 1, enetPacket);
			}
			else
			{
				error = Native.SendPeer(_clientPeer, 0, packet.GetPointer()) < 0;
			}
			Native.DestroyPacket(enetPacket);
			return !error;
		}

		/// <inheritdoc />
		public override bool SendTo(ulong id, NetworkPacket packet)
		{
			if(!_isRunning || _clientPeer != IntPtr.Zero)
				return false;
			if(!_peers.ContainsKey((uint)id))
			{
				NetworkManager.LogWarning($"Client with id {id} is not connected!");
				return false;
			}
			var enetPacket = Native.CreatePacket(packet.GetPointer(), packet.Size, DefaultSendFlags);
			if(enetPacket == IntPtr.Zero)
				return false;
			var error = Native.SendPeer(_peers[(uint)id], 1, enetPacket) < 0;
			Native.DestroyPacket(enetPacket);
			return !error;
		}

		/// <inheritdoc />
		protected override void OnSettingsChanged(ref NetworkSettings newSettings, out bool valid)
		{
			valid = true;
			if (_isRunning)
			{
				if(newSettings.MaxConnections != Settings.MaxConnections 
				|| newSettings.MessageMaxSize != Settings.MessageMaxSize)
				{
					NetworkManager.LogWarning("In ENet, only \"TickRate\" and \"MaxNetworkScripts\" can be changed!");
					valid = false;
				}
				return;
			}
			newSettings.MaxConnections = Math.Min(newSettings.MaxConnections, Native.MaximumPeers);
		}

		#endregion

		#region Native
		private enum PacketFlags {
			None = 0,
			Reliable = 1 << 0,
			Unsequenced = 1 << 1,
			NoAllocate = 1 << 2,
			UnreliableFragmented = 1 << 3,
			Instant = 1 << 4,
			Unthrottled = 1 << 5,
			Sent =  1 << 8
		}

		private enum EventType {
			None = 0,
			Connect = 1,
			Disconnect = 2,
			Receive = 3,
			Timeout = 4
		}

		[StructLayout(LayoutKind.Explicit, Size = 18)]
		private struct ENetAddress {
			[FieldOffset(16)]
			public ushort Port;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct ENetEvent {
			public EventType Type;
			public IntPtr Peer;
			public byte ChannelID;
			public uint Data;
			public IntPtr Packet;
		}

		private static class Native
		{
			public const int MaximumPeers = 0xFFF;
			public const int MaximumPacketSize  = 32 * 1024 * 1024;
			private const string DllName = "__Internal";
			private const CallingConvention Convention = CallingConvention.Cdecl;

			// int enet_initialize(void);

			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_initialize")]
			internal static extern int Initialize();

			// void enet_deinitialize(void);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_deinitialize")]
			internal static extern void Deinitialize();

			// ENetHost * enet_host_create(const ENetAddress *, size_t, size_t, enet_uint32, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_create")]
			internal static extern IntPtr CreateHost(ref ENetAddress address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth);

			// ENetHost * enet_host_create(const ENetAddress *, size_t, size_t, enet_uint32, enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_create")]
			internal static extern IntPtr CreateHost(IntPtr address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth);
			
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
			internal static extern int SendPeer(IntPtr peer, byte channelID, IntPtr packet);

			// void enet_host_broadcast(ENetHost *host, enet_uint8 channelID, ENetPacket *packet)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_host_broadcast")]
			internal static extern void Broadcast(IntPtr host, byte channelID, IntPtr packet);

			// uint enet_peer_get_id(ENetPeer *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_peer_get_id")]
			internal static extern uint GetPeerId(IntPtr peer);

			// ENetPacket* enet_packet_create(const void*,size_t,enet_uint32);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_create")]
			internal static extern IntPtr CreatePacket(IntPtr data, int dataLength, PacketFlags flags);

			// void enet_packet_destroy(ENetPacket *);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_destroy")]
			internal static extern void DestroyPacket(IntPtr packet);

			// void* enet_packet_get_data(ENetPacket *)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_get_data")]
			internal static extern IntPtr GetPacketData(IntPtr packet);

			// enet_uint32 enet_packet_get_length(ENetPacket *)
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_packet_get_length")]
			internal static extern uint GetPacketLength(IntPtr packet);

			// int enet_address_set_host(ENetAddress * address, const char * hostName);
			[DllImport(DllName, CallingConvention = Convention, EntryPoint = "enet_address_set_host")]
			internal static extern int SetHost(ref ENetAddress address, string hostName);


		}
		#endregion
	}
}