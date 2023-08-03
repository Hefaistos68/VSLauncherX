using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The dlg before after.
	/// </summary>
	public partial class dlgBeforeAfter : Form
	{
		private readonly string executablesFilterString = "Executable files (*.exe)|*.exe|" +
															"Batch files (*.bat)|*.bat|" +
															"Command files (*.cmd)|*.cmd|" +
															"PowerShell files (*.ps1)|*.ps1|" +
															"All files (*.*)|*.*";

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
		/// Gets the Options element to be edited
		/// </summary>
		public VsOptions Options { get; private set; }
		
		/// <summary>
		/// Handles Click events for the OK button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void btnOk_Click(object sender, EventArgs e)
		{
			this.Options.RunBefore = string.IsNullOrEmpty(txtRunBefore.Text.Trim()) ?
				null :
				new VsItem(txtRunBefore.Text, txtRunBefore.Text, null)
				{ WaitForCompletion = chkWaitExitBefore.Checked, Commands = txtArgumentsBefore.Text };

			this.Options.RunAfter = string.IsNullOrEmpty(txtRunAfter.Text.Trim()) ?
				null :
				new VsItem(txtRunAfter.Text, txtRunAfter.Text, null)
				{ WaitForCompletion = chkWaitExitAfter.Checked, Commands = txtArgumentsAfter.Text };
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
		/// Handles load events for the dialog
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void dlgBeforeAfter_Load(object sender, EventArgs e)
		{
			txtRunBefore.Text = this.Options.RunBefore?.Path ?? string.Empty;
			txtRunAfter.Text = this.Options.RunAfter?.Path ?? string.Empty;

			txtArgumentsBefore.Text = this.Options.RunBefore?.Commands ?? string.Empty;
			txtArgumentsAfter.Text = this.Options.RunAfter?.Commands ?? string.Empty;

			chkWaitExitBefore.Checked = this.Options.RunBefore?.WaitForCompletion ?? false;
			chkWaitExitAfter.Checked = this.Options.RunAfter?.WaitForCompletion ?? false;
		}

		/// <summary>
		/// Handles changed events for the txtRunAfter item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtRunAfter_TextChanged(object sender, EventArgs e)
		{
			this.chkWaitExitAfter.Enabled = !string.IsNullOrWhiteSpace(txtRunAfter.Text);
			this.txtArgumentsAfter.Enabled = this.chkWaitExitAfter.Enabled;
		}

		/// <summary>
		/// Handles change events for the txtRunBefore item
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtRunBefore_TextChanged(object sender, EventArgs e)
		{
			this.chkWaitExitBefore.Enabled = !string.IsNullOrWhiteSpace(txtRunBefore.Text);
			this.txtArgumentsBefore.Enabled = this.chkWaitExitBefore.Enabled;
		}
	}
}