﻿using System;
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
		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(int index)
		{
			InitializeComponent();
			this.visualStudioCombobox1.SelectedIndex = index; // that is always the same as in the main dialog
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgExecuteVisualStudio"/> class.
		/// </summary>
		public dlgExecuteVisualStudio(object item)
		{
			InitializeComponent();
			string version ="";

			if (item is SolutionGroup g)
			{
				this.ProjectOrSolution = g.Path;
			}
			else if (item is VsProject p)
			{
				this.ProjectOrSolution = p.Path;
				this.AsAdmin = p.RunAsAdmin;
				this.ShowSplash = p.ShowSplash;
				version = p.GetRequiredVersion();
				this.visualStudioCombobox1.SelectFromVersion(version);
			}
			else if (item is VsSolution s)
			{
				this.ProjectOrSolution = s.Path;
				this.AsAdmin = s.RunAsAdmin;
				this.ShowSplash = s.ShowSplash;
				version = s.GetRequiredVersion();
				this.visualStudioCombobox1.SelectFromVersion(version);
			}
			else if (item is VsFolder f)
			{
				this.ProjectOrSolution = f.Path;
			}
			else
			{
				throw new ArgumentException("Unknown type of item");
			}

			if (this.visualStudioCombobox1.SelectedIndex == -1)
			{
				this.txtInfo.Text = $"VS {version} not found";
			}
			else
			{

			}

			UpdateControlsFromData();

			// make this a change dialog and add Save
			this.btnOk.Text = this.btnOk.Tag as string;
		}

		private void UpdateControlsFromData()
		{
			this.txtFoldername.Text = this.ProjectOrSolution;
			this.txtCommand.Text = this.Command;
			this.cbxInstance.Text = this.InstanceName;
			this.chkAdmin.Checked = this.AsAdmin;
			this.chkSplash.Checked = this.ShowSplash;
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
		public string ProjectOrSolution { get; private set; }
		public VisualStudioInstance Instance { get; private set; }

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInstanceName_TextChanged(object sender, EventArgs e)
		{
			this.InstanceName = this.cbxInstance.Text;
		}

		private void visualStudioCombobox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			var s = this.visualStudioCombobox1.Versions[visualStudioCombobox1.SelectedIndex].GetInstances();
			this.cbxInstance.Items.Clear();
			this.cbxInstance.Items.AddRange(s.ToArray());
			this.cbxInstance.SelectedIndex = 0;

			this.Instance = this.visualStudioCombobox1.Versions[visualStudioCombobox1.SelectedIndex];
		}

		private void cbxInstance_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.InstanceName = cbxInstance.Text == "<default>" ? "" : cbxInstance.Text;
		}

		private void cbxInstance_TextUpdate(object sender, EventArgs e)
		{
			this.InstanceName = cbxInstance.Text == "<default>" ? "" : cbxInstance.Text;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			this.AsAdmin = chkAdmin.Checked;
			this.ShowSplash = chkSplash.Checked;
			this.Command = txtCommand.Text;
			this.ProjectOrSolution = txtFoldername.Text;
		}

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

		private void txtInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer.exe", "https://visualstudio.microsoft.com/vs/older-downloads/");

			// open a link in the default browser

		}
	}
}
