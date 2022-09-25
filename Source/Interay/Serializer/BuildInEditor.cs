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
using FlaxEditor.Content;

namespace Interay.Serializer
{
	public sealed partial class BuildIn
	{
		#region Fields
		private Button _button;
		private BuildInSerializerWindow _window;
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
				_window = new BuildInSerializerWindow();
				_window.Show();
				return;
			}
			_window.Window.Close();
			_window = null;
		}
		#endregion
	}

	internal class BuildInSerializerWindow : CustomEditorWindow
	{
		public const string CycleError = "Entity member '{0} field' of type '{1}' causes a cycle in the entity layout";

		private readonly List<EntityEntry> _ownTypes = new List<EntityEntry>();
		private Tabs _tabs;
		private JsonAsset _jsonAsset;
		private bool _initialized;

		public static SerializeTypes Types { get; private set; }
		private ProjectInfo _projectInfo => Editor.Instance.GameProject.References.FirstOrDefault(x => x.Project.Name == "Interay")?.Project ?? null;
		private string _buildPath => (_projectInfo?.ProjectFolderPath ?? Globals.ProjectFolder) + "/Content/buildin.json";

		public override void Initialize(LayoutElementsContainer layout)
		{
			if (_initialized)
				return;
			_initialized = true;
			Types = new SerializeTypes();
			var buttons = new KeyValuePair<string, Action>[] {
				new KeyValuePair<string, Action>("Add", OnAdd),
				new KeyValuePair<string, Action>("Save", OnSave),
				new KeyValuePair<string, Action>("Build", OnBuild),
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
			
			if (Editor.Instance.ContentDatabase.Find(_buildPath) is AssetItem assetItem)
			{
				var asset = (JsonAsset)Content.Load(assetItem.ID);
				var dict = asset.CreateInstance<Dictionary<string, LineStruct[]>>();
				foreach (var pair in dict)
				{
					var entity = new EntityEntry(this, pair.Key);
					_ownTypes.Add(entity);
					_tabs.AddTab(entity);
					foreach (var line in pair.Value)
						entity.AddLine().Set(line);
				}
			}
		}

		protected override void Deinitialize()
		{
			_initialized = false;
			_ownTypes.Clear();
			_tabs?.DisposeChildren();
			_tabs?.Dispose();
		}

		private void OnAdd()
		{
			var lenght = _ownTypes.Count;
			var text = "Entity";
			while (true)
			{
				var check = text + lenght;
				if (!_ownTypes.Any(x => x.Text == check))
					break;
				lenght++;
			}
			var entity = new EntityEntry(this, text + lenght);
			_ownTypes.Add(entity);
			_tabs.AddTab(entity);
			entity.AddLine().Init();
		}

		private void OnRemove(EntityEntry entry)
		{
			_ownTypes.Remove(entry);
			_tabs.RemoveChild(entry);
		}

		private void OnSave()
		{
			var dict = new Dictionary<string, LineStruct[]>();
			foreach (var item in _ownTypes)
			{
				var lines = item.Lines
					.Where(x=>x.Done)
					.Select(x => new LineStruct { Name = x.Name, Category = x.Category, Type = x.Type, IsArray = x.IsArray })
					.ToArray();
				dict.Add(item.Name, lines);
			}
			if (Editor.SaveJsonAsset(_buildPath, dict))
				Debug.LogError("Failed saving serializer JSON!");

		}

		private void OnBuild()
		{
			
		}

		private struct LineStruct
		{
			public string Name;
			public string Category;
			public string Type;
			public bool IsArray;
		}

		private class EntityEntry : Tab
		{
			private readonly List<Line> _lines;
			private VerticalPanel _container;
			private TextBox _name;
			private Line _spare;
			
			public string Name => _name.Text;
			public IReadOnlyList<Line> Lines => _lines;

			public EntityEntry(BuildInSerializerWindow window, string name) : base(name)
			{
				_lines = new List<Line>();
				BuildInSerializerWindow.Types.Game.Add(name);
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
				button.SetColors(Color.Crimson);
				button.Width = 22f;
				button.Clicked += () => {
					_lines.ForEach(x => _container.RemoveChild(x.Container));
					_lines.Clear();
					window.OnRemove(this);
				};

				line.AutoSize = false;
				line.Height = 26f;
				line.Spacing = 4;
				spacer.Height = 20f;
				_spare = new Line(_container.AddChild<HorizontalPanel>(), this);
			}

			public void CheckName(TextBoxBase textBox)
			{
				var text = textBox.Text;
				if (string.IsNullOrWhiteSpace(text))
				{
					GenerateName(textBox);
					return;
				}
				if (_lines.Any(x => x.Container == textBox.Parent ? false : x.Name == text))
					textBox.Text = text + "_Copy";
			}

			public void GenerateName(TextBoxBase textBox)
			{
				var lenght = _lines.Count - 1;
				var text = "Field";
				while (true)
				{
					var check = text + lenght;
					if (!_lines.Any(x => x.Name == check))
						break;
					lenght++;
				}
				textBox.SetText(text + lenght);
			}

			public Line AddLine()
			{
				var line = _spare;
				_spare = new Line(_container.AddChild<HorizontalPanel>(), this);
				_lines.Add(line);
				return line;
			}

			public void RemoveLine(Line line)
			{
				_lines.Remove(line);
				_container.RemoveChild(line.Container);
			}
		}

		private class Line
		{
			public HorizontalPanel Container;
			private TextBox _name;
			private ComboBox _combo1, _combo2;
			private Label _checkboxLabel;
			private CheckBox _checkbox;
			private EntityEntry _entry;
			private Button _button;

			public bool Done => _checkbox?.Visible ?? false;
			public string Name => _name?.Text ?? null;
			public string Category => _combo1.SelectedItem;
			public string Type => _combo2.SelectedItem;
			public bool IsArray => _checkbox.Checked;

			public Line(HorizontalPanel line, EntityEntry entry)
			{
				Container = line;
				Container.Height = 22f;
				line.CullChildren = false;
				_entry = entry;
				_button = Container.AddChild<Button>();
				_button.Text = "+";
				_button.Width = 20f;
				_button.ButtonClicked += btn => {
					if (btn.Text == "+")
					{
						_entry.AddLine();
						Init();
						return;
					}
					_entry.RemoveLine(this);
				};
			}

			public void Init()
			{
				_button.Text = "X";
				_button.SetColors(Color.Crimson);
				_name  = Container.AddChild<TextBox>();
				_combo1 = Container.AddChild<ComboBox>();

				_name.Font.Size = 9;
				_name.Width = 120f;
				_name.TextBoxEditEnd += _entry.CheckName;

				_combo1.Items = BuildInSerializerWindow.Types.Names;
				_combo1.SelectedIndexChanged += PrimaryDropdownChange;
				_combo1.Width = 70f;
			}

			public void Set(LineStruct data)
			{
				Init();
				_combo1.SelectedItem = data.Category;
				_combo2.SelectedItem = data.Type;
				_checkbox.Checked = data.IsArray;
				_name.Text = data.Name;
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
					_checkboxLabel.Width = 30f;
					_checkboxLabel.AutoWidth = false;
					_checkboxLabel.HorizontalAlignment = TextAlignment.Near;
					_checkboxLabel.Text = "Array:";
					_checkbox = Container.AddChild<CheckBox>();
					_checkbox.Checked = false;
				}
				_checkboxLabel.Visible = select;
				_checkbox.Visible = select;
				_name.Enabled = select;
				if (select && string.IsNullOrEmpty(_name.Text))
					_entry.GenerateName(_name);
			}

			private List<string> GetCombo2(string key)
			{
				switch (key)
				{
					case "General":
						return BuildInSerializerWindow.Types.General;
					case "Flax":
						return BuildInSerializerWindow.Types.Flax;
					case "Game":
						return BuildInSerializerWindow.Types.Game;
				}
				return null;
			}
		}

		public class SerializeTypes
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