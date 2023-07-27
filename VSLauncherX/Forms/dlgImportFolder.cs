using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BrightIdeasSoftware;

using VSLauncher.DataModel;

namespace VSLauncher
{
	public partial class dlgImportFolder : Form
	{
		private readonly List<string> extensionsHandled  = new List<string>() { ".sln", ".csproj", ".tsproj", ".esproj", ".vcxproj", ".fsproj", ".vbproj"};
		private bool bSolutionOnly;

		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public VsFolder Solution { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgAddFolder"/> class.
		/// </summary>
		public dlgImportFolder()
		{
			InitializeComponent();
			InitializeList();
			this.Solution = new VsFolder();
			this.chkSolutionOnly.Checked = true;
		}

		/// <summary>
		/// Initializes the list.
		/// </summary>
		private void InitializeList()
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 26;

			this.olvFiles.HierarchicalCheckboxes = true;
			this.olvFiles.TreeColumnRenderer.IsShowLines = true;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;

			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is VsFolder f ? f.Items.Count > 0 : false;
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is VsFolder f ? f.Items : (IEnumerable?)null;
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
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForFile;
			this.olvColumnFilename.AspectGetter = ColumnHelper.GetAspectForFile;
		}

		/// <summary>
		/// Handles Click events for the btnSelectAfter button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnSelectFolder_Click(object sender, EventArgs e)
		{
			// let the user select a folder through the system dialog
			using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
			{
				folderBrowserDialog.ShowNewFolderButton = false;

				// read the last folder from the application settings and set it as the initial folder
				folderBrowserDialog.SelectedPath = Properties.Settings.Default.LastImportFolder;

				folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

				if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				{
					//Get the path of specified file
					var folderPath = folderBrowserDialog.SelectedPath;
					txtFoldername.Text = folderPath;

					// store current folderPath in application settings
					Properties.Settings.Default.LastImportFolder = folderPath;


					this.Cursor = Cursors.WaitCursor;
					UpdateList();
					this.Cursor = Cursors.Default;
				}
			}
		}


		/// <summary>
		/// Iterates the folder.
		/// </summary>
		/// <param name="folderPath">The folder path.</param>
		/// <returns>An IEnumerable.</returns>
		private VsFolder IterateFolder(string folderPath, bool bOnlySolutions)
		{
			VsItemList sg = new(null);

			var root = new VsFolder(Path.GetFileName(folderPath), folderPath);

			// iterate through all files in the folder, returning all folders and subfolders, filter for files matching solution and project files
			foreach (var folder in System.IO.Directory.GetDirectories(folderPath))
			{
				// get the attributes and check if the folder is hidden
				if (!folder.StartsWith('.'))
				{
					var attributes = System.IO.File.GetAttributes(folder);
					if (!attributes.HasFlag(FileAttributes.Hidden))
					{
						var subItem = IterateFolder(folder, bOnlySolutions);

						if (subItem.Items.Count > 0)
						{
							root.Items.Add(subItem);
						}
					}
				}
			}

			foreach (var file in System.IO.Directory.GetFiles(folderPath))
			{
				if (!file.StartsWith('.'))
				{
					if (IsOfInterest(file, bOnlySolutions))
					{
						var item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file);
						item.LastModified = new FileInfo(file).LastAccessTime;

						root.Items.Add(item);
					}
				}
			}

			return root;
		}

		/// <summary>
		/// Are the of interest.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <param name="bOnlySolutions">If true, b only solutions.</param>
		/// <returns>A bool.</returns>
		private bool IsOfInterest(string file, bool bOnlySolutions)
		{
			if (bOnlySolutions)
			{
				return Path.GetExtension(file).ToLower() == ".sln";
			}

			return this.extensionsHandled.Contains(Path.GetExtension(file).ToLower());
		}

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtFoldername_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = txtFoldername.Text.Length > 0;
			btnRefresh.Enabled = txtFoldername.Text.Length > 0;
		}

		/// <summary>
		/// btns the ok_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnOk_Click(object sender, EventArgs e)
		{
			this.Solution.Name = txtFoldername.Text;

			// remove not selected items from the solutions list
			VsFolder sg = new VsFolder(txtFoldername.Text, txtFoldername.Text);
			sg.Items = FilterItems(this.Solution.Items, this.olvFiles.CheckedObjects);
			sg.Checked = false;
		}

		/// <summary>
		/// Filters the items.
		/// </summary>
		/// <param name="origin">The origin.</param>
		/// <param name="checkedItems">The checked items.</param>
		/// <returns>A VsItemList.</returns>
		private VsItemList FilterItems(VsItemList origin, IList checkedItems)
		{
			VsItemList list = new VsItemList(null);

			bool bAdded = false;
			foreach (var i in origin)
			{
				if(checkedItems.Contains(i))
				{
					list.Add(i);
					bAdded = true;
				}

				if (i is VsFolder f)
				{
					var fi = FilterItems2(f.Items, checkedItems);
					fi.Reparent(i);
					list.Changed = true;

					if(!bAdded)
					{
						// may be because the parent item is not in the checkedItems list
						list.Add(i);
					}
				}

				bAdded = false;
			}

			return list;
		}
		/// <summary>
		/// Filters the items2.
		/// </summary>
		/// <param name="origin">The origin.</param>
		/// <param name="checkedItems">The checked items.</param>
		/// <returns>A VsItemList.</returns>
		private VsItemList FilterItems2(VsItemList origin, IList checkedItems)
		{
			VsItemList list = new VsItemList(null);

			bool bAdded = false;
			foreach (var i in origin)
			{
				if(checkedItems.Contains(i))
				{
					list.Add(i);
					bAdded = true;
				}

				if (i is VsFolder f)
				{
					var fi = FilterItems2(f.Items, checkedItems);
					fi.Reparent(i);
					list.Changed = true;

					if (!bAdded)
					{
						// may be because the parent item is not in the checkedItems list
						list.Add(i);
					}
				}

				bAdded = false;
			}

			return list;
		}

		/// <summary>
		/// lists the view files_ cell tool tip showing.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void listViewFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			// e.Text = e.Item.Text;
			e.Text = ((VsItem)e.Item.RowObject).Path;
		}

		/// <summary>
		/// btns the refresh_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnRefresh_Click(object sender, EventArgs e)
		{
			UpdateList();
		}

		/// <summary>
		/// dlgs the import folder_ load.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void dlgImportFolder_Load(object sender, EventArgs e)
		{
			// when the last used folder is empty, already invoke the folder selection dialog
			if (string.IsNullOrEmpty(Properties.Settings.Default.LastImportFolder))
			{
				btnSelectFolder_Click(sender, e);
			}
		}

		/// <summary>
		/// chks the solution only_ checked changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void chkSolutionOnly_CheckedChanged(object sender, EventArgs e)
		{
			this.bSolutionOnly = chkSolutionOnly.Checked;
			UpdateList();
		}

		/// <summary>
		/// Updates the list.
		/// </summary>
		private void UpdateList()
		{
			if (Path.IsPathFullyQualified(txtFoldername.Text))
			{
				this.Cursor = Cursors.WaitCursor;
				this.Solution.Items.Clear();
				this.Solution.Items.Add(IterateFolder(txtFoldername.Text, this.chkSolutionOnly.Checked));
				this.olvFiles.SetObjects(this.Solution.Items);
				this.olvFiles.ExpandAll();
				this.Cursor = Cursors.Default;
			}
		}
	}
}
