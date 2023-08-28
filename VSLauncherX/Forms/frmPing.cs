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
	/// The frm ping.
	/// </summary>
	public partial class frmPing : Form
	{
		public frmPing(int n)
		{
			InitializeComponent();
			this.txtInfo.Text = (1+n).ToString();
			
			if(n >= 0 && n < Screen.AllScreens.Length)
			{
				this.Location = Screen.AllScreens[n].Bounds.Location;
			}
		}

		/// <summary>
		/// Handles Click events for the txtInfo element
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void txtInfo_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Handles tick events for the timer
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void timer1_Tick(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// No paint background.
		/// </summary>
		/// <param name="e">The e.</param>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// base.OnPaintBackground(e);
		}

		/// <summary>
		/// No paint.
		/// </summary>
		/// <param name="e">The e.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			// base.OnPaint(e);
		}

		/// <summary>
		/// Handles load events for the dialog
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void frmPing_Load(object sender, EventArgs e)
		{
			txtInfo.Font = new Font("Segoe UI", this.Height / 2, FontStyle.Bold, GraphicsUnit.Pixel);
		}
	}
}
