using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSLauncher.Controls
{
	/// <summary>
	/// The text box ex.
	/// </summary>
	public partial class TextBoxEx : TextBox
	{
		private readonly Label lblXButton;

		/// <summary>
		/// Gets or sets a value indicating whether show clear button.
		/// </summary>
		public bool ShowClearButton { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextBoxEx"/> class.
		/// </summary>
		public TextBoxEx()
		{
			InitializeComponent();

			this.Resize += PositionX;
			this.TextChanged += ShowHideX;

			this.lblXButton = new Label()
			{
				Location = new Point(100, 0),
				AutoSize = true,
				Text = " x ",
				ForeColor = Color.Gray,
				Visible = false,
				Font = new Font("Tahoma", this.Font.Size * 0.9F),
				// BorderStyle = BorderStyle.FixedSingle,
				Cursor = Cursors.Arrow
			};

			Controls.Add(lblXButton);
			this.lblXButton.Click += (ss, ee) => { ((Label)ss).Visible = false; this.Text = string.Empty; };
			this.lblXButton.BringToFront();
		}

		private void ShowHideX(object sender, EventArgs e) => this.lblXButton.Visible = this.ShowClearButton && !string.IsNullOrEmpty(Text);

		private void PositionX(object sender, EventArgs e) => this.lblXButton.Location = new Point(this.Width - this.lblXButton.Width - 3, ((Height - this.lblXButton.Height) / 2) - 3);
	}
}
