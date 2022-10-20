using System;
using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// The <see cref="NetworkSerializer"/> class is used to serialize data and deserialize packets.
	/// </summary>
	public abstract class NetworkSerializer : System.IDisposable
	{
		#region Properties
		/// <summary>
		/// Tells if server/client is running
		/// </summary>
		[NoSerialize]
		public abstract bool IsInitialized { get; }

		internal NetworkLogDelegate LogInfo => NetworkManager.LogInfo;
		internal NetworkLogDelegate LogWarning => NetworkManager.LogWarning;
		internal NetworkLogDelegate LogError => NetworkManager.LogError;
		internal NetworkLogDelegate LogFatal => NetworkManager.LogFatal;
		#endregion

		#region Methods
		/// <summary>
		/// Initializes serializer.
		/// </summary>
		public abstract bool Initialize();

#if FLAX_EDITOR
		/// <summary>
		/// Initializes editor.
		/// </summary>
		public virtual bool InitializeEditor(FlaxEditor.CustomEditors.LayoutElementsContainer layout) { return true; }

		/// <summary>
		/// Disposes all editor resources created by serializer.
		/// </summary>
		public virtual void DisposeEditor() { }
#endif

		/// <summary>
		/// Serializes data to the packet.
		/// </summary>
		/// <param name="data">Data to serialize</param>
		/// <param name="type">Type of the data</param>
		/// <returns>The packet to serialize data to.</returns>
		public abstract bool Serialize(INetworkPacket packet, Type type, object data);

		/// <summary>
		/// Deserializes data from the packet.
		/// </summary>
		/// <param name="packet">The packet to deserialize data from.</param>
		/// <param name="type">The type of the data to deserialize.</param>
		/// <returns>Deserialized data.</returns>
		public abstract bool Deserialize(INetworkPacket packet, Type type, out object data);

		/// <summary>
		/// Disposes all resources created by serializer.
		/// </summary>
		public abstract void Dispose();
		#endregion
	}

	#region Editor
	#if FLAX_EDITOR
	[CustomEditor(typeof(NetworkSerializer))]
	internal sealed class NetworkSerializerRefEditor : NetworkRefEditor 
	{
		public override void Initialize(FlaxEditor.CustomEditors.LayoutElementsContainer layout)
		{
			base.Initialize(layout);
			TypePicker.Tag = "Serializer";
			if ((string)layout.Control.Parent.Tag == "NetworkManager")
			{
				TypePicker.ValueChanged -= SetInstanceValue;
			}
		}
	}
	#endif
	#endregion
} 