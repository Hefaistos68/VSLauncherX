using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomControls
{
	/// <summary>
	/// The vsl list control.
	/// </summary>
	public partial class VslListControl : UserControl
	{
		public delegate void ItemClickEventHandler(object sender, int Index);

		private VslListControlItem mLastSelected;

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count => flowpanelListBox.Controls.Count;

		public event ItemClickEventHandler ItemClick;

		/// <summary>
		/// Initializes a new instance of the <see cref="VslListControl"/> class.
		/// </summary>
		public VslListControl()
		{
			mLastSelected = null;
			InitializeComponent();
		}

		/// <summary>
		/// Adds the.
		/// </summary>
		/// <param name="Song">The song.</param>
		/// <param name="Artist">The artist.</param>
		/// <param name="Album">The album.</param>
		/// <param name="Duration">The duration.</param>
		/// <param name="SongImage">The song image.</param>
		/// <param name="Rating">The rating.</param>
		public void Add(string Song, string Artist, string Album, string Duration, Image SongImage, int Rating)
		{
			VslListControlItem c = new VslListControlItem();
			c.Name = $"item{flowpanelListBox.Controls.Count + 1}";
			c.Margin = new Padding(0);
			c.SelectionChanged += SelectionChanged;
			c.Click += ItemClicked;
			flowpanelListBox.Controls.Add(c);
			SetupAnchors();
		}

		/// <summary>
		/// Removes the.
		/// </summary>
		/// <param name="Index">The index.</param>
		public void Remove(int Index)
		{
			VslListControlItem c = (VslListControlItem)flowpanelListBox.Controls[Index];
			Remove(c.Name);
		}

		/// <summary>
		/// Removes the.
		/// </summary>
		/// <param name="name">The name.</param>
		public void Remove(string name)
		{
			VslListControlItem c = (VslListControlItem)flowpanelListBox.Controls[name];
			flowpanelListBox.Controls.Remove(c);
			c.SelectionChanged -= SelectionChanged;
			c.Click -= ItemClicked;
			c.Dispose();
			SetupAnchors();
		}

		/// <summary>
		/// Clears the.
		/// </summary>
		public void Clear()
		{
			while (flowpanelListBox.Controls.Count != 0)
			{
				VslListControlItem c = (VslListControlItem)flowpanelListBox.Controls[0];
				flowpanelListBox.Controls.Remove(c);
				c.SelectionChanged -= SelectionChanged;
				c.Click -= ItemClicked;
				c.Dispose();
			}
			mLastSelected = null;
		}

		/// <summary>
		/// Setups the anchors.
		/// </summary>
		private void SetupAnchors()
		{
			if (flowpanelListBox.Controls.Count <= 0)
			{
				return;
			}
			checked
			{
				int num = flowpanelListBox.Controls.Count - 1;
				for (int i = 0; i <= num; i++)
				{
					Control c = flowpanelListBox.Controls[i];
					if (i == 0)
					{
						c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
						c.Width = flowpanelListBox.Width - SystemInformation.VerticalScrollBarWidth;
					}
					else
					{
						c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
					}
				}
			}
		}

		/// <summary>
		/// flps the list box_ resize.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void flowpanelListBox_Resize(object sender, EventArgs e)
		{
			if (flowpanelListBox.Controls.Count != 0)
			{
				flowpanelListBox.Controls[0].Width = checked(flowpanelListBox.Width - SystemInformation.VerticalScrollBarWidth);
			}
		}

		/// <summary>
		/// Selections the changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		private void SelectionChanged(object sender)
		{
			if (mLastSelected != null)
			{
				mLastSelected.Selected = false;
			}
			mLastSelected = (VslListControlItem)sender;
		}

		/// <summary>
		/// Items the clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void ItemClicked(object sender, EventArgs e)
		{
			// ItemClick?.Invoke(this, flowpanelListBox.Controls.IndexOfKey(Conversions.ToString(NewLateBinding.LateGet(sender, null, "name", new object[0], null, null, null))));
		}

	}
}
