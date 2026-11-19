using System.ComponentModel;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Taskbar;
using BrightIdeasSoftware;
using ThemePalette = VSLauncher.Helpers.ThemeHelper.ThemePalette;
using Newtonsoft.Json;

using VSLauncher.DataModel;
using VSLauncher.Forms;
using VSLauncher.Helpers;

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
		private readonly bool bInUpdate;

		private readonly VisualStudioInstanceManager visualStudioInstances = new VisualStudioInstanceManager();
		private DescribedTaskRenderer? itemRenderer;
		private VsFolder solutionGroups = new VsFolder();
		private IDataObject? lastDroppedData;
		private bool solutionDataLoadFailed;
		private int gitRefreshInProgress;
		private int gitRemoteOperationInProgress;
		private Bitmap? currentVsButtonBitmap;

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
				if (e.Model is VsFolder sg)
				{
					sg.Expanded = true;
				}
			};

			this.olvFiles.Collapsed += delegate (object? sender, TreeBranchCollapsedEventArgs e)
			{
				if (e.Model is VsFolder sg)
				{
					sg.Expanded = false;
				}
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is VsFolder sg ? sg.Items : (System.Collections.IEnumerable?)null;
			};

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
			this.olvColumnVersion.AspectGetter = delegate (object rowObject)
			{
				if (rowObject is VsItem item)
				{
					if (string.IsNullOrEmpty(item.VsVersion))
					{
						return "<default>";
					}

					var vs = visualStudioInstances.GetByIdentifier(item.VsVersion);

					if (vs is null)
					{
						return "<default>";
					}

					string s = $"{vs.Year} ({vs.ShortVersion})";

					return s;
				}

				return null;
			};

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

			if (this.selectVisualStudioVersion.Items.Count > 0)
			{
				if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedVSversion))
				{
					VisualStudioInstance? v = this.selectVisualStudioVersion.Versions.FirstOrDefault(v => v.Identifier == Properties.Settings.Default.SelectedVSversion);

					if (v is not null)
					{
						this.selectVisualStudioVersion.SelectedItem = v;
					}
					else
					{
						this.selectVisualStudioVersion.SelectedIndex = 0;
					}
				}
				else
				{
					this.selectVisualStudioVersion.SelectedIndex = 0;
				}
			}

			this.bInUpdate = false;

			LoadSolutionData();

			this.solutionGroups.Items.OnChanged += SolutionData_OnChanged;
			UpdateList(false);
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

			this.currentVsButtonBitmap?.Dispose();
			this.currentVsButtonBitmap = null;
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
			if (this.TaskbarJumpList is null)
			{
				return;
			}

			VisualStudioInstance? targetVs = this.visualStudioInstances.GetByIdentifier(item.VsVersion);

			if (targetVs is null)
			{
				return;
			}

			ItemLauncher il = item is VsFolder folderItem
				? new ItemLauncher(folderItem, targetVs)
				: new ItemLauncher(item, targetVs);

			// Add a jump task to the jump list.
			string exePath = Environment.ProcessPath ?? Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe");
			var jll = new JumpListLink(il.GetLauncherPath(), item.Name ?? string.Empty);
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
			if (this.olvFiles.SelectedItem != null)
			{
				VsItem item = (VsItem)this.olvFiles.SelectedItem.RowObject;

				if (item is not VsFolder && !string.IsNullOrWhiteSpace(item.Path))
				{
					using var process = Process.Start("explorer.exe", $"/select, \"{item.Path}\"");
				}
			}
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
				OLVListItem r = this.olvFiles.SelectedItem;
				VsItemList source = dlg.Solution.Items;

				MergeNewItems(r, source);

				_ = SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// Handles importing from VS button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainImportVS_Click(object sender, EventArgs e)
		{
			dlgImportVisualStudio dlg = new dlgImportVisualStudio();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				OLVListItem r = this.olvFiles.SelectedItem;
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

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(dlg.FileName), dlg.FileName);
				using dlgExecuteVisualStudio dlg2 = new dlgExecuteVisualStudio(item);

				if (dlg2.ShowDialog() == DialogResult.OK && dlg2.Item is not null)
				{
					// add the item to the list
					MergeNewItem(this.olvFiles.SelectedItem, dlg2.Item);
					_ = SolutionData_OnChanged(true);
				}
			}
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
			bool enableOpenInExplorer = false;

			// update status bar text with info on selected item
			if (this.olvFiles.SelectedItem != null)
			{
				object item = this.olvFiles.SelectedItem.RowObject;
				if (item is VsSolution s)
				{
					this.mainStatusLabel.Text = $"Visual Studio Solution: {s.Projects?.Count} Projects, {s.TypeAsName()}";
					enableOpenInExplorer = true;
				}
				else if (item is VsProject p)
				{
					this.mainStatusLabel.Text = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";
					enableOpenInExplorer = true;
				}
				else if (item is VsFolder sg)
				{
					this.mainStatusLabel.Text = $"Contains {sg.ContainedSolutionsCount()} solution{((sg.ContainedSolutionsCount() != 1) ? 's' : "")}";
				}
			}
			else
			{
				this.mainStatusLabel.Text = string.Empty;
			}

			btnExplorer.Enabled = enableOpenInExplorer;
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

			if (this.lastDroppedData is null)
			{
				this.lastDroppedData = dataObject;
				string[]? files = dataObject.GetData(DataFormats.FileDrop) as string[];

				if (files is null || files.Length == 0)
				{
					return;
				}

				foreach (string file in files)
				{
					VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file);

					if (item != null && item.ItemType != ItemTypeEnum.Other)
					{
						MergeNewItem(e.DropTargetItem, item);
					}
					else if (item?.ItemType == ItemTypeEnum.Other && Directory.Exists(file))
					{
						// check if the file is actually a folder, then invoke the import folder dialog
						using dlgImportFolder dlg = new dlgImportFolder(file);

						if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
						{
							VsItemList source = dlg.Solution.Items;
							MergeNewItems(e.DropTargetItem, source);
							_ = SolutionData_OnChanged(true);
						}
					}
				}

				_ = SolutionData_OnChanged(true);
				e.Handled = true;
			}
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
			if (this.olvFiles.SelectedItem is null)
			{
				return;
			}

			object item = this.olvFiles.SelectedItem.RowObject;
			using dlgExecuteVisualStudio dlg = new dlgExecuteVisualStudio(item);

			if (dlg.ShowDialog() == DialogResult.OK)
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
		/// Resolves the Visual Studio instance to use for launching a given item.
		/// </summary>
		/// <param name="item">The item to launch.</param>
		/// <returns>The resolved <see cref="VisualStudioInstance"/>, or <c>null</c> if none are installed.</returns>
		private VisualStudioInstance? ResolveTargetVisualStudio(VsItem item)
		{
			VisualStudioInstance? selectedVs = this.selectVisualStudioVersion.SelectedItem;

			if (item is VsFolder f)
			{
				return (!string.IsNullOrEmpty(f.VsVersion) ? this.visualStudioInstances.FindByIdentifier(f.VsVersion) : null)
					?? selectedVs
					?? this.visualStudioInstances.HighestVersion();
			}

			if (item is VsSolution s)
			{
				return this.visualStudioInstances.FindByIdentifier(s.VsVersion)
					?? this.visualStudioInstances.FindByVersion(s.RequiredVersion)
					?? selectedVs
					?? this.visualStudioInstances.HighestVersion();
			}

			return this.visualStudioInstances.FindByIdentifier(item.VsVersion)
				?? selectedVs
				?? this.visualStudioInstances.HighestVersion();
		}

		/// <summary>
		/// Handles the run as admin menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void runAsAdminToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedItem?.RowObject is not VsItem item)
			{
				return;
			}

			VisualStudioInstance? vs = ResolveTargetVisualStudio(item);

			if (vs is null)
			{
				_ = MessageBox.Show("No Visual Studio installation was found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return;
			}

			if (item is VsFolder sg)
			{
				_ = new ItemLauncher(sg, vs).Launch(true);
			}
			else
			{
				_ = new ItemLauncher(item, vs).Launch(true);
			}
		}

		/// <summary>
		/// Handles the runs menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedItem?.RowObject is not VsItem vsi)
			{
				return;
			}

			VisualStudioInstance? vs = ResolveTargetVisualStudio(vsi);

			if (vs is null)
			{
				_ = MessageBox.Show("No Visual Studio installation was found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				return;
			}

			ItemLauncher? il = null;

			if (vsi is VsFolder f)
			{
				if (!Properties.Settings.Default.DontShowMultiplesWarning)
				{
					var n = f.ContainedSolutionsCount() + f.ContainedProjectsCount();

					if (n > 3)
					{
						using var dlg = new dlgWarnMultiple(n);

						if (dlg.ShowDialog() == DialogResult.Cancel)
						{
							return;
						}
					}
				}

				il = new ItemLauncher(f, vs);
			}
			else if (vsi is VsSolution or VsProject)
			{
				il = new ItemLauncher(vsi, vs);
			}

			if (il != null)
			{
				this.mainStatusLabel.Text = $"Launching '{vsi.Name}'";
				il.Launch().Wait();

				if (il.LastException != null)
				{
					_ = MessageBox.Show(il.LastException.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				this.mainStatusLabel.Text = "";
			}
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
		/// Handles the favorites menu item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void explorerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// open the explorer with the selected item
			if (this.olvFiles.SelectedObject is VsItem i && !string.IsNullOrWhiteSpace(i.Path))
			{
				using var process = Process.Start("explorer.exe", $"/select, \"{i.Path}\"");
			}
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
			// if the currently selected item is a group, enable the "Add..." menu item, otherwise remove it
			this.addToolStripMenuItem.Enabled = this.olvFiles.SelectedObject is VsFolder || this.olvFiles.SelectedObject is null;
			bool bOther = true;

			if (this.olvFiles.SelectedObject is VsItem i)
			{
				this.favoriteToolStripMenuItem.Checked = i.IsFavorite;

				// Check if the item is under Git control

				SetupBranchMenu(i);
			}
			if (this.olvFiles.SelectedObject is null)
			{
				// disable all other menu items
				bOther = false;
			}

			this.runToolStripMenuItem.Enabled = bOther;
			this.runAsAdminToolStripMenuItem.Enabled = bOther;
			this.renameToolStripMenuItem.Enabled = bOther;
			this.removeToolStripMenuItem.Enabled = bOther;
			this.settingsToolStripMenuItem.Enabled = bOther;
			this.favoriteToolStripMenuItem.Enabled = bOther;
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
			string? dir = Path.GetDirectoryName(item.Path);

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
