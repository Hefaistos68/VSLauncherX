using BrightIdeasSoftware;

using Microsoft.Win32;

using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The main dialog.
	/// </summary>
	public partial class MainDialog : Form
	{
		private List<SolutionGroup> solutionGroups = new List<SolutionGroup>();
		private VisualStudioInstanceManager visualStudioInstances = new VisualStudioInstanceManager();

		/// <summary>
		/// Initializes a new instance of the <see cref="MainDialog"/> class.
		/// </summary>
		public MainDialog()
		{
			InitializeComponent();
			InitializeListview(this.solutionGroups);

			BuildTestData();


			if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedVSversion))
			{
				foreach (var v in this.selectVisualStudioVersion.Versions)
				{
					if (v.Identifier == Properties.Settings.Default.SelectedVSversion)
					{
						this.selectVisualStudioVersion.SelectedItem = v;
						break;
					}
				}

			}
			else
			{
				this.selectVisualStudioVersion.SelectedIndex = 0;
			}

			UpdateList();
		}

		/// <summary>
		/// Gets a value indicating whether show tool tips on files.
		/// </summary>
		public bool showToolTipsOnFiles { get; private set; }
		/// <summary>
		/// Move the given item to the given index in the given group
		/// </summary>
		/// <remarks>The item and group must belong to the same ListView</remarks>
		public void MoveToGroup(ListViewItem lvi, ListViewGroup group, int indexInGroup)
		{
			group.ListView?.BeginUpdate();
			lvi.Group = null;
			ListViewItem[] items = new ListViewItem[group.Items.Count + 1];
			group.Items.CopyTo(items, 0);
			Array.Copy(items, indexInGroup, items, indexInGroup + 1, group.Items.Count - indexInGroup);
			items[indexInGroup] = lvi;
			for (int i = 0; i < items.Length; i++)
				items[i].Group = null;
			for (int i = 0; i < items.Length; i++)
				group.Items.Add(items[i]);
			group.ListView?.EndUpdate();
		}

		private void BuildTestData()
		{
			var sg1 = new SolutionGroup("Main");
			sg1.RunBefore = new VsItem("Explorer", "explorer.exe");
			sg1.Solutions.Add(new VsSolution("ObjectListView", @"C:\dev\Repos\ObjectListViewDemo\ObjectListView2012.sln"));

			var sg2 = new SolutionGroup("Some Group");
			sg2.RunAsAdmin = true;

			sg2.Solutions.Add(new VsSolution("Solution 1", @"C:\TestSolution1.sln"));
			sg2.Solutions.Add(new VsSolution("Solution 2", @"C:\Solution2\TestSolution2.sln"));
			sg2.Solutions.Add(new VsSolution("Solution 3", @"C:\Solution3\TestSolution3.sln"));

			var sg3 = new SolutionGroup("small sub group");
			sg3.RunBefore = new VsItem("Explorer", "explorer.exe");
			sg3.RunAsAdmin = true;
			sg3.RunAfter = new VsItem("Explorer", "explorer.exe");

			sg3.Solutions.Add(new VsSolution("Solution 1", @"C:\TestSolution1.sln"));
			sg3.Solutions.Add(new VsSolution("Solution 2", @"C:\Solution2\TestSolution2.sln"));

			sg2.Solutions.Add(sg3);

			solutionGroups.Add(sg1);
			solutionGroups.Add(sg2);
			UpdateList();
		}

		private DescribedTaskRenderer CreateDescribedTaskRenderer()
		{
			// Let's create an appropriately configured renderer.
			DescribedTaskRenderer renderer = new DescribedTaskRenderer();

			// Give the renderer its own collection of images.
			// If this isn't set, the renderer will use the SmallImageList from the ObjectListView.
			// (this is standard Renderer behaviour, not specific to DescribedTaskRenderer).
			renderer.ImageList = this.imageListMainIcons;

			// Tell the renderer which property holds the text to be used as a description
			renderer.DescriptionAspectName = "Description";

			// Change the formatting slightly
			renderer.TitleFont = new Font("Tahoma", 11, FontStyle.Bold);
			renderer.DescriptionFont = new Font("Tahoma", 9);
			renderer.ImageTextSpace = 8;
			renderer.TitleDescriptionSpace = 1;

			// Use older Gdi renderering, since most people think the text looks clearer
			renderer.UseGdiTextRendering = true;

			return renderer;
		}

		private void InitializeListview(List<SolutionGroup> list)
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 26;

			// Add a more interesting focus for editing operations
			this.olvFiles.AddDecoration(new EditingCellBorderDecoration(true));
			this.olvFiles.TreeColumnRenderer.IsShowLines = false;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;


			// Uncomment this block to see a darker theme
			// 			this.olvFiles.UseAlternatingBackColors = false;
			// 			this.olvFiles.BackColor = Color.FromArgb(20, 20, 25);
			// 			this.olvFiles.AlternateRowBackColor = Color.FromArgb(40, 40, 45);
			// 			this.olvFiles.ForeColor = Color.WhiteSmoke;
			// 			this.olvFiles.DisabledItemStyle = new SimpleItemStyle();
			// 			this.olvFiles.DisabledItemStyle.ForeColor = Color.Gray;
			// 			this.olvFiles.DisabledItemStyle.BackColor = Color.FromArgb(30, 30, 35);
			// 			this.olvFiles.DisabledItemStyle.Font = new Font("Stencil", 10);

			// The following line makes getting aspect about 10x faster. Since getting the aspect is
			// the slowest part of building the ListView, it is worthwhile BUT NOT NECESSARY to do.
			TypedObjectListView<SolutionGroup> tlist = new TypedObjectListView<SolutionGroup>(this.olvFiles);
			tlist.GenerateAspectGetters();
			/* The line above the equivilent to typing the following:
			tlist.GetColumn(0).AspectGetter = delegate(SolutionGroup x) { return x.Name; };
			tlist.GetColumn(1).AspectGetter = delegate(SolutionGroup x) { return x.Occupation; };
			tlist.GetColumn(2).AspectGetter = delegate(SolutionGroup x) { return x.CulinaryRating; };
			tlist.GetColumn(3).AspectGetter = delegate(SolutionGroup x) { return x.YearOfBirth; };
			tlist.GetColumn(4).AspectGetter = delegate(SolutionGroup x) { return x.BirthDate; };
			tlist.GetColumn(5).AspectGetter = delegate(SolutionGroup x) { return x.GetRate(); };
			tlist.GetColumn(6).AspectGetter = delegate(SolutionGroup x) { return x.Comments; };
			*/

			//
			// setup the files list
			//
			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is SolutionGroup;
			};
			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				if (x is SolutionGroup sg)
				{
					return sg.Solutions;
				}

				return null;
			};

			//
			// setup the Name/Filename column
			//
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForFile;
			this.olvColumnFilename.AspectGetter = ColumnHelper.GetAspectForFile;

			//
			// setup the Path column
			//
			this.olvColumnPath.AspectGetter = ColumnHelper.GetAspectForPath;

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
			FlagRenderer attributesRenderer = new FlagRenderer();
			attributesRenderer.ImageList = imageList3;
			attributesRenderer.Add(eOptions.RunBeforeOn, "RunBefore");
			attributesRenderer.Add(eOptions.RunBeforeOff, "None");
			attributesRenderer.Add(eOptions.RunAsAdminOn, "RunAsAdmin");
			attributesRenderer.Add(eOptions.RunAsAdminOff, "None");
			attributesRenderer.Add(eOptions.RunAfterOn, "RunAfter");
			attributesRenderer.Add(eOptions.RunAfterOff, "None");
			this.olvColumnOptions.Renderer = attributesRenderer;

			// Tell the filtering subsystem that the attributes column is a collection of flags
			this.olvColumnOptions.ClusteringStrategy = new FlagClusteringStrategy(typeof(eOptions));

			// Drag and drop support
			// You can set up drag and drop explicitly (like this) or, in the IDE, you can set
			// IsSimpleDropSource and IsSimpleDragSource and respond to CanDrop and Dropped events

			this.olvFiles.DragSource = new SimpleDragSource();
			SimpleDropSink dropSink = new SimpleDropSink();
			this.olvFiles.DropSink = dropSink;
			dropSink.CanDropOnItem = true;
			//dropSink.CanDropOnSubItem = true;
			dropSink.FeedbackColor = Color.IndianRed; // just to be different

			dropSink.ModelCanDrop += new EventHandler<ModelDropEventArgs>(delegate (object sender, ModelDropEventArgs e)
			{
				SolutionGroup SolutionGroup = e.TargetModel as SolutionGroup;
				if (SolutionGroup == null)
				{
					e.Effect = DragDropEffects.None;
				}
				else
				{
					if (false)
					{
						e.Effect = DragDropEffects.None;
						e.InfoMessage = "Can't drop on someone who is already married";
					}
					else
					{
						e.Effect = DragDropEffects.Move;
					}
				}
			});

			dropSink.ModelDropped += new EventHandler<ModelDropEventArgs>(delegate (object sender, ModelDropEventArgs e)
			{
				if (e.TargetModel == null)
					return;
				/*
				// Change the dropped people plus the target SolutionGroup to be married
				((SolutionGroup)e.TargetModel).MaritalStatus = MaritalStatus.Married;
				foreach (SolutionGroup p in e.SourceModels)
					p.MaritalStatus = MaritalStatus.Married;
				*/
				// Force them to refresh
				e.ListView.RefreshObject(e.TargetModel);
				e.ListView.RefreshObjects(e.SourceModels);
			});

			this.olvFiles.SetObjects(list);
		}

		private void GetListOfInstalledVisualStudioVersions()
		{
		}

		private void listViewFiles_CellClick(object sender, CellClickEventArgs e)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("clicked ({0}, {1}). model {2}. click count: {3}",
				e.RowIndex, e.ColumnIndex, e.Model, e.ClickCount));
		}

		private void listViewFiles_CellRightClick(object sender, CellRightClickEventArgs e)
		{
			System.Diagnostics.Trace.WriteLine(String.Format("right clicked {0}, {1}). model {2}", e.RowIndex, e.ColumnIndex, e.Model));
			// Show a menu if the click was on first column
			if (e.ColumnIndex == 0)
			{
				e.MenuStrip = this.ctxMenu;
				e.MenuStrip.Show();
			}
		}

		private void listViewFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			if (e.Model is SolutionGroup)
			{
				e.Text = e.Item.Text;
			}
			else
			{
				e.Text = e.SubItem.Text;
			}
		}

		private void listViewFiles_ItemActivate(object sender, EventArgs e)
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

		private void mainFolderAdd_Click(object sender, EventArgs e)
		{
			var dlg = new dlgAddFolder();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					solutionGroups.Add(dlg.Solution);
				}
				else
				{
					if (r.RowObject is SolutionGroup sg)
					{
						sg.Solutions.Add(dlg.Solution);
					}
					else
					{

					}
				}

				UpdateList();
			}
		}

		private void mainImportFolder_Click(object sender, EventArgs e)
		{
			var dlg = new dlgImportFolder();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					// nothing selected, add to the end
					this.solutionGroups.Add(dlg.Solution);
				}
				else
				{
					if (r.RowObject is SolutionGroup sg)
					{
						// add below selected item
						sg.Solutions.Add(dlg.Solution);
					}
					else
					{
						// add at the end
						this.solutionGroups.Add(dlg.Solution);
					}
				}

				UpdateList();
			}
		}

		private void mainImportVS_Click(object sender, EventArgs e)
		{
			var dlg = new dlgImportVisualStudio();
			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				var r = this.olvFiles.SelectedItem;
				if (r == null)
				{
					solutionGroups.Add(dlg.Solution);
				}
				else
				{
					if (r.RowObject is SolutionGroup sg)
					{
						sg.Solutions.Add(dlg.Solution);
					}
					else
					{

					}
				}

				UpdateList();
			}
		}

		private void mainSettings_Click(object sender, EventArgs e)
		{
		}

		private void olv_HotItemChanged(object sender, HotItemChangedEventArgs e)
		{
			if (sender == null)
			{
				this.toolStripStatusLabel3.Text = "";
				return;
			}
		}

		private void treeListView_ModelCanDrop(object sender, ModelDropEventArgs e)
		{
			e.Effect = DragDropEffects.None;
			if (e.TargetModel != null)
			{
				if (e.TargetModel is DirectoryInfo)
					e.Effect = e.StandardDropActionFromKeys;
				else
					e.InfoMessage = "Can only drop on directories";
			}
		}

		private void treeListView_ModelDropped(object sender, ModelDropEventArgs e)
		{
			String msg = String.Format("{2} items were dropped on '{1}' as a {0} operation.",
				e.Effect, ((DirectoryInfo)e.TargetModel).Name, e.SourceModels.Count);
			MessageBox.Show(msg, "OLV Demo", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void UpdateList()
		{
			this.olvFiles.SetObjects(this.solutionGroups);
		}

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
				e.Graphics.DrawString(visualStudioInstances[e.Index].Name, e.Font, Brushes.Black, e.Bounds.Left + 20, e.Bounds.Top + 4);
			}
		}

		private void selectVisualStudioVersion_SelectedIndexChanged(object sender, EventArgs e)
		{
			// update all buttons with icon from selected visual studio version
			if (selectVisualStudioVersion.SelectedIndex >= 0 && selectVisualStudioVersion.SelectedIndex < visualStudioInstances.Count)
			{

				// save the selected item as last selected item
				Properties.Settings.Default.SelectedVSversion = visualStudioInstances[selectVisualStudioVersion.SelectedIndex].Identifier;

				// set icon and text according to selection onto all buttons
				var icon = visualStudioInstances[selectVisualStudioVersion.SelectedIndex].AppIcon.ToBitmap();
				string shortName = visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ShortName;

				btnMainStartVisualStudio1.Text = string.Format(btnMainStartVisualStudio1.Tag.ToString(), shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio1, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio1.Image = icon;

				btnMainStartVisualStudio2.Text = string.Format(btnMainStartVisualStudio2.Tag.ToString(), shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio2, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio2.Image = icon;

				btnMainStartVisualStudio3.Text = string.Format(btnMainStartVisualStudio3.Tag.ToString(), shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio3, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio3.Image = icon;

				btnMainStartVisualStudio4.Text = string.Format(btnMainStartVisualStudio4.Tag.ToString(), shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio4, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio4.Image = icon;

				btnMainStartVisualStudio5.Text = string.Format(btnMainStartVisualStudio5.Tag.ToString(), shortName);
				tooltipForButtons.SetToolTip(btnMainStartVisualStudio5, visualStudioInstances[selectVisualStudioVersion.SelectedIndex].ToString());
				btnMainStartVisualStudio5.Image = icon;
			}
		}

		private void btnMainStartVisualStudio1_Click(object sender, EventArgs e)
		{
			var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];
			vs.Execute();
		}

		private void btnMainStartVisualStudio2_Click(object sender, EventArgs e)
		{
			var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];
			vs.ExecuteAsAdmin();
		}

		private void btnMainStartVisualStudio3_Click(object sender, EventArgs e)
		{
			var dlg = new dlgNewInstance();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];

				vs.ExecuteWithInstance(IsControlPressed(), dlg.InstanceName);
			}
		}

		private static bool IsControlPressed()
		{
			return (Control.ModifierKeys & Keys.Control) == Keys.Control;
		}

		private void btnMainStartVisualStudio4_Click(object sender, EventArgs e)
		{
			var vs = visualStudioInstances[selectVisualStudioVersion.SelectedIndex];
			vs.ExecuteNewProject(IsControlPressed());
		}

		private void btnMainStartVisualStudio5_Click(object sender, EventArgs e)
		{
			var dlg = new dlgExecuteVisualStudio(selectVisualStudioVersion.SelectedIndex);

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				var vs = dlg.Instance;
				vs.ExecuteWith(dlg.AsAdmin, dlg.ShowSplash, dlg.ProjectOrSolution, dlg.InstanceName, dlg.Command);
			}
		}

		private void olvFiles_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{

		}

		private void olvFiles_DoubleClick(object sender, EventArgs e)
		{

		}

		private void olvFiles_CellEditFinished(object sender, CellEditEventArgs e)
		{

		}

		private void olvFiles_Dropped(object sender, OlvDropEventArgs e)
		{

		}
	}
}