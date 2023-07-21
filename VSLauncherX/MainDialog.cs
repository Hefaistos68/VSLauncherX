using BrightIdeasSoftware;

using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The main dialog.
	/// </summary>
	public partial class MainDialog : Form
	{
		private List<SolutionGroup> solutionGroups = new List<SolutionGroup>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MainDialog"/> class.
		/// </summary>
		public MainDialog()
		{
			InitializeComponent();
			InitializeListview(this.solutionGroups);
			BuildTestData();
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
			var sg1 = new SolutionGroup("Test Group 1");
			sg1.RunBefore = new VsItem("Explorer", "explorer.exe");
			sg1.Solutions.Add(new VsSolution("Solution 1", @"C:\TestSolution1.sln"));
			sg1.Solutions.Add(new VsSolution("Solution 2", @"C:\Solution2\TestSolution2.sln"));
			sg1.Solutions.Add(new VsSolution("Solution 3", @"C:\Solution3\TestSolution3.sln"));
			sg1.Solutions.Add(new VsProject("AProject", @"C:\repo\website\project.csproj", eProjectType.CSProject));

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
				e.MenuStrip = this.contextMenuStrip2;
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
	}
}