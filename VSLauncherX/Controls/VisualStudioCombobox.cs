using System.ComponentModel;

using VSLauncher.DataModel;

namespace VSLauncher.Controls
{
	/// <summary>
	/// Designer-compatible Visual Studio instance selector inheriting from <see cref="IdeCombobox"/>.
	/// </summary>
	public class VisualStudioCombobox : IdeCombobox
	{
		private readonly VisualStudioInstanceManager visualStudioVersions = new VisualStudioInstanceManager();

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioCombobox"/> class.
		/// </summary>
		public VisualStudioCombobox()
		{
			LoadInstances(this.visualStudioVersions.All);
		}

		/// <summary>
		/// Gets the discovered Visual Studio instances.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<VisualStudioInstance> Versions => this.visualStudioVersions.All;

		/// <summary>
		/// Gets or sets the selected <see cref="VisualStudioInstance"/>, returning <c>null</c> when <c>&lt;default&gt;</c> is selected.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new VisualStudioInstance? SelectedItem
		{
			get => this.SelectedIdeInstance as VisualStudioInstance;
			set => this.SelectedIdeInstance = value;
		}

		/// <summary>
		/// Gets or sets the selected index, accounting for the default item in the getter while preserving raw index assignment in the setter.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new int SelectedIndex
		{
			get => this.ShowDefault ? base.SelectedIndex - 1 : base.SelectedIndex;
			set => base.SelectedIndex = value;
		}

		/// <summary>
		/// Selects the active item from a version string (exact or major version), a short name, or an identifier.
		/// </summary>
		/// <param name="v">The version, short name, or identifier string.</param>
		internal void SelectFromVersion(string? v)
		{
			if (string.IsNullOrWhiteSpace(v))
			{
				if (this.ShowDefault && v is not null)
				{
					// v is "" so select the default
					SelectDefault();

					return;
				}

				// any NULL value will return the highest version if available
				VisualStudioInstance? highest = this.visualStudioVersions.HighestVersion();

				if (highest is null)
				{
					this.SelectedIndex = this.ShowDefault ? 0 : -1;

					return;
				}

				v = highest.Version;
			}

			int matchIndex = FindVisualStudioMatchIndex(v);

			this.SelectedIndex = matchIndex >= 0
				? (this.ShowDefault ? 1 + matchIndex : matchIndex)
				: -1;
		}

		/// <summary>
		/// Finds the zero-based index of the first Visual Studio instance matching <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The version, short name, or identifier to match.</param>
		/// <returns>The matching instance index, or <c>-1</c> if none matches.</returns>
		private int FindVisualStudioMatchIndex(string value)
		{
			List<VisualStudioInstance> versions = this.visualStudioVersions.All;

			int index = versions.FindIndex(x => x.Version == value);

			if (index >= 0)
			{
				return index;
			}

			index = versions.FindIndex(x => x.Version.StartsWith(value));

			if (index >= 0)
			{
				return index;
			}

			index = versions.FindIndex(x => x.ShortName.Contains(value));

			if (index >= 0)
			{
				return index;
			}

			return versions.FindIndex(x => x.Identifier == value);
		}
	}
}
