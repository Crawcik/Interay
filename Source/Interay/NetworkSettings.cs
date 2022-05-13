using System;
using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// Contains settings on with network will operate.
	/// </summary>
	public sealed class NetworkSettings
	{
		#region Constants
		/// <summary>
		/// Limit of max scripts that can be set.
		/// </summary>
		public const uint InternalMaxScripts = 50000;

		/// <summary>
		/// Minimal package size that can be set.
		/// </summary>
		public const ushort InternalMessageMinSize = 16;
		#endregion

		#region Fields
		private byte _tickRate;
		private ushort _messageMaxSize;
		private uint _maxNetworkScripts;
		#endregion

		#region Properties
		/// <summary>
		/// Default network settings.
		/// </summary>
		public static NetworkSettings Default => new NetworkSettings(false);

		/// <summary>
		/// Tells if transport layer should use multithreading.
		/// </summary>
		public bool MultiThreading { get; set; }

		/// <summary>
		/// The frequencity of <see cref="NetworkManager"/> updates (ticks). Going above 64 is not adviced.
		/// </summary>
		[EditorOrder(10), Tooltip("The frequencity of NetworkManager updates (ticks).")]
		public byte TickRate { get => _tickRate; set => this._tickRate = Math.Max((byte)1, value); }

		/// <summary>
		/// The maximum size of the network buffer per message (in bytes).
		/// 16 - 65535 (64KB).
		/// </summary>
		[EditorOrder(20), Tooltip("The maximum size of the network buffer per message."), Range(InternalMessageMinSize, short.MaxValue)]
		public ushort MessageMaxSize { get => _messageMaxSize; set => this._messageMaxSize = Math.Max(InternalMessageMinSize, value); }

		/// <summary>
		/// The maximum number of players that can connect.
		/// </summary>
		[EditorOrder(30), Tooltip("The maximum number of players that can connect.")]
		public uint MaxConnections { get; set; }
		/// <summary>
		/// The maximum number of network scripts.
		/// 0 - 50000.
		/// </summary>
		[EditorOrder(40), Tooltip("The maximum number of network scripts."), Range(0, InternalMaxScripts)]
		public uint MaxNetworkScripts { get => _maxNetworkScripts; set => this._maxNetworkScripts = Math.Min(InternalMaxScripts, value); }

		internal NetworkSettings Clone => (NetworkSettings)this.MemberwiseClone();
		#endregion

		#region Constructors
		private NetworkSettings() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkSettings"/> struct.
		/// </summary>
		/// <param name="multithreading">If set to <c>true</c> multi threading will be used.</param>
		/// <param name="tick">The tick rate.</param>
		/// <param name="onlyServer">If set to <c>true</c> only server.</param>
		/// <param name="messageMaxSize">The maximum size of the message.</param>
		/// <param name="maxConnections">The maximum number of connections.</param>
		/// <param name="maxNetworkScripts">The maximum number of network scripts.</param>
		public NetworkSettings(bool multithreading = false, byte tick = 16, bool onlyServer = false, ushort messageMaxSize = 2048, uint maxConnections = 10, uint maxNetworkScripts = 500)
		{
			this.MultiThreading = multithreading;
			this.TickRate = Math.Max((byte)1, tick);
			this.MessageMaxSize = Math.Max(InternalMessageMinSize, messageMaxSize);
			this.MaxConnections = maxConnections;
			this.MaxNetworkScripts = Math.Min(InternalMaxScripts, maxNetworkScripts);
		}
		#endregion
	}
}