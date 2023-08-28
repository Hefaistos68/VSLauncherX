using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The visual studio combobox.
	/// </summary>
	public class VisualStudioCombobox : ComboBox
	{
		private VisualStudioInstanceManager visualStudioVersions = new VisualStudioInstanceManager();

		public VisualStudioCombobox()
		{
			this.DrawMode = DrawMode.OwnerDrawFixed;
			this.DropDownStyle = ComboBoxStyle.DropDownList;
			this.IntegralHeight = false;
			this.ItemHeight = 26;
			this.DrawItem += CustomDrawItem;

			this.Items.AddRange(this.visualStudioVersions.All.ToArray());
			// this.SelectedIndex = 0;
		}

		/// <summary>
		/// Gets the versions.
		/// </summary>
		public List<VisualStudioInstance> Versions { get { return visualStudioVersions.All; } }

		/// <summary>
		/// Gets the selected item.
		/// </summary>
		public new VisualStudioInstance SelectedItem 
		{ 
			get 
			{ 
				return (VisualStudioInstance)base.SelectedItem; 
			} 
			set
			{
				base.SelectedItem = value;
			}
		}
		/// <summary>
		/// Selects the active item from a version string (exact or major version), a shortname or an identifier
		/// </summary>
		/// <param name="v">The v.</param>
		internal void SelectFromVersion(string? v)
		{
			if(string.IsNullOrWhiteSpace(v))
			{
				v = this.visualStudioVersions.HighestVersion().Version;
			}

			// find either by exact version or by short name containing the version
			var i = this.visualStudioVersions.All.FindIndex(x => x.Version == v);
			if (i >= 0)
			{
				this.SelectedIndex = i;
				return;
			}

			var k = this.visualStudioVersions.All.FindIndex(x => x.Version.StartsWith(v));
			if (k >= 0)
			{
				this.SelectedIndex = k;
				return;
			}

			// by year
			var n = this.visualStudioVersions.All.FindIndex(x => x.ShortName.Contains(v));
			if (n >= 0)
			{
				this.SelectedIndex = n;
				return;
			}

			// by identifier
			var o = this.visualStudioVersions.All.FindIndex(x => x.Identifier == v);
			if (o >= 0)
			{
				this.SelectedIndex = o;
				return;
			}

			this.SelectedIndex = -1;
		}

		/// <summary>
		/// Custom draws the item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void CustomDrawItem(object sender, DrawItemEventArgs e)
		{
			// draw the selected item with the Visual Studio Icon and the version as text
			if (e.Index >= 0 && e.Index <= this.visualStudioVersions.Count)
			{
				e.DrawBackground();

				var height = 16; 

				Rectangle iconRect = new Rectangle(e.Bounds.Left + this.Margin.Left,
													e.Bounds.Top + ((this.ItemHeight - height) / 2),
													height, height);
				e.Graphics.DrawIcon(visualStudioVersions[e.Index].AppIcon, iconRect);
				e.Graphics.DrawString(visualStudioVersions[e.Index].Name, e.Font, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
			}
		}

	}
}