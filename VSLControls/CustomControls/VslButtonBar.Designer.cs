using System.Windows.Forms;

namespace CustomControls
{
	/// <summary>
	/// The vsl button bar.
	/// </summary>
	partial class VslButtonBar
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VslButtonBar));
			this.ImageList1 = new System.Windows.Forms.ImageList(this.components);
			this.Star5 = new System.Windows.Forms.PictureBox();
			this.Star4 = new System.Windows.Forms.PictureBox();
			this.Star3 = new System.Windows.Forms.PictureBox();
			this.Star2 = new System.Windows.Forms.PictureBox();
			this.Star1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.Star5)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.Star4)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.Star3)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.Star2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.Star1)).BeginInit();
			this.SuspendLayout();
			// 
			// ImageList1
			// 
			this.ImageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ImageList1.ImageStream")));
			this.ImageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.ImageList1.Images.SetKeyName(0, "Add.png");
			this.ImageList1.Images.SetKeyName(1, "AddFavorite.png");
			this.ImageList1.Images.SetKeyName(2, "AddFolder.png");
			this.ImageList1.Images.SetKeyName(3, "ArrowDownEnd.png");
			this.ImageList1.Images.SetKeyName(4, "ArrowUpEnd.png");
			this.ImageList1.Images.SetKeyName(5, "ConnectArrow.png");
			this.ImageList1.Images.SetKeyName(6, "FolderClosedBlue.png");
			this.ImageList1.Images.SetKeyName(7, "FolderOpenBlue.png");
			this.ImageList1.Images.SetKeyName(8, "Refresh.png");
			this.ImageList1.Images.SetKeyName(9, "RightArrowAsterisk.png");
			this.ImageList1.Images.SetKeyName(10, "Run.png");
			this.ImageList1.Images.SetKeyName(11, "RunAll.png");
			this.ImageList1.Images.SetKeyName(12, "RunUpdate.png");
			this.ImageList1.Images.SetKeyName(13, "Settings.png");
			// 
			// Star5
			// 
			this.Star5.Cursor = System.Windows.Forms.Cursors.Default;
			this.Star5.Image = global::CustomControls.Properties.Resources.Settings;
			this.Star5.Location = new System.Drawing.Point(74, 0);
			this.Star5.Margin = new System.Windows.Forms.Padding(0);
			this.Star5.Name = "Star5";
			this.Star5.Size = new System.Drawing.Size(16, 16);
			this.Star5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.Star5.TabIndex = 0;
			this.Star5.TabStop = false;
			// 
			// Star4
			// 
			this.Star4.Cursor = System.Windows.Forms.Cursors.Default;
			this.Star4.Image = global::CustomControls.Properties.Resources.Refresh;
			this.Star4.Location = new System.Drawing.Point(56, 0);
			this.Star4.Margin = new System.Windows.Forms.Padding(0);
			this.Star4.Name = "Star4";
			this.Star4.Size = new System.Drawing.Size(16, 16);
			this.Star4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.Star4.TabIndex = 0;
			this.Star4.TabStop = false;
			// 
			// Star3
			// 
			this.Star3.Cursor = System.Windows.Forms.Cursors.Default;
			this.Star3.Image = global::CustomControls.Properties.Resources.RunAll;
			this.Star3.Location = new System.Drawing.Point(36, 0);
			this.Star3.Margin = new System.Windows.Forms.Padding(0);
			this.Star3.Name = "Star3";
			this.Star3.Size = new System.Drawing.Size(16, 16);
			this.Star3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.Star3.TabIndex = 0;
			this.Star3.TabStop = false;
			// 
			// Star2
			// 
			this.Star2.Cursor = System.Windows.Forms.Cursors.Default;
			this.Star2.Image = global::CustomControls.Properties.Resources.Run;
			this.Star2.Location = new System.Drawing.Point(18, 0);
			this.Star2.Margin = new System.Windows.Forms.Padding(0);
			this.Star2.Name = "Star2";
			this.Star2.Size = new System.Drawing.Size(16, 16);
			this.Star2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.Star2.TabIndex = 0;
			this.Star2.TabStop = false;
			// 
			// Star1
			// 
			this.Star1.Cursor = System.Windows.Forms.Cursors.Default;
			this.Star1.Image = global::CustomControls.Properties.Resources.Add;
			this.Star1.Location = new System.Drawing.Point(0, 0);
			this.Star1.Margin = new System.Windows.Forms.Padding(0);
			this.Star1.Name = "Star1";
			this.Star1.Size = new System.Drawing.Size(16, 16);
			this.Star1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.Star1.TabIndex = 0;
			this.Star1.TabStop = false;
			// 
			// VslButtonBar
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Transparent;
			this.Controls.Add(this.Star5);
			this.Controls.Add(this.Star4);
			this.Controls.Add(this.Star3);
			this.Controls.Add(this.Star2);
			this.Controls.Add(this.Star1);
			this.DoubleBuffered = true;
			this.MaximumSize = new System.Drawing.Size(75, 15);
			this.MinimumSize = new System.Drawing.Size(75, 15);
			this.Name = "VslButtonBar";
			this.Size = new System.Drawing.Size(90, 18);
			((System.ComponentModel.ISupportInitialize)(this.Star5)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.Star4)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.Star3)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.Star2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.Star1)).EndInit();
			this.ResumeLayout(false);

		}

		private PictureBox Star1;
		private PictureBox Star2;
		private PictureBox Star3;
		private PictureBox Star4;
		private PictureBox Star5;
		private ImageList ImageList1 ;

		#endregion
	}
}
