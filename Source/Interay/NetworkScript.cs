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
		private protected byte _tickTime;
		private readonly Dictionary<int, NetworkScript> _instances;
		private int _networkID = 0;
		private bool _isDisposing = false;
		#endregion

		#region Properties
		/// <summary>
		/// Determines if its spawned by the server and connected.
		/// </summary>
		public bool IsNetworkEntity => _networkID != 0;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkScript"/> class.
		/// </summary>
		public NetworkScript()
		{
#if FLAX_EDITOR
			if(!FlaxEditor.Editor.IsPlayMode)
				return;
#endif
			if(this is NetworkManager)
			{
				_instances = new Dictionary<int, NetworkScript>();
				_isDisposing = this != NetworkManager.Singleton;
			}
			Scripting.Update += Validate;
		}

		#region Methods

		/// <inheritdoc />
		public override void OnAwake()
		{
			
		}

		/// <inheritdoc />
		public override void OnDestroy() 
		{
			NetworkManager.Singleton._instances.Remove(_networkID);
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


		private protected void Dispose()
		{
			_isDisposing = true;
			Scripting.Update += Validate;
		}

		private void Validate()
		{
			Scripting.Update -= Validate;
			if(!_isDisposing)
			{
				if(this == NetworkManager.Singleton)
					NetworkManager.Singleton._instances.Add(0, this);
				else if (_networkID != 0)
					NetworkManager.Singleton._instances.Add(_networkID, this);
				return;	
			}
			if(this is NetworkManager)
					Debug.LogWarning("Multiple instances of \"NetworkManager\" script found! Destroying additional instances.");
			Destroy(this);
		}
		#endregion
	}
}