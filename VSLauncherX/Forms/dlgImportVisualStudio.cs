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
	/// <summary>
	/// The dlg import visual studio.
	/// </summary>
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
			this.OnlyDefaultInstances = Properties.Settings.Default.OnlyDefaultInstances;
		}

		/// <summary>
		/// Gets a value indicating whether only default instances.
		/// </summary>
		public bool OnlyDefaultInstances { get; private set; }

		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public VsFolder Solution { get; private set; }

		/// <summary>
		/// btns the ok_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnOk_Click(object sender, EventArgs e)
		{
			this.Solution.Name = "";

			// remove not selected items from the solutions list
			VsFolder sg = new VsFolder("", "");
			sg.Items = ImportHelper.FilterCheckedItems(this.Solution.Items, this.olvFiles.CheckedObjects);
			sg.Checked = false;

			this.Solution.Items = sg.Items;
		}

		/// <summary>
		/// Handles Click events for the btnSelectAfter button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnRefresh_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// chks the default instance_ checked changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void chkDefaultInstance_CheckedChanged(object sender, EventArgs e)
		{
			this.OnlyDefaultInstances = chkDefaultInstance.Checked;
			Properties.Settings.Default.OnlyDefaultInstances = this.OnlyDefaultInstances;
			this.UpdateList();
		}

		/// <summary>
		/// Creates the described renderer.
		/// </summary>
		/// <returns>A DescribedTaskRenderer.</returns>
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

		/// <summary>
		/// Initializes the list.
		/// </summary>
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
				return x is VsFolder f ? string.IsNullOrEmpty(f.Path) : false;
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is VsFolder sg ? sg.Items : (IEnumerable?)null;
			};

			// take care of check states
			this.olvFiles.CheckStateGetter = ColumnHelper.GetCheckState;
			this.olvFiles.CheckStatePutter = delegate (object rowObject, CheckState newValue)
			{
				var cs = ColumnHelper.SetCheckState(rowObject, newValue);
				this.olvFiles.Invalidate();
				return cs;
			};

			//
			// setup the Name/Filename column
			//
			this.itemRenderer = CreateDescribedRenderer();

			this.olvFiles.TreeColumnRenderer.SubRenderer = this.itemRenderer;
			this.olvColumnFilename.AspectName = "Name";
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForMru;
			this.olvColumnFilename.CellPadding = new Rectangle(4, 2, 4, 2);
		}

		/// <summary>
		/// lists the view files_ cell tool tip showing.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
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

		/// <summary>
		/// Updates the list.
		/// </summary>
		private void UpdateList()
		{
			this.Cursor = Cursors.WaitCursor;
			this.olvFiles.Items.Clear();
			this.olvFiles.ClearObjects();

			var items = this.visualStudioVersions.GetRecentProjects(this.OnlyDefaultInstances);

			this.Solution = new VsFolder
			{
				Items = items
			};

			this.olvFiles.SetObjects(items);
			this.olvFiles.ExpandAll();
			this.Cursor = Cursors.Default;
		}

		/// <summary>
		/// dlgs the import visual studio_ load.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void dlgImportVisualStudio_Load(object sender, EventArgs e)
		{
			UpdateList();
		}
	}
}
