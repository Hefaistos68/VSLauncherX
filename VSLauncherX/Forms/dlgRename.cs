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
	public partial class dlgRename : Form
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="dlgRename"/> class.
		/// </summary>
		public dlgRename(VsItem vsi)
		{
			InitializeComponent();
			this.ItemName = vsi.Name ?? "";
		}

		/// <summary>
		/// Gets the instance name.
		/// </summary>
		public string ItemName { get; private set; } = string.Empty;

		/// <summary>
		/// Handles text changes
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtItemName_TextChanged(object sender, EventArgs e)
		{
			this.ItemName = this.txtItemName.Text;
			this.btnOk.Enabled = !string.IsNullOrWhiteSpace(this.ItemName);
		}
	}
}
