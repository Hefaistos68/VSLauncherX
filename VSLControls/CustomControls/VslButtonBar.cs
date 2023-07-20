using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomControls
{
	/// <summary>
	/// The vsl button bar.
	/// </summary>
	public partial class VslButtonBar : UserControl
	{

		/*
		/// <summary>
		/// Gets or sets the star1.
		/// </summary>
		public PictureBox Star1
		{
			get
			{
				return star1;
			}
			set
			{
				EventHandler value2 = Star1_Click;
				PictureBox star = star1;
				if (star != null)
				{
					star.Click -= value2;
				}
				star1 = value;
				star = star1;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		/// <summary>
		/// Gets or sets the image list1.
		/// </summary>
		public ImageList ImageList1
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the star2.
		/// </summary>
		public PictureBox Star2
		{
			get
			{
				return star2;
			}
			set
			{
				EventHandler value2 = Star2_Click;
				PictureBox star = star2;
				if (star != null)
				{
					star.Click -= value2;
				}
				star2 = value;
				star = star2;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		/// <summary>
		/// Gets or sets the star3.
		/// </summary>
		public PictureBox Star3
		{
			get
			{
				return star3;
			}
			set
			{
				EventHandler value2 = Star3_Click;
				PictureBox star = star3;
				if (star != null)
				{
					star.Click -= value2;
				}
				star3 = value;
				star = star3;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		/// <summary>
		/// Gets or sets the star4.
		/// </summary>
		public PictureBox Star4
		{
			get
			{
				return star4;
			}
			set
			{
				EventHandler value2 = Star4_Click;
				PictureBox star = star4;
				if (star != null)
				{
					star.Click -= value2;
				}
				star4 = value;
				star = star4;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

		/// <summary>
		/// Gets or sets the star5.
		/// </summary>
		public PictureBox Star5
		{
			get
			{
				return star5;
			}
			set
			{
				EventHandler value2 = Star5_Click;
				PictureBox star = star5;
				if (star != null)
				{
					star.Click -= value2;
				}
				star5 = value;
				star = star5;
				if (star != null)
				{
					star.Click += value2;
				}
			}
		}

*/
		/// <summary>
		/// Initializes a new instance of the <see cref="VslButtonBar"/> class.
		/// </summary>
		public VslButtonBar()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Setups the stars.
		/// </summary>
		private void SetupStars()
		{
			// 			Star1.Image = Resources.ResourceManager.;
			// 			Star2.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars >= 2, "full", "empty") }, null, null, null);
			// 			Star3.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars >= 3, "full", "empty") }, null, null, null);
			// 			Star4.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars >= 4, "full", "empty") }, null, null, null);
			// 			Star5.Image = (Image)NewLateBinding.LateGet(ImageList1.Images, null, "Item", new object[1] { Interaction.IIf(mStars == 5, "full", "empty") }, null, null, null);
		}

		/// <summary>
		/// Star1_S the click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void Star1_Click(object sender, EventArgs e)
		{
			SetupStars();
		}

		/// <summary>
		/// Star2_S the click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void Star2_Click(object sender, EventArgs e)
		{
			SetupStars();
		}

		/// <summary>
		/// Star3_S the click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void Star3_Click(object sender, EventArgs e)
		{
			SetupStars();
		}

		/// <summary>
		/// Star4_S the click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void Star4_Click(object sender, EventArgs e)
		{
			SetupStars();
		}

		/// <summary>
		/// Star5_S the click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void Star5_Click(object sender, EventArgs e)
		{
			SetupStars();
		}

	}
}
