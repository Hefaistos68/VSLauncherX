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
using VSLauncher.Helpers;

namespace VSLauncher
{
	/// <summary>
	/// The dlg import folder.
	/// </summary>
	public partial class dlgImportFolder : Form
	{
		private readonly List<string> extensionsHandled  = new List<string>() { ".sln", ".slnx", ".csproj", ".tsproj", ".esproj", ".vcxproj", ".fsproj", ".vbproj"};
		private bool bSolutionOnly;
		private bool bFlat;

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
			ThemeHelper.ApplyCurrentTheme(this);
			this.Solution = new VsFolder();
			this.chkSolutionOnly.Checked = true;

			ThemeHelper.ApplyDarkMode(this, Properties.Settings.Default.DarkMode);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgImportFolder"/> class.
		/// </summary>
		/// <param name="folder">The folder</param>
		public dlgImportFolder(string folder) : this()
		{
			Properties.Settings.Default.LastImportFolder = folder;
		}

		/// <summary>
		/// Initializes the list.
		/// </summary>
		private void InitializeList()
		{
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.RowHeight = 32;

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
			this.olvColumnFilename.ImageGetter = ColumnHelper.GetImageNameForFileImport;
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

					try
					{
						UpdateList();
					}
					finally
					{
						this.Cursor = Cursors.Default;
					}
				}
			}
		}


		/// <summary>
		/// Iterates the folder.
		/// </summary>
		/// <param name="folderPath">The folder path.</param>
		/// <returns>An IEnumerable.</returns>
		private VsFolder IterateFolder(string folderPath, bool bOnlySolutions, bool bFlat)
		{
			var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			return IterateFolderInternal(folderPath, bOnlySolutions, bFlat, visited);
		}

		/// <summary>
		/// Recursively iterates a directory while guarding against inaccessible paths and symlink cycles.
		/// </summary>
		/// <param name="folderPath">The folder path.</param>
		/// <param name="bOnlySolutions">Whether to include only solution files.</param>
		/// <param name="bFlat">Whether to flatten the hierarchy.</param>
		/// <param name="visited">Set of already visited canonical paths.</param>
		/// <returns>A populated <see cref="VsFolder"/>.</returns>
		private VsFolder IterateFolderInternal(string folderPath, bool bOnlySolutions, bool bFlat, HashSet<string> visited)
		{
			string fullPath = Path.GetFullPath(folderPath);
			string folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			var root = new VsFolder(string.IsNullOrEmpty(folderName) ? folderPath : folderName, folderPath);

			if (!visited.Add(fullPath) || !Directory.Exists(folderPath))
			{
				return root;
			}

			// iterate through all subdirectories, skipping hidden/dot directories and reparse points
			try
			{
				foreach (var folder in Directory.GetDirectories(folderPath))
				{
					string dirName = Path.GetFileName(folder);

					if (string.IsNullOrEmpty(dirName) || dirName.StartsWith('.'))
					{
						continue;
					}

					try
					{
						var attributes = File.GetAttributes(folder);

						if (attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.ReparsePoint))
						{
							continue;
						}
					}
					catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
					{
						continue;
					}

					var subItem = IterateFolderInternal(folder, bOnlySolutions, bFlat, visited);

					if (subItem.Items.Count > 0)
					{
						if (!bFlat)
						{
							root.Items.Add(subItem);
						}
						else
						{
							subItem.Items.ForEach(x => root.Items.Add(x));
						}
					}
				}
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or PathTooLongException)
			{
			}

			try
			{
				foreach (var file in Directory.GetFiles(folderPath))
				{
					string fileName = Path.GetFileName(file);

					if (string.IsNullOrEmpty(fileName) || fileName.StartsWith('.'))
					{
						continue;
					}

					if (IsOfInterest(file, bOnlySolutions))
					{
						var item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(file), file);
						item.LastModified = new FileInfo(file).LastWriteTime;

						root.Items.Add(item);
					}
				}
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or PathTooLongException)
			{
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
			string ext = Path.GetExtension(file).ToLower();

			if (bOnlySolutions)
			{
				return ext == ".sln" || ext == ".slnx";
			}

			return this.extensionsHandled.Contains(ext);
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
			sg.Items = ImportHelper.FilterCheckedItems(this.Solution.Items, this.olvFiles.CheckedObjects);
			sg.Checked = false;
			this.Solution = sg;

		}

		/// <summary>
		/// lists the view files_ cell tool tip showing.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void listViewFiles_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
		{
			if (e.Item?.RowObject is VsItem vsItem)
			{
				e.Text = vsItem.Path;
			}
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
			else
			{
				txtFoldername.Text = Properties.Settings.Default.LastImportFolder;
				UpdateList();
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
		/// chks the solution only_ checked changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void chkFlat_CheckedChanged(object sender, EventArgs e)
		{
			this.bFlat = chkFlat.Checked;
			UpdateList();
		}

		/// <summary>
		/// Updates the list.
		/// </summary>
		private void UpdateList()
		{
			if (Path.IsPathFullyQualified(txtFoldername.Text) && Directory.Exists(txtFoldername.Text))
			{
				this.Cursor = Cursors.WaitCursor;

				try
				{
					this.Solution.Items.Clear();
					var items = IterateFolder(txtFoldername.Text, this.bSolutionOnly, this.bFlat);

					if (!this.bFlat)
					{
						this.Solution.Items.Add(items);
					}
					else
					{
						items.Items.ForEach(x => this.Solution.Items.Add(x));
					}

					this.olvFiles.SetObjects(this.Solution.Items);
					this.olvFiles.ExpandAll();
				}
				finally
				{
					this.Cursor = Cursors.Default;
				}
			}
		}
	}
}
