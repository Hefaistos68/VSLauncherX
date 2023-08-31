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
	/// Warning dialog when multiple items are launched
	/// </summary>
	public partial class dlgWarnMultiple : Form
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="dlgWarnMultiple"/> class.
		/// </summary>
		public dlgWarnMultiple(int n)
		{
			InitializeComponent();
			this.lblItemNumber.Text = n.ToString();
		}

		/// <summary>
		/// Gets the instance name.
		/// </summary>
		public string ItemName { get; private set; } = string.Empty;

		private void btnOk_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.DontShowMultiplesWarning = this.chkDontShow.Checked;
			Properties.Settings.Default.Save();
		}
	}
}
