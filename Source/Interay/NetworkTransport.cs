using System.Net;
using FlaxEngine;
#if FLAX_EDITOR
using FlaxEditor.Scripting;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;
using FlaxEditor.CustomEditors.Elements;
#endif

namespace Interay
{
	/// <summary>
	/// The <see cref="NetworkTransport"/> class is used to transfer data between players and server.
	/// </summary>
	public abstract class NetworkTransport
	{
		private NetworkSettings _settings;

		#region Properties
		/// <summary>
		/// Tells if server/client is running
		/// </summary>
		[NoSerialize]
		public bool IsActive { get; private set; }

		/// <summary>
		/// Operational settings of transport layer.
		/// </summary>
		[NoSerialize]
		public NetworkSettings Settings 
		{
			get => _settings;
			set 
			{
				_settings = Settings;
				OnSettingsChanged();
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Starts server.
		/// </summary>
		public abstract void StartServer(ushort port);

		/// <summary>
		/// Starts client.
		/// </summary>
		/// <param name="address">Designated IP address of server</param>
		/// <param name="port">Designated port of server</param>
		public abstract void StartClient(IPAddress address, ushort port);

		/// <summary>
		/// Stops server/client.
		/// </summary>
		public abstract void Stop();

		/// <summary>
		/// Called when settings are changed.
		/// </summary>
		protected abstract void OnSettingsChanged();
		#endregion
	}

	#if FLAX_EDITOR
	/// <summary>
    /// Implementation of the inspector used to edit reference to the <see cref="NetworkTransport"/> inheritors.
    /// </summary>
    [CustomEditor(typeof(NetworkTransport))]
    public sealed class NetworkTransportRefEditor : CustomEditor
    {
        private CustomElement<TypePickerControl> _element;

        /// <inheritdoc />
        public override DisplayStyle Style => DisplayStyle.Inline;

        /// <inheritdoc />
        public override void Initialize(LayoutElementsContainer layout)
        {
            if (!HasDifferentTypes)
            {
                _element = layout.Custom<TypePickerControl>();
                _element.CustomControl.Type = Values.Type.Type != typeof(object) || Values[0] == null ? Values.Type : TypeUtils.GetObjectType(Values[0]);
                _element.CustomControl.ValueChanged += () => SetValue(_element.CustomControl.Value.CreateInstance());
				_element.CustomControl.CheckValid += type => !type.IsAbstract;
            }
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            base.Refresh();

            if (!HasDifferentValues)
            {
                _element.CustomControl.Value = TypeUtils.GetObjectType(Values[0]);
            }
        }
    }
	#endif
}