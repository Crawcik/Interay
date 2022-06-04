#if FLAX_EDITOR
using FlaxEngine.GUI;
using FlaxEditor.CustomEditors;

namespace Interay.Serializer
{
	public sealed partial class Internal
	{
		private Button _button;
		private bool _initializedEditor;
		
		public override bool InitializeEditor(LayoutElementsContainer layout)
		{
			if (_initializedEditor)
				return true;
			_button = layout.Button("Serializer Entity Creator").Button;
			 _initializedEditor = true;
			 return true;
		}

		public override void DisposeEditor()
		{
			_button?.Dispose();
			_button = null;
			_initializedEditor = false;
		}
	}
}
#endif