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
		private readonly ProjectScanner projectScanner = new ProjectScanner();
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

					VsFolder items = this.projectScanner.Scan(txtFoldername.Text, this.bSolutionOnly, this.bFlat);

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
