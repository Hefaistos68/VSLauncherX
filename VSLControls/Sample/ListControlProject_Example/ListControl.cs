using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace ListControlProject_Example;

[DesignerGenerated]
public class ListControl : UserControl
{
	public delegate void ItemClickEventHandler(object sender, int Index);

	private IContainer components;

	[CompilerGenerated]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[AccessedThroughProperty("flpListBox")]
	private FlowLayoutPanel _flpListBox;

	private ListControlItem mLastSelected;

	internal virtual FlowLayoutPanel flpListBox
	{
		[CompilerGenerated]
		get
		{
			return _flpListBox;
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		[CompilerGenerated]
		set
		{
			EventHandler value2 = flpListBox_Resize;
			FlowLayoutPanel flowLayoutPanel = _flpListBox;
			if (flowLayoutPanel != null)
			{
				flowLayoutPanel.Resize -= value2;
			}
			_flpListBox = value;
			flowLayoutPanel = _flpListBox;
			if (flowLayoutPanel != null)
			{
				flowLayoutPanel.Resize += value2;
			}
		}
	}

	public int Count => flpListBox.Controls.Count;

	public event ItemClickEventHandler ItemClick;

	public ListControl()
	{
		mLastSelected = null;
		InitializeComponent();
	}

	[DebuggerNonUserCode]
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

	[System.Diagnostics.DebuggerStepThrough]
	private void InitializeComponent()
	{
		this.flpListBox = new System.Windows.Forms.FlowLayoutPanel();
		base.SuspendLayout();
		this.flpListBox.AutoScroll = true;
		this.flpListBox.Dock = System.Windows.Forms.DockStyle.Fill;
		this.flpListBox.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
		this.flpListBox.Location = new System.Drawing.Point(0, 0);
		this.flpListBox.Margin = new System.Windows.Forms.Padding(0);
		this.flpListBox.Name = "flpListBox";
		this.flpListBox.Size = new System.Drawing.Size(148, 148);
		this.flpListBox.TabIndex = 0;
		this.flpListBox.WrapContents = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.SystemColors.Window;
		base.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		base.Controls.Add(this.flpListBox);
		base.Margin = new System.Windows.Forms.Padding(0);
		base.Name = "ListControl";
		base.Size = new System.Drawing.Size(148, 148);
		base.ResumeLayout(false);
	}

	public void Add(string Song, string Artist, string Album, string Duration, Image SongImage, int Rating)
	{
		ListControlItem c = new ListControlItem();
		ListControlItem listControlItem = c;
		listControlItem.Name = "item" + Conversions.ToString(checked(flpListBox.Controls.Count + 1));
		listControlItem.Margin = new Padding(0);
		listControlItem.Song = Song;
		listControlItem.Artist = Artist;
		listControlItem.Album = Album;
		listControlItem.Duration = Duration;
		listControlItem.Image = SongImage;
		listControlItem.Rating = Rating;
		listControlItem = null;
		c.SelectionChanged += SelectionChanged;
		c.Click += ItemClicked;
		flpListBox.Controls.Add(c);
		SetupAnchors();
	}

	public void Remove(int Index)
	{
		ListControlItem c = (ListControlItem)flpListBox.Controls[Index];
		Remove(c.Name);
	}

	public void Remove(string name)
	{
		ListControlItem c = (ListControlItem)flpListBox.Controls[name];
		flpListBox.Controls.Remove(c);
		c.SelectionChanged -= SelectionChanged;
		c.Click -= ItemClicked;
		c.Dispose();
		SetupAnchors();
	}

	public void Clear()
	{
		while (flpListBox.Controls.Count != 0)
		{
			ListControlItem c = (ListControlItem)flpListBox.Controls[0];
			flpListBox.Controls.Remove(c);
			c.SelectionChanged -= SelectionChanged;
			c.Click -= ItemClicked;
			c.Dispose();
		}
		mLastSelected = null;
	}

	private void SetupAnchors()
	{
		if (flpListBox.Controls.Count <= 0)
		{
			return;
		}
		checked
		{
			int num = flpListBox.Controls.Count - 1;
			for (int i = 0; i <= num; i++)
			{
				Control c = flpListBox.Controls[i];
				if (i == 0)
				{
					c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
					c.Width = flpListBox.Width - SystemInformation.VerticalScrollBarWidth;
				}
				else
				{
					c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
				}
			}
		}
	}

	private void flpListBox_Resize(object sender, EventArgs e)
	{
		if (flpListBox.Controls.Count != 0)
		{
			flpListBox.Controls[0].Width = checked(flpListBox.Width - SystemInformation.VerticalScrollBarWidth);
		}
	}

	private void SelectionChanged(object sender)
	{
		if (mLastSelected != null)
		{
			mLastSelected.Selected = false;
		}
		mLastSelected = (ListControlItem)sender;
	}

	private void ItemClicked(object sender, EventArgs e)
	{
		ItemClick?.Invoke(this, flpListBox.Controls.IndexOfKey(Conversions.ToString(NewLateBinding.LateGet(sender, null, "name", new object[0], null, null, null))));
	}
}
