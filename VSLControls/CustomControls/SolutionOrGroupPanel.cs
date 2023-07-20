using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSLauncher
{
	public partial class SolutionOrGroupPanel : UserControl
	{
		/// <summary>
		/// The item type.
		/// </summary>
		public enum SolutionOrGroupPanelItemType
		{
			None,
			Group,
			Solution,
			Project
		}

		private bool isFocused = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionOrGroupPanel"/> class.
		/// </summary>
		public SolutionOrGroupPanel()
		{
			InitializeComponent();
			if (this.DesignMode)
			{
				this.ItemName = "SolutionOrGroupPanel";
				this.ItemPath = "Solution\\Or\\GroupPanel";
				this.ItemType = SolutionOrGroupPanelItemType.Group;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionOrGroupPanel"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public SolutionOrGroupPanel(string name, string path)
		{
			InitializeComponent();
			this.ItemName = name;
			this.ItemPath = path;
			this.ItemType = SolutionOrGroupPanelItemType.Group;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionOrGroupPanel"/> class.
		/// </summary>
		/// <param name="itemType">The item type.</param>
		/// <param name="itemName">The item name.</param>
		/// <param name="itemPath">The item path.</param>
		/// <param name="requireAdmin">If true, require admin.</param>
		public SolutionOrGroupPanel(SolutionOrGroupPanelItemType itemType, string itemName, string itemPath, bool requireAdmin)
		{
			this.ItemType = itemType;
			this.ItemName = itemName;
			this.ItemPath = itemPath;
			this.RequireAdmin = requireAdmin;
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (isFocused)
			{
				ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle,
					SystemColors.Highlight, ButtonBorderStyle.Solid);
			}
		}

		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			isFocused = true;
			Invalidate();
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			isFocused = false;
			Invalidate();
		}
		
		public SolutionOrGroupPanelItemType ItemType { get; private set; }

		public string ItemName
		{
			get { return this.labelDescription.Text; }
			private set { this.labelDescription.Text = value; }
		}

		public string ItemPath
		{
			get
			{
				return this.itemDescription.Text;
			}
			private set
			{
				this.itemDescription.Text = value;
			}
		}

		public bool RequireAdmin { get; set; }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (this.Parent != null)
			{
				this.Width = this.Parent.Width;
			}
		}
		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			this.Size = new Size(this.Parent.Width - (this.Parent.Margin.Left + this.Parent.Margin.Right), this.GetPreferredSize(this.Size).Height);
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			this.isFocused = true;
			this.Focus();
		}

		private void itemDescription_Click(object sender, EventArgs e)
		{
			// forward click event to parent
			this.OnClick(e);

		}

		private void labelDescription_Click(object sender, EventArgs e)
		{
			this.OnClick(e);
		}

		private void tableLayoutPanel_Click(object sender, EventArgs e)
		{
			this.OnClick(e);
		}

		private void itemDescription_Enter(object sender, EventArgs e)
		{
			this.OnEnter(e);
		}
	}
}
