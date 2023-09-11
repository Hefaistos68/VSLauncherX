using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSLauncher.Forms
{
	/// <summary>
	/// The dlg settings.
	/// </summary>
	public partial class dlgSettings : Form
	{
		public dlgSettings()
		{
			InitializeComponent();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.AlwaysAdmin = chkAlwaysAdmin.Checked;
			Properties.Settings.Default.AutoStart = chkAutostart.Checked;
			Properties.Settings.Default.SynchronizeVS = chkSync.Checked;
			Properties.Settings.Default.ShowPathForSolutions = chkShowPath.Checked;
			Properties.Settings.Default.Save();
		}

		private void dlgSettings_Load(object sender, EventArgs e)
		{
			chkAutostart.Checked = Properties.Settings.Default.AutoStart;
			chkAlwaysAdmin.Checked = Properties.Settings.Default.AlwaysAdmin;
			chkSync.Checked = Properties.Settings.Default.SynchronizeVS;
			chkShowPath.Checked = Properties.Settings.Default.ShowPathForSolutions;
		}
	}
}
