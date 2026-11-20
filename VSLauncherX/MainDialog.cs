using System.ComponentModel;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Taskbar;
using BrightIdeasSoftware;
using ThemePalette = VSLauncher.Helpers.ThemeHelper.ThemePalette;
using Newtonsoft.Json;

using VSLauncher.DataModel;
using VSLauncher.Forms;
using VSLauncher.Helpers;
using VSLauncher.Ide;

using VSLXshared.Helpers;
using System.Reflection;
using LibGit2Sharp;
using System.Runtime.InteropServices;

namespace VSLauncher
{
	/// <summary>
	/// The main dialog.
	/// </summary>
	public partial class MainDialog : Form
	{
		/// <summary>
		/// used to indicate that some internal update is going on
		/// </summary>
		private bool bInUpdate;

		private readonly VisualStudioInstanceManager visualStudioInstances = new VisualStudioInstanceManager();
		private readonly IdeManager ideManager                             = new IdeManager();
		private readonly VsCodeUpdateService vsCodeUpdateService           = new VsCodeUpdateService();
		private readonly Dictionary<string, VsCodeUpdateInfo> vsCodeUpdateStates = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);
		private readonly CancellationTokenSource vsCodeUpdateCts           = new CancellationTokenSource();
		private DescribedTaskRenderer? itemRenderer;
		private VsFolder solutionGroups = new VsFolder();
		private IDataObject? lastDroppedData;
		private bool solutionDataLoadFailed;
		private bool isUpdatingTreeExpansion;
		private int gitRefreshInProgress;
		private int gitRemoteOperationInProgress;
		private int vsCodeUpdateCheckInProgress;
		private int vsCodeUpdateInstallInProgress;
		private Bitmap? currentVsButtonBitmap;
		private Bitmap? currentVsCodeButtonBitmap;

		public JumpList? TaskbarJumpList { get; private set; }

		private ThemePalette currentTheme;


		/// <summary>
		/// Initializes the listview.
		/// </summary>
		/// <param name="list">The list.</param>
		private void InitializeListview()
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 56;
			this.olvFiles.UseHotItem = false;
			this.olvFiles.OwnerDraw = true;

			// Add a more interesting focus for editing operations this.olvFiles.AddDecoration(new EditingCellBorderDecoration(true));
			this.olvFiles.TreeColumnRenderer.IsShowLines = true;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;
			this.olvFiles.TreeColumnRenderer.CornerRoundness = 0;
			this.olvFiles.TreeColumnRenderer.FillBrush = new SolidBrush(Color.CornflowerBlue);
			this.olvFiles.TreeColumnRenderer.FramePen = new Pen(Color.DarkBlue, 1);

			this.olvFiles.UseFilterIndicator = true;

			// The following line makes getting aspect about 10x faster. Since getting the aspect is the slowest part of
			// building the ListView, it is worthwhile BUT NOT NECESSARY to do.
			TypedObjectListView<VsItem> tlist = new TypedObjectListView<VsItem>(this.olvFiles);
			tlist.GenerateAspectGetters();

			// setup the files list
			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is VsFolder;
			};

			this.olvFiles.Expanded += delegate (object? sender, TreeBranchExpandedEventArgs e)
			{
				if (!this.isUpdatingTreeExpansion && e.Model is VsFolder sg)
				{
					sg.Expanded = true;
				}
			};

			this.olvFiles.Collapsed += delegate (object? sender, TreeBranchCollapsedEventArgs e)
			{
				if (!this.isUpdatingTreeExpansion && e.Model is VsFolder sg)
				{
					sg.Expanded = false;
				}
			};

			this.olvFiles.ParentGetter = delegate (object x)
			{
				VsFolder? parent = this.solutionGroups.FindParent(x);

				return ReferenceEquals(parent, this.solutionGroups) ? null : parent;
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is VsFolder sg ? sg.Items : (System.Collections.IEnumerable?)null;
			};

			this.olvFiles.BeforeSearching += olvFiles_BeforeSearching;

			this.itemRenderer = CreateDescribedRenderer();

			this.olvFiles.TreeColumnRenderer.SubRenderer = this.itemRenderer;
			this.olvColumnFilename.AspectName = "Name";
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForFile;
			this.olvColumnFilename.CellPadding = new Rectangle(4, 2, 4, 2);

			// // // setup the Path column // this.olvColumnPath.AspectGetter = ColumnHelper.GetAspectForPath;
			// this.olvColumnPath.Hideable = false; this.olvColumnPath.UseFiltering = false;

			// setup the Git column
			ImageRenderer ir = new ImageRenderer();
			this.olvColumnGit.Renderer = ir;
			this.olvColumnGit.AspectGetter = ColumnHelper.GetAspectForGit;
			this.olvColumnGit.ImageGetter = ColumnHelper.GetImageForGit;

			this.olvColumnGitName.AspectGetter = ColumnHelper.GetAspectForGitName;

			// setup the Date column
			this.olvColumnDate.AspectGetter = ColumnHelper.GetAspectForDate;

			// setup the Version column
			this.olvColumnVersion.AspectGetter = rowObject => GetVersionColumnAspect(rowObject);

			// setup the Options column
			this.olvColumnOptions.AspectGetter = ColumnHelper.GetAspectForOptions;
			this.olvColumnOptions.ToolTipText = "";

			// Show the attributes for this object A FlagRenderer masks off various values and draws zero or more images
			// based on the presence of individual bits.
			FlagRenderer attributesRenderer = new FlagRenderer
			{
				ImageList = this.imageList3
			};
			attributesRenderer.Add(OptionsEnum.RunBeforeOn, "RunBefore");
			attributesRenderer.Add(OptionsEnum.RunBeforeOff, "None");
			attributesRenderer.Add(OptionsEnum.RunAsAdminOn, "RunAsAdmin");
			attributesRenderer.Add(OptionsEnum.RunAsAdminOff, "None");
			attributesRenderer.Add(OptionsEnum.RunAfterOn, "RunAfter");
			attributesRenderer.Add(OptionsEnum.RunAfterOff, "None");

			this.olvColumnOptions.Renderer = attributesRenderer;

			// Tell the filtering subsystem that the attributes column is a collection of flags
			this.olvColumnOptions.ClusteringStrategy = new FlagClusteringStrategy(typeof(OptionsEnum));

			// this.olvFiles.SetObjects(list);
		}

		[DllImport("shell32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsUserAnAdmin();

		/// <summary>
		/// Initializes a new instance of the <see cref="MainDialog"/> class.
		/// </summary>
		public MainDialog()
		{
			InitializeComponent();
			InitializeListview();
			SetupDragAndDrop();
			ApplyTheme(Properties.Settings.Default.DarkMode);

			bool bAdmin = AdminInfo.IsCurrentUserAdmin();
			bool bElevated = AdminInfo.IsElevated();

			if (bAdmin || bElevated)
			{
				if (bAdmin && bElevated)
					this.Text += $" (Administrator, Elevated)";
				else if (bAdmin)
					this.Text += $" (Administrator)";
				else
					this.Text += $" (Elevated)";
			}

			this.bInUpdate = true;

			try
			{
				InitializeVisualStudioSelector();
				InitializeVsCodeSelector();
				ApplyInstalledIdeState();
			}
			finally
			{
				this.bInUpdate = false;
			}

			LoadSolutionData();

			this.solutionGroups.Items.OnChanged += SolutionData_OnChanged;
			UpdateList(false);
		}

		/// <summary>
		/// Formats the Version / IDE column text for a row in <see cref="olvFiles"/>.
		/// </summary>
		/// <param name="rowObject">The row model object.</param>
		/// <returns>The version or IDE label, or <c>null</c> when <paramref name="rowObject"/> is not a <see cref="VsItem"/>.</returns>
		private string? GetVersionColumnAspect(object rowObject)
		{
			if (rowObject is VsCodeProject codeProject)
			{
				return FormatVsCodeEditionLabel(codeProject.VsVersion, "<default>");
			}

			if (rowObject is VsFolder folder && folder.IsVsCodeLaunchTarget(allowDiskProbe: false) && this.ideManager.HasInstalled(IdeType.VsCode))
			{
				return FormatVsCodeEditionLabel(folder.VsCodeOptions?.Edition, "VS Code");
			}

			if (rowObject is VsItem item)
			{
				if (string.IsNullOrEmpty(item.VsVersion))
				{
					return "<default>";
				}

				VisualStudioInstance? vs = this.visualStudioInstances.GetByIdentifier(item.VsVersion);

				if (vs is null)
				{
					return "<default>";
				}

				return $"{vs.Year} ({vs.ShortVersion})";
			}

			return null;
		}

		/// <summary>
		/// Resolves the short display label for a pinned VS Code edition identifier or returns a fallback label.
		/// </summary>
		/// <param name="editionIdentifier">The pinned VS Code instance identifier, if any.</param>
		/// <param name="fallback">The fallback label when unpinned or not installed.</param>
		/// <returns>The edition's short name or <paramref name="fallback"/>.</returns>
		private string FormatVsCodeEditionLabel(string? editionIdentifier, string fallback)
		{
			if (string.IsNullOrWhiteSpace(editionIdentifier))
			{
				return fallback;
			}

			return this.ideManager.GetProvider(IdeType.VsCode)?.FindByIdentifier(editionIdentifier)?.ShortName ?? fallback;
		}

		/// <summary>
		/// Returns the currently selected default instance targets from the Visual Studio and VS Code sidebar selectors.
		/// </summary>
		/// <returns>An array of configured <see cref="IdeDefaultTarget"/> entries.</returns>
		internal IdeDefaultTarget[] GetCurrentDefaultTargets()
		{
			var targets = new List<IdeDefaultTarget>(2);

			if (this.selectVisualStudioVersion.SelectedItem is VisualStudioInstance vs && !string.IsNullOrWhiteSpace(vs.Identifier))
			{
				targets.Add(new IdeDefaultTarget(IdeType.VisualStudio, vs.Identifier));
			}

			if (this.selectVsCodeEdition.SelectedIdeInstance is IIdeInstance vsCode && !string.IsNullOrWhiteSpace(vsCode.Identifier))
			{
				targets.Add(new IdeDefaultTarget(IdeType.VsCode, vsCode.Identifier));
			}

			return targets.ToArray();
		}

		/// <summary>
		/// Restores the previously selected Visual Studio instance or selects the first installed instance.
		/// </summary>
		private void InitializeVisualStudioSelector()
		{
			if (this.selectVisualStudioVersion.Items.Count <= 0)
			{
				return;
			}

			string savedVsVersion = Properties.Settings.Default.SelectedVSversion;

			if (!string.IsNullOrEmpty(savedVsVersion))
			{
				VisualStudioInstance? savedInstance = this.selectVisualStudioVersion.Versions.FirstOrDefault(v => v.Identifier == savedVsVersion);

				if (savedInstance is not null)
				{
					this.selectVisualStudioVersion.SelectedItem = savedInstance;

					return;
				}
			}

			this.selectVisualStudioVersion.SelectedIndex = 0;
		}

		/// <summary>
		/// Populates the VS Code edition selector and restores the saved edition or default instance.
		/// </summary>
		private void InitializeVsCodeSelector()
		{
			IReadOnlyList<IIdeInstance> vsCodeInstances = this.ideManager.GetInstances(IdeType.VsCode);

			this.selectVsCodeEdition.LoadInstances(vsCodeInstances);

			if (this.selectVsCodeEdition.Items.Count <= 0)
			{
				return;
			}

			string savedEdition = Properties.Settings.Default.SelectedVsCodeEdition;

			if (!string.IsNullOrWhiteSpace(savedEdition))
			{
				this.selectVsCodeEdition.SelectByIdentifier(savedEdition);
			}

			if (this.selectVsCodeEdition.SelectedIndex >= 0)
			{
				return;
			}

			IIdeInstance? defaultInstance = this.ideManager.GetProvider(IdeType.VsCode)?.DefaultInstance;

			if (defaultInstance is not null)
			{
				this.selectVsCodeEdition.SelectedIdeInstance = defaultInstance;
			}
			else
			{
				this.selectVsCodeEdition.SelectedIndex = 0;
			}
		}

		/// <summary>
		/// Enables or disables sidebar controls and selects the initial IDE tab based on installed IDEs.
		/// </summary>
		private void ApplyInstalledIdeState()
		{
			bool hasVs   = this.ideManager.HasInstalled(IdeType.VisualStudio);
			bool hasCode = this.ideManager.HasInstalled(IdeType.VsCode);

			SetVisualStudioSidebarEnabled(hasVs);
			SetVsCodeSidebarEnabled(hasCode);

			TabPage targetTab     = ResolveInitialIdeTab(hasVs, hasCode);
			bool previousInUpdate = this.bInUpdate;

			this.bInUpdate = true;

			try
			{
				this.tabControlIdes.SelectedTab = targetTab;
			}
			finally
			{
				this.bInUpdate = previousInUpdate;
			}
		}

		/// <summary>
		/// Enables or disables the Visual Studio selector and launch action buttons while keeping the installer button active.
		/// </summary>
		/// <param name="enabled"><c>true</c> if Visual Studio is installed; otherwise <c>false</c>.</param>
		private void SetVisualStudioSidebarEnabled(bool enabled)
		{
			this.selectVisualStudioVersion.Enabled = enabled;
			this.btnMainStartVisualStudio1.Enabled = enabled;
			this.btnMainStartVisualStudio2.Enabled = enabled;
			this.btnMainStartVisualStudio3.Enabled = enabled;
			this.btnMainStartVisualStudio4.Enabled = enabled;
			this.btnMainStartVisualStudio5.Enabled = enabled;
			this.btnMainOpenActivityLog.Enabled    = enabled;
		}

		/// <summary>
		/// Enables or disables VS Code quick-launch controls and refreshes the VS Code installer/update button state.
		/// </summary>
		/// <param name="enabled"><c>true</c> if VS Code is installed; otherwise <c>false</c>.</param>
		private void SetVsCodeSidebarEnabled(bool enabled)
		{
			this.selectVsCodeEdition.Enabled      = enabled;
			this.btnVsCodeStart.Enabled           = enabled;
			this.btnVsCodeStartAdmin.Enabled      = enabled;
			this.btnVsCodeStartNewWindow.Enabled  = enabled;
			this.btnVsCodeStartNoExt.Enabled      = enabled;
			this.btnVsCodeOpenActivityLog.Enabled = enabled;
			this.btnVsCodeInstaller.Visible       = true;

			UpdateVsCodeInstallerButtonState();
		}

		/// <summary>
		/// Determines which sidebar IDE tab should be active on startup.
		/// </summary>
		/// <param name="hasVs">Whether Visual Studio is installed.</param>
		/// <param name="hasCode">Whether VS Code is installed.</param>
		/// <returns>The <see cref="TabPage"/> to select.</returns>
		private TabPage ResolveInitialIdeTab(bool hasVs, bool hasCode)
		{
			if (!hasVs && hasCode)
			{
				return this.tabVsCode;
			}

			if (hasVs && !hasCode)
			{
				return this.tabVisualStudio;
			}

			if (hasVs && hasCode)
			{
				return string.Equals(Properties.Settings.Default.SelectedIdeTab, "VsCode", StringComparison.OrdinalIgnoreCase)
					? this.tabVsCode
					: this.tabVisualStudio;
			}

			return this.tabVisualStudio;
		}

		/// <summary>
		/// Handles the load event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void MainDialog_Load(object sender, EventArgs e)
		{
			if (Properties.Settings.Default.AppState == "saved")
			{
				// we don't want a minimized window at startup
				this.WindowState = Properties.Settings.Default.AppWindow == FormWindowState.Minimized ? FormWindowState.Normal : Properties.Settings.Default.AppWindow;

				Rectangle savedBounds = new Rectangle(Properties.Settings.Default.AppLocation, Properties.Settings.Default.AppSize);

				if (savedBounds.Width >= 200 && savedBounds.Height >= 200 && Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(savedBounds)))
				{
					this.Location = Properties.Settings.Default.AppLocation;
					this.Size     = Properties.Settings.Default.AppSize;
				}

				// now set the columns
				this.olvColumnDate.IsVisible = Properties.Settings.Default.ColumnDateVisible;
				this.olvColumnOptions.IsVisible = Properties.Settings.Default.ColumnOptionsVisible;

				this.olvFiles.RebuildColumns();
			}

			FindVisualStudioInstaller();
			InitializeVsCodeUpdateCheck();

			SetupTaskbarTasks();

			gitTimer.Tick += GitTimer_Tick;
			gitTimer.Interval = 5000;
			gitTimer.Start();

			_ = this.txtFilter.Focus();
		}

		/// <summary>
		/// Handles timer ticks to update Git status every 5 seconds
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GitTimer_Tick(object? sender, EventArgs e)
		{
			// Only execute if this application is the foreground window
			// Debug info for foreground window and focus conditions
			Debug.WriteLine($"Form.ActiveForm == this: {Form.ActiveForm == this}");
			Debug.WriteLine($"this.Focused: {this.Focused}");

			if (Form.ActiveForm != this)
			{
				return;
			}

			FetchGitStatusAsync(this.solutionGroups);
		}


		/// <summary>
		/// Handles the form closing, saves state
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void MainDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			Properties.Settings.Default.AppWindow = this.WindowState;
			if (this.WindowState == FormWindowState.Normal)
			{
				// save location and size if the state is normal
				Properties.Settings.Default.AppLocation = this.Location;
				Properties.Settings.Default.AppSize = this.Size;
			}
			else
			{
				// save the RestoreBounds if the form is minimized or maximized!
				Properties.Settings.Default.AppLocation = this.RestoreBounds.Location;
				Properties.Settings.Default.AppSize = this.RestoreBounds.Size;
			}

			Properties.Settings.Default.ColumnDateVisible = this.olvColumnDate.IsVisible;
			Properties.Settings.Default.ColumnOptionsVisible = this.olvColumnOptions.IsVisible;

			Properties.Settings.Default.AppState = "saved";

			// don't forget to save the settings
			Properties.Settings.Default.Save();

			if (!this.solutionDataLoadFailed)
			{
				SaveSolutionData();
			}

			this.vsCodeUpdateTimer.Stop();
			this.vsCodeUpdateCts.Cancel();
			this.vsCodeUpdateCts.Dispose();
			this.vsCodeUpdateService.Dispose();

			this.currentVsButtonBitmap?.Dispose();
			this.currentVsButtonBitmap = null;
			this.currentVsCodeButtonBitmap?.Dispose();
			this.currentVsCodeButtonBitmap = null;
		}



		#region Taskbar handling

		private void SetupTaskbarTasks()
		{
			// Create a jump list.
			try
			{
				this.TaskbarJumpList = JumpList.CreateJumpList();
			}
			catch (System.Exception ex)
			{
				Debug.WriteLine(ex);
			}

			RebuildTaskbarItems();
		}

		/// <summary>
		/// Adds the item to taskbar.
		/// </summary>
		/// <param name="item">The item.</param>
		private void AddItemToTaskbar(VsItem item)
		{
			if (this.TaskbarJumpList is null || !this.ideManager.AnyInstalled)
			{
				return;
			}

			IdeDefaultTarget[] defaultTargets = GetCurrentDefaultTargets();
			string? legacyTarget              = this.selectVisualStudioVersion.SelectedItem?.Location;
			ItemLauncher il                   = item is VsFolder folderItem
				? new ItemLauncher(folderItem, defaultTargets, legacyTarget)
				: new ItemLauncher(item, defaultTargets, legacyTarget);

			// Add a jump task to the jump list.
			string exePath = Environment.ProcessPath ?? Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe");
			var jll        = new JumpListLink(il.GetLauncherPath(), item.Name ?? string.Empty);

			jll.Arguments     = il.CreateLaunchInfoString();
			jll.IconReference = new Microsoft.WindowsAPICodePack.Shell.IconReference(exePath, 0);
			jll.ShowCommand   = Microsoft.WindowsAPICodePack.Shell.WindowShowCommand.Hide;

			this.TaskbarJumpList.AddUserTasks(jll);
		}

		/// <summary>
		/// Experimental method to rebuild the taskbar items
		/// </summary>
		/// <param name="folder"></param>
		private void RebuildTaskbarItems(VsFolder? folder = null)
		{
			if (this.TaskbarJumpList is null)
			{
				return;
			}

			folder ??= this.solutionGroups;

			// iterate through all items in SolutionItems and add the favorite items to the taskbar
			foreach (VsItem item in folder.Items)
			{
				if (item.IsFavorite)
				{
					AddItemToTaskbar(item);
				}

				if (item is VsFolder f)
				{
					RebuildTaskbarItems(f);
				}
			}

			if (folder == this.solutionGroups)
			{
				try
				{
					this.TaskbarJumpList.Refresh();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}
		}
		#endregion

		#region Main button handling
		/// <summary>
		/// Handles adding a folder.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainFolderAdd_Click(object sender, EventArgs e)
		{
			dlgAddFolder dlg = new dlgAddFolder();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				OLVListItem r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					this.solutionGroups.Items.Add(dlg.Solution);
				}
				else
				{
					if (r.RowObject is VsFolder sg)
					{
						sg.Items.Add(dlg.Solution);
					}
					else
					{
						VsFolder? p = this.solutionGroups.FindParent(r.RowObject);

						p?.Items.Insert(p.Items.IndexOf((VsItem)r.RowObject), dlg.Solution);
					}
				}

				_ = SolutionData_OnChanged(true);
			}
		}
		private void mainOpenInExplorer_Click(object sender, EventArgs e)
		{
			OpenSelectedItemInExplorer();
		}

		/// <summary>
		/// Opens Windows Explorer for the selected item's directory or selects the target file.
		/// </summary>
		private void OpenSelectedItemInExplorer()
		{
			if (this.olvFiles.SelectedObject is not VsItem item || string.IsNullOrWhiteSpace(item.Path))
			{
				return;
			}

			string arguments = Directory.Exists(item.Path)
				? $"\"{item.Path}\""
				: $"/select, \"{item.Path}\"";

			using Process? process = Process.Start("explorer.exe", arguments);
		}

		/// <summary>
		/// Handles importing from a folder.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainImportFolder_Click(object sender, EventArgs e)
		{
			dlgImportFolder dlg = new dlgImportFolder();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				OLVListItem r     = this.olvFiles.SelectedItem;
				VsItemList source = dlg.Solution.Items;

				ApplyDefaultEditionForNewWorkspaces(source);
				MergeNewItems(r, source);

				_ = SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// Handles importing recent projects from installed IDEs.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainImportVS_Click(object sender, EventArgs e)
		{
			using dlgImportVisualStudio dlg = new dlgImportVisualStudio(this.ideManager);

			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				OLVListItem r     = this.olvFiles.SelectedItem;
				VsItemList source = dlg.Solution.Items;

				MergeNewItems(r, source);

				_ = SolutionData_OnChanged(true);
			}
		}

		private void mainImportSoP_Click(object sender, EventArgs e)
		{
			using OpenFileDialog dlg = new OpenFileDialog
			{
				Filter = FileHelper.SolutionFilterString,
				Title  = "Select a solution or project file"
			};

			if (dlg.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(dlg.FileName), dlg.FileName);

			ApplyDefaultEditionForNewWorkspace(item);

			if (ShowItemSettingsDialog(item))
			{
				// add the item to the list
				MergeNewItem(this.olvFiles.SelectedItem, item);
				_ = SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// Pre-selects the preferred installed VS Code edition when adding a new <c>.code-workspace</c> item without an explicit edition.
		/// </summary>
		/// <param name="item">The newly created item.</param>
		private void ApplyDefaultEditionForNewWorkspace(VsItem item)
		{
			ApplyDefaultEditionForNewWorkspaces(new[] { item });
		}

		/// <summary>
		/// Recursively pre-selects the preferred installed VS Code edition for newly imported <c>.code-workspace</c> items without an explicit edition.
		/// </summary>
		/// <param name="items">The imported items or folder trees.</param>
		private void ApplyDefaultEditionForNewWorkspaces(IEnumerable<VsItem> items)
		{
			this.ideManager.ApplyDefaultEditionToWorkspaces(
				items,
				this.selectVsCodeEdition.SelectedIdeInstance?.Identifier);
		}

		/// <summary>
		/// Displays the tool-appropriate settings dialog for <paramref name="item"/>.
		/// </summary>
		/// <param name="item">The item whose settings should be configured.</param>
		/// <returns><c>true</c> if the user accepted the changes; otherwise <c>false</c>.</returns>
		private bool ShowItemSettingsDialog(VsItem item)
		{
			bool useVsCodeDialog = item is VsCodeProject
				|| (item is VsFolder && this.tabControlIdes.SelectedTab == this.tabVsCode);

			if (useVsCodeDialog)
			{
				using dlgExecuteVsCode vsCodeDlg = new dlgExecuteVsCode(item, this.ideManager);

				return vsCodeDlg.ShowDialog(this) == DialogResult.OK;
			}

			using dlgExecuteVisualStudio vsDlg = new dlgExecuteVisualStudio(item);

			if (vsDlg.ShowDialog(this) != DialogResult.OK)
			{
				return false;
			}

			if (item is VsFolder folder)
			{
				folder.Refresh();
			}

			return true;
		}
		/// <summary>
		/// Handles the refresh button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainRefresh_Click(object sender, EventArgs e)
		{
			this.solutionGroups.Items.OnChanged -= SolutionData_OnChanged;
			bool loaded = LoadSolutionData();
			this.solutionGroups.Items.OnChanged += SolutionData_OnChanged;

			if (loaded)
			{
				UpdateList(true);
			}
		}

		/// <summary>
		/// mains the settings_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainSettings_Click(object sender, EventArgs e)
		{
			bool wasAlwaysAdmin = Properties.Settings.Default.AlwaysAdmin;
			bool wasAutoStart = Properties.Settings.Default.AutoStart;
			using dlgSettings dlg = new dlgSettings();

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				bool needsAdminRegistration = Properties.Settings.Default.AlwaysAdmin && (!wasAlwaysAdmin || !AdminInfo.IsElevated());

				if (Properties.Settings.Default.AlwaysAdmin || Properties.Settings.Default.AutoStart)
				{
					if (needsAdminRegistration)
					{
						if (RestartOurselves())
						{
							Application.Exit();

							return;
						}
					}
					else if (Properties.Settings.Default.AutoStart != wasAutoStart || Properties.Settings.Default.AlwaysAdmin != wasAlwaysAdmin)
					{
						Program.UpdateTaskScheduler();
					}
				}
				else if (wasAlwaysAdmin || wasAutoStart)
				{
					Program.RemoveTaskScheduler();
				}

				ApplyTheme(Properties.Settings.Default.DarkMode);
				_ = SolutionData_OnChanged(true);
			}
		}

		private void ApplyTheme(bool useDarkMode)
		{
			this.currentTheme = ThemeHelper.GetPalette(useDarkMode);

			ThemeHelper.ApplyTheme(this, this.currentTheme);
			ThemeHelper.ApplyTheme(this.ctxMenu, this.currentTheme);
			this.olvFiles.Invalidate();
		}

		#endregion

		#region ListView event handling

		/// <summary>
		/// Handles ending label editing on the list view.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			// Method intentionally left empty.
		}

		/// <summary>
		/// Handles right click on items
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_CellRightClick(object sender, CellRightClickEventArgs e)
		{
			// System.Diagnostics.Trace.WriteLine(String.Format("right clicked {0}, {1}). model {2}", e.RowIndex,
			// e.ColumnIndex, e.Model));

			e.MenuStrip = this.ctxMenu;
			e.MenuStrip.Show();
		}

		/// <summary>
		/// Handles showing the tooltip contents.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			if (e.Column == this.olvColumnOptions && e.Model is VsOptions itemOptions)
			{
				e.Text = itemOptions.ToString();
			}
			else
			{
				e.Text = e.Model is VsFolder ? e.Item?.Text : e.SubItem?.Text;
			}
		}

		/// <summary>
		/// Handles double click on items
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_DoubleClick(object sender, EventArgs e)
		{
			runToolStripMenuItem_Click(sender, e);
		}

		/// <summary>
		/// Clears the status text when changing items.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_HotItemChanged(object sender, HotItemChangedEventArgs e)
		{
			if (sender == null)
			{
				this.toolStripStatusLabel3.Text = "";
			}
		}
		/// <summary>
		/// Handles selected index changed in the list
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_SelectedIndexChanged(object? sender, EventArgs e)
		{
			VsItem? selectedItem = this.olvFiles.SelectedItem?.RowObject as VsItem;

			// update status bar text with info on selected item
			if (selectedItem is VsSolution s)
			{
				this.mainStatusLabel.Text = $"Visual Studio Solution: {s.Projects?.Count} Projects, {s.TypeAsName()}";
			}
			else if (selectedItem is VsProject p)
			{
				this.mainStatusLabel.Text = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";
			}
			else if (selectedItem is VsCodeProject cp)
			{
				this.mainStatusLabel.Text = $"VS Code: {cp.TypeAsName()}";
			}
			else if (selectedItem is VsFolder sg)
			{
				this.mainStatusLabel.Text = FormatFolderStatusText(sg);
			}
			else if (selectedItem is not null)
			{
				this.mainStatusLabel.Text = selectedItem.Path ?? string.Empty;
			}
			else
			{
				this.mainStatusLabel.Text = string.Empty;
			}

			this.btnExplorer.Enabled = HasExistingDiskPath(selectedItem);
		}

		/// <summary>
		/// Formats the status bar summary text for a selected <see cref="VsFolder"/>.
		/// </summary>
		/// <param name="folder">The selected folder.</param>
		/// <returns>The formatted summary string.</returns>
		private static string FormatFolderStatusText(VsFolder folder)
		{
			int solutions = folder.ContainedSolutionsCount();
			int projects  = folder.ContainedProjectsCount();

			if (solutions == 0 && projects == 0)
			{
				if (folder.IsWorkspaceBacked())
				{
					return "VS Code Workspace Folder";
				}

				return folder.IsVsCodeConfigured() ? "VS Code Folder" : "Contains 0 solutions";
			}

			var parts = new List<string>(2);

			if (solutions > 0)
			{
				parts.Add($"{solutions} solution{(solutions != 1 ? "s" : "")}");
			}

			if (projects > 0)
			{
				parts.Add($"{projects} project{(projects != 1 ? "s" : "")}");
			}

			return "Contains " + string.Join(" and ", parts);
		}

		/// <summary>
		/// olvs the files_ key press.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_KeyPress(object sender, KeyPressEventArgs e)
		{
			// Action keys (Enter, F5, Delete) are handled in olvFiles_KeyDown to avoid double firing and ASCII collisions.
		}

		/// <summary>
		/// olvs the files_ key down.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				removeToolStripMenuItem_Click(sender, e);
				e.Handled          = true;
				e.SuppressKeyPress = true;
			}
			else if ((e.KeyCode == Keys.Enter) || (e.KeyCode == Keys.F5))
			{
				if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
				{
					settingsToolStripMenuItem_Click(sender, e);
				}
				else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
				{
					runAsAdminToolStripMenuItem_Click(sender, e);
				}
				else
				{
					runToolStripMenuItem_Click(sender, e);
				}

				e.Handled          = true;
				e.SuppressKeyPress = true;
			}
		}

		#endregion

		#region Drag and Drop handling

		/// <summary>
		/// Handles files dropped from external source.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_Dropped(object? sender, OlvDropEventArgs e)
		{
			if (e.DataObject is not IDataObject dataObject || !dataObject.GetDataPresent(DataFormats.FileDrop))
			{
				return;
			}

			if (this.lastDroppedData is not null)
			{
				return;
			}

			this.lastDroppedData = dataObject;
			string[]? files      = dataObject.GetData(DataFormats.FileDrop) as string[];

			if (files is null || files.Length == 0)
			{
				return;
			}

			bool changed = false;

			foreach (string file in files)
			{
				if (ImportDroppedFile(file, e.DropTargetItem))
				{
					changed = true;
				}
			}

			if (changed)
			{
				_ = SolutionData_OnChanged(true);
			}

			e.Handled = true;
		}

		/// <summary>
		/// Imports a single dropped file or directory into the main tree.
		/// </summary>
		/// <param name="file">The dropped filesystem path.</param>
		/// <param name="dropTarget">The optional target list item receiving the drop.</param>
		/// <returns><c>true</c> if items were added to the tree; otherwise <c>false</c>.</returns>
		private bool ImportDroppedFile(string file, OLVListItem? dropTarget)
		{
			if (Directory.Exists(file))
			{
				using dlgImportFolder dlg = new dlgImportFolder(file);

				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return false;
				}

				ApplyDefaultEditionForNewWorkspaces(dlg.Solution.Items);
				MergeNewItems(dropTarget, dlg.Solution.Items);

				return true;
			}

			VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file);

			if (item is not null && item.ItemType != ItemTypeEnum.Other)
			{
				ApplyDefaultEditionForNewWorkspace(item);
				MergeNewItem(dropTarget, item);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Handles the files can drop event from external sources
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_CanDrop(object? sender, OlvDropEventArgs e)
		{
			if (e.DataObject is IDataObject dataObject && dataObject.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
				e.Handled = true;
				this.lastDroppedData = null;
			}
		}

		/// <summary>
		/// Setups the drag and drop.
		/// </summary>
		private void SetupDragAndDrop()
		{
			// Setup the tree so that it can drop and drop. Drag and drop support You can set up drag and drop
			// explicitly (like this) or, in the IDE, you can set IsSimpleDropSource and IsSimpleDragSource and respond
			// to CanDrop and Dropped events

			this.olvFiles.DragSource = new SimpleDragSource();
			this.olvFiles.IsSimpleDragSource = true;
			this.olvFiles.IsSimpleDropSink = true;

			this.olvFiles.ModelCanDrop += olvFiles_ModelCanDropHandler;
			this.olvFiles.ModelDropped += olvFiles_ModelDroppedHandler;

			this.olvFiles.CanDrop += olvFiles_CanDrop;
			this.olvFiles.Dropped += olvFiles_Dropped;
		}

		/// <summary>
		/// Handles requests if a model can drop on another model.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_ModelCanDropHandler(object sender, ModelDropEventArgs e)
		{
			e.Effect = DragDropEffects.None;

			if (e.TargetModel == null || e.SourceModels == null || e.SourceModels.Count == 0)
			{
				return;
			}

			object? obj = e.SourceModels[0];

			if (obj == null || obj == e.TargetModel)
			{
				return;
			}

			if (e.TargetModel is VsFolder)
			{
				if (obj is VsFolder sourceFolder && sourceFolder.FindParent(e.TargetModel) != null)
				{
					e.InfoMessage = "Cannot drop a group into its own descendant";

					return;
				}

				if (e.TargetModel != this.solutionGroups.FindParent(obj))
				{
					e.Effect = e.StandardDropActionFromKeys;
				}
				else if (IsControlPressed())
				{
					e.Effect = e.StandardDropActionFromKeys;
				}
			}
			else
			{
				e.InfoMessage = "Can only drop on a group";
			}
		}

		/// <summary>
		/// Handles model dropped on another model.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_ModelDroppedHandler(object? sender, ModelDropEventArgs e)
		{
			if (e.SourceModels.Count == 1)
			{
				if (e.TargetModel == e.SourceModels[0])
				{
					e.Handled = true;

					return;
				}

				VsFolder? target = e.TargetModel as VsFolder;
				object? source = e.SourceModels[0];

				if (target is null || source is null)
				{
					e.Handled = true;

					return;
				}

				if (source is VsFolder sourceFolder && sourceFolder.FindParent(target) != null)
				{
					e.Handled = true;

					return;
				}

				if (e.Effect == System.Windows.Forms.DragDropEffects.Move)
				{
					VsFolder? parent = this.solutionGroups.FindParent(source);
					parent?.Items.Remove((VsItem)source);

					if (source is VsFolder f)
					{
						target.Items.Add(f);
					}
					else
					{
						target.Items.Add((VsItem)source);
					}

					if (parent == null)
					{
						Debug.WriteLine("parent is null");
					}

					// update source and target models
					this.olvFiles.UpdateObject(target);
					this.olvFiles.UpdateObject(source);

					if (parent != null)
					{
						this.olvFiles.UpdateObject(parent);
					}
				}

				if (e.Effect == System.Windows.Forms.DragDropEffects.Copy)
				{
					// deep clone avoids sharing mutable item lists when copying folders
					target.Items.Add(((VsItem)source).Clone());
					this.olvFiles.UpdateObject(target);
					this.olvFiles.UpdateObject(source);
				}

				this.solutionGroups.Items.Changed = true;
				e.Handled = true;
			}
		}

		#endregion

		#region Context Menu item handling

		/// <summary>
		/// Handles the settings menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedItem?.RowObject is not VsItem item)
			{
				return;
			}

			if (ShowItemSettingsDialog(item))
			{
				_ = SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// Handles the rename menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void renameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedItem is null)
			{
				return;
			}

			object item = this.olvFiles.SelectedItem.RowObject;

			if (item is VsItem vsi)
			{
				using dlgRename dlg = new dlgRename(vsi);

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					vsi.Name = dlg.ItemName;

					_ = SolutionData_OnChanged(true);
				}
			}
		}

		/// <summary>
		/// Handles removing an item through the context menu
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void removeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedItem is null)
			{
				return;
			}

			object item = this.olvFiles.SelectedItem.RowObject;

			// only ask the user if the selected item is not an empty folder
			if (item is not VsFolder sg || sg.Items.Count != 0)
			{
				// ask user if he wants to delete this item really
				if (MessageBox.Show($"Are you sure you want to delete '{((VsItem)item).Name}'", "Delete item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
				{
					return;
				}
			}

			VsFolder? owner = this.solutionGroups.FindParent(item);
			owner?.Items.Remove((VsItem)item);

			_ = SolutionData_OnChanged(true);
		}

		/// <summary>
		/// Handles the New Group context menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void newGroupToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// redirect to existing code
			mainFolderAdd_Click(sender, e);
		}

		/// <summary>
		/// Handles the From Folder context menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void fromFolderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// redirect to existing code
			mainImportFolder_Click(sender, e);
		}

		/// <summary>
		/// Handles the Solution or project context menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void solutionProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mainImportSoP_Click(sender, e);
		}

		/// <summary>
		/// Handles the run as admin menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void runAsAdminToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LaunchSelectedItem(forceAdmin: true);
		}

		/// <summary>
		/// Handles the runs menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LaunchSelectedItem(forceAdmin: false);
		}

		/// <summary>
		/// Validates IDE availability, confirms multi-item folder launches, and invokes <see cref="ItemLauncher"/> for the selected item.
		/// </summary>
		/// <param name="forceAdmin">Whether to force UAC elevation for the launched process.</param>
		private void LaunchSelectedItem(bool forceAdmin)
		{
			if (this.olvFiles.SelectedItem?.RowObject is not VsItem vsi)
			{
				return;
			}

			if (!this.ideManager.AnyInstalled)
			{
				_ = MessageBox.Show(this, "No supported IDE installation was found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return;
			}

			IdeDefaultTarget[] defaultTargets      = GetCurrentDefaultTargets();
			Dictionary<IdeType, string> defaultMap = BuildDefaultTargetMap(defaultTargets);
			string? legacyTarget                   = this.selectVisualStudioVersion.SelectedItem?.Location;
			ItemLauncher? il                       = CreateLauncherForItem(vsi, defaultTargets, defaultMap, legacyTarget);

			if (il is null)
			{
				return;
			}

			this.mainStatusLabel.Text = $"Launching '{vsi.Name}'";
			il.Launch(forceAdmin).Wait();

			if (il.LastException is not null)
			{
				_ = MessageBox.Show(this, il.LastException.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			this.mainStatusLabel.Text = string.Empty;
		}

		/// <summary>
		/// Converts configured default targets into a lookup dictionary keyed by <see cref="IdeType"/>.
		/// </summary>
		/// <param name="defaultTargets">The configured default target entries.</param>
		/// <returns>A dictionary mapping each configured IDE family to its target identifier.</returns>
		private static Dictionary<IdeType, string> BuildDefaultTargetMap(IdeDefaultTarget[] defaultTargets)
		{
			var map = new Dictionary<IdeType, string>(defaultTargets.Length);

			foreach (IdeDefaultTarget target in defaultTargets)
			{
				if (!string.IsNullOrWhiteSpace(target.TargetIdentifier))
				{
					map[target.IdeType] = target.TargetIdentifier;
				}
			}

			return map;
		}

		/// <summary>
		/// Validates IDE availability and confirms multi-item folder launches before constructing an <see cref="ItemLauncher"/>.
		/// </summary>
		/// <param name="item">The selected item or folder to launch.</param>
		/// <param name="defaultTargets">Configured default IDE targets from the sidebar.</param>
		/// <param name="defaultMap">Lookup map of default target identifiers by IDE family.</param>
		/// <param name="legacyTarget">Optional legacy Visual Studio executable path.</param>
		/// <returns>The configured <see cref="ItemLauncher"/>, or <c>null</c> if launch was cancelled or unavailable.</returns>
		private ItemLauncher? CreateLauncherForItem(
			VsItem item,
			IdeDefaultTarget[] defaultTargets,
			IReadOnlyDictionary<IdeType, string> defaultMap,
			string? legacyTarget)
		{
			if (item is VsFolder folder)
			{
				if (folder.IsVsCodeConfigured() && !this.ideManager.HasInstalled(IdeType.VsCode) && folder.Items.Count == 0)
				{
					_ = MessageBox.Show(this, "Visual Studio Code is not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					return null;
				}

				if (!ConfirmFolderLaunch(folder))
				{
					return null;
				}

				return new ItemLauncher(folder, defaultTargets, legacyTarget);
			}

			if (item is VsSolution or VsProject or VsCodeProject)
			{
				if (this.ideManager.ResolveInstance(item, defaultMap) is null)
				{
					string message = item is VsCodeProject
						? "Visual Studio Code is not installed."
						: "No compatible IDE installation was found.";

					_ = MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					return null;
				}

				return new ItemLauncher(item, defaultTargets, legacyTarget);
			}

			return null;
		}

		/// <summary>
		/// Prompts for confirmation when launching a non-workspace folder that expands to more than three launch targets.
		/// </summary>
		/// <param name="folder">The folder being launched.</param>
		/// <returns><c>true</c> to continue launching; <c>false</c> if the user cancelled.</returns>
		private bool ConfirmFolderLaunch(VsFolder folder)
		{
			if (folder.IsVsCodeLaunchTarget() && this.ideManager.HasInstalled(IdeType.VsCode))
			{
				return true;
			}

			if (Properties.Settings.Default.DontShowMultiplesWarning)
			{
				return true;
			}

			int launchCount = CountLaunchTargets(folder);

			if (launchCount <= 3)
			{
				return true;
			}

			using var dlg = new dlgWarnMultiple(launchCount);

			return dlg.ShowDialog(this) != DialogResult.Cancel;
		}

		/// <summary>
		/// Counts the number of IDE launch targets produced by executing <paramref name="folder"/>,
		/// treating each workspace-backed subfolder as a single launch when VS Code is installed.
		/// </summary>
		/// <param name="folder">The folder to inspect.</param>
		/// <returns>The total number of launch operations.</returns>
		private int CountLaunchTargets(VsFolder folder)
		{
			bool hasVsCode = this.ideManager.HasInstalled(IdeType.VsCode);

			if (folder.ItemType == ItemTypeEnum.Project || (folder.IsVsCodeLaunchTarget() && hasVsCode))
			{
				return 1;
			}

			int count = 0;

			foreach (VsItem child in folder.Items)
			{
				if (child is VsFolder subFolder)
				{
					count += CountLaunchTargets(subFolder);
				}
				else if (child is VsSolution or VsProject or VsCodeProject)
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// Handles the favorites menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void favoriteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// make the selected item a favorite and add to the taskbar
			if (this.olvFiles.SelectedObject is VsItem i)
			{
				i.IsFavorite = !i.IsFavorite;

				SetupTaskbarTasks();
				_ = SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// Handles the explorer menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void explorerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSelectedItemInExplorer();
		}

		#endregion

		/// <summary>
		/// Handles visual studio version item drawing with icon.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void selectVisualStudioVersion_DrawItem(object sender, DrawItemEventArgs e)
		{
			var palette = this.currentTheme;
			Color backColor = palette.SurfaceAlt;
			Color textColor = palette.Text;
			Color selectedBackColor = palette.Selection;

			if (e.Index >= 0 && e.Index < this.visualStudioInstances.Count)
			{
				bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
				using var backBrush = new SolidBrush(selected ? selectedBackColor : backColor);
				using var textBrush = new SolidBrush(textColor);
				e.Graphics.FillRectangle(backBrush, e.Bounds);

				int height = 16;
				Rectangle iconRect = new Rectangle(e.Bounds.Left + this.selectVisualStudioVersion.Margin.Left,
													e.Bounds.Top + ((this.selectVisualStudioVersion.ItemHeight - height) / 2),
													height, height);
				e.Graphics.DrawIcon(this.visualStudioInstances[e.Index].AppIcon, iconRect);
				e.Graphics.DrawString(this.visualStudioInstances[e.Index].Name, e.Font!, textBrush, e.Bounds.Left + 20, e.Bounds.Top + 4);

				e.DrawFocusRectangle();
			}
			else if (e.Index == -1 && this.selectVisualStudioVersion.SelectedIndex >= 0)
			{
				// paint the edit portion when closed
				using var backBrush = new SolidBrush(backColor);
				using var textBrush = new SolidBrush(textColor);
				e.Graphics.FillRectangle(backBrush, e.Bounds);

				int height = 16;
				Rectangle iconRect = new Rectangle(e.Bounds.Left + this.selectVisualStudioVersion.Margin.Left,
													e.Bounds.Top + ((this.selectVisualStudioVersion.ItemHeight - height) / 2),
													height, height);
				var vs = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex];
				e.Graphics.DrawIcon(vs.AppIcon, iconRect);
				e.Graphics.DrawString(vs.Name, e.Font!, textBrush, e.Bounds.Left + 20, e.Bounds.Top + 4);
			}
		}

		/// <summary>
		/// Handles visual studio version selected index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void selectVisualStudioVersion_SelectedIndexChanged(object sender, EventArgs e)
		{
			// update all buttons with icon from selected visual studio version
			if (this.selectVisualStudioVersion.SelectedIndex >= 0 && this.selectVisualStudioVersion.SelectedIndex < this.visualStudioInstances.Count)
			{
				// save the selected item as last selected item
				if (!this.bInUpdate)
				{
					Properties.Settings.Default.SelectedVSversion = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].Identifier;
					Properties.Settings.Default.Save();
				}

				// set icon and text according to selection onto all buttons
				Bitmap icon = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].AppIcon.ToBitmap();
				string shortName = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ShortName;
				Bitmap? previousBitmap = this.currentVsButtonBitmap;
				this.currentVsButtonBitmap = icon;

				this.btnMainStartVisualStudio1.Text = string.Format((string)this.btnMainStartVisualStudio1.Tag!, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio1, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio1.Image = icon;

				this.btnMainStartVisualStudio2.Text = string.Format((string)this.btnMainStartVisualStudio2.Tag!, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio2, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio2.Image = icon;

				this.btnMainStartVisualStudio3.Text = string.Format((string)this.btnMainStartVisualStudio3.Tag!, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio3, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio3.Image = icon;

				this.btnMainStartVisualStudio4.Text = string.Format((string)this.btnMainStartVisualStudio4.Tag!, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio4, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio4.Image = icon;

				this.btnMainStartVisualStudio5.Text = string.Format((string)this.btnMainStartVisualStudio5.Tag!, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio5, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio5.Image = icon;

				previousBitmap?.Dispose();
			}
		}

		/// <summary>
		/// Persists the active sidebar IDE tab when both Visual Studio and VS Code are installed.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void tabControlIdes_SelectedIndexChanged(object? sender, EventArgs e)
		{
			if (this.bInUpdate)
			{
				return;
			}

			bool hasVs   = this.ideManager.HasInstalled(IdeType.VisualStudio);
			bool hasCode = this.ideManager.HasInstalled(IdeType.VsCode);

			if (!hasVs || !hasCode)
			{
				return;
			}

			Properties.Settings.Default.SelectedIdeTab = this.tabControlIdes.SelectedTab == this.tabVsCode ? "VsCode" : "VisualStudio";
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Saves the newly selected VS Code edition and refreshes the VS Code quick-action buttons.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void selectVsCodeEdition_SelectedIndexChanged(object? sender, EventArgs e)
		{
			if (this.selectVsCodeEdition.SelectedIdeInstance is not IIdeInstance selected)
			{
				return;
			}

			if (!this.bInUpdate)
			{
				Properties.Settings.Default.SelectedVsCodeEdition = selected.Identifier;
				Properties.Settings.Default.Save();
			}

			UpdateVsCodeQuickActionButtons(selected);
			UpdateVsCodeInstallerButtonState();
		}

		/// <summary>
		/// Updates the VS Code quick-action buttons with the icon, short name, and tooltip of <paramref name="selected"/>.
		/// </summary>
		/// <param name="selected">The selected VS Code instance.</param>
		private void UpdateVsCodeQuickActionButtons(IIdeInstance selected)
		{
			Bitmap icon            = selected.AppIcon.ToBitmap();
			string shortName       = selected.ShortName;
			string tooltip         = selected.ToString() ?? selected.Name;
			Bitmap? previousBitmap = this.currentVsCodeButtonBitmap;

			this.currentVsCodeButtonBitmap = icon;

			Button[] vsCodeButtons =
			{
				this.btnVsCodeStart,
				this.btnVsCodeStartAdmin,
				this.btnVsCodeStartNewWindow,
				this.btnVsCodeStartNoExt
			};

			foreach (Button button in vsCodeButtons)
			{
				if (button.Tag is string format)
				{
					button.Text = string.Format(format, shortName);
				}

				this.tooltipForButtons.SetToolTip(button, tooltip);
				button.Image = icon;
			}

			previousBitmap?.Dispose();
		}

		#region Solution Data Handling
		/// <summary>
		/// Handles changes to Solution data
		/// </summary>
		private bool SolutionData_OnChanged(bool changed)
		{
			if (changed)
			{
				this.solutionGroups.LastModified = DateTime.Now;
				SaveSolutionData();
				UpdateList(true);
			}

			return false;
		}

		#endregion

		#region Main VS Execution Buttons handling

		/// <summary>
		/// Gets the currently selected Visual Studio instance from the combobox, or <c>null</c> if none is selected.
		/// </summary>
		/// <returns>The selected <see cref="VisualStudioInstance"/> or <c>null</c>.</returns>
		private VisualStudioInstance? GetSelectedVisualStudioInstance()
		{
			int index = this.selectVisualStudioVersion.SelectedIndex;

			if (index >= 0 && index < this.visualStudioInstances.Count)
			{
				return this.visualStudioInstances[index];
			}

			return null;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio1 button (Start VS)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio1_Click(object sender, EventArgs e)
		{
			VisualStudioInstance? vs = GetSelectedVisualStudioInstance();

			if (vs is null)
			{
				return;
			}

			this.Cursor = Cursors.WaitCursor;

			try
			{
				vs.Execute();
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio2 button (Start VS as admin)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio2_Click(object sender, EventArgs e)
		{
			VisualStudioInstance? vs = GetSelectedVisualStudioInstance();

			if (vs is null)
			{
				return;
			}

			this.Cursor = Cursors.WaitCursor;

			try
			{
				vs.ExecuteAsAdmin();
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio3 button (Start VS with an instance)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio3_Click(object sender, EventArgs e)
		{
			VisualStudioInstance? vs = GetSelectedVisualStudioInstance();

			if (vs is null)
			{
				return;
			}

			using dlgNewInstance dlg = new dlgNewInstance();

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.Cursor = Cursors.WaitCursor;

				try
				{
					vs.ExecuteWithInstance(IsControlPressed(), dlg.InstanceName);
				}
				finally
				{
					this.Cursor = Cursors.Default;
				}
			}
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio4 button (Start VS with new project)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio4_Click(object sender, EventArgs e)
		{
			VisualStudioInstance? vs = GetSelectedVisualStudioInstance();

			if (vs is null)
			{
				return;
			}

			this.Cursor = Cursors.WaitCursor;

			try
			{
				vs.ExecuteNewProject(IsControlPressed());
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio5 button (Start VS with dialog)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio5_Click(object sender, EventArgs e)
		{
			using dlgExecuteVisualStudio dlg = new dlgExecuteVisualStudio(this.selectVisualStudioVersion.SelectedIndex);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.Cursor = Cursors.WaitCursor;

				try
				{
					VisualStudioInstance? vs = dlg.VsVersion ?? GetSelectedVisualStudioInstance();

					if (vs is not null && dlg.Item is not null)
					{
						vs.ExecuteWith(dlg.Item.RunAsAdmin, dlg.Item.ShowSplash, dlg.Item.Path ?? string.Empty, dlg.Item.Instance, dlg.Item.Commands);
					}
				}
				finally
				{
					this.Cursor = Cursors.Default;
				}
			}
		}

		/// <summary>
		/// Handles click on the btnMainOpenActivityLog button
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainOpenActivityLog_Click(object sender, EventArgs e)
		{
			VisualStudioInstance? vs = GetSelectedVisualStudioInstance();

			if (vs is null)
			{
				return;
			}

			string version = $"{vs.MainVersion}.0_{vs.Identifier}";
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string logPath = Path.Combine(appData, "Microsoft", "VisualStudio", version, "ActivityLog.xml");

			if (!File.Exists(logPath))
			{
				_ = MessageBox.Show($"ActivityLog.xml was not found at '{logPath}'.", "ActivityLog", MessageBoxButtons.OK, MessageBoxIcon.Information);

				return;
			}

			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName        = logPath,
				Verb            = "open",
				UseShellExecute = true
			};

			try
			{
				using var process = Process.Start(psi);
			}
			catch (Win32Exception ex)
			{
				_ = MessageBox.Show($"Unable to open ActivityLog.xml: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Handles click on the btnVsInstaller button
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnVsInstaller_Click(object sender, EventArgs e)
		{
			if (btnVsInstaller.Tag is not string vsi || string.IsNullOrWhiteSpace(vsi))
			{
				return;
			}

			try
			{
				if (vsi.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					using var p = Process.Start("explorer.exe", vsi);
				}
				else
				{
					// ask user if installer should be started with elevated privileges
					bool bIsElevated = AdminInfo.IsCurrentUserAdmin() || AdminInfo.IsElevated();
					bool runElevated = !bIsElevated && MessageBox.Show("The Visual Studio Installer may require elevated privileges, do you want to run it as administrator?", "Start installer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

					ProcessStartInfo psi = new ProcessStartInfo
					{
						Verb            = runElevated ? "runas" : string.Empty,
						UseShellExecute = true,
						FileName        = vsi
					};

					using var p = Process.Start(psi);
				}
			}
			catch (Win32Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Gets the currently selected VS Code instance from <see cref="selectVsCodeEdition"/>, or <c>null</c> if none is selected.
		/// </summary>
		/// <returns>The selected <see cref="VsCodeInstance"/> or <c>null</c>.</returns>
		private VsCodeInstance? GetSelectedVsCodeInstance()
		{
			return this.selectVsCodeEdition.SelectedIdeInstance as VsCodeInstance;
		}

		/// <summary>
		/// Runs a quick-launch action against the currently selected VS Code edition while displaying a wait cursor.
		/// </summary>
		/// <param name="launchAction">The launch callback to invoke on the selected instance.</param>
		private void ExecuteSelectedVsCodeAction(Action<VsCodeInstance> launchAction)
		{
			VsCodeInstance? vsCode = GetSelectedVsCodeInstance();

			if (vsCode is null)
			{
				return;
			}

			this.Cursor = Cursors.WaitCursor;

			try
			{
				launchAction(vsCode);
			}
			finally
			{
				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on <see cref="btnVsCodeStart"/> to launch the selected VS Code edition.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnVsCodeStart_Click(object? sender, EventArgs e)
		{
			ExecuteSelectedVsCodeAction(vsCode => vsCode.Execute());
		}

		/// <summary>
		/// Handles click on <see cref="btnVsCodeStartAdmin"/> to launch the selected VS Code edition as administrator.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnVsCodeStartAdmin_Click(object? sender, EventArgs e)
		{
			ExecuteSelectedVsCodeAction(vsCode => vsCode.ExecuteAsAdmin());
		}

		/// <summary>
		/// Handles click on <see cref="btnVsCodeStartNewWindow"/> to launch the selected VS Code edition in a new window.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnVsCodeStartNewWindow_Click(object? sender, EventArgs e)
		{
			ExecuteSelectedVsCodeAction(vsCode => vsCode.ExecuteNewWindow());
		}

		/// <summary>
		/// Handles click on <see cref="btnVsCodeStartNoExt"/> to launch the selected VS Code edition with extensions disabled.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnVsCodeStartNoExt_Click(object? sender, EventArgs e)
		{
			ExecuteSelectedVsCodeAction(vsCode => vsCode.ExecuteWithoutExtensions());
		}

		/// <summary>
		/// Handles click on <see cref="btnVsCodeOpenActivityLog"/> to open the selected VS Code edition's logs folder in Explorer.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void btnVsCodeOpenActivityLog_Click(object? sender, EventArgs e)
		{
			VsCodeInstance? vsCode = GetSelectedVsCodeInstance();

			if (vsCode is null)
			{
				return;
			}

			string logsPath = Path.Combine(vsCode.UserDataDirectory, "logs");

			if (!Directory.Exists(logsPath))
			{
				_ = MessageBox.Show($"VS Code logs folder was not found at '{logsPath}'.", "ActivityLog", MessageBoxButtons.OK, MessageBoxIcon.Information);

				return;
			}

			try
			{
				using var process = Process.Start("explorer.exe", $"\"{logsPath}\"");
			}
			catch (Win32Exception ex)
			{
				_ = MessageBox.Show($"Unable to open VS Code logs folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Starts the daily VS Code update check timer and triggers an initial background check when VS Code is installed.
		/// </summary>
		private void InitializeVsCodeUpdateCheck()
		{
			UpdateVsCodeInstallerButtonState();

			if (!this.ideManager.HasInstalled(IdeType.VsCode))
			{
				return;
			}

			this.vsCodeUpdateTimer.Tick += VsCodeUpdateTimer_Tick;
			this.vsCodeUpdateTimer.Start();

			CheckVsCodeUpdatesAsync(forceRefresh: false);
		}

		/// <summary>
		/// Handles periodic ticks to re-evaluate the 24-hour VS Code update check window while the application remains running.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private void VsCodeUpdateTimer_Tick(object? sender, EventArgs e)
		{
			CheckVsCodeUpdatesAsync(forceRefresh: false);
		}

		/// <summary>
		/// Runs a non-blocking update check for all discovered VS Code editions and updates the installer button on the UI thread.
		/// </summary>
		/// <param name="forceRefresh">When <c>true</c>, bypasses the 24-hour cache.</param>
		private void CheckVsCodeUpdatesAsync(bool forceRefresh)
		{
			if (!this.ideManager.HasInstalled(IdeType.VsCode))
			{
				return;
			}

			if (Interlocked.CompareExchange(ref this.vsCodeUpdateCheckInProgress, 1, 0) != 0)
			{
				return;
			}

			CancellationToken ct;

			try
			{
				ct = this.vsCodeUpdateCts.Token;
			}
			catch (ObjectDisposedException)
			{
				Interlocked.Exchange(ref this.vsCodeUpdateCheckInProgress, 0);

				return;
			}

			List<VsCodeInstance> editions = this.ideManager.GetInstances(IdeType.VsCode)
				.OfType<VsCodeInstance>()
				.ToList();

			_ = Task.Run(async () =>
			{
				try
				{
					var results = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);

					foreach (VsCodeInstance edition in editions)
					{
						ct.ThrowIfCancellationRequested();

						VsCodeUpdateInfo info = await this.vsCodeUpdateService
							.CheckForUpdateAsync(edition, forceRefresh, ct)
							.ConfigureAwait(false);

						results[edition.Identifier] = info;
					}

					SafeInvokeOnUiThread(() =>
					{
						foreach (KeyValuePair<string, VsCodeUpdateInfo> pair in results)
						{
							this.vsCodeUpdateStates[pair.Key] = pair.Value;
						}

						UpdateVsCodeInstallerButtonState();
					});
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
				finally
				{
					Interlocked.Exchange(ref this.vsCodeUpdateCheckInProgress, 0);
				}
			});
		}

		/// <summary>
		/// Synchronizes <see cref="btnVsCodeInstaller"/> text, icon, enabled state, and tooltip with installation and update status.
		/// </summary>
		private void UpdateVsCodeInstallerButtonState()
		{
			if (!this.ideManager.HasInstalled(IdeType.VsCode))
			{
				this.btnVsCodeInstaller.Text    = "Download VS Code";
				this.btnVsCodeInstaller.Image   = Resources.Download;
				this.btnVsCodeInstaller.Enabled = true;
				this.tooltipForButtons.SetToolTip(this.btnVsCodeInstaller, "Opens the VS Code download page");

				return;
			}

			this.btnVsCodeInstaller.Image = Resources.Installer;

			if (Volatile.Read(ref this.vsCodeUpdateInstallInProgress) != 0)
			{
				this.btnVsCodeInstaller.Text    = "Updating VS Code...";
				this.btnVsCodeInstaller.Enabled = false;
				this.tooltipForButtons.SetToolTip(this.btnVsCodeInstaller, "Downloading and installing VS Code update...");

				return;
			}

			VsCodeInstance? selected = GetSelectedVsCodeInstance();

			if (selected is not null
				&& this.vsCodeUpdateStates.TryGetValue(selected.Identifier, out VsCodeUpdateInfo? info)
				&& info.IsUpdateAvailable)
			{
				this.btnVsCodeInstaller.Text    = "Update VS Code";
				this.btnVsCodeInstaller.Enabled = true;
				this.tooltipForButtons.SetToolTip(this.btnVsCodeInstaller, BuildVsCodeUpdateAvailableTooltip(selected, info));

				return;
			}

			this.btnVsCodeInstaller.Text    = "VS Code Installer";
			this.btnVsCodeInstaller.Enabled = false;
			this.tooltipForButtons.SetToolTip(this.btnVsCodeInstaller, BuildVsCodeUpToDateTooltip(selected));
		}

		/// <summary>
		/// Builds the tooltip text displayed when an update is available for <paramref name="selected"/>.
		/// </summary>
		private static string BuildVsCodeUpdateAvailableTooltip(VsCodeInstance selected, VsCodeUpdateInfo info)
		{
			bool hasCurrent = !string.IsNullOrWhiteSpace(info.CurrentVersion);
			bool hasLatest  = !string.IsNullOrWhiteSpace(info.LatestVersion);

			if (hasCurrent && hasLatest && !info.CurrentVersion.Equals(info.LatestVersion, StringComparison.OrdinalIgnoreCase))
			{
				return $"Update {selected.ShortName} from {info.CurrentVersion} to {info.LatestVersion}";
			}

			if (hasLatest)
			{
				return $"Update {selected.ShortName} to {info.LatestVersion}";
			}

			return $"Update {selected.ShortName}";
		}

		/// <summary>
		/// Builds the tooltip text displayed when no VS Code update is currently available.
		/// </summary>
		private string BuildVsCodeUpToDateTooltip(VsCodeInstance? selected)
		{
			if (selected is not null
				&& this.vsCodeUpdateStates.TryGetValue(selected.Identifier, out VsCodeUpdateInfo? info)
				&& info.CheckSucceeded)
			{
				string version   = string.IsNullOrWhiteSpace(info.CurrentVersion) ? selected.Version : info.CurrentVersion;
				string verSuffix = string.IsNullOrWhiteSpace(version) ? string.Empty : $" ({version})";

				return $"{selected.ShortName}{verSuffix} is up to date";
			}

			return "Checks daily for VS Code updates";
		}

		/// <summary>
		/// Handles click on <see cref="btnVsCodeInstaller"/> to download VS Code when absent or trigger an in-place update when an update is available.
		/// </summary>
		/// <param name="sender">The event source.</param>
		/// <param name="e">The event arguments.</param>
		private async void btnVsCodeInstaller_Click(object? sender, EventArgs e)
		{
			if (!this.ideManager.HasInstalled(IdeType.VsCode))
			{
				OpenVsCodeDownloadPage();

				return;
			}

			VsCodeInstance? vsCode = GetSelectedVsCodeInstance();

			if (vsCode is null
				|| !this.vsCodeUpdateStates.TryGetValue(vsCode.Identifier, out VsCodeUpdateInfo? updateInfo)
				|| !updateInfo.IsUpdateAvailable)
			{
				return;
			}

			await RunVsCodeUpdateAsync(vsCode, updateInfo);
		}

		/// <summary>
		/// Downloads, verifies, and launches the official VS Code installer for <paramref name="vsCode"/>, then refreshes update state.
		/// </summary>
		private async Task RunVsCodeUpdateAsync(VsCodeInstance vsCode, VsCodeUpdateInfo updateInfo)
		{
			if (Interlocked.CompareExchange(ref this.vsCodeUpdateInstallInProgress, 1, 0) != 0)
			{
				return;
			}

			CancellationToken ct;

			try
			{
				ct = this.vsCodeUpdateCts.Token;
			}
			catch (ObjectDisposedException)
			{
				Interlocked.Exchange(ref this.vsCodeUpdateInstallInProgress, 0);

				return;
			}

			UpdateVsCodeInstallerButtonState();

			this.Cursor               = Cursors.WaitCursor;
			this.mainStatusLabel.Text = $"Downloading {vsCode.ShortName} update...";

			string? installerPath = null;

			try
			{
				installerPath = await this.vsCodeUpdateService.DownloadInstallerAsync(updateInfo, ct);

				if (this.IsDisposed || ct.IsCancellationRequested)
				{
					return;
				}

				this.mainStatusLabel.Text = $"Updating {vsCode.ShortName}...";

				int exitCode = await LaunchVsCodeInstallerAsync(installerPath, updateInfo.IsSystemInstall, ct);

				await HandleVsCodeInstallerResultAsync(vsCode, updateInfo, exitCode, ct);
			}
			catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
			{
				Debug.WriteLine(ex);
			}
			catch (Exception ex)
			{
				if (!this.IsDisposed)
				{
					_ = MessageBox.Show(this, $"Unable to update {vsCode.ShortName}: {ex.Message}", "VS Code Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				VsCodeUpdateService.TryDeleteInstallerFile(installerPath);
				Interlocked.Exchange(ref this.vsCodeUpdateInstallInProgress, 0);

				if (!this.IsDisposed)
				{
					this.Cursor               = Cursors.Default;
					this.mainStatusLabel.Text = string.Empty;

					UpdateVsCodeInstallerButtonState();
				}
			}
		}

		/// <summary>
		/// Refreshes version metadata, action button tooltips, and update state after a successful installer exit, or displays a warning on non-zero failure exit codes.
		/// </summary>
		private async Task HandleVsCodeInstallerResultAsync(VsCodeInstance vsCode, VsCodeUpdateInfo updateInfo, int exitCode, CancellationToken cancellationToken)
		{
			if (this.IsDisposed || cancellationToken.IsCancellationRequested)
			{
				return;
			}

			if (exitCode == 0)
			{
				VsCodeUpdateInfo refreshed = await this.vsCodeUpdateService.CheckForUpdateAsync(vsCode, forceRefresh: true, cancellationToken);
				string fallbackVersion     = string.IsNullOrWhiteSpace(refreshed.CurrentVersion)
					? updateInfo.LatestVersion
					: refreshed.CurrentVersion;

				_ = vsCode.RefreshVersion(fallbackVersion);

				if (!this.IsDisposed)
				{
					this.vsCodeUpdateStates[vsCode.Identifier] = refreshed;

					if (string.Equals(GetSelectedVsCodeInstance()?.Identifier, vsCode.Identifier, StringComparison.OrdinalIgnoreCase))
					{
						UpdateVsCodeQuickActionButtons(vsCode);
					}
				}

				return;
			}

			if (exitCode != -1)
			{
				_ = MessageBox.Show(this, $"{vsCode.ShortName} installer exited with code {exitCode}.", "VS Code Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		/// <summary>
		/// Launches the downloaded VS Code installer and waits for completion, returning <c>-1</c> if the user cancelled a UAC prompt.
		/// </summary>
		private static async Task<int> LaunchVsCodeInstallerAsync(string installerPath, bool isSystemInstall, CancellationToken cancellationToken)
		{
			ProcessStartInfo startInfo = VsCodeUpdateService.BuildInstallerStartInfo(installerPath, isSystemInstall);

			try
			{
				using Process? process = Process.Start(startInfo);

				if (process is null)
				{
					throw new InvalidOperationException("Failed to start the VS Code installer.");
				}

				await process.WaitForExitAsync(cancellationToken);

				return process.ExitCode;
			}
			catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
			{
				// User declined the UAC elevation prompt for a System installation.
				return -1;
			}
		}

		/// <summary>
		/// Opens the official Visual Studio Code download page in the default browser.
		/// </summary>
		private void OpenVsCodeDownloadPage()
		{
			string url = this.ideManager.GetProvider(IdeType.VsCode)?.DownloadUrl ?? "https://code.visualstudio.com/download";

			try
			{
				var psi = new ProcessStartInfo
				{
					FileName        = url,
					UseShellExecute = true
				};

				using var process = Process.Start(psi);
			}
			catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
			{
				Debug.WriteLine(ex);
			}
		}
		#endregion

		/// <summary>
		/// Handles text changes in the filter field and updates the list
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void txtFilter_TextChanged(object sender, EventArgs e)
		{
			RebuildFilters();
		}

		/// <summary>
		/// Handles resize of the main panel, resizes the filter field to fit the panel
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainPanel_Resize(object sender, EventArgs e)
		{
			int w = this.txtFilter.Parent.Width;
			w -= (34 * 7) + 12 + 12; // 7 buttons + 2 spacer
			w -= 34 + 12;
			w -= this.txtFilter.Location.X;
			this.txtFilter.Width = w;
		}

		/// <summary>
		/// Handles context menu opening, enables/disables menu items based on the selected list item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void ctxMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			VsItem? selectedItem = this.olvFiles.SelectedObject as VsItem;
			bool hasSelection    = selectedItem is not null;
			bool hasDiskPath     = HasExistingDiskPath(selectedItem);

			// if the currently selected item is a group, enable the "Add..." menu item, otherwise disable it
			this.addToolStripMenuItem.Enabled = selectedItem is VsFolder || selectedItem is null;

			if (selectedItem is not null)
			{
				this.favoriteToolStripMenuItem.Checked = selectedItem.IsFavorite;

				// Check if the item is under Git control
				SetupBranchMenu(selectedItem);
			}
			else
			{
				this.favoriteToolStripMenuItem.Checked = false;
				this.gitToolStripMenuItem.Enabled      = false;
			}

			this.runToolStripMenuItem.Enabled          = hasSelection;
			this.runAsAdminToolStripMenuItem.Enabled   = hasSelection;
			this.renameToolStripMenuItem.Enabled       = hasSelection;
			this.removeToolStripMenuItem.Enabled       = hasSelection;
			this.settingsToolStripMenuItem.Enabled     = hasSelection;
			this.favoriteToolStripMenuItem.Enabled     = hasSelection;
			this.explorerToolStripMenuItem.Enabled     = hasDiskPath;
			this.openInVsCodeToolStripMenuItem.Enabled = hasDiskPath && this.ideManager.HasInstalled(IdeType.VsCode);
		}

		/// <summary>
		/// Handles the Open in VS Code context menu item by opening the selected item's target path directly in VS Code.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters.</param>
		private void openInVsCodeToolStripMenuItem_Click(object? sender, EventArgs e)
		{
			if (this.olvFiles.SelectedObject is not VsItem item || !HasExistingDiskPath(item))
			{
				return;
			}

			VsCodeInstance? vsCode = (this.selectVsCodeEdition.SelectedIdeInstance as VsCodeInstance)
				?? (this.ideManager.GetProvider(IdeType.VsCode)?.DefaultInstance as VsCodeInstance);

			if (vsCode is null)
			{
				_ = MessageBox.Show(this, "Visual Studio Code is not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return;
			}

			string itemPath    = item.Path!;
			string? targetPath = (Directory.Exists(itemPath) || Path.GetExtension(itemPath).Equals(".code-workspace", StringComparison.OrdinalIgnoreCase))
				? itemPath
				: Path.GetDirectoryName(itemPath);

			if (string.IsNullOrWhiteSpace(targetPath))
			{
				return;
			}

			vsCode.OpenPath(targetPath);
		}

		/// <summary>
		/// Determines whether <paramref name="item"/> points to an existing file or directory on disk.
		/// </summary>
		/// <param name="item">The item to inspect.</param>
		/// <returns><c>true</c> if the item's path exists on disk; otherwise <c>false</c>.</returns>
		private static bool HasExistingDiskPath(VsItem? item)
		{
			if (item is null || string.IsNullOrWhiteSpace(item.Path))
			{
				return false;
			}

			return File.Exists(item.Path) || Directory.Exists(item.Path);
		}

		/// <summary>
		/// Handles key press on the filter field, clears the filter if the user presses ESC
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void txtFilter_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Escape)
			{
				this.txtFilter.Text = string.Empty;
			}
		}

		/// <summary>
		/// Discovers the Git repository path for an item.
		/// </summary>
		/// <param name="item">The item whose path should be checked.</param>
		/// <returns>The discovered repository path, or <c>null</c> if not inside a Git repository.</returns>
		private static string? DiscoverRepositoryPath(VsItem item)
		{
			return DiscoverRepositoryPath(item.Path);
		}

		/// <summary>
		/// Discovers the Git repository path for a file or directory path.
		/// </summary>
		/// <param name="itemPath">The file or directory path to inspect.</param>
		/// <returns>The discovered repository path, or <c>null</c> if not inside a Git repository.</returns>
		private static string? DiscoverRepositoryPath(string? itemPath)
		{
			if (string.IsNullOrWhiteSpace(itemPath))
			{
				return null;
			}

			string? dir = Directory.Exists(itemPath) ? itemPath : Path.GetDirectoryName(itemPath);

			if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
			{
				return null;
			}

			return Repository.Discover(dir);
		}

		/// <summary>
		/// Refreshes cached Git branch and dirty status for a single item and updates its row in the list.
		/// </summary>
		/// <param name="item">The item to update.</param>
		/// <param name="repoPath">The discovered repository path.</param>
		private void RefreshGitItemStatus(VsItem item, string repoPath)
		{
			try
			{
				using var repo = new Repository(repoPath);

				item.BranchName = repo.Head?.FriendlyName ?? item.BranchName;
				item.Status     = repo.RetrieveStatus().IsDirty ? "*" : "!";

				if (!this.IsDisposed)
				{
					this.olvFiles.RefreshObject(item);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Displays a Git operation failure message, highlighting authentication guidance when relevant.
		/// </summary>
		/// <param name="title">The dialog title.</param>
		/// <param name="result">The failed <see cref="GitOperationResult"/>.</param>
		private void ShowGitOperationError(string title, GitOperationResult result)
		{
			if (this.IsDisposed)
			{
				return;
			}

			if (result.AuthFailed)
			{
				string detail = string.IsNullOrWhiteSpace(result.ErrorMessage) ? string.Empty : $"\r\n\r\nDetails:\r\n{result.ErrorMessage}";

				_ = MessageBox.Show(
					this,
					$"Git authentication failed. Sign in with Git Credential Manager or check your configured credentials/SSH keys.{detail}",
					"Git Authentication",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);

				return;
			}

			_ = MessageBox.Show(this, result.ErrorMessage, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		/// <summary>
		/// Handles the click event for branch menu items.
		/// Uses GitHelper for credential-aware remote fetching when a branch is not present locally.
		/// </summary>
		private async void BranchMenuItem_Click(object sender, EventArgs e, VsItem item, string branchName)
		{
			if (Interlocked.CompareExchange(ref this.gitRemoteOperationInProgress, 1, 0) != 0)
			{
				return;
			}

			this.Cursor               = Cursors.WaitCursor;
			this.mainStatusLabel.Text = $"Switching branch to '{branchName}'...";

			try
			{
				string? repoPath = DiscoverRepositoryPath(item);

				if (string.IsNullOrEmpty(repoPath))
				{
					_ = MessageBox.Show(this, "No Git repository found for this item.", "Branch Checkout", MessageBoxButtons.OK, MessageBoxIcon.Warning);

					return;
				}

				bool branchExistsLocally;

				using (var repo = new Repository(repoPath))
				{
					branchExistsLocally = repo.Branches[branchName] != null;
				}

				if (!branchExistsLocally)
				{
					GitOperationResult fetchResult = await Task.Run(() => GitHelper.Fetch(repoPath, "origin"));

					if (!fetchResult.Success)
					{
						ShowGitOperationError("Branch Checkout", fetchResult);

						return;
					}
				}

				using (var repo = new Repository(repoPath))
				{
					Branch? target = repo.Branches[branchName];

					if (target == null)
					{
						_ = MessageBox.Show(this, $"Branch '{branchName}' not found after fetch.", "Branch Checkout", MessageBoxButtons.OK, MessageBoxIcon.Warning);

						return;
					}

					_ = Commands.Checkout(repo, target);
				}

				RefreshGitItemStatus(item, repoPath);

				if (!this.IsDisposed)
				{
					_ = MessageBox.Show(this, $"Checked out branch: {branchName}", "Branch Checkout", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				if (!this.IsDisposed)
				{
					_ = MessageBox.Show(this, $"Error checking out branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				Interlocked.Exchange(ref this.gitRemoteOperationInProgress, 0);

				if (!this.IsDisposed)
				{
					this.Cursor               = Cursors.Default;
					this.mainStatusLabel.Text = string.Empty;
				}
			}
		}

		/// <summary>
		/// Fetch with credentials.
		/// </summary>
		private async void fetchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedObject is not (VsItem item and not VsFolder))
			{
				return;
			}

			if (Interlocked.CompareExchange(ref this.gitRemoteOperationInProgress, 1, 0) != 0)
			{
				return;
			}

			this.Cursor               = Cursors.WaitCursor;
			this.mainStatusLabel.Text = $"Fetching '{item.Name}'...";

			try
			{
				string? repoPath = DiscoverRepositoryPath(item);

				if (string.IsNullOrEmpty(repoPath))
				{
					_ = MessageBox.Show(this, "No Git repository found for this item.", "Branch Fetch", MessageBoxButtons.OK, MessageBoxIcon.Warning);

					return;
				}

				GitOperationResult result = await Task.Run(() => GitHelper.Fetch(repoPath));

				if (!result.Success)
				{
					ShowGitOperationError("Branch Fetch", result);

					return;
				}

				RefreshGitItemStatus(item, repoPath);

				if (!this.IsDisposed)
				{
					string summary = string.IsNullOrWhiteSpace(result.Summary) ? string.Empty : $"\r\n\r\n{result.Summary}";

					_ = MessageBox.Show(this, $"Fetched: {result.RemoteName} ({item.BranchName}){summary}", "Branch Fetch", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				if (!this.IsDisposed)
				{
					_ = MessageBox.Show(this, $"Error fetching: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				Interlocked.Exchange(ref this.gitRemoteOperationInProgress, 0);

				if (!this.IsDisposed)
				{
					this.Cursor               = Cursors.Default;
					this.mainStatusLabel.Text = string.Empty;
				}
			}
		}

		/// <summary>
		/// Pull with credentials.
		/// </summary>
		private async void pullToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedObject is not (VsItem item and not VsFolder))
			{
				return;
			}

			if (Interlocked.CompareExchange(ref this.gitRemoteOperationInProgress, 1, 0) != 0)
			{
				return;
			}

			try
			{
				string? repoPath = DiscoverRepositoryPath(item);

				if (string.IsNullOrEmpty(repoPath))
				{
					_ = MessageBox.Show(this, "No Git repository found for this item.", "Branch Pull", MessageBoxButtons.OK, MessageBoxIcon.Warning);

					return;
				}

				bool isDirty;

				using (var repo = new Repository(repoPath))
				{
					isDirty = repo.RetrieveStatus().IsDirty;
				}

				if (isDirty)
				{
					DialogResult msgResult = MessageBox.Show(this, "There are uncommitted changes. Continue with pull?", "Uncommitted Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

					if (msgResult == DialogResult.No)
					{
						return;
					}
				}

				this.Cursor               = Cursors.WaitCursor;
				this.mainStatusLabel.Text = $"Pulling '{item.Name}'...";

				GitOperationResult result = await Task.Run(() => GitHelper.Pull(repoPath));

				if (!result.Success)
				{
					ShowGitOperationError("Branch Pull", result);

					return;
				}

				RefreshGitItemStatus(item, repoPath);

				if (!this.IsDisposed)
				{
					string summary = string.IsNullOrWhiteSpace(result.Summary) ? string.Empty : $"\r\n\r\n{result.Summary}";

					_ = MessageBox.Show(this, $"Pulled: {result.RemoteName} ({item.BranchName}){summary}", "Branch Pull", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				if (!this.IsDisposed)
				{
					_ = MessageBox.Show(this, $"Error pulling: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			finally
			{
				Interlocked.Exchange(ref this.gitRemoteOperationInProgress, 0);

				if (!this.IsDisposed)
				{
					this.Cursor               = Cursors.Default;
					this.mainStatusLabel.Text = string.Empty;
				}
			}
		}
	}
}
