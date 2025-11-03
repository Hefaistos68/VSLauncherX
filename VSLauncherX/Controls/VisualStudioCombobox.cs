using VSLauncher.DataModel;

namespace VSLauncher.Controls
{
    /// <summary>
    /// The visual studio combobox.
    /// </summary>
    public class VisualStudioCombobox : ComboBox
    {
        private VisualStudioInstanceManager visualStudioVersions = new VisualStudioInstanceManager();
        private bool showDefault;

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioCombobox"/> class.
		/// </summary>
		public VisualStudioCombobox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            IntegralHeight = false;
            ItemHeight = 26;
            DrawItem += CustomDrawItem;

            Items.AddRange(visualStudioVersions.All.ToArray());
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show default.
        /// </summary>
        public bool ShowDefault
        {
            get => showDefault;
            set
            {
                showDefault = value;

                if (value)
                {
                    Items.Insert(0, "<default>");
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
        /// Gets the versions.
        /// </summary>
        public List<VisualStudioInstance> Versions { get { return visualStudioVersions.All; } }

        /// <summary>
        /// Gets the selected item.
        /// </summary>
        public new VisualStudioInstance? SelectedItem
        {
            get
            {
                if (ShowDefault)
                {
                    return base.SelectedIndex > 0 ? (VisualStudioInstance)base.SelectedItem : null;
                }

                return (VisualStudioInstance)base.SelectedItem;
            }
            set
            {
                base.SelectedItem = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected index, accounting for the default item which will result in -1
        /// </summary>
        public new int SelectedIndex
        {
            get
            {
                return ShowDefault ? base.SelectedIndex - 1 : base.SelectedIndex;
            }
            set
            {
                base.SelectedIndex = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether default is selected.
        /// </summary>
        public bool IsDefaultSelected
        {
            get
            {
                return ShowDefault && base.SelectedIndex == 0;
            }
        }

        /// <summary>
        /// Selects the default item if existing
        /// </summary>
        public void SelectDefault()
        {
            if (ShowDefault)
            {
                SelectedIndex = 0;
            }
        }

        protected override void OnDropDown(EventArgs e)
		{
			int selectedItem = SelectedIndex; // remember the item selected before clearing the list

			Items.Clear();

            if (ShowDefault)
            {
                Items.Add("<default>");
            }

            Items.AddRange(visualStudioVersions.All.ToArray());

			SelectedIndex = selectedItem < 0 ? -1 : selectedItem;   // set the previously selected item back

			base.OnDropDown(e);
        }

        /// <summary>
        /// Selects the active item from a version string (exact or major version), a shortname or an identifier
        /// </summary>
        /// <param name="v">The v.</param>
        internal void SelectFromVersion(string? v)
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                if (ShowDefault)
                {
                    if (v != null)  // v is "" so select the default
                    {
                        SelectDefault();
                        return;
                    }
                }

                // any NULL value will return the highest version
                v = visualStudioVersions.HighestVersion().Version;
            }

            // find either by exact version or by short name containing the version
            var i = visualStudioVersions.All.FindIndex(x => x.Version == v);
            if (i >= 0)
            {
                SelectedIndex = ShowDefault ? 1 + i : i;
                return;
            }

            var k = visualStudioVersions.All.FindIndex(x => x.Version.StartsWith(v));
            if (k >= 0)
            {
                SelectedIndex = ShowDefault ? 1 + k : k;
                return;
            }

            // by year
            var n = visualStudioVersions.All.FindIndex(x => x.ShortName.Contains(v));
            if (n >= 0)
            {
                SelectedIndex = ShowDefault ? 1 + n : n;
                return;
            }

            // by identifier
            var o = visualStudioVersions.All.FindIndex(x => x.Identifier == v);
            if (o >= 0)
            {
                SelectedIndex = ShowDefault ? 1 + o : o;
                return;
            }

            SelectedIndex = -1;
        }

        /// <summary>
        /// Custom draws the item
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void CustomDrawItem(object sender, DrawItemEventArgs e)
        {
            // draw the selected item with the Visual Studio Icon and the version as text
            if (e.Index >= 0 && e.Index <= visualStudioVersions.Count)
            {
                e.DrawBackground();

                var height = 16;

                Rectangle iconRect = new Rectangle(e.Bounds.Left + Margin.Left,
                                                    e.Bounds.Top + (ItemHeight - height) / 2,
                                                    height, height);
                if (ShowDefault)
                {
                    if (e.Index > 0)
                    {
                        e.Graphics.DrawIcon(visualStudioVersions[e.Index - 1].AppIcon, iconRect);
                        e.Graphics.DrawString(visualStudioVersions[e.Index - 1].Name, e.Font, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
                    }
                    else
                    {
                        e.Graphics.DrawIcon(Resources.AppLogo, iconRect);
                        e.Graphics.DrawString("<default>", e.Font, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
                    }
                }
                else
                {
                    e.Graphics.DrawIcon(visualStudioVersions[e.Index].AppIcon, iconRect);
                    e.Graphics.DrawString(visualStudioVersions[e.Index].Name, e.Font, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
                }
            }
        }

    }
}