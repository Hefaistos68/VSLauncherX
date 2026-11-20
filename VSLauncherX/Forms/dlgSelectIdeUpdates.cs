using System.ComponentModel;

using VSLauncher.Helpers;
using VSLauncher.Ide;

namespace VSLauncher.Forms
{
	/// <summary>
	/// Modal checklist dialog allowing the user to select which VS Code-based IDEs to update when multiple updates are available.
	/// </summary>
	public partial class dlgSelectIdeUpdates : Form
	{
		private readonly IReadOnlyList<VsCodeUpdateCandidate> availableUpdates;
		private bool isBulkChecking;

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgSelectIdeUpdates"/> class.
		/// </summary>
		/// <param name="availableUpdates">The available IDE update candidates to present for selection.</param>
		public dlgSelectIdeUpdates(IReadOnlyList<VsCodeUpdateCandidate> availableUpdates)
		{
			ArgumentNullException.ThrowIfNull(availableUpdates);

			this.availableUpdates = availableUpdates;

			InitializeComponent();
			ThemeHelper.ApplyCurrentTheme(this);
			PopulateUpdatesList();
			UpdateActionButtonState();
		}

		/// <summary>
		/// Gets the IDE update candidates selected by the user when confirming the dialog.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IReadOnlyList<VsCodeUpdateCandidate> SelectedUpdates { get; private set; } = Array.Empty<VsCodeUpdateCandidate>();

		/// <summary>
		/// Populates <see cref="lvUpdates"/> with each candidate pre-checked and paired with its 16x16 IDE icon.
		/// </summary>
		private void PopulateUpdatesList()
		{
			this.isBulkChecking = true;
			this.lvUpdates.BeginUpdate();

			try
			{
				this.lvUpdates.Items.Clear();
				this.ideImageList.Images.Clear();
				_ = this.ideImageList.Handle;

				foreach (VsCodeUpdateCandidate candidate in this.availableUpdates)
				{
					if (candidate is null)
					{
						continue;
					}

					AddCandidateRow(candidate);
				}
			}
			finally
			{
				this.lvUpdates.EndUpdate();
				this.isBulkChecking = false;
			}
		}

		/// <summary>
		/// Adds a single pre-checked row for <paramref name="candidate"/> to <see cref="lvUpdates"/>.
		/// </summary>
		/// <param name="candidate">The update candidate to display.</param>
		private void AddCandidateRow(VsCodeUpdateCandidate candidate)
		{
			int imageIndex      = AddCandidateIcon(candidate.Instance);
			string installedVer = ResolveInstalledVersionText(candidate);
			string availableVer = ResolveAvailableVersionText(candidate);
			var item            = new ListViewItem(new[] { candidate.Instance.Name, installedVer, availableVer })
			{
				Checked     = true,
				ImageIndex  = imageIndex,
				Tag         = candidate,
				ToolTipText = candidate.Instance.Location
			};

			this.lvUpdates.Items.Add(item);
		}

		/// <summary>
		/// Extracts a 16x16 bitmap from <paramref name="instance"/>, stores it in <see cref="ideImageList"/>, and returns its index.
		/// </summary>
		/// <param name="instance">The IDE instance whose icon should be added.</param>
		/// <returns>The zero-based icon index in <see cref="ideImageList"/>.</returns>
		private int AddCandidateIcon(VsCodeInstance instance)
		{
			Icon sourceIcon = instance.AppIcon ?? Resources.DefaultIcon;

			using Bitmap rawBitmap    = sourceIcon.ToBitmap();
			using Bitmap scaledBitmap = new Bitmap(rawBitmap, new Size(16, 16));

			this.ideImageList.Images.Add(scaledBitmap);

			return this.ideImageList.Images.Count - 1;
		}

		/// <summary>
		/// Resolves the installed version column label for <paramref name="candidate"/>.
		/// </summary>
		/// <param name="candidate">The update candidate.</param>
		/// <returns>The installed version string or <c>"Unknown"</c>.</returns>
		private static string ResolveInstalledVersionText(VsCodeUpdateCandidate candidate)
		{
			if (!string.IsNullOrWhiteSpace(candidate.UpdateInfo.CurrentVersion))
			{
				return candidate.UpdateInfo.CurrentVersion;
			}

			return string.IsNullOrWhiteSpace(candidate.Instance.Version) ? "Unknown" : candidate.Instance.Version;
		}

		/// <summary>
		/// Resolves the available version column label for <paramref name="candidate"/>.
		/// </summary>
		/// <param name="candidate">The update candidate.</param>
		/// <returns>The target version string or <c>"Available"</c>.</returns>
		private static string ResolveAvailableVersionText(VsCodeUpdateCandidate candidate)
		{
			return string.IsNullOrWhiteSpace(candidate.UpdateInfo.LatestVersion)
				? "Available"
				: candidate.UpdateInfo.LatestVersion;
		}

		/// <summary>
		/// Checks every IDE item in <see cref="lvUpdates"/>.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnSelectAll_Click(object? sender, EventArgs e)
		{
			SetAllChecked(true);
		}

		/// <summary>
		/// Unchecks every IDE item in <see cref="lvUpdates"/>.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnSelectNone_Click(object? sender, EventArgs e)
		{
			SetAllChecked(false);
		}

		/// <summary>
		/// Synchronizes <see cref="btnUpdate"/> availability whenever an item's check state changes.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void lvUpdates_ItemChecked(object? sender, ItemCheckedEventArgs e)
		{
			if (this.isBulkChecking)
			{
				return;
			}

			UpdateActionButtonState();
		}

		/// <summary>
		/// Collects the checked <see cref="VsCodeUpdateCandidate"/> items into <see cref="SelectedUpdates"/> before confirming.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnUpdate_Click(object? sender, EventArgs e)
		{
			var selected = new List<VsCodeUpdateCandidate>(this.lvUpdates.CheckedItems.Count);

			foreach (ListViewItem item in this.lvUpdates.CheckedItems)
			{
				if (item.Tag is VsCodeUpdateCandidate candidate)
				{
					selected.Add(candidate);
				}
			}

			this.SelectedUpdates = selected;
		}

		/// <summary>
		/// Sets the checked state of every item in <see cref="lvUpdates"/> in one batch and refreshes button state.
		/// </summary>
		/// <param name="isChecked"><c>true</c> to check all items; <c>false</c> to uncheck all items.</param>
		private void SetAllChecked(bool isChecked)
		{
			this.isBulkChecking = true;
			this.lvUpdates.BeginUpdate();

			try
			{
				foreach (ListViewItem item in this.lvUpdates.Items)
				{
					item.Checked = isChecked;
				}
			}
			finally
			{
				this.lvUpdates.EndUpdate();
				this.isBulkChecking = false;
			}

			UpdateActionButtonState();
		}

		/// <summary>
		/// Enables <see cref="btnUpdate"/> if and only if at least one item in <see cref="lvUpdates"/> is checked.
		/// </summary>
		private void UpdateActionButtonState()
		{
			this.btnUpdate.Enabled = this.lvUpdates.CheckedItems.Count > 0;
		}
	}
}
