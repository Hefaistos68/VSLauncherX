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
	public partial class dlgAddFolder : Form
	{
		/// <summary>
		/// Gets the solution group selected by the user
		/// </summary>
		public VsFolder Solution { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="dlgAddFolder"/> class.
		/// </summary>
		public dlgAddFolder()
		{
			InitializeComponent();
			this.Solution = new VsFolder();
		}

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtFoldername_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = txtFoldername.Text.Length > 0;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			this.Solution.Name = txtFoldername.Text;
		}
	}
}
