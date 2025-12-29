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

using VSLXshared.Helpers;

namespace VSLauncher
{
	/// <summary>
	/// The dlg new instance.
	/// </summary>
	public partial class dlgExecuteVisualStudio : Form
	{
		private VsItem? currentItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(int index)
		{
			InitializeComponent();
			
			this.cbxVisualStudioVersion.SelectedIndex = 1 + index; // that is always the same as in the main dialog, but main has no default item
			
			this.Item = new VsItem();
			this.bIsListItem = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(object item)
		{
			InitializeComponent();

			this.Item = (VsItem)item;
			this.bIsListItem = true;
		}

		/// <summary>
		/// Setups the multi monitor.
		/// </summary>
		private void SetupMultiMonitor()
		{
			// get list of monitors in system and show in cbxMonitors, if only one, disable the combobox
			var monitors = Screen.AllScreens;

			this.cbxMonitors.Items.Clear();
			this.cbxMonitors.Items.Add("<default>");
			this.cbxMonitors.Items.AddRange(monitors.Select(m => m.DeviceName).ToArray());
			this.cbxMonitors.SelectedIndex = 0;

			this.cbxMonitors.Enabled = monitors.Length > 1;
			this.btnPingMonitor.Enabled = monitors.Length > 1;
		}

		/// <summary>
		/// Updates the controls from data.
		/// </summary>
		private void UpdateControlsFromData()
		{
			if (this.bIsListItem && this.Item is not null)
			{
				this.txtName.Text = this.Item.Name;
				this.cbxVisualStudioVersion.SelectFromVersion(this.Item.VsVersion);
				this.txtFoldername.Text = this.Item.Path;
				this.txtCommand.Text = this.Item.Commands;
				this.cbxInstance.Text = string.IsNullOrWhiteSpace(this.Item.Instance) ? "<default>" : this.Item.Instance;
				this.chkAdmin.Checked = this.Item.RunAsAdmin;
				this.chkSplash.Checked = this.Item.ShowSplash;
				this.cbxMonitors.SelectedItem = this.Item.PreferredMonitor.HasValue ? this.Item.PreferredMonitor : 0;
			}
		}

		/// <summary>
		/// Gets the vs version.
		/// </summary>
		public VisualStudioInstance VsVersion { get; private set; }

		/// <summary>
		/// Gets or sets the item.
		/// </summary>
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
		/// Used internally to indicate that no list item is being executed, rather an arbitrary VS command is executed
		/// </summary>
		private readonly bool bIsListItem;

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
			if(cbxVisualStudioVersion.SelectedIndex >= 0)
			{
				var s = this.cbxVisualStudioVersion.SelectedItem!.GetInstances();
				this.cbxInstance.Enabled = true;
				this.cbxInstance.Items.Clear();
				this.cbxInstance.Items.AddRange(s.ToArray());
				this.cbxInstance.SelectedIndex = 0;

				this.VsVersion = cbxVisualStudioVersion.SelectedItem;
			}
			else
			{
				this.VsVersion = null;
				this.cbxInstance.Enabled = false;
			}
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
				this.Item.PreferredMonitor = cbxMonitors.SelectedIndex > 0 ? cbxMonitors.SelectedIndex - 1 : null;

				string? vsVersionIdentifier = null;

				if (!cbxVisualStudioVersion.IsDefaultSelected)
				{
					vsVersionIdentifier = cbxVisualStudioVersion.SelectedItem.Identifier;
				}
				else
				{
					vsVersionIdentifier = "";
				}

				this.Item.VsVersion = vsVersionIdentifier;

				if (this.Item is VsProject p)
				{
					p.VsVersion = vsVersionIdentifier;
				}
				else if (this.Item is VsSolution s)
				{
					s.RequiredVersion = vsVersionIdentifier;
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
				openFileDialog.Filter = FileHelper.SolutionFilterString;
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
				// the item is already updated
			}
		}

		/// <summary>
		/// txts the foldername_ text changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtFoldername_TextChanged(object sender, EventArgs e)
		{
			btnBeforeAfter.Enabled = this.bIsListItem ? !string.IsNullOrWhiteSpace(txtFoldername.Text) : false;
		}

		/// <summary>
		/// dlgs the execute visual studio_ load.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void dlgExecuteVisualStudio_Load(object sender, EventArgs e)
		{
			string? version = null;

			// make this a change dialog and add Save
			if (this.Item != null)
			{
				this.btnOk.Text = this.btnOk.Tag as string;
			}

			if (this.bIsListItem)
			{
				if (this.Item is VsProject p)
				{
					if (p.VsVersion == null)
					{
						version = p.GetRequiredVersion();
						this.cbxVisualStudioVersion.SelectFromVersion(version);
					}
					else
					{
						this.cbxVisualStudioVersion.SelectDefault();
					}
				}
				else if (this.Item is VsSolution s)
				{
					if (s.RequiredVersion == null)
					{
						version = s.GetRequiredVersion();
						this.cbxVisualStudioVersion.SelectFromVersion(version);
					}
					// the required version can also be "", then it was set to be default instead of specific
					else
					{
						this.cbxVisualStudioVersion.SelectDefault();
					}
				}
				else if (this.Item is VsFolder f)
				{
					// this.cbxVisualStudioVersion.Enabled = false;
					this.cbxInstance.Enabled = false;
					this.txtFoldername.Focus();
				}
				else if (this.Item is VsItem vsi && vsi.ItemType == ItemTypeEnum.Other)
				{
				}
				else
				{
					throw new ArgumentException("Unknown type of item");
				}
			}
			else
			{
				txtName.Enabled = false;
				btnOk.Text = "&Start";
				txtFoldername.Focus();
				txtFoldername.Select();
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

			SetupMultiMonitor();
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
		/// Releases a DC
		/// </summary>
		/// <param name="hwnd">The hwnd.</param>
		/// <param name="dc">The dc.</param>
		/// <returns>An int.</returns>
		[DllImport("User32.dll")]
		static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

		/// <summary>
		/// Handles the btnPingMonitor button click
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnPingMonitor_Click(object sender, EventArgs e)
		{
			// show an transparent window on the selected monitor in cbxMonitors
			if (cbxMonitors.SelectedIndex > 0)
			{
				var monitor = cbxMonitors.SelectedIndex - 1;
				var f = new frmPing(monitor);
				f.Show();
			}
		}
	}
}
