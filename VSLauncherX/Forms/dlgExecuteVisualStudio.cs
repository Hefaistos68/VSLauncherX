using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using VSLauncher.DataModel;
using VSLauncher.Forms;

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

		private VsItem? currentItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(int index)
		{
			InitializeComponent();
			this.cbxVisualStudioVersion.SelectedIndex = index; // that is always the same as in the main dialog
			this.Item = new VsItem();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(object item)
		{
			InitializeComponent();

			this.Item = (VsItem)item;

			SetupMultiMonitor();
		}

		/// <summary>
		/// Setups the multi monitor.
		/// </summary>
		private void SetupMultiMonitor()
		{
			// get list of monitors in system and show in cbxMonitors, if only one, disable the combobox
			var monitors = Screen.AllScreens;

			this.cbxMonitors.Items.Clear();
			this.cbxMonitors.Items.AddRange(monitors.Select(m => m.DeviceName).ToArray());
			this.cbxMonitors.SelectedIndex = 0;

			this.cbxMonitors.Enabled = monitors.Length > 1;
		}

		/// <summary>
		/// Updates the controls from data.
		/// </summary>
		private void UpdateControlsFromData()
		{
			if (this.Item is not null)
			{
				this.txtName.Text = this.Item.Name;
				this.cbxVisualStudioVersion.SelectFromVersion(this.Item.VsVersion);
				this.txtFoldername.Text = this.Item.Path;
				this.txtCommand.Text = this.Item.Commands;
				this.cbxInstance.Text = string.IsNullOrWhiteSpace(this.Item.Instance) ? "<default>" : this.Item.Instance;
				this.chkAdmin.Checked = this.Item.RunAsAdmin;
				this.chkSplash.Checked = this.Item.ShowSplash;
				this.cbxMonitors.SelectedItem = this.Item.PreferredMonitor ?? 0;
			}
		}

		/// <summary>
		/// Gets the vs version.
		/// </summary>
		public VisualStudioInstance VsVersion { get; private set; }

		public VsItem? Item
		{
			get
			{
				return this.currentItem;
			}

			set
			{
				this.currentItem = value;
			}
		}

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInstanceName_TextChanged(object sender, EventArgs e)
		{
		}

		/// <summary>
		/// visuals the studio combobox1_ selected index changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void cbxVisualStudioVersion_SelectedIndexChanged(object sender, EventArgs e)
		{
			var s = this.cbxVisualStudioVersion.Versions[cbxVisualStudioVersion.SelectedIndex].GetInstances();
			this.cbxInstance.Items.Clear();
			this.cbxInstance.Items.AddRange(s.ToArray());
			this.cbxInstance.SelectedIndex = 0;

			this.VsVersion = cbxVisualStudioVersion.SelectedItem;
		}

		/// <summary>
		/// btns the ok_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnOk_Click(object sender, EventArgs e)
		{
			if (this.Item is not null)
			{
				this.Item.Path = txtFoldername.Text;
				this.Item.Name = txtName.Text;
				this.Item.RunAsAdmin = chkAdmin.Checked;
				this.Item.ShowSplash = chkSplash.Checked;
				this.Item.Instance = (cbxInstance.Text == "<default>") || string.IsNullOrWhiteSpace(cbxInstance.Text) ? null : cbxInstance.Text;
				this.Item.Commands = txtCommand.Text;
				this.Item.PreferredMonitor = cbxMonitors.SelectedIndex;
				this.Item.VsVersion = cbxVisualStudioVersion.SelectedItem.Identifier;

				if (this.Item is VsProject p)
				{
					p.VsVersion = this.cbxVisualStudioVersion.SelectedItem.Identifier;
				}
				else if (this.Item is VsSolution s)
				{
					s.RequiredVersion = this.cbxVisualStudioVersion.SelectedItem.Identifier;
				}
			}
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
			System.Diagnostics.Process.Start("explorer.exe", @"https://visualstudio.microsoft.com/vs/older-downloads/");
		}

		/// <summary>
		/// txts the info_ link clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInfoCommands_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// open a link in the default browser
			System.Diagnostics.Process.Start("explorer.exe", @"https://learn.microsoft.com/en-us/visualstudio/ide/reference/command-devenv-exe");
		}

		/// <summary>
		/// txts the info_ link clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInfoInstances_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// open a link in the default browser
			System.Diagnostics.Process.Start("explorer.exe", @"https://learn.microsoft.com/en-us/visualstudio/extensibility/devenv-command-line-switches-for-vspackage-development");
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
			string? version ="";

			// make this a change dialog and add Save
			if (this.Item != null)
			{
				this.btnOk.Text = this.btnOk.Tag as string;
			}

			if (this.Item is VsProject p)
			{
				version = p.GetRequiredVersion();
				this.cbxVisualStudioVersion.SelectFromVersion(version);
			}
			else if (this.Item is VsSolution s)
			{
				version = s.GetRequiredVersion();
				this.cbxVisualStudioVersion.SelectFromVersion(version);
			}
			else if (this.Item is VsFolder f)
			{
				this.cbxVisualStudioVersion.Enabled = false;
				this.cbxInstance.Enabled = false;
				this.txtFoldername.Focus();
			}
			else
			{
				throw new ArgumentException("Unknown type of item");
			}

			if (this.cbxVisualStudioVersion.SelectedIndex == -1)
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

		/// <summary>
		/// Gets the d c.
		/// </summary>
		/// <param name="hwnd">The hwnd.</param>
		/// <returns>An IntPtr.</returns>
		[DllImport("User32.dll")]
		static extern IntPtr GetDC(IntPtr hwnd);

		/// <summary>
		/// Releases the d c.
		/// </summary>
		/// <param name="hwnd">The hwnd.</param>
		/// <param name="dc">The dc.</param>
		/// <returns>An int.</returns>
		[DllImport("User32.dll")]
		static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

		/// <summary>
		/// btns the ping monitor_ click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnPingMonitor_Click(object sender, EventArgs e)
		{
			// show an transparent window on the selected monitor in cbxMonitors
			if (cbxMonitors.SelectedIndex != -1)
			{
				var monitor = cbxMonitors.SelectedIndex;
				var f = new frmPing(monitor);
				f.Show();
			}
		}
	}
}
