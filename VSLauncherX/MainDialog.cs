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
		/// Returns if the control key is pressed.
		/// </summary>
		/// <returns>A bool.</returns>
		private static bool IsControlPressed()
		{
			return (Control.ModifierKeys & Keys.Control) == Keys.Control;
		}

		/// <summary>
		/// Creates the described task renderer.
		/// </summary>
		/// <returns>A DescribedTaskRenderer.</returns>
		private DescribedTaskRenderer CreateDescribedRenderer()
		{
			// Let's create an appropriately configured renderer.
			DescribedTaskRenderer renderer = new DescribedTaskRenderer
			{
				// Give the renderer its own collection of images. If this isn't set, the renderer will use the
				// SmallImageList from the ObjectListView. (this is standard Renderer behaviour, not specific to DescribedTaskRenderer).
				ImageList = this.imageListMainIcons,

				// Tell the renderer which property holds the text to be used as a description
				DescriptionGetter = ColumnHelper.GetDescription,

				// Change the formatting slightly
				TitleFont = new Font("Verdana", 11, FontStyle.Bold),
				DescriptionFont = new Font("Verdana", 8),
				ImageTextSpace = 8,
				TitleDescriptionSpace = 1,

				// Use older Gdi renderering, since most people think the text looks clearer
				UseGdiTextRendering = true,
				Aspect = "Name"
			};

			return renderer;
		}

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

			// setup the Date column
			this.olvColumnDate.AspectGetter = ColumnHelper.GetAspectForDate;

			// setup the Version column
			this.olvColumnVersion.AspectGetter = delegate (object rowObject)
			{
				if (rowObject is VsItem item)
				{																
					if(item.VsVersion is null)
					{
						return "<default>";
					}

					var vs = visualStudioInstances.GetByVersion(item.VsVersion);
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

		/// <summary>
		/// Initializes a new instance of the <see cref="MainDialog"/> class.
		/// </summary>
		public MainDialog()
		{

			InitializeComponent();
			InitializeListview();
			SetupDragAndDrop();

			// BuildTestData();

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
			UpdateList();
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

			SetupTaskbarTasks();

			_ = this.txtFilter.Focus();
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

		/// <summary>
		/// Merges new items into the selected list item
		/// </summary>
		/// <param name="r">The selected item</param>
		/// <param name="source">The source to add</param>
		private void MergeNewItems(OLVListItem r, VsItemList source)
		{
			if (r == null)
			{
				// nothing selected, add to the end
				this.solutionGroups.Items.AddRange(source);
			}
			else
			{
				if (r.RowObject is VsFolder sg)
				{
					// add below selected item
					sg.Items.AddRange(source);
				}
				else
				{
					// get parent of this item
					_ = this.solutionGroups.FindParent(r.RowObject as VsItem);

					// add at the end
					this.solutionGroups.Items.AddRange(source);
				}
			}
		}

		/// <summary>
		/// Merges a new item into the selected list item
		/// </summary>
		/// <param name="r">The selected list item</param>
		/// <param name="source">The source to add</param>
		private void MergeNewItem(OLVListItem r, VsItem source)
		{
			if (r == null)
			{
				// nothing selected, add to the end
				this.solutionGroups.Items.Add(source);
			}
			else
			{
				if (r.RowObject is VsFolder sg)
				{
					// add below selected item
					sg.Items.Add(source);
				}
				else
				{
					// get parent of this item
					VsFolder? vsi = this.solutionGroups.FindParent(r.RowObject as VsItem);

					// add at the end
					vsi?.Items.Add(source);
				}
			}
		}

		#region Taskbar handling

		private void SetupTaskbarTasks()
		{
			var cat = new JumpListCustomCategory ( "Test" );
			// Create a jump list.
			this.TaskbarJumpList = JumpList.CreateJumpList();

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
		private void RebuildTaskbarItems(VsFolder? folder = null)
		{
			if (folder is null)
			{
				folder = this.solutionGroups;
			}

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
					UpdateList();
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

			UpdateList();
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
			// update status bar text with info on selected item
			if (this.olvFiles.SelectedItem != null)
			{
				object item = this.olvFiles.SelectedItem.RowObject;
				if (item is VsSolution s)
				{
					this.mainStatusLabel.Text = $"Visual Studio Solution: {s.Projects?.Count} Projects, {s.TypeAsName()}";
				}
				else if (item is VsProject p)
				{
					this.mainStatusLabel.Text = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";
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
			if (e.KeyValue == (char)Keys.Delete)
			{
				removeToolStripMenuItem_Click(sender, e);
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

				UpdateList();

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
					var n = f.ContainedSolutionsCount()+f.ContainedProjectsCount();
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
				il.Launch().Wait();

				if (il.LastException != null)
				{
					_ = MessageBox.Show(il.LastException.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
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
				UpdateList();
			}

			return false;
		}

		/// <summary>
		/// Loads the solution data.
		/// </summary>
		private void LoadSolutionData()
		{
			// load this.solutionGroups data from a JSON file in the users data folder
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			if (File.Exists(fileName))
			{
				string json = File.ReadAllText(fileName);
				JsonSerializerSettings settings = new JsonSerializerSettings()
				{
					TypeNameHandling = TypeNameHandling.All
				};

				VsFolder? data = null;

				try
				{
					data = JsonConvert.DeserializeObject<VsFolder>(json, settings);
				}
				catch (System.Exception)
				{
					// probably wrong format
				}

				if (data is null)
				{
					// alert the user that the datafile was unreadable
					_ = MessageBox.Show($"The datafile was unreadable. Please check the file in '{fileName}' and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					data.Refresh();
					this.solutionGroups = data;
				}
			}
		}

		/// <summary>
		/// Saves the solution data.
		/// </summary>
		private void SaveSolutionData()
		{
			// 			if (true)
			// 			{
			// 				JsonSerializerSettings settings = new JsonSerializerSettings()
			// 				{
			// 					Formatting = Formatting.Indented,
			// 					TypeNameHandling = TypeNameHandling.All
			// 				};
			// 
			// 				string json = JsonConvert.SerializeObject(this.solutionGroups, settings);
			// 				// testing out storing the file in the google drive
			// 				new GoogleDriveStorage().Upload(json);
			// 			}

			// save this.solutionGroups data to a JSON file in the users data folder
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			try
			{
				string? dir = Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					// make sure the path exists
					_ = Directory.CreateDirectory(dir);
				}

				JsonSerializerSettings settings = new JsonSerializerSettings()
				{
					Formatting = Formatting.Indented,
					TypeNameHandling = TypeNameHandling.All
				};

				string json = JsonConvert.SerializeObject(this.solutionGroups, settings);
				try
				{
					File.WriteAllText(fileName, json);
				}
				catch (System.Exception ex)
				{
					// alert user of an error saving the data
					_ = MessageBox.Show($"There was an error saving the data. \r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			catch (System.Exception ex)
			{
				_ = MessageBox.Show($"There was an error saving the data. \r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Rebuilds the filters.
		/// </summary>
		private void RebuildFilters()
		{
			this.olvFiles.ModelFilter = string.IsNullOrEmpty(this.txtFilter.Text) ? null : new TextMatchFilter(this.olvFiles, this.txtFilter.Text);
			// this.olvFiles.AdditionalFilter = filters.Count == 0 ? null : new CompositeAllFilter(filters);
		}

		/// <summary>
		/// Updates the list.
		/// </summary>
		private void UpdateList()
		{
			// TODO: must verify items before loading, indicate missing items through warning icon
			this.olvFiles.SetObjects(this.solutionGroups.Items);

			IterateAndExpandItems();
			// this.olvFiles.ExpandAll();
		}

		private void IterateAndExpandItems()
		{
			foreach (var item in this.olvFiles.Objects)
			{
				if (item is VsFolder folder)
				{
					if (folder.Expanded)
					{
						this.olvFiles.Expand(item);
					}
					else
					{
						this.olvFiles.Collapse(item);
					}
				}
			}
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
			w -= (34 * 6) + 12; // 6 buttons + spacer
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
			}
			if(this.olvFiles.SelectedObject is null)
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

	}
}
