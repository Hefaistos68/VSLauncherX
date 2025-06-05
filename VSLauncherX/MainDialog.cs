using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Taskbar;

using BrightIdeasSoftware;

using Newtonsoft.Json;

using VSLauncher.DataModel;
using VSLauncher.Forms;
using VSLauncher.Helpers;

using VSLXshared.Helpers;
using System.Globalization;
using System.Reflection;
using LibGit2Sharp;
using Windows.Devices.Geolocation;
using static System.Windows.Forms.AxHost;
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
		private DataObject? lastDroppedData;

		public JumpList TaskbarJumpList { get; private set; }


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
					if (item.VsVersion is null)
					{
						return "<default>";
					}

					var vs = visualStudioInstances.GetByIdentifier(item.VsVersion);
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

			if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedVSversion))
			{
				VisualStudioInstance v = this.selectVisualStudioVersion.Versions.Where(v => v.Identifier == Properties.Settings.Default.SelectedVSversion).Single();
				this.selectVisualStudioVersion.SelectedItem = v;
			}
			else
			{
				this.selectVisualStudioVersion.SelectedIndex = 0;
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
				this.Location = Properties.Settings.Default.AppLocation;
				this.Size = Properties.Settings.Default.AppSize;

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

			toolStripStatusGit.Visible = true;
			FetchGitStatusAsync(this.solutionGroups);
			this.olvFiles.Invalidate();
			this.olvFiles.Update();
			toolStripStatusGit.Visible = false;
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

			SaveSolutionData();
		}



		#region Taskbar handling

		private void SetupTaskbarTasks()
		{
			var cat = new JumpListCustomCategory("Test");
			// Create a jump list.
			try
			{
				this.TaskbarJumpList = JumpList.CreateJumpList();
			}
			catch (System.Exception ex)
			{

			}

			RebuildTaskbarItems();
		}

		/// <summary>
		/// Adds the item to taskbar.
		/// </summary>
		/// <param name="item">The item.</param>
		private void AddItemToTaskbar(VsItem item)
		{
			var il = new ItemLauncher(item, visualStudioInstances.GetByIdentifier(item.VsVersion));

			// Note: to add items grouped by their category, create a custom category and add the items to that category.
			// but, since the taskbar does not support hierarchies, we should think about a different way to do this
			// the group/category must be added in the calling method, not here, then add the JumpListItem to the parent passed to this method.
			// 
			// 
			// var cat = new JumpListCustomCategory(item.Parent.Name);
			// Add a jump task to the jump list.
			var jll = new JumpListLink(il.GetLauncherPath(), item.Name);
			// need to find a way to pass admin requirement
			jll.Arguments = il.CreateLaunchInfoString();
			jll.IconReference = new Microsoft.WindowsAPICodePack.Shell.IconReference(Assembly.GetExecutingAssembly().Location, 1);
			jll.ShowCommand = Microsoft.WindowsAPICodePack.Shell.WindowShowCommand.Hide;

			// cat.AddJumpListItems(jll);

			this.TaskbarJumpList.AddUserTasks(jll);
			// this.TaskbarJumpList.AddCustomCategories(cat);

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

			if (folder is null || folder == this.solutionGroups)
			{
				this.TaskbarJumpList.Refresh();
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

				if (item is not VsFolder)
				{
					Process.Start("explorer.exe", "/select, " + item.Path);
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
			OpenFileDialog dlg = new OpenFileDialog
			{
				Filter = FileHelper.SolutionFilterString,
				Title = "Select a solution or project file"
			};

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(dlg.FileName), dlg.FileName);
				dlgExecuteVisualStudio dlg2 = new dlgExecuteVisualStudio(item);
				if (dlg2.ShowDialog() == DialogResult.OK)
				{
					// add the item to the list
					MergeNewItem(this.olvFiles.SelectedItem, dlg2.Item!);
					UpdateList(true);
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
			this.solutionGroups.Items.Clear();
			LoadSolutionData();
			this.solutionGroups.Items.OnChanged += SolutionData_OnChanged;

			UpdateList(true);
			// FetchGitStatus(this.solutionGroups);
		}

		/// <summary>
		/// mains the settings_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void mainSettings_Click(object sender, EventArgs e)
		{
			dlgSettings dlg = new dlgSettings();

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				if (Properties.Settings.Default.AlwaysAdmin || Properties.Settings.Default.AutoStart)
				{
					if (Properties.Settings.Default.AlwaysAdmin)
					{
						RestartOurselves();
						Application.Exit();
					}
					else
					{
						Program.UpdateTaskScheduler();
					}
				}
				else
				{
					Program.RemoveTaskScheduler();
				}

				_ = SolutionData_OnChanged(true);
			}
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
			if (e.ColumnIndex == this.olvColumnOptions.DisplayIndex)
			{
				VsOptions itemOptions = (VsOptions)e.Model;
				e.Text = itemOptions.ToString();
			}
			else
			{
				e.Text = e.Model is VsFolder ? e.Item.Text : e.SubItem.Text;
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
			if (e.KeyChar == (char)Keys.Enter)
			{
				if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
				{
					settingsToolStripMenuItem_Click(sender, e);
				}
				else
				{
					runToolStripMenuItem_Click(sender, e);
				}

				e.Handled = true;
			}
			else if (e.KeyChar == (char)Keys.Delete)
			{
				removeToolStripMenuItem_Click(sender, e);
				e.Handled = true;
			}
		}

		/// <summary>
		/// olvs the files_ key down.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void olvFiles_KeyDown(object sender, KeyEventArgs e)
		{
			if ((e.KeyValue == (char)Keys.Delete) && ((Control.ModifierKeys & Keys.Shift) == Keys.Shift))
			{
				removeToolStripMenuItem_Click(sender, e);
				e.Handled = true;
			}
			else if ((e.KeyValue == (char)Keys.Enter) || (e.KeyValue == (char)Keys.F5))
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

				e.Handled = true;
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
			// for some reason yet unknown, this event is fired twice, so we need to check if we already handled it
			if (this.lastDroppedData is null)
			{
				this.lastDroppedData = (DataObject)e.DataObject;
				string[] files = (string[])this.lastDroppedData.GetData(DataFormats.FileDrop);

				foreach (string file in files)
				{
					VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file);
					if (item != null && item.ItemType != ItemTypeEnum.Other)
					{
						MergeNewItem(e.DropTargetItem, item);
					}
					else if (item?.ItemType == ItemTypeEnum.Other)
					{
						// check if the file is actually a folder, then invoke the import folder dialog
						FileInfo fi = new FileInfo(file);
						if (fi.Attributes.HasFlag(FileAttributes.Directory))
						{
							dlgImportFolder dlg = new dlgImportFolder(file);
							if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
							{
								OLVListItem r = this.olvFiles.SelectedItem;
								VsItemList source = dlg.Solution.Items;

								MergeNewItems(r, source);

								_ = SolutionData_OnChanged(true);
							}
						}
					}
				}

				UpdateList(true);

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
			if (e.DataObject is not null)
			{
				if (((DataObject)e.DataObject).GetDataPresent(DataFormats.FileDrop))
				{
					e.Effect = DragDropEffects.Copy;
					e.Handled = true;
					this.lastDroppedData = null;
				}
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
			if (e.TargetModel == null)
			{
				return;
			}

			if (e.SourceModels[0] == e.TargetModel)
			{
				return;
			}

			if (e.TargetModel is VsFolder)
			{
				object? obj = e.SourceModels[0];

				if (obj != null)
				{
					if (e.TargetModel != this.solutionGroups.FindParent(obj))
					{
						e.Effect = e.StandardDropActionFromKeys;
					}
					else
					{
						if (IsControlPressed())
						{
							e.Effect = e.StandardDropActionFromKeys;
						}
					}
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
					return;
				}

				VsFolder? target = e.TargetModel as VsFolder;
				object? source = e.SourceModels[0];

				if (target is null || source is null)
				{
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
					this.olvFiles.UpdateObject(parent);
				}

				if (e.Effect == System.Windows.Forms.DragDropEffects.Copy)
				{
					// make sure to add "copy" if same parent

					target.Items.Add(((VsItem)source).Clone());
					this.olvFiles.UpdateObject(target);
					this.olvFiles.UpdateObject(source);
				}

				this.solutionGroups.Items.Changed = true;
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
			object item = this.olvFiles.SelectedItem.RowObject;
			dlgExecuteVisualStudio dlg = new dlgExecuteVisualStudio(item);

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
			object item = this.olvFiles.SelectedItem.RowObject;
			if (item is VsItem vsi)
			{
				dlgRename dlg = new dlgRename(vsi);
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
			object item = this.olvFiles.SelectedItem.RowObject;
			VisualStudioInstance vs = this.selectVisualStudioVersion.SelectedItem ?? throw new InvalidCastException();

			if (item is VsFolder sg)
			{
				_ = new ItemLauncher(sg, vs).Launch(true);
			}
			else if (item is VsSolution s)
			{
				_ = new ItemLauncher(s, vs).Launch(true);
			}
		}

		/// <summary>
		/// Handles the runs menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			object item = this.olvFiles.SelectedItem.RowObject;
			VisualStudioInstance? vs = this.selectVisualStudioVersion.SelectedItem;
			ItemLauncher? il = null;

			if (item is VsFolder f)
			{
				if (!Properties.Settings.Default.DontShowMultiplesWarning)
				{
					var n = f.ContainedSolutionsCount() + f.ContainedProjectsCount();
					if (n > 3)
					{
						var dlg = new dlgWarnMultiple(n);

						if (dlg.ShowDialog() == DialogResult.Cancel)
						{
							return;
						}
					}
				}

				vs = string.IsNullOrEmpty(f.VsVersion) ? this.visualStudioInstances.GetByIdentifier(f.VsVersion) : vs;
				il = new ItemLauncher(f, vs);
			}
			else if (item is VsSolution s)
			{
				vs = this.visualStudioInstances.GetByVersion(s.RequiredVersion) ?? vs;
				il = new ItemLauncher(s, vs);
			}
			else if (item is VsProject p)
			{
				vs = this.visualStudioInstances.GetByIdentifier(p.VsVersion) ?? vs;
				il = new ItemLauncher(p, vs);
			}

			if (il != null)
			{
				var vsi = item as VsItem;
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
				//RebuildTaskbarItems();
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
			if (this.olvFiles.SelectedObject is VsItem i)
			{
				Process.Start("explorer.exe", $"/select, \"{i.Path}\"");
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
			// draw the selected item with the Visual Studio Icon and the version as text
			if (e.Index >= 0 && e.Index <= this.visualStudioInstances.Count)
			{
				e.DrawBackground();

				int height = 16; // selectVisualStudioVersion.Height - (selectVisualStudioVersion.Margin.Top+selectVisualStudioVersion.Margin.Bottom);

				Rectangle iconRect = new Rectangle(e.Bounds.Left + this.selectVisualStudioVersion.Margin.Left,
													e.Bounds.Top + ((this.selectVisualStudioVersion.ItemHeight - height) / 2),
													height, height);
				e.Graphics.DrawIcon(this.visualStudioInstances[e.Index].AppIcon, iconRect);
				e.Graphics.DrawString(this.visualStudioInstances[e.Index].Name, e.Font!, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
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

				this.btnMainStartVisualStudio1.Text = string.Format((string)this.btnMainStartVisualStudio1.Tag, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio1, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio1.Image = icon;

				this.btnMainStartVisualStudio2.Text = string.Format((string)this.btnMainStartVisualStudio2.Tag, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio2, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio2.Image = icon;

				this.btnMainStartVisualStudio3.Text = string.Format((string)this.btnMainStartVisualStudio3.Tag, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio3, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio3.Image = icon;

				this.btnMainStartVisualStudio4.Text = string.Format((string)this.btnMainStartVisualStudio4.Tag, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio4, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio4.Image = icon;

				this.btnMainStartVisualStudio5.Text = string.Format((string)this.btnMainStartVisualStudio5.Tag, shortName);
				this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio5, this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex].ToString());
				this.btnMainStartVisualStudio5.Image = icon;
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
		/// Handles click on the btnMainStartVisualStudio1 button (Start VS)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio1_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			VisualStudioInstance vs = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex];
			vs.Execute();
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio2 button (Start VS as admin)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio2_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			VisualStudioInstance vs = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex];
			vs.ExecuteAsAdmin();
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio3 button (Start VS with an instance)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio3_Click(object sender, EventArgs e)
		{
			dlgNewInstance dlg = new dlgNewInstance();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.Cursor = Cursors.WaitCursor;
				VisualStudioInstance vs = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex];

				vs.ExecuteWithInstance(IsControlPressed(), dlg.InstanceName);
				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio4 button (Start VS with new project)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio4_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			VisualStudioInstance vs = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex];
			vs.ExecuteNewProject(IsControlPressed());
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio5 button (Start VS with dialog)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainStartVisualStudio5_Click(object sender, EventArgs e)
		{
			dlgExecuteVisualStudio dlg = new dlgExecuteVisualStudio(this.selectVisualStudioVersion.SelectedIndex);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.Cursor = Cursors.WaitCursor;
				VisualStudioInstance vs = dlg.VsVersion;

				if (dlg.Item is not null)
				{
					vs.ExecuteWith(dlg.Item.RunAsAdmin, dlg.Item.ShowSplash, dlg.Item.Path!, dlg.Item.Instance, dlg.Item.Commands);
				}

				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on the btnMainOpenActivityLog button
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnMainOpenActivityLog_Click(object sender, EventArgs e)
		{
			VisualStudioInstance vs = this.visualStudioInstances[this.selectVisualStudioVersion.SelectedIndex];
			string version = $"{vs.MainVersion}.0_{vs.Identifier}";
			string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = $"{s}\\Microsoft\\VisualStudio\\{version}\\ActivityLog.xml",
				Verb = "open",
				UseShellExecute = true
			};
			Process.Start(psi);
		}

		/// <summary>
		/// Handles click on the btnVsInstaller button
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters</param>
		private void btnVsInstaller_Click(object sender, EventArgs e)
		{
			string vsi = (string)btnVsInstaller.Tag;

			if (vsi.StartsWith("http"))
			{
				_ = Process.Start("explorer.exe", vsi);
			}
			else
			{
				// ask user if installer should be started with elevated privileges
				bool bIsElevated = AdminInfo.IsCurrentUserAdmin() || AdminInfo.IsElevated();

				if (!bIsElevated && MessageBox.Show("The Visual Studio Installer may required elevated privileges, do you want to run it as administrator?", "Start installer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					ProcessStartInfo psi = new ProcessStartInfo
					{
						Verb = "runas",
						UseShellExecute = true,
						FileName = vsi
					};

					_ = Process.Start(psi);
				}
				else
				{
					_ = Process.Start(vsi);
				}
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
		/// Handles the click event for branch menu items.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event parameters.</param>
		/// <param name="repo">The repository.</param>
		/// <param name="branchName">The branch name.</param>
		private void BranchMenuItem_Click(object sender, EventArgs e, VsItem item, string branchName)
		{
			try
			{
				// Checkout the selected branch
				using (var repo = new Repository(Path.GetDirectoryName(item.Path)))
				{
					Commands.Checkout(repo, branchName);
					MessageBox.Show($"Checked out branch: {branchName}", "Branch Checkout", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error checking out branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void fetchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedObject is VsItem item and not VsFolder)
			{
				try
				{
					// Checkout the selected branch
					using (var repo = new Repository(Path.GetDirectoryName(item.Path)))
					{
						var remote = repo.Network.Remotes[repo.Head.RemoteName];

						Commands.Fetch(repo, repo.Head.RemoteName, remote.FetchRefSpecs.Select(rs => rs.Specification), new FetchOptions(), string.Empty);
						MessageBox.Show(this, $"Fetched branch: {repo.Head.RemoteName} ({repo.Head.FriendlyName})", "Branch Fetch", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, $"Error checking out branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void pullToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.olvFiles.SelectedObject is VsItem item and not VsFolder)
			{
				try
				{
					// Checkout the selected branch
					using (var repo = new Repository(Path.GetDirectoryName(item.Path)))
					{
						var remote = repo.Network.Remotes[repo.Head.RemoteName];

						var stat = repo.RetrieveStatus();

						if(stat.IsDirty)
						{
							var msgResult = MessageBox.Show(this, "There are uncommitted changes in the current branch. Do you want to continue with the pull operation?",
												 "Uncommitted Changes",
												 MessageBoxButtons.YesNo,
												 MessageBoxIcon.Warning);

							if (msgResult == DialogResult.No)
							{
								return;
							}
						}

						var result = Commands.Pull(repo, new Signature("VSLauncherX", "user@example.com", DateTimeOffset.Now), new PullOptions());
						MessageBox.Show(this, $"Pulled: {repo.Head.RemoteName} ({repo.Head.FriendlyName})\r\nStatus: {result.Status}", "Branch Fetch", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error checking out branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}

		}
	}
}
