#if FLAX_EDITOR
using FlaxEngine;
using FlaxEditor;
using FlaxEngine.GUI;
using FlaxEditor.GUI.Tabs;
using FlaxEditor.CustomEditors;
using System.Collections.Generic;
using System.Linq;
using System;
using FlaxEditor.GUI;

namespace Interay.Serializer
{
	public sealed partial class Internal
	{
		#region Fields
		private Button _button;
		private InternalSerializerWindow _window;
		private bool _initializedEditor;
		#endregion
		
		#region Methods
		public override bool InitializeEditor(LayoutElementsContainer layout)
		{
			if (_initializedEditor)
				return true;
			_button = layout.Button("Serializer Entity Creator").Button;
			_button.Clicked += ToggleWindow;
			_initializedEditor = true;
			return true;
		}

		public override void DisposeEditor()
		{
			_window?.Window.Close();
			_window = null;
			_button?.Dispose();
			_button = null;
			_initializedEditor = false;
		}

		private void ToggleWindow()
		{
			if(_window is null)
			{
				_window = new InternalSerializerWindow();
				_window.Show();
				return;
			}
			_window.Window.Close();
			_window = null;
		}
		#endregion
	}

	internal class InternalSerializerWindow : CustomEditorWindow
	{
		public const string CycleError = "Entity member '{0} field' of type '{1}' causes a cycle in the entity layout";
		private readonly Dictionary<string, EntityEntry> _ownTypes = new Dictionary<string, EntityEntry>();

		private SerializeTypes _types = new SerializeTypes();

		private Tabs _tabs;

		public override void Initialize(LayoutElementsContainer layout)
		{
			var buttons = new KeyValuePair<string, Action>[] {
				new KeyValuePair<string, Action>("Add", OnAdd),
				new KeyValuePair<string, Action>("Remove", OnRemove),
				new KeyValuePair<string, Action>("Save All", OnSaveAll),
			};

			var panel = new VerticalPanel {
				IsScrollable = false,
				AnchorPreset = AnchorPresets.StretchAll,
				AutoSize = false,
                Parent = Window
			};
			var buttonPanel = new HorizontalPanel()
			{
				Height = 20f,
				Spacing = 3f,
				IsScrollable = false,
				AnchorPreset = AnchorPresets.HorizontalStretchTop,
				AutoSize = false,
				Margin = Margin.Zero,
				Offset = Vector2.Zero,
				Parent = panel
			};
			
			foreach (var item in buttons)
			{
				var button = buttonPanel.AddChild<Button>();
				button.Text = item.Key;
				button.Clicked += item.Value;
			}

			_tabs = new Tabs
            {
                Orientation = Orientation.Vertical,
                AnchorPreset = AnchorPresets.StretchAll,
                Offsets = new Margin(0f, 0f, buttonPanel.Height, 0f),
                TabsSize = new Vector2(100, 20),
                Parent = panel
            };
		}

		protected override void Deinitialize()
		{
			_tabs?.DisposeChildren();
			_tabs?.Dispose();
		}

		private void OnAdd()
		{
			_tabs.AddTab(new EntityEntry(_types, "OnConn"));
			_tabs.AddTab(new EntityEntry(_types, "OnDisconn"));
		}

		private void OnRemove()
		{

		}

		private void OnSaveAll()
		{

		}

		private void OnRestoreAll()
		{

		}

		private class EntityEntry : Tab
		{
			private readonly SerializeTypes _types;
			private readonly List<Line> _lines;
			private VerticalPanel _container;
			private TextBox _name;
			

			public EntityEntry(SerializeTypes types, string name) : base(name)
			{
				_lines = new List<Line>();
				_types = types;
				types.Game.Add(name);
				_container = new VerticalPanel
				{
					Parent = this,
					AutoSize = true,
					Width = 500f,
					LeftMargin = 50f
				};
				var line = _container.AddChild<HorizontalPanel>();
				var spacer = _container.AddChild<Spacer>();
				_name  = line.AddChild<TextBox>();
				var button = line.AddChild<Button>();

				_name.Font.Size = 12;
				_name.Width = 120f;
				_name.Text = name;
				_name.TextBoxEditEnd += textbox => this.Text = textbox.Text;

				button.Text = "X";
				button.Width = 22f;
				button.Clicked += () => {
					_lines.Clear();
					DisposeChildren();
					Dispose();
				};

				line.AutoSize = false;
				line.Height = 26f;
				line.Spacing = 4;
				spacer.Height = 20f;
				AddNewLine();
			}

			private void AddNewLine()
			{
				var line = new Line(_container.AddChild<HorizontalPanel>(), _types);			
				_lines.Add(line);
			}
		}

		private class Line
		{
			public HorizontalPanel Container;
			private TextBox _name;
			private ComboBox _combo1, _combo2;
			private Label _checkboxLabel;
			private CheckBox _checkbox;
			private SerializeTypes _types;

			public bool Done => _checkbox.Visible;

			public string Name => _combo2.SelectedItem;

			public string Category => _combo1.SelectedItem;

			public string Type => _combo2.SelectedItem;

			public bool IsArray => _checkbox.Checked;

			public Line(HorizontalPanel line, SerializeTypes types)
			{
				Container = line;
				line.CullChildren = false;
				_types = types;
				_name  = line.AddChild<TextBox>();
				_combo1 = line.AddChild<ComboBox>();

				_name.Font.Size = 9;
				_name.Width = 120f;

				_combo1.Items = _types.Names;
				_combo1.SelectedIndexChanged += PrimaryDropdownChange;

				line.Height = 22f;
			}

			private void PrimaryDropdownChange(ComboBox obj)
			{
				if (_combo2 is null)
				{
					_combo2 = Container.AddChild<ComboBox>();
					_combo2.SelectedIndexChanged += SecondaryDropdownChange;
				}
				_combo2.SelectedIndex = -1;
				_combo2.Items = GetCombo2(obj.SelectedItem);
			}

			private void SecondaryDropdownChange(ComboBox obj)
			{
				var select = obj.SelectedIndex != -1;
				if (_checkbox is null)
				{
					_checkboxLabel = Container.AddChild<Label>();
					_checkboxLabel.HorizontalAlignment = TextAlignment.Near;
					_checkboxLabel.Text = "Array: ";
					_checkbox = new CheckBox();
					_checkbox.Parent = _checkboxLabel;
					_checkbox.Offsets = new Margin(20f, 0f, 0f, 0f);
					_checkbox.Checked = false;
				}
				_checkboxLabel.Visible = select;
				_checkbox.Visible = select;
				_name.Enabled = select;
				if (!select)
					_name.Text = "";
			}

			private List<string> GetCombo2(string key)
			{
				switch (key)
				{
					case "General":
						return _types.General;
					case "Flax":
						return _types.General;
					case "Game":
						return _types.General;
				}
				return null;
			}
		}

		private class SerializeTypes
		{
			public readonly List<string> Names = new List<string>()
			{
				"General",
				"Flax",
				"Game"
			};

			public readonly List<string> General = new List<string>()
			{
				"Bool", 
				"Byte", "SByte",
				"Short", "UShort",
				"Int", "UInt",
				"Long", "ULong",
				"Float", "Double",
				"Char", "String"
			};

			public readonly List<string> Flax = new List<string>()
			{
				"Vector2", "Vector3", "Vector4",
				"Quaternion", "Transform",
				"Matrix2x2", "Matrix3x3", "Matrix"
			};

			public readonly List<string> Game = new List<string>();
		}


	}
}
#endif