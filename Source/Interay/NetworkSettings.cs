using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// Contains settings on with network will operate.
	/// </summary>
	public struct NetworkSettings
	{
		#region Fields
		/// <summary>
		/// The frequencity of <see cref="NetworkManager"/> updates (ticks).
		/// </summary>
		/// <remarks>
		/// If 0 then ticks are as fast as possible! Try to not use that value!
		/// </remarks>
		[EditorOrder(0), Tooltip("The frequencity of NetworkManager updates (ticks). Using 0 not recommended!")]
		public byte TickRate;

		/// <summary>
		/// If true - host will only relay messages and host server, if false - host will be treated like a client and server.
		/// </summary>
		[EditorOrder(10), Tooltip("If true - host will only relay messages and host server, if false - host will be treated like a client and server.")]
		public bool OnlyServer;

		/// <summary>
		/// The maximum size of the network buffer per message.
		/// </summary>
		[EditorOrder(20), Tooltip("The maximum size of the network buffer per message.")]
		public uint MessageMaxSize;

		/// <summary>
		/// The maximum number of players that can connect.
		/// </summary>
		[EditorOrder(30), Tooltip("The maximum number of players that can connect.")]
		public uint MaxConnections;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkSettings"/> struct.
		/// </summary>
		/// <param name="tick">The tick rate.</param>
		/// <param name="onlyServer">If set to true only server.</param>
		/// <param name="messageMaxSize">The maximum size of the message.</param>
		/// <param name="maxConnections">The maximum number of connections.</param>
		public NetworkSettings(byte tick = 16, bool onlyServer = false, uint messageMaxSize = 2048, uint maxConnections = 10)
		{
			this.TickRate = tick;
			this.OnlyServer = onlyServer;
			this.MessageMaxSize = messageMaxSize;
			this.MaxConnections = maxConnections;
		}
	}
}