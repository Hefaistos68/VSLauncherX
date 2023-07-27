using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSLauncher
{
	/// <summary>
	/// The dlg new instance.
	/// </summary>
	public partial class dlgNewInstance : Form
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="dlgNewInstance"/> class.
		/// </summary>
		public dlgNewInstance()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Gets the instance name.
		/// </summary>
		public string InstanceName { get; private set; } = string.Empty;

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInstanceName_TextChanged(object sender, EventArgs e)
		{
			this.InstanceName = this.txtInstanceName.Text;
			this.btnOk.Enabled = !string.IsNullOrWhiteSpace(this.InstanceName);
		}
	}
}
