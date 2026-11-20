using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using VSLauncher.DataModel;
using VSLauncher.Helpers;
using VSLauncher.Ide;

namespace VSLauncher.Forms
{
	/// <summary>
	/// Configures general application preferences and manually registered VS Code-based IDEs.
	/// </summary>
	public partial class dlgSettings : Form
	{
		private const string ExecutableFileFilter = "Executable files (*.exe)|*.exe";

		private readonly IdeManager? ideManager;
		private readonly CustomVsCodeIdeStore customIdeStore;
		private readonly List<CustomVsCodeIdeEntry> initialCustomIdes = new List<CustomVsCodeIdeEntry>();
		private readonly List<CustomVsCodeIdeEntry> pendingCustomIdes = new List<CustomVsCodeIdeEntry>();

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgSettings"/> class.
		/// </summary>
		/// <param name="ideManager">Optional IDE manager used to inspect and rescan discovered VS Code-based IDEs.</param>
		/// <param name="customIdeStore">Optional custom IDE store used to load and persist manually registered IDEs.</param>
		public dlgSettings(IdeManager? ideManager = null, CustomVsCodeIdeStore? customIdeStore = null)
		{
			InitializeComponent();

			this.ideManager     = ideManager;
			this.customIdeStore = customIdeStore ?? new CustomVsCodeIdeStore();

			ThemeHelper.ApplyCurrentTheme(this);
		}

		/// <summary>
		/// Gets a value indicating whether manually registered VS Code-based IDEs were modified and saved.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CustomIdesChanged { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the user triggered an IDE rescan while the dialog was open.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IdesRescanned { get; private set; }

		/// <summary>
		/// Persists general settings and any pending custom VS Code IDE edits when the user confirms the dialog.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnOk_Click(object sender, EventArgs e)
		{
			if (HaveCustomEntriesChanged())
			{
				try
				{
					this.customIdeStore.Save(this.pendingCustomIdes);
					this.CustomIdesChanged = true;
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
				{
					_ = MessageBox.Show(
						this,
						$"Failed to save custom VS Code-based IDEs: {ex.Message}",
						"Save Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);

					this.DialogResult = DialogResult.None;

					return;
				}
			}

			Properties.Settings.Default.AlwaysAdmin          = chkAlwaysAdmin.Checked;
			Properties.Settings.Default.AutoStart            = chkAutostart.Checked;
			Properties.Settings.Default.SynchronizeVS        = chkSync.Checked;
			Properties.Settings.Default.ShowPathForSolutions = chkShowPath.Checked;
			Properties.Settings.Default.DarkMode             = chkDarkMode.Checked;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Populates general checkboxes and the VS Code-based IDEs list when the dialog loads.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void dlgSettings_Load(object sender, EventArgs e)
		{
			chkAutostart.Checked        = Properties.Settings.Default.AutoStart;
			chkAlwaysAdmin.Checked      = Properties.Settings.Default.AlwaysAdmin;
			chkSync.Checked             = Properties.Settings.Default.SynchronizeVS;
			chkShowPath.Checked         = Properties.Settings.Default.ShowPathForSolutions;
			chkDarkMode.Checked         = Properties.Settings.Default.DarkMode;
			btnRescanVsCodeIdes.Enabled = this.ideManager is not null;

			LoadCustomIdes();
			RebuildVsCodeIdesListView();
			UpdateRemoveButtonState();
		}

		/// <summary>
		/// Prompts for a custom VS Code-based IDE executable, inspects it, and adds it to the pending list.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnAddVsCodeIde_Click(object? sender, EventArgs e)
		{
			using var openFileDialog = new OpenFileDialog
			{
				Filter           = ExecutableFileFilter,
				FilterIndex      = 1,
				CheckFileExists  = true,
				Multiselect      = false,
				RestoreDirectory = true,
				Title            = "Select VS Code-based IDE Executable"
			};

			if (openFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			CustomVsCodeIdeInspection? inspection = this.customIdeStore.TryInspectExecutable(openFileDialog.FileName);

			if (inspection is null)
			{
				_ = MessageBox.Show(
					this,
					"The selected file is not a valid Windows executable.",
					"Invalid Executable",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);

				return;
			}

			if (IsExecutableAlreadyListed(inspection.ExecutablePath))
			{
				_ = MessageBox.Show(
					this,
					$"'{inspection.ExecutablePath}' is already listed.",
					"Duplicate IDE",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);

				return;
			}

			if (!ConfirmUnverifiedExecutableIfNeeded(inspection))
			{
				return;
			}

			var entry = new CustomVsCodeIdeEntry
			{
				ExecutablePath    = inspection.ExecutablePath,
				DisplayName       = string.IsNullOrWhiteSpace(inspection.SuggestedDisplayName) ? null : inspection.SuggestedDisplayName,
				UserDataDirectory = string.IsNullOrWhiteSpace(inspection.SuggestedUserDataDirectory) ? null : inspection.SuggestedUserDataDirectory
			};

			this.pendingCustomIdes.Add(entry);
			RebuildVsCodeIdesListView(entry.ExecutablePath);
			UpdateRemoveButtonState();
		}

		/// <summary>
		/// Removes the currently selected manual IDE entry from the pending custom IDE list.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnRemoveVsCodeIde_Click(object? sender, EventArgs e)
		{
			if (!TryGetSelectedListRow(out VsCodeIdeListRow? selectedRow) || !selectedRow.IsManual)
			{
				return;
			}

			int removed = this.pendingCustomIdes.RemoveAll(entry =>
			{
				string canonical = TryNormalizePath(entry.ExecutablePath) ?? entry.ExecutablePath;

				return string.Equals(canonical, selectedRow.CanonicalPath, StringComparison.OrdinalIgnoreCase);
			});

			if (removed > 0)
			{
				RebuildVsCodeIdesListView();
				UpdateRemoveButtonState();
			}
		}

		/// <summary>
		/// Re-runs VS Code discovery on <see cref="ideManager"/> and refreshes the displayed IDE list.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnRescanVsCodeIdes_Click(object? sender, EventArgs e)
		{
			if (this.ideManager is null)
			{
				return;
			}

			this.Cursor = Cursors.WaitCursor;

			try
			{
				this.ideManager.Refresh(IdeType.VsCode);
				this.IdesRescanned = true;
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}

			RebuildVsCodeIdesListView();
			UpdateRemoveButtonState();
		}

		/// <summary>
		/// Updates the Remove button state when the selection in <see cref="lvVsCodeIdes"/> changes.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void lvVsCodeIdes_SelectedIndexChanged(object? sender, EventArgs e)
		{
			UpdateRemoveButtonState();
		}

		/// <summary>
		/// Loads persisted custom IDE entries into the initial snapshot and pending working list.
		/// </summary>
		private void LoadCustomIdes()
		{
			IReadOnlyList<CustomVsCodeIdeEntry> loaded = this.customIdeStore.Load();

			this.initialCustomIdes.Clear();
			this.pendingCustomIdes.Clear();
			this.initialCustomIdes.AddRange(loaded);
			this.pendingCustomIdes.AddRange(loaded);
		}

		/// <summary>
		/// Rebuilds <see cref="lvVsCodeIdes"/> by merging auto-discovered instances with pending manual entries, deduplicated by canonical path.
		/// </summary>
		/// <param name="selectCanonicalPath">Optional canonical executable path to select after rebuilding.</param>
		private void RebuildVsCodeIdesListView(string? selectCanonicalPath = null)
		{
			List<VsCodeInstance> allDiscovered = GetDiscoveredVsCodeInstances();
			var seenPaths                      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			this.lvVsCodeIdes.BeginUpdate();

			try
			{
				this.lvVsCodeIdes.Items.Clear();

				foreach (VsCodeInstance instance in allDiscovered.Where(i => !i.IsCustom))
				{
					string canonicalPath = TryNormalizePath(instance.Location) ?? instance.Location;

					if (seenPaths.Add(canonicalPath))
					{
						AddIdeListViewItem(instance.Name, "Auto", canonicalPath, isManual: false, selectCanonicalPath);
					}
				}

				foreach (CustomVsCodeIdeEntry entry in this.pendingCustomIdes)
				{
					string canonicalPath = TryNormalizePath(entry.ExecutablePath) ?? entry.ExecutablePath;

					if (seenPaths.Add(canonicalPath))
					{
						string displayName = ResolveManualEntryDisplayName(entry, canonicalPath, allDiscovered);

						AddIdeListViewItem(displayName, "Manual", canonicalPath, isManual: true, selectCanonicalPath);
					}
				}
			}
			finally
			{
				this.lvVsCodeIdes.EndUpdate();
			}
		}

		/// <summary>
		/// Adds a single row to <see cref="lvVsCodeIdes"/> and optionally selects it when matching <paramref name="selectCanonicalPath"/>.
		/// </summary>
		/// <param name="name">The display name column text.</param>
		/// <param name="source">The source column text (<c>Auto</c> or <c>Manual</c>).</param>
		/// <param name="canonicalPath">The canonical executable path.</param>
		/// <param name="isManual"><c>true</c> for manually registered entries; otherwise <c>false</c>.</param>
		/// <param name="selectCanonicalPath">Optional canonical path to select and scroll into view.</param>
		private void AddIdeListViewItem(
			string name,
			string source,
			string canonicalPath,
			bool isManual,
			string? selectCanonicalPath)
		{
			var item = new ListViewItem(new[] { name, source, canonicalPath })
			{
				Tag         = new VsCodeIdeListRow(isManual, canonicalPath),
				ToolTipText = File.Exists(canonicalPath) ? canonicalPath : $"{canonicalPath} (executable not found)"
			};

			this.lvVsCodeIdes.Items.Add(item);

			if (!string.IsNullOrWhiteSpace(selectCanonicalPath)
				&& string.Equals(canonicalPath, selectCanonicalPath, StringComparison.OrdinalIgnoreCase))
			{
				item.Selected = true;
				item.Focused  = true;
				item.EnsureVisible();
			}
		}

		/// <summary>
		/// Returns all currently discovered <see cref="VsCodeInstance"/> entries from <see cref="ideManager"/>.
		/// </summary>
		/// <returns>A list of discovered <see cref="VsCodeInstance"/> entries.</returns>
		private List<VsCodeInstance> GetDiscoveredVsCodeInstances()
		{
			if (this.ideManager is null)
			{
				return new List<VsCodeInstance>();
			}

			return this.ideManager.GetInstances(IdeType.VsCode)
				.OfType<VsCodeInstance>()
				.ToList();
		}

		/// <summary>
		/// Resolves a display name for a manual entry from its stored override, a matching discovered instance, or its executable filename.
		/// </summary>
		/// <param name="entry">The manual IDE entry.</param>
		/// <param name="canonicalPath">The canonical executable path.</param>
		/// <param name="allDiscovered">Discovered VS Code instances.</param>
		/// <returns>A non-empty display name.</returns>
		private static string ResolveManualEntryDisplayName(
			CustomVsCodeIdeEntry entry,
			string canonicalPath,
			IReadOnlyList<VsCodeInstance> allDiscovered)
		{
			if (!string.IsNullOrWhiteSpace(entry.DisplayName))
			{
				return entry.DisplayName.Trim();
			}

			VsCodeInstance? matched = allDiscovered.FirstOrDefault(instance =>
				string.Equals(TryNormalizePath(instance.Location) ?? instance.Location, canonicalPath, StringComparison.OrdinalIgnoreCase));

			if (matched is not null)
			{
				return matched.Name;
			}

			string stem = Path.GetFileNameWithoutExtension(canonicalPath);

			return string.IsNullOrWhiteSpace(stem) ? canonicalPath : stem;
		}

		/// <summary>
		/// Checks whether <paramref name="canonicalPath"/> already exists among auto-discovered or pending manual IDE entries.
		/// </summary>
		/// <param name="canonicalPath">The canonical executable path to check.</param>
		/// <returns><c>true</c> if already present; otherwise <c>false</c>.</returns>
		private bool IsExecutableAlreadyListed(string canonicalPath)
		{
			bool matchesAuto = GetDiscoveredVsCodeInstances()
				.Where(i => !i.IsCustom)
				.Any(i => string.Equals(TryNormalizePath(i.Location) ?? i.Location, canonicalPath, StringComparison.OrdinalIgnoreCase));

			if (matchesAuto)
			{
				return true;
			}

			return this.pendingCustomIdes.Any(entry =>
				string.Equals(TryNormalizePath(entry.ExecutablePath) ?? entry.ExecutablePath, canonicalPath, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Prompts the user for confirmation when the inspected executable lacks VS Code metadata files.
		/// </summary>
		/// <param name="inspection">The inspection result for the candidate executable.</param>
		/// <returns><c>true</c> to proceed with adding the executable; otherwise <c>false</c>.</returns>
		private bool ConfirmUnverifiedExecutableIfNeeded(CustomVsCodeIdeInspection inspection)
		{
			if (inspection.IsVerifiedVsCodeFork)
			{
				return true;
			}

			DialogResult result = MessageBox.Show(
				this,
				"The selected executable does not appear to contain VS Code metadata (resources\\app\\product.json and cli.js). Add it anyway?",
				"Add Custom IDE",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning);

			return result == DialogResult.Yes;
		}

		/// <summary>
		/// Determines whether the pending custom IDE list differs from the initially loaded entries.
		/// </summary>
		/// <returns><c>true</c> if entries were added, removed, or modified; otherwise <c>false</c>.</returns>
		private bool HaveCustomEntriesChanged()
		{
			if (this.initialCustomIdes.Count != this.pendingCustomIdes.Count)
			{
				return true;
			}

			for (int i = 0; i < this.initialCustomIdes.Count; i++)
			{
				CustomVsCodeIdeEntry initial = this.initialCustomIdes[i];
				CustomVsCodeIdeEntry pending = this.pendingCustomIdes[i];

				if (!string.Equals(initial.ExecutablePath, pending.ExecutablePath, StringComparison.OrdinalIgnoreCase)
					|| !string.Equals(initial.DisplayName, pending.DisplayName, StringComparison.Ordinal)
					|| !string.Equals(initial.UserDataDirectory, pending.UserDataDirectory, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Enables <see cref="btnRemoveVsCodeIde"/> only when a <c>Manual</c> entry is selected.
		/// </summary>
		private void UpdateRemoveButtonState()
		{
			this.btnRemoveVsCodeIde.Enabled = TryGetSelectedListRow(out VsCodeIdeListRow? selectedRow) && selectedRow.IsManual;
		}

		/// <summary>
		/// Retrieves metadata for the currently selected row in <see cref="lvVsCodeIdes"/>.
		/// </summary>
		/// <param name="row">The selected row metadata when available.</param>
		/// <returns><c>true</c> when a row is selected; otherwise <c>false</c>.</returns>
		private bool TryGetSelectedListRow([NotNullWhen(true)] out VsCodeIdeListRow? row)
		{
			if (this.lvVsCodeIdes.SelectedItems.Count > 0 && this.lvVsCodeIdes.SelectedItems[0].Tag is VsCodeIdeListRow taggedRow)
			{
				row = taggedRow;

				return true;
			}

			row = null;

			return false;
		}

		/// <summary>
		/// Canonicalizes a filesystem path via <see cref="Path.GetFullPath(string)"/>, returning <c>null</c> on invalid input.
		/// </summary>
		/// <param name="path">The path to normalize.</param>
		/// <returns>The canonical path, or <c>null</c> when invalid.</returns>
		private static string? TryNormalizePath(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			try
			{
				return Path.GetFullPath(path.Trim());
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				Debug.WriteLine(ex);

				return null;
			}
		}

		/// <summary>
		/// Tracks whether a row in <see cref="lvVsCodeIdes"/> represents a manual custom entry and stores its canonical path.
		/// </summary>
		private sealed record VsCodeIdeListRow(bool IsManual, string CanonicalPath);
	}
}
