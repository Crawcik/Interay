using System;
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
		private uint _networkID = 0;
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
		protected internal uint NetworkID
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
		public virtual void OnClientConnect(ulong connection) { }

		/// <summary>
		/// Called when client disconnects from the server.
		/// </summary>
		public virtual void OnClientDisconnect(ulong connection) { }

		public void Send(Action function)
		{
			CheckTarget(function.Target);
			NetworkManager.Singleton.Send(new NetworkMessage(function.Target as NetworkScript, function.Method, 1));
		}

		public void Send(Action<ulong> function)
		{
			CheckTarget(function.Target);
			NetworkManager.Singleton.Send(new NetworkMessage(function.Target as NetworkScript, function.Method, 5));
		}

		public void Send<T>(Action<T> function, T data)
		{
			CheckTarget(function.Target);
			if (data == null)
				throw new ArgumentNullException("Sended data cannot be null" ,"data");
			NetworkManager.Singleton.Send(new NetworkMessage(function.Target as NetworkScript, function.Method, 3)
			{
				DataType = typeof(T),
				Data = data
			});
		}

		public void Send<T>(Action<ulong, T> function, T data)
		{
			CheckTarget(function.Target);
			if (data == null)
				throw new ArgumentNullException("Sended data cannot be null" ,"data");
			NetworkManager.Singleton.Send(new NetworkMessage(function.Target as NetworkScript, function.Method, 7)
			{
				DataType = typeof(T),
				Data = data
			});
		}

		/// <summary>
		/// Destroys this instance.
		/// </summary>
		protected internal virtual void Dispose()
		{
			if(!_disposed)
				Destroy(this);
		}

		private void CheckTarget(object target)
		{
			if (_networkID == 0)
				if (!(this is NetworkManager))
					throw new InvalidOperationException("This instance is not registered by the networking");
			if (target != this)
				throw new ArgumentException("Function that is outside of this instance can't be called.", "function");
		}
		#endregion
	}
}