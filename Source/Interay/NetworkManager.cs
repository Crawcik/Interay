using System;
using System.Collections.Generic;
using System.Net;
using FlaxEngine;
using FlaxEngine.GUI;
#if FLAX_EDITOR
using FlaxEditor;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.GUI;
using FlaxEditor.CustomEditors.Editors;
using FlaxEditor.CustomEditors.Elements;
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
		private NetworkSerializer _serializer;
		private NetworkScript[] _instances;
		private Stack<uint> _emptyInstances;
		private float _tickTimeNow;
		private uint _biggestNetworkID = 0;
		private uint _biggestClientID = 0;
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
				if (!Editor.IsPlayMode)
				{
					_transport = value;
					return;
				}
#endif
				if (IsRunning)
					return;
				if (_transport == value)
					return;
				if (!value.Initialize())
				{
					Debug.LogError("Failed to initialize network transport layer.");
					return;
				}
				_transport?.Dispose();
				_transport = value;
			}
		}

		/// <summary>
		/// Networking serialization method.
		/// </summary>
		[EditorOrder(-975), Tooltip("Networking serialization method.")]
		public NetworkSerializer Serializer
		{
			get => _serializer;
			set 
			{
#if FLAX_EDITOR
				if (!Editor.IsPlayMode)
				{
					_serializer = value;
					return;
				}
#endif
				if (IsRunning)
					return;
				if (_serializer == value)
					return;
				if (!value.Initialize())
				{
					Debug.LogError("Failed to initialize network serializer.");
					return;
				}
				_serializer?.Dispose();
				_serializer = value;
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

		#region Events
		/// <summary>
		/// Occurs when network is started.
		/// </summary>
		public event Action<HostType> StartHostEvent;

		/// <summary>
		/// Occurs when network is stopped.
		/// </summary>
		public event Action StopHostEvent;
		#endregion
		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkManager"/> class.
		/// </summary>
		public NetworkManager()
		{
#if FLAX_EDITOR
			if(!Editor.IsPlayMode)
				return;
#endif
			if (Singleton is null)
			{
				#if FLAX_EDITOR
				Editor.Instance.StateMachine.PlayingState.SceneRestored += Dispose;
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
			if (isServer && Settings.MaxConnections == 0)
			{
				if (isClient)
				{
					LogError("If type is \"host\", then MaxConnections setting must be greater than 0.");
					return false;
				}
				else
				{
					LogWarning("MaxConnections is 0. It means that server will not accept any connections.");
				}
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
			if (!_transport.Start(hostType, address.ToString(), Port))
				return false;
			LogInfo($"Started {(isServer ? isClient ? "host" : "server" : "client")} on {address}:{Port}");
			Scripting.FixedUpdate += FixedUpdate;
			StartHostEvent?.Invoke(hostType);
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
			return true;
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
			
			StopHostEvent?.Invoke();
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
			_transport.OnClientDisconnected -= OnClientDisconnected;
			_transport.Stop();
			IsServer = false;
			IsClient = false;
			_biggestNetworkID = 0;
			_instances = null;
			_emptyInstances = null;
		}

		internal void Send(in NetworkMessage message)
		{
			if (!IsRunning)
				return;

		}

		internal bool RegisterNetworkScript(NetworkScript instance, uint id = 0)
		{
			if (IsServer)
			{
				id = _biggestNetworkID + 1;
				if (_emptyInstances?.Count > 0)
				{
					id = _emptyInstances.Pop();
				}
			}
			if (id >= _instances.Length)
			{
				var maxNetworkScripts = _transport.Settings.MaxNetworkScripts + 1;
				if (id < maxNetworkScripts)
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
			if (IsServer)
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
			if (IsServer)
			{
				if (_instances[id] is null)
					return;
				_instances[id].Dispose();
				_instances[id] = null;
				_emptyInstances.Push(id);
			}
		}

		internal void UnregisterAllNetworkScript()
		{
			if (IsServer)
			{
				foreach(var script in _instances)
				{
					if(script is null)
						continue;
					script.Dispose();
				}
				_emptyInstances.Clear();
				_biggestNetworkID = 0;
			}
		}

		private static void LogFilter(LogType type, object message)
		{
			if(type >= Singleton?.LogLevel)
				Debug.Logger.Log(type, "Network Manager", message);
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
			Editor.Instance.StateMachine.PlayingState.SceneRestored -= Dispose;
			#endif
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
					if(script is null)
						continue;
					try
					{
						script.OnTick();
					}
					catch (Exception exception)
					{
						Debug.LogException(exception, script);
					}
				}
				Profiler.EndEvent();
				_tickTimeNow -= (1f / Settings.TickRate);
			}
			_tickTimeNow += Time.DeltaTime;
		}

		private void OnPacketReceived(ulong sender, INetworkPacket packet)
		{
			
		}

		private void OnClientConnected(ulong clientID)
		{
			for (int i = 0; i <= _biggestNetworkID; i++)
			{
				var script = _instances[i];
				if(script is null)
					continue;
				try
				{
					script.OnClientConnect(clientID);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, script);
				}
			}
		}

		private void OnClientDisconnected(ulong clientID)
		{
			for (int i = 0; i <= _biggestNetworkID; i++)
			{
				var script = _instances[i];
				if(script is null)
					continue;
				try
				{
					script.OnClientDisconnect(clientID);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, script);
				}
			}
		}
		#endregion
	}

	#region Editor
	#if FLAX_EDITOR
	[CustomEditor(typeof(NetworkManager))]
	internal sealed class NetworkManagerEditor : GenericEditor
	{
		private LayoutElementsContainer _mainLayout;
		private CustomElementsContainer<HorizontalPanel> _container;
		private NetworkManager _manager;
		private NetworkSerializer _serializer;	
		private TypePickerControl _serializerTypePicker;
		public override void Initialize(LayoutElementsContainer layout)
		{
			layout.Control.Tag = "NetworkManager";
			base.Initialize(layout);
			layout.Space(20f);

			_mainLayout = layout;
			_manager = (NetworkManager)this.Values[0];
			
			if (!Editor.IsPlayMode)
			{
				if (_serializerTypePicker is object)
				{
					_serializerTypePicker.ValueChanged -= SerializerChanged;
				}
				_serializerTypePicker = (TypePickerControl)layout.ContainerControl.GetChild<PropertiesList>().Children.Find(x => (x.Tag as string) == "Serializer");
				_serializerTypePicker.ValueChanged +=  SerializerChanged;
				SerializerChanged();
				return;
			}
			_manager.StartHostEvent += OnStartHost;
			_manager.StopHostEvent += OnStopHost;

			_container = layout.CustomContainer<HorizontalPanel>();
			_container.Control.Height = 30f;
			_container.Control.AnchorPreset = AnchorPresets.BottomCenter;
			_container.Control.LocalX = 0f;
			_container.CustomControl.Spacing = 10f;

			OnStopHost();
		}

		private void OnStartHost(HostType type)
		{
			_container.CustomControl.DisposeChildren();
			SetButton(_container.Button(type == HostType.Client ? "Disconnect" : "Stop").Button, 0, false);
		}

		private void OnStopHost()
		{
			_container.CustomControl.DisposeChildren();
			SetButton(_container.Button("Host").Button, HostType.Host, true);
			SetButton(_container.Button("Server only").Button, HostType.Server, true);
			SetButton(_container.Button("Connect").Button, HostType.Client, true);
		}

		protected override void Deinitialize()
		{
			try
			{
				if (_serializerTypePicker is object)
					_serializerTypePicker.ValueChanged -= SerializerChanged;
				_serializer?.DisposeEditor();
			}
			catch (System.Exception exception)
			{
				Debug.LogException(exception);
			}

			_serializer = null;
			_serializerTypePicker = null;
			
			if (_manager is object)
			{
				_manager.Serializer.DisposeEditor();
				_manager.StartHostEvent -= OnStartHost;
				_manager.StopHostEvent -= OnStopHost;
			}
			_container?.CustomControl.DisposeChildren();
			base.Deinitialize();
		}

		private void SetButton(Button button, HostType hostType, bool start) {
			button.Width = 70f;
			button.Height = 30f;
			button.Clicked += () => { if (start) _manager.Start(hostType); else _manager.Stop(); };
		}

		private void SerializerChanged()
		{
			if (_serializerTypePicker.Value.Type is null && _serializerTypePicker.Value.IScriptType is null)
				_manager.Serializer = null;
			else
				_manager.Serializer = (NetworkSerializer)_serializerTypePicker.Value.CreateInstance();

			if (Editor.IsPlayMode || _manager.Serializer == _serializer)
				return;
			try
			{
				_serializer?.DisposeEditor();
			}
			catch (System.Exception exception)
			{
				Debug.LogException(exception);
			}
			_serializer = _manager.Serializer;

			var error = false;
			try
			{
				if (_serializer is null)
					return;
				error = !_serializer.InitializeEditor(_mainLayout);
			}
			catch (System.Exception exception)
			{
				Debug.LogException(exception);
			}

			if (error)
				Debug.LogError("Failed to initialize network serializer editor.");
		}
	}
	#endif
	#endregion
}