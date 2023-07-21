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

		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public SolutionGroup Solution { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgAddFolder"/> class.
		/// </summary>
		public dlgImportFolder()
		{
			InitializeComponent();
			InitializeList();
			this.Solution = new SolutionGroup();
		}

		private void InitializeList()
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 26;

			this.olvFiles.HierarchicalCheckboxes = true;
			this.olvFiles.TreeColumnRenderer.IsShowLines = true;
			this.olvFiles.TreeColumnRenderer.UseTriangles = true;

			this.olvFiles.CanExpandGetter = delegate (object x)
			{
				return x is VsFolder f ? f.Items.Count > 0 : false ;
			};

			this.olvFiles.ChildrenGetter = delegate (object x)
			{
				return x is VsFolder f ? f.Items : (IEnumerable?)null;
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
				folderBrowserDialog.Description = "Select the folder to import";
				folderBrowserDialog.ShowNewFolderButton = false;
				folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

				if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				{
					//Get the path of specified file
					var folderPath = folderBrowserDialog.SelectedPath;
					txtFoldername.Text = folderPath;
					this.olvFiles.SetObjects(IterateFolder(folderPath));
				}
			}
		}


		/// <summary>
		/// Iterates the folder.
		/// </summary>
		/// <param name="folderPath">The folder path.</param>
		/// <returns>An IEnumerable.</returns>
		private List<VsItem> IterateFolder(string folderPath)
		{
			List<VsItem> sg = new();

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
// 						var sub = new VsFolder(Path.GetFileName(folder), folder);
// 						sub.Items = IterateFolder(folder);
						var subItems = IterateFolder(folder);
						
						if(subItems.Count > 0)
						{
							root.Items.Add(subItems.First());
						}
					}
				}
			}

			foreach (var file in System.IO.Directory.GetFiles(folderPath))
			{
				if (!file.StartsWith('.'))
				{
					if (IsOfInterest(file))
					{
						root.Items.Add(ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file));
					}
				}
			}

			if(root.Items.Count > 0)
			{
				sg.Add(root);
			}

			return sg;
		}

		private bool IsOfInterest(string file)
		{
			return this.extensionsHandled.Contains(Path.GetExtension(file));
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

		private void btnOk_Click(object sender, EventArgs e)
		{
			this.Solution.Name = txtFoldername.Text;
		}

		private void listViewFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			e.Text = e.Item.Text;
		}

	}
}
