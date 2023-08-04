using System.Diagnostics;
using BrightIdeasSoftware;
using Newtonsoft.Json;
using VSLauncher.DataModel;
using VSLauncher.Forms;
using VSLauncher.Helpers;

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

		private DescribedTaskRenderer? itemRenderer;
		private VsFolder solutionGroups = new VsFolder();
		private VisualStudioInstanceManager visualStudioInstances = new VisualStudioInstanceManager();

		/// <summary>
		/// Initializes a new instance of the <see cref="MainDialog"/> class.
		/// </summary>
		public MainDialog()
		{
			LoadSolutionData();

			this.solutionGroups.Items.OnChanged += SolutionData_OnChanged;

			InitializeComponent();
			InitializeListview(this.solutionGroups.Items);
			SetupDragAndDrop();

			// BuildTestData();

			this.bInUpdate = true;

			if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedVSversion))
			{
				var v = this.selectVisualStudioVersion.Versions.Where(v => v.Identifier == Properties.Settings.Default.SelectedVSversion).Single();
				this.selectVisualStudioVersion.SelectedItem = v;
			}
			else
			{
				this.selectVisualStudioVersion.SelectedIndex = 0;
			}

			this.bInUpdate = false;

			UpdateList();
		}

		/// <summary>
		/// Is the control key pressed.
		/// </summary>
		/// <returns>A bool.</returns>
		private static bool IsControlPressed()
		{
			return (Control.ModifierKeys & Keys.Control) == Keys.Control;
		}

		/// <summary>
		/// Builds the test data.
		/// </summary>
		private void BuildTestData()
		{
			var sg1 = new VsFolder("Main", "");
			sg1.RunBefore = new VsItem("Explorer", "explorer.exe", null);
			sg1.Items.Add(new VsSolution("ObjectListView", @"C:\dev\Repos\ObjectListViewDemo\ObjectListView2012.sln"));

			var sg2 = new VsFolder("Some Group", "");
			sg2.RunAsAdmin = true;

			sg2.Items.Add(new VsSolution("Solution 1", @"C:\TestSolution1.sln"));
			sg2.Items.Add(new VsSolution("Solution 2", @"C:\Solution2\TestSolution2.sln"));
			sg2.Items.Add(new VsSolution("Solution 3", @"C:\Solution3\TestSolution3.sln"));

			var sg3 = new VsFolder ("small sub group", "");
			sg3.RunBefore = new VsItem("Explorer", "explorer.exe", null);
			sg3.RunAsAdmin = true;
			sg3.RunAfter = new VsItem("Explorer", "explorer.exe", null);

			sg3.Items.Add(new VsSolution("Solution 1", @"C:\TestSolution1.sln"));
			sg3.Items.Add(new VsSolution("Solution 2", @"C:\Solution2\TestSolution2.sln"));

			sg2.Items.Add(sg3);

			solutionGroups.Items.Add(sg1);
			solutionGroups.Items.Add(sg2);
			UpdateList();
		}

		/// <summary>
		/// Creates the described task renderer.
		/// </summary>
		/// <returns>A DescribedTaskRenderer.</returns>
		private DescribedTaskRenderer CreateDescribedRenderer()
		{
			// Let's create an appropriately configured renderer.
			DescribedTaskRenderer renderer = new DescribedTaskRenderer();

			// Give the renderer its own collection of images.
			// If this isn't set, the renderer will use the SmallImageList from the ObjectListView.
			// (this is standard Renderer behaviour, not specific to DescribedTaskRenderer).
			renderer.ImageList = this.imageListMainIcons;

			// Tell the renderer which property holds the text to be used as a description
			renderer.DescriptionGetter = ColumnHelper.GetDescription;

			// Change the formatting slightly
			renderer.TitleFont = new Font("Verdana", 11, FontStyle.Bold);
			renderer.DescriptionFont = new Font("Verdana", 8);
			renderer.ImageTextSpace = 8;
			renderer.TitleDescriptionSpace = 1;

			// Use older Gdi renderering, since most people think the text looks clearer
			renderer.UseGdiTextRendering = true;
			renderer.Aspect = "Name";

			return renderer;
		}

		/// <summary>
		/// deletes the tool strip menu item_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var item = olvFiles.SelectedItem.RowObject;

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
			if (owner != null)
			{
				owner.Items.Remove((VsItem)item);
			}

			this.SolutionData_OnChanged(true);
		}

		/// <summary>
		/// Initializes the listview.
		/// </summary>
		/// <param name="list">The list.</param>
		private void InitializeListview(VsItemList list)
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 56;
			this.olvFiles.UseHotItem = false;
			this.olvFiles.OwnerDraw = true;

			// Add a more interesting focus for editing operations
			// this.olvFiles.AddDecoration(new EditingCellBorderDecoration(true));
			this.olvFiles.TreeColumnRenderer.IsShowLines = true;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;
			this.olvFiles.TreeColumnRenderer.CornerRoundness = 0;
			this.olvFiles.TreeColumnRenderer.FillBrush = new SolidBrush(Color.CornflowerBlue);
			this.olvFiles.TreeColumnRenderer.FramePen = new Pen(Color.DarkBlue, 1);

			this.olvFiles.UseFilterIndicator = true;

			// The following line makes getting aspect about 10x faster. Since getting the aspect is
			// the slowest part of building the ListView, it is worthwhile BUT NOT NECESSARY to do.
			TypedObjectListView<VsItem> tlist = new TypedObjectListView<VsItem>(this.olvFiles);
			tlist.GenerateAspectGetters();

			//
			// setup the files list
			//
			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is VsFolder;
			};
			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				if (x is VsFolder sg)
				{
					return sg.Items;
				}

				return null;
			};

			this.itemRenderer = CreateDescribedRenderer();

			this.olvFiles.TreeColumnRenderer.SubRenderer = this.itemRenderer;
			this.olvColumnFilename.AspectName = "Name";
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForFile;
			this.olvColumnFilename.CellPadding = new Rectangle(4, 2, 4, 2);

			// 			//
			// 			// setup the Path column
			// 			//
			// 			this.olvColumnPath.AspectGetter = ColumnHelper.GetAspectForPath;
			// 			this.olvColumnPath.Hideable = false;
			// 			this.olvColumnPath.UseFiltering = false;

			//
			// setup the Date column
			//
			this.olvColumnDate.AspectGetter = ColumnHelper.GetAspectForDate;

			//
			// setup the Options column
			//
			this.olvColumnOptions.AspectGetter = ColumnHelper.GetAspectForOptions;
			this.olvColumnOptions.ToolTipText = "*******";

			// Show the attributes for this object
			// A FlagRenderer masks off various values and draws zero or more images based
			// on the presence of individual bits.
			FlagRenderer attributesRenderer = new FlagRenderer
			{
				ImageList = imageList3
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

			// 			this.olvFiles.SetObjects(list);
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
				var data = JsonConvert.DeserializeObject<VsFolder>(json, settings);

				if (data is null)
				{
					// alert the user that the datafile was unreadable
					MessageBox.Show($"The datafile was unreadable. Please check the file in '{fileName}' and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					data.Refresh();
					this.solutionGroups = data;
				}
			}
		}

		/// <summary>
		/// Mains the dialog_ load.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void MainDialog_Load(object sender, EventArgs e)
		{
			if (Properties.Settings.Default.AppState == "saved")
			{
				// we don't want a minimized window at startup
				this.WindowState = Properties.Settings.Default.AppWindow == FormWindowState.Minimized ? FormWindowState.Normal : Properties.Settings.Default.AppWindow;
				this.Location = Properties.Settings.Default.AppLocation;
				this.Size = Properties.Settings.Default.AppSize;
			}

			txtFilter.Focus();
		}

		/// <summary>
		/// mains the folder add_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void mainFolderAdd_Click(object sender, EventArgs e)
		{
			var dlg = new dlgAddFolder();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					solutionGroups.Items.Add(dlg.Solution);
				}
				else
				{
					if (r.RowObject is VsFolder sg)
					{
						sg.Items.Add(dlg.Solution);
					}
					else
					{
						var p = this.solutionGroups.FindParent(r.RowObject);

						p?.Items.Insert(p.Items.IndexOf((VsItem)r.RowObject), dlg.Solution);
					}
				}

				this.SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// mains the import folder_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void mainImportFolder_Click(object sender, EventArgs e)
		{
			var dlg = new dlgImportFolder();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					// nothing selected, add to the end
					this.solutionGroups.Items.Add(dlg.Solution.Items.First());
				}
				else
				{
					if (r.RowObject is VsFolder sg)
					{
						// add below selected item
						sg.Items.Add(dlg.Solution.Items.First());
					}
					else
					{
						// add at the end
						this.solutionGroups.Items.Add(dlg.Solution.Items.First());
					}
				}

				this.SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// mains the import v s_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void mainImportVS_Click(object sender, EventArgs e)
		{
			var dlg = new dlgImportVisualStudio();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					solutionGroups = dlg.Solution;
				}
				else
				{
					if (r.RowObject is VsFolder sg)
					{
						sg.Items.Add(dlg.Solution);
					}
					else
					{
					}
				}

				this.SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// mains the refresh_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
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
		/// <param name="e">The e.</param>
		private void mainSettings_Click(object sender, EventArgs e)
		{
			var dlg = new dlgSettings();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// olvs the files_ after label edit.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
		}

		/// <summary>
		/// lists the view files_ cell click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_CellClick(object sender, CellClickEventArgs e)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("clicked ({0}, {1}). model {2}. click count: {3}", e.RowIndex, e.ColumnIndex, e.Model, e.ClickCount));
		}

		/// <summary>
		/// olvs the files_ cell edit finished.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_CellEditFinished(object sender, CellEditEventArgs e)
		{
		}

		/// <summary>
		/// lists the view files_ cell right click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_CellRightClick(object sender, CellRightClickEventArgs e)
		{
			// System.Diagnostics.Trace.WriteLine(String.Format("right clicked {0}, {1}). model {2}", e.RowIndex, e.ColumnIndex, e.Model));

			e.MenuStrip = this.ctxMenu;
			e.MenuStrip.Show();
		}

		/// <summary>
		/// lists the view files_ cell tool tip showing.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			if (e.Model is VsFolder)
			{
				e.Text = e.Item.Text;
			}
			else
			{
				e.Text = e.SubItem.Text;
			}
		}

		/// <summary>
		/// olvs the files_ double click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_DoubleClick(object sender, EventArgs e)
		{
			this.runToolStripMenuItem_Click(sender, e);
		}

		/// <summary>
		/// olvs the files_ dropped.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_Dropped(object sender, OlvDropEventArgs e)
		{
		}

		/// <summary>
		/// olv_S the hot item changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_HotItemChanged(object sender, HotItemChangedEventArgs e)
		{
			if (sender == null)
			{
				this.toolStripStatusLabel3.Text = "";
				return;
			}
		}

		/// <summary>
		/// lists the view files_ item activate.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_ItemActivate(object sender, EventArgs e)
		{
			Object rowObject = this.olvFiles.SelectedObject;
			if (rowObject == null)
				return;

			if (rowObject is DirectoryInfo)
			{
			}
			else
			{
				// ShellUtilities.Execute(((FileInfo)rowObject).FullName);
			}
		}

		/// <summary>
		/// olvs the files_ model can drop handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
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
				var obj = e.SourceModels[0];

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
		/// olvs the files_ model dropped handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_ModelDroppedHandler(object sender, ModelDropEventArgs e)
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
					var parent = solutionGroups.FindParent(source);
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

		/// <summary>
		/// olvs the files_ selected index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void olvFiles_SelectedIndexChanged(object sender, EventArgs e)
		{
			// update status bar text with info on selected item
			if (olvFiles.SelectedItem != null)
			{
				var item = olvFiles.SelectedItem.RowObject;
				if (item is VsSolution s)
				{
					mainStatusLabel.Text = $"Visual Studio Solution: {s.Projects?.Count} Projects, {s.TypeAsName()}";
				}
				else if (item is VsProject p)
				{
					mainStatusLabel.Text = $"Visual Studio Project: {p.TypeAsName()}, .NET {p.FrameworkVersion}";
				}
				else if (item is VsFolder sg)
				{
					mainStatusLabel.Text = $"Contains {sg.ContainedSolutionsCount()} solution{((sg.ContainedSolutionsCount() != 1) ? 's' : "")}";
				}
			}
			else
			{
				mainStatusLabel.Text = string.Empty;
			}
		}

		/// <summary>
		/// Rebuilds the filters.
		/// </summary>
		private void RebuildFilters()
		{
			this.olvFiles.ModelFilter = String.IsNullOrEmpty(this.txtFilter.Text) ? null : new TextMatchFilter(this.olvFiles, this.txtFilter.Text);
			// this.olvFiles.AdditionalFilter = filters.Count == 0 ? null : new CompositeAllFilter(filters);
		}

		/// <summary>
		/// renames the tool strip menu item_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void renameToolStripMenuItem_Click(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// runs the as admin tool strip menu item_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void runAsAdminToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var item = olvFiles.SelectedItem.RowObject;
			var vs = this.selectVisualStudioVersion.SelectedItem as VisualStudioInstance ?? throw new InvalidCastException();

			if (item is VsFolder sg)
			{
				new ItemLauncher(sg, vs).Launch(true);
			}
			else if (item is VsSolution s)
			{
				new ItemLauncher(s, vs).Launch(true);
			}
		}

		/// <summary>
		/// runs the tool strip menu item_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void runToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var item = olvFiles.SelectedItem.RowObject;
			var vs = this.selectVisualStudioVersion.SelectedItem as VisualStudioInstance ?? throw new InvalidCastException();

			if (item is VsFolder f)
			{
				new ItemLauncher(f, vs).Launch();
			}
			else if (item is VsSolution s)
			{
				vs = visualStudioInstances.GetByVersion(s.RequiredVersion) ?? vs;
				new ItemLauncher(s, vs).Launch();
			}
			else if (item is VsProject p)
			{
				vs = visualStudioInstances.GetByIdentifier(p.VsVersion) ?? vs;
				new ItemLauncher(p, vs).Launch();
			}
		}

		/// <summary>
		/// Saves the solution data.
		/// </summary>
		private void SaveSolutionData()
		{
			// save this.solutionGroups data to a JSON file in the users data folder
			string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VSLauncher", "VSLauncher.json");

			try
			{
				var dir = Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					// make sure the path exists
					Directory.CreateDirectory(dir);
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
					MessageBox.Show($"There was an error saving the data. \r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show($"There was an error saving the data. \r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// selects the visual studio version_ draw item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void selectVisualStudioVersion_DrawItem(object sender, DrawItemEventArgs e)
		{
			// draw the selected item with the Visual Studio Icon and the version as text
			if (e.Index >= 0 && e.Index <= this.visualStudioInstances.Count)
			{
				e.DrawBackground();

				var height = 16; // selectVisualStudioVersion.Height - (selectVisualStudioVersion.Margin.Top+selectVisualStudioVersion.Margin.Bottom);

				Rectangle iconRect = new Rectangle(e.Bounds.Left + selectVisualStudioVersion.Margin.Left,
													e.Bounds.Top + ((selectVisualStudioVersion.ItemHeight - height) / 2),
													height, height);
				e.Graphics.DrawIcon(visualStudioInstances[e.Index].AppIcon, iconRect);
				e.Graphics.DrawString(visualStudioInstances[e.Index].Name, e.Font!, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
			}
		}

		/// <summary>
		/// selects the visual studio version_ selected index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void selectVisualStudioVersion_SelectedIndexChanged(object sender, EventArgs e)
		{
			// update all buttons with icon from selected visual studio version
			if (selectVisualStudioVersion.SelectedIndex >= 0 && selectVisualStudioVersion.SelectedIndex < visualStudioInstances.Count)
			{
				// save the selected item as last selected item
				if (!this.bInUpdate)
				{
					Properties.Settings.Default.SelectedVSversion = visualStudioInstances[selectVisualStudioVersion.SelectedIndex].Identifier;
					Properties.Settings.Default.Save();
				}

				// set icon and text according to selection onto all buttons
				var icon = visualStudioInstances[selectVisualStudioVersion.SelectedIndex].AppIcon.ToBitmap();
				string shortName = visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ShortName;

				btnMainStartVisualStudio1.Text = string.Format((string)btnMainStartVisualStudio1.Tag, shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio1, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio1.Image = icon;

				btnMainStartVisualStudio2.Text = string.Format((string)btnMainStartVisualStudio2.Tag, shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio2, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio2.Image = icon;

				btnMainStartVisualStudio3.Text = string.Format((string)btnMainStartVisualStudio3.Tag, shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio3, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio3.Image = icon;

				btnMainStartVisualStudio4.Text = string.Format((string)btnMainStartVisualStudio4.Tag, shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio4, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio4.Image = icon;

				btnMainStartVisualStudio5.Text = string.Format((string)btnMainStartVisualStudio5.Tag, shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio5, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio5.Image = icon;
			}
		}

		/// <summary>
		/// settings the tool strip menu item_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			object item = olvFiles.SelectedItem.RowObject;
			var dlg = new dlgExecuteVisualStudio(item);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.SolutionData_OnChanged(true);
			}
		}

		/// <summary>
		/// Setups the drag and drop.
		/// </summary>
		private void SetupDragAndDrop()
		{
			// Setup the tree so that it can drop and drop.
			// Drag and drop support
			// You can set up drag and drop explicitly (like this) or, in the IDE, you can set
			// IsSimpleDropSource and IsSimpleDragSource and respond to CanDrop and Dropped events

			this.olvFiles.DragSource = new SimpleDragSource();
			this.olvFiles.IsSimpleDragSource = true;
			this.olvFiles.IsSimpleDropSink = true;

			this.olvFiles.ModelCanDrop += olvFiles_ModelCanDropHandler;
			this.olvFiles.ModelDropped += olvFiles_ModelDroppedHandler;
		}

		/// <summary>
		/// Solutions the data_ on changed.
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

		#region Main Buttons

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio1 button (Start VS)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnMainStartVisualStudio1_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];
			vs.Execute();
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio2 button (Start VS as admin)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnMainStartVisualStudio2_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];
			vs.ExecuteAsAdmin();
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio3 button (Start VS with an instance)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnMainStartVisualStudio3_Click(object sender, EventArgs e)
		{
			var dlg = new dlgNewInstance();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.Cursor = Cursors.WaitCursor;
				var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];

				vs.ExecuteWithInstance(IsControlPressed(), dlg.InstanceName);
				this.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio4 button (Start VS with new project)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnMainStartVisualStudio4_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];
			vs.ExecuteNewProject(IsControlPressed());
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Handles click on the btnMainStartVisualStudio5 button (Start VS with dialog)
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnMainStartVisualStudio5_Click(object sender, EventArgs e)
		{
			var dlg = new dlgExecuteVisualStudio(selectVisualStudioVersion.SelectedIndex);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				this.Cursor = Cursors.WaitCursor;
				var vs = dlg.VsVersion;

				if (dlg.Item is not null)
				{
					vs.ExecuteWith(dlg.Item.RunAsAdmin, dlg.Item.ShowSplash, dlg.Item.Path!, dlg.Item.Instance, dlg.Item.Commands);
				}

				this.Cursor = Cursors.Default;
			}
		}

		#endregion Main Buttons

		/// <summary>
		/// Handles text changes in the filter field and updates the list
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtFilter_TextChanged(object sender, EventArgs e)
		{
			RebuildFilters();
		}

		/// <summary>
		/// Updates the list.
		/// </summary>
		private void UpdateList()
		{
			// TODO: must verify items before loading, indicate missing items through warning icon
			this.olvFiles.SetObjects(this.solutionGroups.Items);
			this.olvFiles.ExpandAll();
		}

		/// <summary>
		/// Handles the form closing, saves state
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
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

			Properties.Settings.Default.AppState = "saved";

			// don't forget to save the settings
			Properties.Settings.Default.Save();
		}
	}
}