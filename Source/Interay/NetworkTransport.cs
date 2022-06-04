using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// The <see cref="NetworkTransport"/> class is used to transfer data between players and server.
	/// </summary>
	public abstract class NetworkTransport : System.IDisposable
	{
		private NetworkSettings _settings;

		#region Properties
		/// <summary>
		/// Tells if server/client is running
		/// </summary>
		[NoSerialize]
		public abstract bool IsInitialized { get; }

		/// <summary>
		/// Tells if server/client is running
		/// </summary>
		[NoSerialize]
		public abstract bool IsRunning { get; }

		/// <summary>
		/// Operational settings of transport layer.
		/// </summary>
		[NoSerialize]
		public NetworkSettings Settings 
		{
			get => _settings.Clone;
			set 
			{
				var oldSettings = _settings;
				_settings = value.Clone;
				OnSettingsChanged(ref value, out var result);
				if(result)
					_settings = value;
			}
		}

		internal NetworkLogDelegate LogInfo => NetworkManager.LogInfo;
		internal NetworkLogDelegate LogWarning => NetworkManager.LogWarning;
		internal NetworkLogDelegate LogError => NetworkManager.LogError;
		internal NetworkLogDelegate LogFatal => NetworkManager.LogFatal;
		#endregion

		#region Events
		internal event OnClientDelegate OnClientConnected;
		internal event OnClientDelegate OnClientDisconnected;
		internal event OnMessageReceivedDelegate OnPacketReceived;
		#endregion

		#region Delegates
		internal delegate void OnClientDelegate(ulong clientId);
		internal delegate void OnMessageReceivedDelegate(ulong sender, INetworkPacket message);

		#endregion

		#region Methods
		/// <summary>
		/// Initializes the transport layer.
		/// </summary>
		public abstract bool Initialize();

		/// <summary>
		/// Starts or connects to server.
		/// </summary>
		/// <param name="hostType">The type of host.</param>
		/// <param name="address">Designated IP address of server</param>
		/// <param name="port">Designated port of server</param>
		public abstract bool Start(HostType hostType, string address, ushort port);

		/// <summary>
		/// Stops server/client.
		/// </summary>
		public abstract void Stop();

		/// <summary>
		/// Disconnects clients connected to server. (Server only)
		/// </summary>
		/// <param name="id">ID of client to disconnect</param>
		public abstract void Disconnect(ulong id);

		/// <summary>
		/// Disposes all resources created by transport layer.
		/// </summary>
		public abstract void Dispose();

		/// <summary>
		/// Sends message to all client.
		/// </summary>
		public abstract bool Send(INetworkPacket message);

		/// <summary>
		/// Sends message to specific client.
		/// </summary>
		public abstract bool SendTo(ulong clientId, INetworkPacket message);

		/// <summary>
		/// Preforms transport layer update.
		/// </summary>
		/// <param name="deltaTime">Time since last tick</param>
		public abstract void Tick(float deltaTime);

		/// <summary>
		/// Creates packet specific for this transport type
		/// </summary>
		/// <param name="size">Size of packet</param>
		/// <returns>Newly created packet</returns>
		public abstract INetworkPacket CreatePacket(int size);

		/// <summary>
		/// Called when setting validation is needed.
		/// </summary>
		/// <param name="newSettings">New settings</param>
		/// <param name="changeSettings">Set to true if settings are valid and changes will be accepted</param>
		protected abstract void OnSettingsChanged(ref NetworkSettings newSettings, out bool changeSettings); // I did it this way to not confuse user

		/// <summary>
		/// Invokes event <see cref="OnClientConnected"/>.
		/// </summary>
		protected void InvokeOnClientConnected(ulong clientId) => OnClientConnected?.Invoke(clientId);

		/// <summary>
		/// Invokes event <see cref="OnClientDisconnected"/>.
		/// </summary>
		protected void InvokeOnClientDisconnected(ulong clientId) => OnClientDisconnected?.Invoke(clientId);

		/// <summary>
		/// Invokes event <see cref="OnPacketReceived"/>.
		/// </summary>
		protected void InvokeOnPacketReceived(ulong sender, INetworkPacket message) => OnPacketReceived?.Invoke(sender, message);
		#endregion
	}

	#region Editor
	#if FLAX_EDITOR
	[CustomEditor(typeof(NetworkTransport))]
	internal sealed class NetworkTransportRefEditor : NetworkRefEditor { }

	
	#endif
	#endregion
}