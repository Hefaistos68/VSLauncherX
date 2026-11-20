using System.ComponentModel;
using System.Diagnostics;

using VSLauncher.DataModel;
using VSLauncher.Forms;
using VSLauncher.Helpers;
using VSLauncher.Ide;

namespace VSLauncher
{
	/// <summary>
	/// Configures launch settings for a Visual Studio Code project, workspace, or folder item.
	/// </summary>
	public partial class dlgExecuteVsCode : Form
	{
		private const string DefaultOptionText   = "<default>";
		private const string VsCodeCliDocsUrl    = "https://code.visualstudio.com/docs/editor/command-line";
		private const string WorkspaceFileFilter = "VS Code Workspaces (*.code-workspace)|*.code-workspace|All files (*.*)|*.*";

		private readonly IdeManager ideManager;
		private readonly VsOptions pendingBeforeAfterOptions;
		private string? missingEditionIdentifier;
		private string? initialPath;
		private bool isLoading;

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVsCode"/> class.
		/// </summary>
		/// <param name="item">The <see cref="VsCodeProject"/> or <see cref="VsFolder"/> item to configure.</param>
		/// <param name="ideManager">The IDE manager providing installed VS Code editions and profiles.</param>
		public dlgExecuteVsCode(VsItem item, IdeManager ideManager)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(ideManager);

			InitializeComponent();
			ThemeHelper.ApplyCurrentTheme(this);

			this.Item                      = item;
			this.ideManager                = ideManager;
			this.pendingBeforeAfterOptions = new VsOptions
			{
				RunBefore = item.RunBefore?.Clone(),
				RunAfter  = item.RunAfter?.Clone()
			};
		}

		/// <summary>
		/// Gets the configured item.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public VsItem Item { get; }

		/// <summary>
		/// Populates controls from <see cref="Item"/> and discovered VS Code installations on form load.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void dlgExecuteVsCode_Load(object? sender, EventArgs e)
		{
			this.isLoading = true;

			try
			{
				this.initialPath           = this.Item.Path;
				this.txtName.Text          = this.Item.Name ?? string.Empty;
				this.txtPath.Text          = this.Item.Path ?? string.Empty;
				this.chkAdmin.Checked      = this.Item.RunAsAdmin;
				this.btnBrowseFile.Enabled = this.Item is not VsFolder;

				SetupMultiMonitor();
				LoadLaunchOptions();
				UpdateBeforeAfterButtonState();
			}
			finally
			{
				this.isLoading = false;
			}
		}

		/// <summary>
		/// Repopulates available profiles when the selected VS Code edition changes while preserving typed profile text.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void cbxVsCodeEdition_SelectedIndexChanged(object? sender, EventArgs e)
		{
			if (this.isLoading)
			{
				return;
			}

			if (this.cbxVsCodeEdition.SelectedIndex >= 0)
			{
				this.missingEditionIdentifier = null;
			}

			UpdateMissingEditionHint();
			PopulateProfiles();
		}

		/// <summary>
		/// Persists edited settings back to <see cref="Item"/> when the user confirms the dialog.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnOk_Click(object? sender, EventArgs e)
		{
			string trimmedPath = this.txtPath.Text.Trim();

			if (this.Item is VsFolder && IsInvalidFolderFilePath(trimmedPath))
			{
				_ = MessageBox.Show(
					this,
					"A folder item cannot target a file. Enter a directory path or leave the path empty.",
					"Invalid Folder Path",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);

				this.DialogResult = DialogResult.None;
				this.txtPath.Focus();

				return;
			}

			string? edition        = ResolveSelectedEdition();
			string? profile        = ResolveProfileValue(this.cbxProfile.Text);
			VsCodeWindowMode mode  = GetSelectedWindowMode();
			bool disableExtensions = this.chkDisableExtensions.Checked;
			string? commands       = string.IsNullOrWhiteSpace(this.txtCommand.Text) ? null : this.txtCommand.Text.Trim();

			this.Item.Name             = this.txtName.Text.Trim();
			this.Item.Path             = trimmedPath;
			this.Item.RunAsAdmin       = this.chkAdmin.Checked;
			this.Item.PreferredMonitor = this.cbxMonitors.SelectedIndex > 0 ? this.cbxMonitors.SelectedIndex - 1 : null;
			this.Item.RunBefore        = this.pendingBeforeAfterOptions.RunBefore;
			this.Item.RunAfter         = this.pendingBeforeAfterOptions.RunAfter;

			SaveToolSpecificOptions(edition, profile, mode, disableExtensions, commands);
		}

		/// <summary>
		/// Opens a file dialog to choose a <c>.code-workspace</c> file.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnBrowseFile_Click(object? sender, EventArgs e)
		{
			using var openFileDialog = new OpenFileDialog
			{
				Filter           = WorkspaceFileFilter,
				FilterIndex      = 1,
				CheckFileExists  = true,
				Multiselect      = false,
				InitialDirectory = ResolveInitialDirectory(),
				RestoreDirectory = true
			};

			if (openFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			string filePath    = openFileDialog.FileName;
			string? folderPath = Path.GetDirectoryName(filePath);

			this.txtPath.Text = filePath;

			if (!string.IsNullOrEmpty(folderPath))
			{
				Properties.Settings.Default.LastExecuteFolder = folderPath;
			}

			AutoFillNameIfEmpty(filePath, isFile: true);

			if (filePath.EndsWith(".code-workspace", StringComparison.OrdinalIgnoreCase))
			{
				SelectDefaultVsCodeEditionIfUnset();
			}
		}

		/// <summary>
		/// Automatically selects the default installed VS Code edition when the edition selector is currently set to <c>&lt;default&gt;</c>.
		/// </summary>
		private void SelectDefaultVsCodeEditionIfUnset()
		{
			if (!this.cbxVsCodeEdition.IsDefaultSelected)
			{
				return;
			}

			string? defaultEditionId = this.ideManager.ResolvePreferredIdentifier(IdeType.VsCode);

			if (!string.IsNullOrWhiteSpace(defaultEditionId))
			{
				this.cbxVsCodeEdition.SelectByIdentifier(defaultEditionId);
			}
		}

		/// <summary>
		/// Opens a folder browser dialog to choose a VS Code project directory.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnBrowseFolder_Click(object? sender, EventArgs e)
		{
			using var folderBrowserDialog = new FolderBrowserDialog
			{
				ShowNewFolderButton = false,
				SelectedPath        = ResolveInitialDirectory()
			};

			if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			string folderPath = folderBrowserDialog.SelectedPath;

			this.txtPath.Text                             = folderPath;
			Properties.Settings.Default.LastExecuteFolder = folderPath;

			AutoFillNameIfEmpty(folderPath, isFile: false);
		}

		/// <summary>
		/// Updates dependent button states when <see cref="txtPath"/> changes.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void txtPath_TextChanged(object? sender, EventArgs e)
		{
			UpdateBeforeAfterButtonState();
		}

		/// <summary>
		/// Opens the VS Code command-line documentation in the default browser.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The link clicked event arguments.</param>
		private void lnkCommandsHelp_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
		{
			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName        = VsCodeCliDocsUrl,
					UseShellExecute = true
				};

				using Process? process = Process.Start(startInfo);
			}
			catch (Win32Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Opens the Before/After task configuration dialog for the item.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnBeforeAfter_Click(object? sender, EventArgs e)
		{
			string title = !string.IsNullOrWhiteSpace(this.txtName.Text)
				? this.txtName.Text.Trim()
				: (this.Item.Name ?? string.Empty);

			using var dlg = new dlgBeforeAfter(this.pendingBeforeAfterOptions, title);

			dlg.ShowDialog(this);
		}

		/// <summary>
		/// Displays a temporary ping overlay on the currently selected monitor.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnPingMonitor_Click(object? sender, EventArgs e)
		{
			if (this.cbxMonitors.SelectedIndex <= 0)
			{
				return;
			}

			int monitor  = this.cbxMonitors.SelectedIndex - 1;
			var pingForm = new frmPing(monitor);

			pingForm.Show();
		}

		/// <summary>
		/// Populates the monitor dropdown and enables multi-monitor controls when multiple displays are attached.
		/// </summary>
		private void SetupMultiMonitor()
		{
			Screen[] monitors        = Screen.AllScreens;
			bool hasMultipleMonitors = monitors.Length > 1;

			this.cbxMonitors.Items.Clear();
			this.cbxMonitors.Items.Add(DefaultOptionText);
			this.cbxMonitors.Items.AddRange(monitors.Select(m => (object)m.DeviceName).ToArray());

			int monitorIndex = this.Item.PreferredMonitor.HasValue ? this.Item.PreferredMonitor.Value + 1 : 0;

			this.cbxMonitors.SelectedIndex = (monitorIndex >= 0 && monitorIndex < this.cbxMonitors.Items.Count) ? monitorIndex : 0;
			this.cbxMonitors.Enabled       = hasMultipleMonitors;
			this.btnPingMonitor.Enabled    = hasMultipleMonitors;
		}

		/// <summary>
		/// Loads edition, profile, window mode, disable-extensions, and command-line arguments from <see cref="Item"/>.
		/// </summary>
		private void LoadLaunchOptions()
		{
			(string? edition, string? profile, VsCodeWindowMode windowMode, bool disableExtensions, string? commands) = ReadItemLaunchOptions();

			this.cbxVsCodeEdition.ShowDefault = true;
			this.cbxVsCodeEdition.LoadInstances(this.ideManager.GetInstances(IdeType.VsCode));
			this.cbxVsCodeEdition.SelectByIdentifier(edition);

			this.missingEditionIdentifier = !string.IsNullOrWhiteSpace(edition) && this.cbxVsCodeEdition.SelectedIndex == -1
				? edition
				: null;

			UpdateMissingEditionHint();
			PopulateProfiles(profile);
			SelectWindowMode(windowMode);

			this.chkDisableExtensions.Checked = disableExtensions;
			this.txtCommand.Text              = commands ?? string.Empty;
		}

		/// <summary>
		/// Reads VS Code launch options from <see cref="Item"/> according to its concrete runtime type.
		/// </summary>
		/// <returns>The tuple of edition, profile, window mode, disable-extensions flag, and command arguments.</returns>
		private (string? Edition, string? Profile, VsCodeWindowMode WindowMode, bool DisableExtensions, string? Commands) ReadItemLaunchOptions()
		{
			if (this.Item is VsCodeProject project)
			{
				return (project.VsVersion, project.Instance, project.WindowMode, project.DisableExtensions, project.Commands);
			}

			if (this.Item is VsFolder folder)
			{
				VsCodeLaunchOptions? options = folder.VsCodeOptions;

				return (
					options?.Edition,
					options?.Profile,
					options?.WindowMode ?? VsCodeWindowMode.Default,
					options?.DisableExtensions ?? false,
					options?.Commands);
			}

			return (this.Item.VsVersion, this.Item.Instance, VsCodeWindowMode.Default, false, this.Item.Commands);
		}

		/// <summary>
		/// Writes VS Code launch options back to <see cref="Item"/> according to its concrete runtime type.
		/// </summary>
		/// <param name="edition">The selected VS Code edition identifier, or <c>null</c> for default.</param>
		/// <param name="profile">The selected or entered profile name, or <c>null</c> for default.</param>
		/// <param name="windowMode">The selected window mode.</param>
		/// <param name="disableExtensions">Whether extensions should be disabled.</param>
		/// <param name="commands">Additional command-line arguments.</param>
		private void SaveToolSpecificOptions(
			string? edition,
			string? profile,
			VsCodeWindowMode windowMode,
			bool disableExtensions,
			string? commands)
		{
			if (this.Item is VsCodeProject project)
			{
				UpdateProjectKindIfTargetTypeChanged(project);

				project.VsVersion         = edition ?? string.Empty;
				project.Instance          = profile;
				project.WindowMode        = windowMode;
				project.DisableExtensions = disableExtensions;
				project.Commands          = commands;

				project.Refresh();

				return;
			}

			if (this.Item is VsFolder folder)
			{
				folder.IdeType       = IdeType.VsCode;
				folder.VsCodeOptions ??= new VsCodeLaunchOptions();

				folder.VsCodeOptions.Edition           = edition;
				folder.VsCodeOptions.Profile           = profile;
				folder.VsCodeOptions.WindowMode        = windowMode;
				folder.VsCodeOptions.DisableExtensions = disableExtensions;
				folder.VsCodeOptions.Commands          = commands;

				folder.Refresh();

				return;
			}

			this.Item.VsVersion = edition ?? string.Empty;
			this.Item.Instance  = profile;
			this.Item.Commands  = commands;
		}

		/// <summary>
		/// Synchronizes <see cref="VsCodeProject.ProjectKind"/> and <see cref="VsCodeProject.ManifestFile"/> when the target path switches between a workspace file and a folder.
		/// </summary>
		/// <param name="project">The VS Code project being saved.</param>
		private void UpdateProjectKindIfTargetTypeChanged(VsCodeProject project)
		{
			if (string.IsNullOrWhiteSpace(project.Path))
			{
				return;
			}

			if (project.Path.EndsWith(".code-workspace", StringComparison.OrdinalIgnoreCase))
			{
				project.ProjectKind  = VsCodeProjectKind.Workspace;
				project.ManifestFile = null;

				return;
			}

			bool pathChanged = !string.Equals(this.initialPath, project.Path, StringComparison.OrdinalIgnoreCase);

			if (Directory.Exists(project.Path) && (project.ProjectKind == VsCodeProjectKind.Workspace || pathChanged))
			{
				VsCodeProjectKind? detectedKind = ProjectScanner.DetectDirectoryManifest(project.Path, out string? manifestFile);

				project.ProjectKind  = detectedKind ?? VsCodeProjectKind.Folder;
				project.ManifestFile = manifestFile;
			}
		}

		/// <summary>
		/// Updates visibility, text, and warning color of <see cref="lblMissingEdition"/> when the saved edition is not installed.
		/// </summary>
		private void UpdateMissingEditionHint()
		{
			bool hasMissing = !string.IsNullOrWhiteSpace(this.missingEditionIdentifier);

			this.lblMissingEdition.Visible = hasMissing;

			if (!hasMissing)
			{
				this.lblMissingEdition.Text = string.Empty;
				this.toolTip1.SetToolTip(this.lblMissingEdition, null);

				return;
			}

			string message = $"Edition '{this.missingEditionIdentifier}' not found";

			this.lblMissingEdition.Text      = message;
			this.lblMissingEdition.ForeColor = Properties.Settings.Default.DarkMode
				? Color.FromArgb(255, 175, 90)
				: Color.FromArgb(196, 82, 0);
			this.toolTip1.SetToolTip(this.lblMissingEdition, message);
		}

		/// <summary>
		/// Determines whether <paramref name="trimmedPath"/> points to an existing file or looks like a file path instead of a directory.
		/// </summary>
		/// <param name="trimmedPath">The trimmed path text entered by the user.</param>
		/// <returns><c>true</c> if the path represents a file and should be rejected for a <see cref="VsFolder"/>; otherwise <c>false</c>.</returns>
		private static bool IsInvalidFolderFilePath(string trimmedPath)
		{
			if (string.IsNullOrWhiteSpace(trimmedPath) || Directory.Exists(trimmedPath))
			{
				return false;
			}

			return File.Exists(trimmedPath)
				|| (!Path.EndsInDirectorySeparator(trimmedPath) && Path.HasExtension(trimmedPath));
		}

		/// <summary>
		/// Repopulates <see cref="cbxProfile"/> from the selected or default VS Code instance while preserving any typed or loaded profile text.
		/// </summary>
		/// <param name="preferredProfileText">Optional explicit profile text to restore instead of the current combobox text.</param>
		private void PopulateProfiles(string? preferredProfileText = null)
		{
			string currentText    = preferredProfileText ?? this.cbxProfile.Text;
			List<string> profiles = ResolveAvailableProfiles();
			string targetText     = string.IsNullOrWhiteSpace(currentText) ? DefaultOptionText : currentText.Trim();
			int matchIndex        = FindProfileIndex(profiles, targetText);

			this.cbxProfile.BeginUpdate();

			try
			{
				this.cbxProfile.Items.Clear();
				this.cbxProfile.Items.AddRange(profiles.Cast<object>().ToArray());

				if (matchIndex >= 0)
				{
					this.cbxProfile.SelectedIndex = matchIndex;
				}
				else
				{
					this.cbxProfile.SelectedIndex = -1;
					this.cbxProfile.Text          = targetText;
				}
			}
			finally
			{
				this.cbxProfile.EndUpdate();
			}
		}

		/// <summary>
		/// Retrieves the profile list for the selected VS Code edition, falling back to the default VS Code instance or <c>&lt;default&gt;</c>.
		/// </summary>
		/// <returns>A non-empty list of profile names.</returns>
		private List<string> ResolveAvailableProfiles()
		{
			List<string>? profiles = this.cbxVsCodeEdition.SelectedIdeInstance?.GetProfiles()
				?? this.ideManager.GetProvider(IdeType.VsCode)?.DefaultInstance?.GetProfiles();

			if (profiles is null || profiles.Count == 0)
			{
				return new List<string> { DefaultOptionText };
			}

			return new List<string>(profiles);
		}

		/// <summary>
		/// Finds the index of <paramref name="targetProfile"/> within <paramref name="profiles"/> using case-insensitive comparison.
		/// </summary>
		/// <param name="profiles">The available profile names.</param>
		/// <param name="targetProfile">The profile name to match.</param>
		/// <returns>The zero-based index, or <c>-1</c> if not found.</returns>
		private static int FindProfileIndex(IReadOnlyList<string> profiles, string targetProfile)
		{
			for (int i = 0; i < profiles.Count; i++)
			{
				if (string.Equals(profiles[i], targetProfile, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Resolves the selected VS Code edition identifier, returning <c>null</c> when <c>&lt;default&gt;</c> is selected.
		/// </summary>
		/// <returns>The edition identifier, or <c>null</c> for default selection.</returns>
		private string? ResolveSelectedEdition()
		{
			if (this.cbxVsCodeEdition.IsDefaultSelected)
			{
				return null;
			}

			if (this.cbxVsCodeEdition.SelectedIdeInstance is not null)
			{
				return this.cbxVsCodeEdition.SelectedIdeInstance.Identifier;
			}

			return this.missingEditionIdentifier;
		}

		/// <summary>
		/// Normalizes profile input, converting blank or <c>&lt;default&gt;</c> values to <c>null</c>.
		/// </summary>
		/// <param name="profileText">The raw text from <see cref="cbxProfile"/>.</param>
		/// <returns>The trimmed profile name, or <c>null</c> for the default profile.</returns>
		private static string? ResolveProfileValue(string? profileText)
		{
			if (string.IsNullOrWhiteSpace(profileText))
			{
				return null;
			}

			string trimmed = profileText.Trim();

			if (string.Equals(trimmed, DefaultOptionText, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			return trimmed;
		}

		/// <summary>
		/// Selects the dropdown entry matching <paramref name="mode"/> in <see cref="cbxWindowMode"/>.
		/// </summary>
		/// <param name="mode">The window mode to select.</param>
		private void SelectWindowMode(VsCodeWindowMode mode)
		{
			int index = mode switch
			{
				VsCodeWindowMode.NewWindow   => 1,
				VsCodeWindowMode.ReuseWindow => 2,
				_                            => 0
			};

			if (this.cbxWindowMode.Items.Count > index)
			{
				this.cbxWindowMode.SelectedIndex = index;
			}
		}

		/// <summary>
		/// Converts the current <see cref="cbxWindowMode"/> selection to <see cref="VsCodeWindowMode"/>.
		/// </summary>
		/// <returns>The selected <see cref="VsCodeWindowMode"/> value.</returns>
		private VsCodeWindowMode GetSelectedWindowMode()
		{
			return this.cbxWindowMode.SelectedIndex switch
			{
				1 => VsCodeWindowMode.NewWindow,
				2 => VsCodeWindowMode.ReuseWindow,
				_ => VsCodeWindowMode.Default
			};
		}

		/// <summary>
		/// Enables <see cref="btnBeforeAfter"/> when a target path is entered or the edited item is a <see cref="VsFolder"/>.
		/// </summary>
		private void UpdateBeforeAfterButtonState()
		{
			this.btnBeforeAfter.Enabled = this.Item is VsFolder || !string.IsNullOrWhiteSpace(this.txtPath.Text);
		}

		/// <summary>
		/// Resolves an existing initial directory from <see cref="txtPath"/> or <see cref="Properties.Settings.LastExecuteFolder"/>.
		/// </summary>
		/// <returns>An existing directory path, or an empty string if none is available.</returns>
		private string ResolveInitialDirectory()
		{
			string currentPath = this.txtPath.Text.Trim();

			if (!string.IsNullOrEmpty(currentPath))
			{
				if (Directory.Exists(currentPath))
				{
					return currentPath;
				}

				string? parentDir = TryGetDirectoryName(currentPath);

				if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
				{
					return parentDir;
				}
			}

			string lastFolder = Properties.Settings.Default.LastExecuteFolder;

			if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
			{
				return lastFolder;
			}

			return string.Empty;
		}

		/// <summary>
		/// Safely extracts the parent directory from a candidate path string.
		/// </summary>
		/// <param name="path">The file or directory path.</param>
		/// <returns>The directory name, or <c>null</c> if unavailable or invalid.</returns>
		private static string? TryGetDirectoryName(string path)
		{
			try
			{
				return Path.GetDirectoryName(path);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		/// <summary>
		/// Populates <see cref="txtName"/> from the selected file or folder path when no name has been entered yet.
		/// </summary>
		/// <param name="selectedPath">The newly picked workspace file or folder path.</param>
		/// <param name="isFile"><c>true</c> if <paramref name="selectedPath"/> is a file; <c>false</c> if it is a directory.</param>
		private void AutoFillNameIfEmpty(string selectedPath, bool isFile)
		{
			if (!string.IsNullOrWhiteSpace(this.txtName.Text))
			{
				return;
			}

			string candidateName = isFile
				? Path.GetFileNameWithoutExtension(selectedPath)
				: Path.GetFileName(Path.TrimEndingDirectorySeparator(selectedPath));

			if (string.IsNullOrWhiteSpace(candidateName))
			{
				candidateName = Path.GetFileName(selectedPath);
			}

			if (string.IsNullOrWhiteSpace(candidateName))
			{
				candidateName = selectedPath;
			}

			this.txtName.Text = candidateName;
		}
	}
}
