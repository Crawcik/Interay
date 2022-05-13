using System;
using System.Collections.Generic;
using System.Net;
using FlaxEngine;
#if FLAX_EDITOR
using FlaxEditor.CustomEditors;
#endif

namespace Interay
{
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
				if(_transport?.IsRunning ?? false)
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

		#region Delegates
		internal delegate void NetworkLogDelegate(object message);
		internal static NetworkLogDelegate LogInfo;
		internal static NetworkLogDelegate LogWarning;
		internal static NetworkLogDelegate LogError;
		internal static NetworkLogDelegate LogFatal;
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
				Singleton = this;
				LogInfo = message => Singleton.LogFilter(LogType.Info, message);
				LogWarning = message => Singleton.LogFilter(LogType.Warning, message);
				LogError = message => Singleton.LogFilter(LogType.Error, message);
				LogFatal = message => Singleton.LogFilter(LogType.Fatal, message);
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

			if (Transport is null)
			{
				LogError("No transport layer is set.");
				return false;
			}
			if(isServer && !IPAddress.TryParse(Hostname, out address))
			{
				LogError("Invalid hostname.");
				return false;
			}
			IsServer = isServer;
			IsClient = isClient;
			Transport.Settings = Settings;
			_instances = new NetworkScript[Math.Min(InitialArrayCapacity, Settings.MaxNetworkScripts)];
			_tickTimeNow = 0;
			return Transport.Start(hostType, address, Port);
		}

		/// <summary>
		/// Starts server.
		/// </summary>
		/// <returns>True if server started successfully.</returns>
		public bool StartServer() => Start( HostType.Server);

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

		internal bool RegisterNetworkScript(NetworkScript instance, uint id = 0)
		{
			if (IsServer)
				id = _biggestNetworkID + 1;
			if (id >= _instances.Length)
			{
				var maxNetworkScripts = _transport.Settings.MaxNetworkScripts;
				if (_emptyInstances?.Count > 0)
				{
					id = _emptyInstances.Pop();
				}
				else if (id < maxNetworkScripts)
				{
					NetworkScript[] newItems = new NetworkScript[Math.Min(_instances.Length + InitialArrayCapacity, maxNetworkScripts)];
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
				_biggestNetworkID++;
				if(_instances[id] is object)
				{
					LogError("Network script with id " + id + " already exists.");
					return false;
				}
			}
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

		private void LogFilter(LogType type, object message)
		{
			if(type >= LogLevel)
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
				foreach (NetworkScript script in _instances)
					script.OnTick();
				Profiler.EndEvent();
				_tickTimeNow -= (1f / Settings.TickRate);
			}
			_tickTimeNow += Time.DeltaTime;
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