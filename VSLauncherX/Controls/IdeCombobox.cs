using System.ComponentModel;

using VSLauncher.Ide;

using ThemePalette = VSLauncher.Helpers.ThemeHelper.ThemePalette;

namespace VSLauncher.Controls
{
	/// <summary>
	/// An owner-drawn <see cref="ComboBox"/> that displays <see cref="IIdeInstance"/> entries with their icons and optional <c>&lt;default&gt;</c> item.
	/// </summary>
	public class IdeCombobox : ComboBox
	{
		private const string DefaultItemText = "<default>";

		private List<IIdeInstance> instances = new List<IIdeInstance>();
		private bool showDefault;
		private ThemePalette? currentTheme;

		/// <summary>
		/// Initializes a new instance of the <see cref="IdeCombobox"/> class.
		/// </summary>
		public IdeCombobox()
		{
			DrawMode       = DrawMode.OwnerDrawFixed;
			DropDownStyle  = ComboBoxStyle.DropDownList;
			IntegralHeight = false;
			ItemHeight     = 26;
			DrawItem      += CustomDrawItem;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to include the <c>&lt;default&gt;</c> entry at index 0.
		/// </summary>
		[DefaultValue(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public bool ShowDefault
		{
			get => this.showDefault;
			set
			{
				this.showDefault = value;

				if (value)
				{
					if (Items.Count == 0 || Items[0] is not string)
					{
						Items.Insert(0, DefaultItemText);
					}
				}
				else
				{
					if (Items.Count > 0 && Items[0] is string)
					{
						Items.RemoveAt(0);
					}
				}
			}
		}

		/// <summary>
		/// Gets the loaded IDE instances.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IReadOnlyList<IIdeInstance> Instances => this.instances;

		/// <summary>
		/// Gets or sets the selected <see cref="IIdeInstance"/>, returning <c>null</c> when none or <c>&lt;default&gt;</c> is selected.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IIdeInstance? SelectedIdeInstance
		{
			get
			{
				if (this.ShowDefault && base.SelectedIndex == 0)
				{
					return null;
				}

				return base.SelectedItem as IIdeInstance;
			}
			set
			{
				if (value is null)
				{
					base.SelectedItem = null;

					return;
				}

				base.SelectedItem = value;

				if (!ReferenceEquals(base.SelectedItem, value))
				{
					SelectByIdentifier(value.Identifier);
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether the <c>&lt;default&gt;</c> entry is currently selected.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsDefaultSelected => this.ShowDefault && base.SelectedIndex == 0;

		/// <summary>
		/// Replaces the combobox items with <paramref name="instances"/> while preserving <see cref="ShowDefault"/>.
		/// </summary>
		/// <param name="instances">The IDE instances to display.</param>
		public void LoadInstances(IReadOnlyList<IIdeInstance> instances)
		{
			ArgumentNullException.ThrowIfNull(instances);

			this.instances = instances.Where(i => i is not null).ToList();

			RebuildItems();
		}

		/// <summary>
		/// Selects the <c>&lt;default&gt;</c> item if <see cref="ShowDefault"/> is enabled.
		/// </summary>
		public void SelectDefault()
		{
			if (this.ShowDefault && Items.Count > 0)
			{
				base.SelectedIndex = 0;
			}
		}

		/// <summary>
		/// Selects the IDE instance matching <paramref name="id"/>, or selects <c>&lt;default&gt;</c> when <see cref="ShowDefault"/> is enabled and <paramref name="id"/> is empty.
		/// </summary>
		/// <param name="id">The target <see cref="IIdeInstance.Identifier"/>.</param>
		public void SelectByIdentifier(string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				if (this.ShowDefault)
				{
					SelectDefault();
				}
				else
				{
					base.SelectedIndex = -1;
				}

				return;
			}

			int index = FindInstanceIndex(i => string.Equals(i.Identifier, id, StringComparison.OrdinalIgnoreCase));

			if (index >= 0)
			{
				base.SelectedIndex = this.ShowDefault ? index + 1 : index;

				return;
			}

			base.SelectedIndex = -1;
		}

		/// <summary>
		/// Applies themed colors to this combobox.
		/// </summary>
		/// <param name="palette">The active theme palette.</param>
		internal void ApplyTheme(ThemePalette palette)
		{
			this.currentTheme = palette;
			BackColor         = palette.SurfaceAlt;
			ForeColor         = palette.Text;
			FlatStyle         = palette.IsDark ? FlatStyle.Flat : FlatStyle.Standard;

			Invalidate();
		}

		/// <summary>
		/// Finds the zero-based index in <see cref="Instances"/> matching <paramref name="match"/>.
		/// </summary>
		/// <param name="match">The predicate to evaluate.</param>
		/// <returns>The matching zero-based index, or <c>-1</c> if none matches.</returns>
		protected int FindInstanceIndex(Predicate<IIdeInstance> match)
		{
			ArgumentNullException.ThrowIfNull(match);

			return this.instances.FindIndex(match);
		}

		/// <summary>
		/// Rebuilds <see cref="ComboBox.Items"/> from the loaded instances and <see cref="ShowDefault"/>.
		/// </summary>
		protected void RebuildItems()
		{
			Items.Clear();

			if (this.showDefault)
			{
				Items.Add(DefaultItemText);
			}

			foreach (IIdeInstance instance in this.instances)
			{
				Items.Add(instance);
			}
		}

		/// <summary>
		/// Custom-draws each combobox item with its icon and display name.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The draw item event arguments.</param>
		private void CustomDrawItem(object? sender, DrawItemEventArgs e)
		{
			if (e.Index < 0 || e.Index >= Items.Count)
			{
				return;
			}

			ThemePalette? palette = this.currentTheme;
			bool isDisabled       = !this.Enabled || (e.State & DrawItemState.Disabled) == DrawItemState.Disabled;
			bool isSelected       = !isDisabled && (e.State & DrawItemState.Selected) == DrawItemState.Selected;
			Color backColor       = isSelected ? (palette?.Selection ?? SystemColors.Highlight) : (palette?.SurfaceAlt ?? SystemColors.Window);
			Color textColor       = isDisabled
				? (palette?.DisabledText ?? SystemColors.GrayText)
				: (isSelected ? (palette?.SelectionText ?? SystemColors.HighlightText) : (palette?.Text ?? SystemColors.ControlText));
			Font drawFont         = e.Font ?? Font;

			(Icon? itemIcon, string itemText) = ResolveDrawContent(e.Index);

			using (var backBrush = new SolidBrush(backColor))
			using (var textBrush = new SolidBrush(textColor))
			{
				e.Graphics.FillRectangle(backBrush, e.Bounds);

				const int iconSize = 16;
				int iconTop        = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - iconSize) / 2);
				int iconLeft       = e.Bounds.Left + Math.Max(2, Margin.Left);
				var iconRect       = new Rectangle(iconLeft, iconTop, iconSize, iconSize);

				if (itemIcon is not null)
				{
					e.Graphics.DrawIcon(itemIcon, iconRect);
				}

				int textTop  = e.Bounds.Top + Math.Max(2, (e.Bounds.Height - drawFont.Height) / 2);
				int textLeft = e.Bounds.Left + Math.Max(20, Margin.Left + iconSize + 4);

				e.Graphics.DrawString(itemText, drawFont, textBrush, textLeft, textTop);
			}

			e.DrawFocusRectangle();
		}

		/// <summary>
		/// Resolves the icon and display text for the item at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The index into <see cref="ComboBox.Items"/>.</param>
		/// <returns>The icon and text to draw.</returns>
		private (Icon? Icon, string Text) ResolveDrawContent(int index)
		{
			object? item = Items[index];

			if (item is IIdeInstance instance)
			{
				return (instance.AppIcon, instance.Name);
			}

			return (Resources.AppLogo, item?.ToString() ?? DefaultItemText);
		}
	}
}
