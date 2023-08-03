namespace VSLauncher.Forms
{
	partial class frmPing
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			txtInfo = new Label();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			SuspendLayout();
			// 
			// txtInfo
			// 
			txtInfo.Dock = DockStyle.Fill;
			txtInfo.Font = new Font("Segoe UI", 500F, FontStyle.Bold, GraphicsUnit.Point);
			txtInfo.Location = new Point(0, 0);
			txtInfo.Name = "txtInfo";
			txtInfo.Size = new Size(479, 542);
			txtInfo.TabIndex = 0;
			txtInfo.Text = "1";
			txtInfo.TextAlign = ContentAlignment.MiddleCenter;
			txtInfo.Click += txtInfo_Click;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 1000;
			this.timer1.Tick += timer1_Tick;
			// 
			// frmPing
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(479, 542);
			this.ControlBox = false;
			this.Controls.Add(txtInfo);
			this.DoubleBuffered = true;
			this.FormBorderStyle = FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmPing";
			this.Opacity = 0.5D;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "frmPing";
			this.TopMost = true;
			this.TransparencyKey = Color.White;
			this.WindowState = FormWindowState.Maximized;
			Load += frmPing_Load;
			ResumeLayout(false);
		}

		#endregion

		private Label txtInfo;
		private System.Windows.Forms.Timer timer1;
	}
}