#if FLAX_EDITOR
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;
using FlaxEditor.Scripting;

namespace Interay
{
	internal abstract class NetworkRefEditor : CustomEditor
	{
		public TypePickerControl TypePicker { get; private set;}

		/// <inheritdoc />
		public override DisplayStyle Style => DisplayStyle.Inline;

		/// <inheritdoc />
		public override void Initialize(LayoutElementsContainer layout)
		{
			if (!HasDifferentTypes)
			{
				var element = layout.Custom<TypePickerControl>().CustomControl;
				TypePicker = element;
				element.Type = Values.Type.Type != typeof(object) || Values[0] == null ? Values.Type : TypeUtils.GetObjectType(Values[0]);
				element.ValueChanged += SetInstanceValue;
				element.CheckValid += type => !type.IsAbstract;
			}
		}

		/// <inheritdoc />
		public override void Refresh()
		{
			base.Refresh();

			if (!HasDifferentValues)
			{
				TypePicker.Value = TypeUtils.GetObjectType(Values[0]);
			}
		}

		public void SetInstanceValue() =>  SetValue(TypePicker.Value.CreateInstance());

		protected override void Deinitialize()
		{
			TypePicker?.Dispose();
			TypePicker = null;
		}
	}
}
#endif