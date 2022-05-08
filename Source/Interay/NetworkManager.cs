using System.Collections.Generic;
using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// The <see cref="NetworkManager"/> manages network aspect, such as connecting, managing network data, etc.
	/// </summary>
	public class NetworkManager : NetworkScript
	{
		#region Fields
		/// <summary>
		/// The network designated hostname.
		/// </summary>
		[Header("Network")]
        [EditorOrder(-1000), Tooltip("The network designated hostname.")] 
		public string Hostname = "localhost:7777";

		/// <summary>
		/// Determines how much details will be logged.
		/// </summary>
        [EditorOrder(-990), Tooltip("Determines how much details will be logged.")]
		public LogType LogLevel = LogType.Error;

		/// <summary>
		/// Implementation of network transport layer.
		/// </summary>
		[EditorOrder(-980), Tooltip("Implementation of network transport layer.")]
		public NetworkTransport Transport;

		private NetworkSettings _settings = new NetworkSettings(14, false, 4096, 20);
		#endregion

		#region Properties
		/// <summary>
		/// The main instance of <see cref="NetworkManager"/>.
		/// </summary>
		public static NetworkManager Singleton { get; private set; }

		/// <summary>
		/// Settings on with network will be working. They cant be changed while network is running.
		/// </summary>
		[EditorOrder(-970), ShowInEditor, Tooltip("Settings on with network will be working. They cant be changed while network is running.")]
        public NetworkSettings Settings
        {
            get => Transport?.IsActive ?? false ? Transport.Settings : _settings;
            set => _settings = value;
        }
		#endregion


		/// <summary>
		/// Initializes a new instance of the <see cref="NetworkManager"/> class.
		/// </summary>
		public NetworkManager() : base()
		{
#if FLAX_EDITOR
			if(!FlaxEditor.Editor.IsPlayMode)
				return;
#endif
			if (Singleton is null)
				Singleton = this;
		}
	}
}