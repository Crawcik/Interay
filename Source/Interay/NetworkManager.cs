using System;
using System.Collections.Generic;
using System.Net;
using FlaxEngine;
#if FLAX_EDITOR
using FlaxEditor.CustomEditors;
#endif

namespace Interay
{
	internal delegate void NetworkLogDelegate(object message);

	/// <summary>
	/// The <see cref="NetworkManager"/> manages network aspect, such as connecting, managing network data, etc.
	/// </summary>
	public class NetworkManager : NetworkScript
	{
		private const int InitialArrayCapacity = 50;

		#region Fields
		/// <summary>
		/// The network designated hostname.
		/// </summary>
		[Header("Network")]
		[EditorOrder(-1000), Tooltip("The network designated hostname.")] 
		public string Hostname = "localhost";

		/// <summary>
		/// The network designated port.
		/// </summary>
		[EditorOrder(-1000), Tooltip("The network designated port.")] 
		public ushort Port = 7777;

		/// <summary>
		/// Determines how much details will be logged.
		/// </summary>
		[EditorOrder(-990), Tooltip("Determines how much details will be logged.")]
		public LogType LogLevel = LogType.Error;

		internal static readonly NetworkLogDelegate LogInfo = (message) => LogFilter(LogType.Info, message);
		internal static readonly NetworkLogDelegate LogWarning = (message) => LogFilter(LogType.Warning, message);
		internal static readonly NetworkLogDelegate LogError = (message) => LogFilter(LogType.Error, message);
		internal static readonly NetworkLogDelegate LogFatal = (message) => LogFilter(LogType.Fatal, message);
		private NetworkSettings _settings = NetworkSettings.Default;
		private NetworkTransport _transport;
		private NetworkScript[] _instances;
		private Stack<uint> _emptyInstances;
		private float _tickTimeNow;
		private uint _biggestNetworkID = 0;
		#endregion

		#region Properties
		/// <summary>
		/// The main instance of <see cref="NetworkManager"/>.
		/// </summary>
		public static NetworkManager Singleton { get; private set; }

		/// <summary>
		/// Implementation of network transport layer.
		/// </summary>
		[EditorOrder(-980), Tooltip("Implementation of network transport layer.")]
		public NetworkTransport Transport 
		{
			get => _transport;
			set 
			{
#if FLAX_EDITOR
				if (!FlaxEditor.Editor.IsPlayMode)
				{
					_transport = value;
					return;
				}
#endif
				
				if(_transport?.IsRunning ?? false)
					return;
				if (_transport == value)
					return;
				if(!value.Initialize())
				{
					Debug.LogError("Failed to initialize network transport layer.");
					return;
				}
				_transport?.Dispose();
				_transport = value;
			}
		}

		/// <summary>
		/// Settings on with network will be working. They cant be changed while network is running.
		/// </summary>
		[EditorOrder(-970), ShowInEditor, Tooltip("Settings on with network will be working. They cant be changed while network is running.")]
		public NetworkSettings Settings
		{
			get => IsRunning ? Transport.Settings : _settings;
			set => _settings = value;
		}

		/// <summary>
		/// Tells if server/client is running
		/// </summary>
		public bool IsRunning => _transport?.IsRunning ?? false;

		/// <summary>
		/// Tells if is running as server.
		/// </summary>
		public bool IsServer { get; private set; }

		/// <summary>
		/// Tells if is running as client.
		/// </summary>
		public bool IsClient { get; private set; }
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkManager"/> class.
		/// </summary>
		public NetworkManager()
		{
#if FLAX_EDITOR
			if(!FlaxEditor.Editor.IsPlayMode)
				return;
#endif
			if (Singleton is null)
			{
				#if FLAX_EDITOR
				FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored += Dispose;
				#endif
				Singleton = this;
				return;
			}
			LogWarning("Multiple instances of \"NetworkManager\" script found! Destroying additional instances.");
			Destroy(this);
		}

		#region Methods
		/// <summary>
		/// Starts hosting or connects to server.
		/// </summary>
		/// <returns>True if started successfully.</returns>
		public bool Start(HostType hostType)
		{
			var address = IPAddress.Any;
			var isServer = (hostType & HostType.Server) == HostType.Server;
			var isClient = (hostType & HostType.Client) == HostType.Client;

			if (_transport is null)
			{
				LogError("No transport layer is set.");
				return false;
			}
			if (!_transport.IsInitialized)
			{
				LogError("Transport layer is not initialized or is disposed.");
				return false;
			}
			if (!isServer)
			{
				if (Hostname.Contains("localhost"))
					address = IPAddress.Loopback;
				else if (!IPAddress.TryParse(Hostname, out address))
				{
					LogError("Invalid hostname.");
					return false;
				}
			}
			IsServer = isServer;
			IsClient = isClient;
			_transport.Settings = Settings;
			_transport.OnPacketReceived += OnPacketReceived;
			_transport.OnClientConnected += OnClientConnected;
			_instances = new NetworkScript[Math.Min(InitialArrayCapacity, Settings.MaxNetworkScripts + 1)];
			_emptyInstances = new Stack<uint>();
			_instances[0] = this;
			_tickTimeNow = 0;
			_biggestNetworkID = 0;
			var ret = _transport.Start(hostType, address.ToString(), Port);
			if (ret)
				Scripting.FixedUpdate += FixedUpdate;
			for (int i = 0; i <= _biggestNetworkID; i++)
			{
				var script = _instances[i];
				if(script is null)
					continue;
				try
				{
					script.OnStartHost();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, script);
				}
			}
			return ret;
		}

		/// <summary>
		/// Starts server.
		/// </summary>
		/// <returns>True if server started successfully.</returns>
		public bool StartServer() => Start(HostType.Server);

		/// <summary>
		/// Starts server.
		/// </summary>
		/// <returns>True if client connected successfully.</returns>
		public bool StartClient() => Start(HostType.Client);

		/// <summary>
		/// Starts host (server and client).
		/// </summary>
		/// <returns>True if host started successfully.</returns>
		public bool StartHost() => Start(HostType.Host);

		/// <summary>
		/// Stops network.
		/// </summary>
		public void Stop() 
		{
			if (!IsRunning)
				return;
				
			for (int i = 0; i <= _biggestNetworkID; i++)
			{
				var script = _instances[i];
				if(script is null)
					continue;
				try
				{
					script.OnStopHost();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, script);
				}
			}
			_transport.OnPacketReceived -= OnPacketReceived;
			_transport.OnClientConnected -= OnClientConnected;
			_transport.Stop();
			IsServer = false;
			IsClient = false;
			_biggestNetworkID = 0;
			_instances = null;
			_emptyInstances = null;
		}

		internal bool RegisterNetworkScript(NetworkScript instance, uint id = 0)
		{
			if (IsServer)
				id = _biggestNetworkID + 1;
			if (id >= _instances.Length)
			{
				var maxNetworkScripts = _transport.Settings.MaxNetworkScripts + 1;
				if (_emptyInstances?.Count > 0)
				{
					id = _emptyInstances.Pop();
				}
				else if (id < maxNetworkScripts)
				{
					var newItems = new NetworkScript[Math.Min(_instances.Length + InitialArrayCapacity, maxNetworkScripts)];
					Array.Copy(_instances, 0, newItems, 0, _instances.Length);
					_instances = newItems;
				}
				else
				{
					LogError("No more space for new network script.");
					return false;
				}
			}
			if(IsServer)
			{
				if(_instances[id] is object)
				{
					LogError("Network script with id " + id + " already exists.");
					return false;
				}
				_biggestNetworkID++;
			}
			else if(IsClient && !IsServer && _biggestNetworkID < id)
				_biggestNetworkID = id;
			_instances[id] = instance;
			return true;
		}

		internal void UnregisterNetworkScript(uint id)
		{
			if(IsServer)
			{
				_instances[id].Dispose();
				_instances[id] = null;
			}
		}

		private static void LogFilter(LogType type, object message)
		{
			if(type >= Singleton?.LogLevel)
				Debug.Logger.Log(type, "Network Manager", message);
		}
		#endregion

		#region Event handlers
		private void FixedUpdate()
		{
			if (!IsRunning)
				Scripting.FixedUpdate -= FixedUpdate;
			if (Settings.TickRate == 0 || _tickTimeNow >= 1f / Settings.TickRate)
			{
				Profiler.BeginEvent("Network.Tick");
				this.Transport.Tick(this._tickTimeNow);
				for (int i = 0; i <= _biggestNetworkID; i++)
				{
					var script = _instances[i];
					if(script is object)
						script.OnTick();
				}
				Profiler.EndEvent();
				_tickTimeNow -= (1f / Settings.TickRate);
			}
			_tickTimeNow += Time.DeltaTime;
		}

		private void OnPacketReceived(ulong sender, NetworkPacket packet)
		{
			Debug.Log(packet.ReadByte()); // Packet type
			Debug.Log(packet.ReadByte()); // Packet type
		}

		private void OnClientConnected(ulong clientID)
		{
			if (IsServer)
			{
				var bytes = new byte[] { 64, 128 };
				using (var packet = new NetworkPacket(ref bytes))
				{
					this._transport.SendTo(clientID, packet);
				}
			}
		}

		/// <inheritdoc />
		protected internal override void Dispose()
		{
			Singleton = null;
			if (IsRunning)
				Stop();
			if (_transport?.IsInitialized ?? false)
				_transport.Dispose();
			#if FLAX_EDITOR
			FlaxEditor.Editor.Instance.StateMachine.PlayingState.SceneRestored -= Dispose;
			#endif
		}
		#endregion
	}

	// TODO: NetorkManagerEditor button: Host/Connect/Server only

	/// <summary>
	/// Host type of the network
	/// </summary>
	public enum HostType : byte
	{
		/// Server.
		Server = 0b01,
		/// Client.
		Client = 0b10,
		/// Server and Client.
		Host = 0b11
	}
}