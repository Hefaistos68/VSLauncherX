using System.Collections;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using BrightIdeasSoftware;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLauncher
{
	public partial class dlgImportVisualStudio : Form
	{
		private DescribedTaskRenderer itemRenderer;

		private VisualStudioInstanceManager visualStudioVersions = new VisualStudioInstanceManager();

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgImportVisualStudio"/> class.
		/// </summary>
		public dlgImportVisualStudio()
		{
			InitializeComponent();
			InitializeList();
		}

		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public VsFolder Solution { get; private set; }
		
		private void btnOk_Click(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// Handles Click events for the btnSelectAfter button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnRefresh_Click(object sender, EventArgs e)
		{

		}

		private DescribedTaskRenderer CreateDescribedRenderer()
		{
			// Let's create an appropriately configured renderer.
			DescribedTaskRenderer renderer = new DescribedTaskRenderer
			{
				DescriptionGetter = ColumnHelper.GetDescription,

				// Change the formatting slightly
				TitleFont = new Font("Verdana", 11, FontStyle.Bold),
				DescriptionFont = new Font("Verdana", 8),
				ImageTextSpace = 8,
				TitleDescriptionSpace = 1,

				// Use older Gdi renderering, since most people think the text looks clearer
				UseGdiTextRendering = true
			};

			return renderer;
		}

		private void InitializeList()
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 56;
			this.olvFiles.OwnerDraw = true;

			this.olvFiles.HierarchicalCheckboxes = true;
			this.olvFiles.TreeColumnRenderer.IsShowLines = true;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;

			// 			TypedObjectListView<SolutionGroup> tlist = new TypedObjectListView<SolutionGroup>(this.olvFiles);
			// 			tlist.GenerateAspectGetters();

			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is VsFolder;
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is VsFolder sg ? sg.Items : (IEnumerable?)null;
			};
			//
			// setup the Name/Filename column
			//
			this.itemRenderer = CreateDescribedRenderer();

			this.olvFiles.TreeColumnRenderer.SubRenderer = this.itemRenderer;
			this.olvColumnFilename.AspectName = "Name";
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForMru;
			this.olvColumnFilename.CellPadding = new Rectangle(4, 2, 4, 2);
			// 			this.olvColumnFilename.AspectGetter = ColumnHelper.GetAspectForFile;

			this.Solution = new VsFolder();
			var items = this.visualStudioVersions.GetRecentProjects();

			this.Solution.Items = items;

			this.olvFiles.SetObjects(items);
			this.olvFiles.ExpandAll();
		}
		
		private void listViewFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
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

	}



}
