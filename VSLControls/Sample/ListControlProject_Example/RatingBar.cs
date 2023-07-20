using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ListControlProject_Example
{
	public class RatingBar : UserControl
	{
		private IContainer components;

		private PictureBox _Star1;
		private PictureBox _Star2;
		private PictureBox _Star3;
		private PictureBox _Star4;
		private PictureBox _Star5;

		private int mStars;

		public PictureBox Star1
		{
			get
			{
				return _Star1;
			}
			set
			{
				EventHandler value2 = Star1_Click;
				PictureBox star = _Star1;
				if (star != null)
				{
					star.Click -= value2;
				}
				_Star1 = value;
				star = _Star1;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		public ImageList ImageList1
		{
			get;
			set;
		}

		public PictureBox Star2
		{
			get
			{
				return _Star2;
			}
			set
			{
				EventHandler value2 = Star2_Click;
				PictureBox star = _Star2;
				if (star != null)
				{
					star.Click -= value2;
				}
				_Star2 = value;
				star = _Star2;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		public PictureBox Star3
		{
			get
			{
				return _Star3;
			}
			set
			{
				EventHandler value2 = Star3_Click;
				PictureBox star = _Star3;
				if (star != null)
				{
					star.Click -= value2;
				}
				_Star3 = value;
				star = _Star3;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		public PictureBox Star4
		{
			get
			{
				return _Star4;
			}
			set
			{
				EventHandler value2 = Star4_Click;
				PictureBox star = _Star4;
				if (star != null)
				{
					star.Click -= value2;
				}
				_Star4 = value;
				star = _Star4;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		public PictureBox Star5
		{
			get
			{
				return _Star5;
			}
			set
			{
				EventHandler value2 = Star5_Click;
				PictureBox star = _Star5;
				if (star != null)
				{
					star.Click -= value2;
				}
				_Star5 = value;
				star = _Star5;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		public int Stars
		{
			get
			{
				return mStars;
			}
			set
			{
				mStars = value;
				SetupStars();
			}
		}

		public RatingBar()
		{
			mStars = 3;
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing && components != null)
				{
					components.Dispose();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ListControlProject_Example.RatingBar));
			this.Star1 = new System.Windows.Forms.PictureBox();
			this.ImageList1 = new System.Windows.Forms.ImageList(this.components);
			this.Star2 = new System.Windows.Forms.PictureBox();
			this.Star3 = new System.Windows.Forms.PictureBox();
			this.Star4 = new System.Windows.Forms.PictureBox();
			this.Star5 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)this.Star1).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.Star2).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.Star3).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.Star4).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.Star5).BeginInit();
			base.SuspendLayout();
			this.Star1.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Star1.Location = new System.Drawing.Point(0, 0);
			this.Star1.Margin = new System.Windows.Forms.Padding(0);
			this.Star1.Name = "Star1";
			this.Star1.Size = new System.Drawing.Size(13, 13);
			this.Star1.TabIndex = 0;
			this.Star1.TabStop = false;
			this.ImageList1.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("ImageList1.ImageStream");
			this.ImageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.ImageList1.Images.SetKeyName(0, "empty");
			this.ImageList1.Images.SetKeyName(1, "full");
			this.Star2.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Star2.Location = new System.Drawing.Point(13, 0);
			this.Star2.Margin = new System.Windows.Forms.Padding(0);
			this.Star2.Name = "Star2";
			this.Star2.Size = new System.Drawing.Size(13, 13);
			this.Star2.TabIndex = 0;
			this.Star2.TabStop = false;
			this.Star3.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Star3.Location = new System.Drawing.Point(26, 0);
			this.Star3.Margin = new System.Windows.Forms.Padding(0);
			this.Star3.Name = "Star3";
			this.Star3.Size = new System.Drawing.Size(13, 13);
			this.Star3.TabIndex = 0;
			this.Star3.TabStop = false;
			this.Star4.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Star4.Location = new System.Drawing.Point(39, 0);
			this.Star4.Margin = new System.Windows.Forms.Padding(0);
			this.Star4.Name = "Star4";
			this.Star4.Size = new System.Drawing.Size(13, 13);
			this.Star4.TabIndex = 0;
			this.Star4.TabStop = false;
			this.Star5.Cursor = System.Windows.Forms.Cursors.Hand;
			this.Star5.Location = new System.Drawing.Point(52, 0);
			this.Star5.Margin = new System.Windows.Forms.Padding(0);
			this.Star5.Name = "Star5";
			this.Star5.Size = new System.Drawing.Size(13, 13);
			this.Star5.TabIndex = 0;
			this.Star5.TabStop = false;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Transparent;
			base.Controls.Add(this.Star5);
			base.Controls.Add(this.Star4);
			base.Controls.Add(this.Star3);
			base.Controls.Add(this.Star2);
			base.Controls.Add(this.Star1);
			this.DoubleBuffered = true;
			this.MaximumSize = new System.Drawing.Size(75, 15);
			this.MinimumSize = new System.Drawing.Size(75, 15);
			base.Name = "RatingBar";
			base.Size = new System.Drawing.Size(75, 15);
			((System.ComponentModel.ISupportInitialize)this.Star1).EndInit();
			((System.ComponentModel.ISupportInitialize)this.Star2).EndInit();
			((System.ComponentModel.ISupportInitialize)this.Star3).EndInit();
			((System.ComponentModel.ISupportInitialize)this.Star4).EndInit();
			((System.ComponentModel.ISupportInitialize)this.Star5).EndInit();
			base.ResumeLayout(false);
		}

		private void SetupStars()
		{
// 			Star1.Image = Resources.ResourceManager.;
// 			Star2.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars >= 2, "full", "empty") }, null, null, null);
// 			Star3.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars >= 3, "full", "empty") }, null, null, null);
// 			Star4.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars >= 4, "full", "empty") }, null, null, null);
// 			Star5.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars == 5, "full", "empty") }, null, null, null);
		}

		private void Star1_Click(object sender, EventArgs e)
		{
			mStars = 1;
			SetupStars();
		}

		private void Star2_Click(object sender, EventArgs e)
		{
			mStars = 2;
			SetupStars();
		}

		private void Star3_Click(object sender, EventArgs e)
		{
			mStars = 3;
			SetupStars();
		}

		private void Star4_Click(object sender, EventArgs e)
		{
			mStars = 4;
			SetupStars();
		}

		private void Star5_Click(object sender, EventArgs e)
		{
			mStars = 5;
			SetupStars();
		}
	}
}