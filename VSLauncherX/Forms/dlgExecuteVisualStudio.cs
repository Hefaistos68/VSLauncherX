using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The dlg new instance.
	/// </summary>
	public partial class dlgExecuteVisualStudio : Form
	{
		private readonly string solutionFilterString =   "Solutions (*.sln)|*.sln|" +
															"C# Projects (*.csproj)|*.csproj" +
															"F# Projects (*.fsproj)|*.fsproj" +
															"TS/JS Projects (*.esproj, *.tsproj)|*.esproj" +
															"Cxx Projects (*.vcxproj)|*.vcxproj" +
															"All files (*.*)|*.*";

		public VsItem Item;

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(int index)
		{
			InitializeComponent();
			this.visualStudioCombobox1.SelectedIndex = index; // that is always the same as in the main dialog
			this.Item = new VsItem();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(object item)
		{
			InitializeComponent();

			this.Item = (VsItem)item;

			// make this a change dialog and add Save
			this.btnOk.Text = this.btnOk.Tag as string;
		}

		/// <summary>
		/// Updates the controls from data.
		/// </summary>
		private void UpdateControlsFromData()
		{
			this.txtFoldername.Text = this.Item.Path;
			this.txtCommand.Text = this.Item.Commands;
			this.cbxInstance.Text = string.IsNullOrWhiteSpace(this.Item.Instance) ? "<default>" : this.Item.Instance;
			this.chkAdmin.Checked = this.Item.RunAsAdmin;
			this.chkSplash.Checked = this.Item.ShowSplash;
		}

		/// <summary>
		/// Gets a value indicating whether as admin.
		/// </summary>
		public bool AsAdmin { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether show splash.
		/// </summary>
		public bool ShowSplash { get; internal set; }

		/// <summary>
		/// Gets the instance name.
		/// </summary>
		public string InstanceName { get; private set; } = string.Empty;

		/// <summary>
		/// Gets the command.
		/// </summary>
		public string Command { get; internal set; }

		/// <summary>
		/// Gets the project or solution.
		/// </summary>
		public string ProjectOrSolution { get; private set; }

		/// <summary>
		/// Gets the vs version.
		/// </summary>
		public VisualStudioInstance VsVersion { get; private set; }

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInstanceName_TextChanged(object sender, EventArgs e)
		{
			this.InstanceName = this.cbxInstance.Text;
		}

		/// <summary>
		/// visuals the studio combobox1_ selected index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void visualStudioCombobox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			var s = this.visualStudioCombobox1.Versions[visualStudioCombobox1.SelectedIndex].GetInstances();
			this.cbxInstance.Items.Clear();
			this.cbxInstance.Items.AddRange(s.ToArray());
			this.cbxInstance.SelectedIndex = 0;

			this.VsVersion = this.visualStudioCombobox1.Versions[visualStudioCombobox1.SelectedIndex];
		}

		/// <summary>
		/// cbxes the instance_ selected index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void cbxInstance_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.InstanceName = cbxInstance.Text == "<default>" ? "" : cbxInstance.Text;
		}

		/// <summary>
		/// cbxes the instance_ text update.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void cbxInstance_TextUpdate(object sender, EventArgs e)
		{
			this.InstanceName = cbxInstance.Text == "<default>" ? "" : cbxInstance.Text;
		}

		/// <summary>
		/// btns the ok_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnOk_Click(object sender, EventArgs e)
		{
			this.AsAdmin = chkAdmin.Checked;
			this.ShowSplash = chkSplash.Checked;
			this.Command = txtCommand.Text;
			this.ProjectOrSolution = txtFoldername.Text;

			this.Item.Path = txtFoldername.Text;
			this.Item.RunAsAdmin = chkAdmin.Checked;
			this.Item.ShowSplash = chkSplash.Checked;
			this.Item.Commands = txtCommand.Text;
		}

		/// <summary>
		/// btns the select folder_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnSelectFolder_Click(object sender, EventArgs e)
		{
			// let the user select a folder through the system dialog
			using (OpenFileDialog openFileDialog = new())
			{
				openFileDialog.Filter = solutionFilterString;
				openFileDialog.FilterIndex = 1;
				openFileDialog.CheckFileExists = true;
				openFileDialog.Multiselect = false;
				openFileDialog.InitialDirectory = Properties.Settings.Default.LastExecuteFolder;
				openFileDialog.RestoreDirectory = true;

				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					//Get the path of specified file
					var folderPath = openFileDialog.FileName;
					txtFoldername.Text = folderPath;

					// store current folderPath in application settings
					Properties.Settings.Default.LastExecuteFolder = Path.GetDirectoryName(folderPath);
				}
			}
		}

		/// <summary>
		/// txts the info_ link clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// open a link in the default browser
			System.Diagnostics.Process.Start("explorer.exe", "https://visualstudio.microsoft.com/vs/older-downloads/");
		}

		/// <summary>
		/// btns the before after_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnBeforeAfter_Click(object sender, EventArgs e)
		{
			var dlg = new dlgBeforeAfter(this.Item, this.Item.Name);

			if (dlg.ShowDialog() == DialogResult.OK)
			{

			}
		}

		/// <summary>
		/// txts the foldername_ text changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtFoldername_TextChanged(object sender, EventArgs e)
		{
			btnBeforeAfter.Enabled = !string.IsNullOrWhiteSpace(txtFoldername.Text);
		}

		/// <summary>
		/// dlgs the execute visual studio_ load.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void dlgExecuteVisualStudio_Load(object sender, EventArgs e)
		{
			string version ="";

			if (this.Item is VsProject p)
			{
				this.ProjectOrSolution = p.Path;
				this.AsAdmin = p.RunAsAdmin;
				this.ShowSplash = p.ShowSplash;
				version = p.GetRequiredVersion();
				this.visualStudioCombobox1.SelectFromVersion(version);
			}
			else if (this.Item is VsSolution s)
			{
				this.ProjectOrSolution = s.Path;
				this.AsAdmin = s.RunAsAdmin;
				this.ShowSplash = s.ShowSplash;
				version = s.GetRequiredVersion();
				this.visualStudioCombobox1.SelectFromVersion(version);
			}
			else if (this.Item is VsFolder f)
			{
				this.ProjectOrSolution = f.Path;
				this.AsAdmin = f.RunAsAdmin;
				this.ShowSplash = f.ShowSplash;
			}
			else
			{
				throw new ArgumentException("Unknown type of item");
			}

			if (this.visualStudioCombobox1.SelectedIndex == -1)
			{
				if (string.IsNullOrEmpty(version))
				{
					this.txtInfo.Text = $"no specific version required";
				}
				else
				{
					this.txtInfo.Text = $"VS {version} not found";
				}
			}
			else
			{

			}

			UpdateControlsFromData();
		}
	}
}
