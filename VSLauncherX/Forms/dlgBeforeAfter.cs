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
	public partial class dlgBeforeAfter : Form
	{
		private readonly string executablesFilterString =   "Executable files (*.exe)|*.exe|" +
															"Batch files (*.bat)|*.bat|" +
															"Command files (*.cmd)|*.cmd|" +
															"PowerShell files (*.ps1)|*.ps1|" +
															"All files (*.*)|*.*";

		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public VsOptions Options { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgAddFolder"/> class.
		/// </summary>
		public dlgBeforeAfter(VsOptions options, string title)
		{
			InitializeComponent();
			this.Options = options;
			this.txtTitle.Text = title;
			;
		}

		/// <summary>
		/// Handles Click events for the btnSelectBefore button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnSelectBefore_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.Filter = executablesFilterString;
				openFileDialog.FilterIndex = 1;
				openFileDialog.RestoreDirectory = true;

				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					//Get the path of specified file
					var filePath = openFileDialog.FileName;
					txtRunBefore.Text = filePath;
				}
			}

		}

		/// <summary>
		/// Handles Click events for the btnSelectAfter button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnSelectAfter_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.Filter = executablesFilterString;
				openFileDialog.FilterIndex = 1;
				openFileDialog.RestoreDirectory = true;

				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					//Get the path of specified file
					var filePath = openFileDialog.FileName;
					txtRunAfter.Text = filePath;
				}
			}

		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			this.Options.RunBefore = string.IsNullOrEmpty(txtRunBefore.Text.Trim()) ? null : new VsItem(txtRunBefore.Text, txtRunBefore.Text, null) { WaitExit = chkWaitExitBefore.Checked };
			this.Options.RunAfter = string.IsNullOrEmpty(txtRunAfter.Text.Trim()) ? null : new VsItem(txtRunAfter.Text, txtRunAfter.Text, null) { WaitExit = chkWaitExitAfter.Checked };
		}
	}
}
