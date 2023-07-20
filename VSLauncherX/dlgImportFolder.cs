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
		private readonly string executablesFilterString =   "Executable files (*.exe)|*.exe|" +
															"Batch files (*.bat)|*.bat|" +
															"Command files (*.cmd)|*.cmd|" +
															"PowerShell files (*.ps1)|*.ps1|" +
															"All files (*.*)|*.*";

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
			this.olvFiles.HierarchicalCheckboxes = true;
			this.Solution = new SolutionGroup();
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

		private IEnumerable IterateFolder(string folderPath)
		{
			yield return folderPath;
			// iterate through all files in the folder, returning all folders and subfolders, filter for files matching solution and project files
			foreach (var folder in System.IO.Directory.GetDirectories(folderPath))
			{
				// get the attributes and check if the folder is hidden

				if (!folder.StartsWith('.'))
				{
					var attributes = System.IO.File.GetAttributes(folder);
					if (!attributes.HasFlag(FileAttributes.Hidden))
					{
						yield return IterateFolder(folder);
					}
				}
			}

			foreach (var file in System.IO.Directory.GetFiles(folderPath))
			{
				if (System.IO.Path.GetExtension(file).ToLower() == ".sln")
				{
					yield return new VsSolution(file, file);
				}
				else if (System.IO.Path.GetExtension(file).ToLower() == ".csproj")
				{
					yield return new VsProject(file, file, eProjectType.CSharp);
				}
				else if (System.IO.Path.GetExtension(file).ToLower() == ".vbproj")
				{
					yield return new VsProject(file, file, eProjectType.VisualBasic);
				}
				else if (System.IO.Path.GetExtension(file).ToLower() == ".vcxproj")
				{
					yield return new VsProject(file, file, eProjectType.Cpp);
				}
				else if (System.IO.Path.GetExtension(file).ToLower() == ".vcproj")
				{
					yield return new VsProject(file, file, eProjectType.Cpp);
				}
				else if (System.IO.Path.GetExtension(file).ToLower() == ".fsproj")
				{
					yield return new VsProject(file, file, eProjectType.FSharp);
				}
				else if (System.IO.Path.GetExtension(file).ToLower() == ".jsproj")
				{
					yield return new VsProject(file, file, eProjectType.JScript);
				}
			}
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
			e.Text = String.Format("Tool tip for '{0}', column '{1}'\r\nValue shown: '{2}'",
				e.Model, e.Column.Text, e.SubItem.Text);
		}

	}
}
