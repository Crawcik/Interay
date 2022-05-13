using System.Collections.Generic;
using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// This script gives ability to transfer and receive data from other players (when spawned from host or network utilities).
	/// </summary>
	public class NetworkScript : Script
	{
		#region Fields
		private ushort _networkID = 0;
		private bool _disposed = false;
		#endregion

		#region Properties
		/// <summary>
		/// Determines if its spawned by the server and connected.
		/// </summary>
		public bool IsNetworkConnected => _networkID != 0;

		/// <summary>
		/// The network ID of this instance.
		/// </summary>
		protected internal ushort NetworkID
		{
			get => _networkID;
			set => _networkID = value;
		}
		#endregion

		#region Methods
		/// <inheritdoc />
		public override void OnDestroy()
		{
			if(!_disposed && _networkID != 0)
			{
				NetworkManager.Singleton.UnregisterNetworkScript(_networkID);
				_disposed = true;
			}
		}
		/// <summary>
		/// Called when hosts starts the server.
		/// </summary>
		public virtual void OnStartHost() { }

		/// <summary>
		/// Called when hosts stops the server.
		/// </summary>
		public virtual void OnStopHost() { }

		/// <summary>
		/// Called when server update is preformed.
		/// </summary>
		public virtual void OnTick() { }

		/// <summary>
		/// Called when client connects to the server.
		/// </summary>
		public virtual void OnClientConnect(int connection) { }

		/// <summary>
		/// Called when client disconnects from the server.
		/// </summary>
		public virtual void OnClientDisconnect(int connection) { }

		/// <summary>
		/// Destroys this instance.
		/// </summary>
		protected internal virtual void Dispose()
		{
			if(!_disposed)
				Destroy(this);
		}
		#endregion
	}
}