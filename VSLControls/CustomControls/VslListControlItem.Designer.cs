using System.Windows.Forms;

namespace CustomControls
{
	/// <summary>
	/// The vsl list control item.
	/// </summary>
	partial class VslListControlItem
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.SuspendLayout();

			// add sub controls here
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomControls.VslListControl));
			this.lblDuration = new System.Windows.Forms.Label();
			this.RatingBar1 = new VslButtonBar();
			base.SuspendLayout();
			this.lblDuration.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.lblDuration.AutoSize = true;
			this.lblDuration.BackColor = System.Drawing.Color.Transparent;
			this.lblDuration.Location = new System.Drawing.Point(433, 34);
			this.lblDuration.Name = "lblDuration";
			this.lblDuration.Size = new System.Drawing.Size(39, 17);
			this.lblDuration.TabIndex = 3;
			this.lblDuration.Text = "00:00";
			this.RatingBar1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.RatingBar1.BackColor = System.Drawing.Color.Transparent;
			this.RatingBar1.Location = new System.Drawing.Point(397, 10);
			this.RatingBar1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.RatingBar1.MaximumSize = new System.Drawing.Size(75, 15);
			this.RatingBar1.MinimumSize = new System.Drawing.Size(75, 15);
			this.RatingBar1.Name = "ButtonBar1";
			this.RatingBar1.Size = new System.Drawing.Size(75, 15);
			this.RatingBar1.TabIndex = 2;
			base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 17f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.lblDuration);
			base.Controls.Add(this.RatingBar1);
			//
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Segoe UI Light", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			base.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			base.Name = "VslListControlItem";
			base.Size = new System.Drawing.Size(484, 75);
			base.ResumeLayout(false);
			base.PerformLayout();
		}

		private Label lblDuration;
		private VslButtonBar RatingBar1;

		#endregion
	}
}
