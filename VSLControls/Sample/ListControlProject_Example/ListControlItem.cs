using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace ListControlProject_Example;

[DesignerGenerated]
public class ListControlItem : UserControl
{
	public delegate void SelectionChangedEventHandler(object sender);

	private enum MouseCapture
	{
		Outside,
		Inside
	}

	private enum ButtonState
	{
		ButtonUp,
		ButtonDown,
		Disabled
	}

	private IContainer components;

	private Timer _tmrMouseLeave;

	private Image mImage;

	private string mSong;

	private string mArtist;

	private string mAlbum;

	private bool mSelected;

	private ButtonState bState;

	private MouseCapture bMouse;

	internal virtual RatingBar RatingBar1
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	internal virtual Label lblDuration
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	internal virtual ImageList ImageList1
	{
		get; [MethodImpl(MethodImplOptions.Synchronized)]
		set;
	}

	internal virtual Timer tmrMouseLeave
	{
		get
		{
			return _tmrMouseLeave;
		}
		set
		{
			EventHandler value2 = tmrMouseLeave_Tick;
			Timer timer = _tmrMouseLeave;
			if (timer != null)
			{
				timer.Tick -= value2;
			}
			_tmrMouseLeave = value;
			timer = _tmrMouseLeave;
			if (timer != null)
			{
				timer.Tick += value2;
			}
		}
	}

	public Image Image
	{
		get
		{
			return mImage;
		}
		set
		{
			mImage = value;
			Refresh();
		}
	}

	public string Song
	{
		get
		{
			return mSong;
		}
		set
		{
			mSong = value;
			Refresh();
		}
	}

	public string Artist
	{
		get
		{
			return mArtist;
		}
		set
		{
			mArtist = value;
			Refresh();
		}
	}

	public string Album
	{
		get
		{
			return mAlbum;
		}
		set
		{
			mAlbum = value;
			Refresh();
		}
	}

	public string Duration
	{
		get
		{
			return lblDuration.Text;
		}
		set
		{
			lblDuration.Text = value;
		}
	}

	public bool Selected
	{
		get
		{
			return mSelected;
		}
		set
		{
			mSelected = value;
			Refresh();
		}
	}

	public int Rating
	{
		get
		{
			return RatingBar1.Stars;
		}
		set
		{
			RatingBar1.Stars = value;
		}
	}

	public event SelectionChangedEventHandler SelectionChanged;

	public ListControlItem()
	{
		base.MouseClick += ListControlItem_MouseClick;
		base.MouseDown += metroRadioGroup_MouseDown;
		base.MouseEnter += metroRadioGroup_MouseEnter;
		base.MouseUp += metroRadioGroup_MouseUp;
		base.Paint += PaintEvent;
		tmrMouseLeave = new Timer
		{
			Interval = 10
		};
		mSong = "[Song Name]";
		mArtist = "[Artist]";
		mAlbum = "[Album]";
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ListControlProject_Example.ListControlItem));
		this.lblDuration = new System.Windows.Forms.Label();
		this.ImageList1 = new System.Windows.Forms.ImageList(this.components);
		this.RatingBar1 = new ListControlProject_Example.RatingBar();
		base.SuspendLayout();
		this.lblDuration.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.lblDuration.AutoSize = true;
		this.lblDuration.BackColor = System.Drawing.Color.Transparent;
		this.lblDuration.Location = new System.Drawing.Point(433, 34);
		this.lblDuration.Name = "lblDuration";
		this.lblDuration.Size = new System.Drawing.Size(39, 17);
		this.lblDuration.TabIndex = 3;
		this.lblDuration.Text = "00:00";
		this.ImageList1.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("ImageList1.ImageStream");
		this.ImageList1.TransparentColor = System.Drawing.Color.Transparent;
		this.ImageList1.Images.SetKeyName(0, "default");
		this.RatingBar1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.RatingBar1.BackColor = System.Drawing.Color.Transparent;
		this.RatingBar1.Location = new System.Drawing.Point(397, 10);
		this.RatingBar1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		this.RatingBar1.MaximumSize = new System.Drawing.Size(75, 15);
		this.RatingBar1.MinimumSize = new System.Drawing.Size(75, 15);
		this.RatingBar1.Name = "RatingBar1";
		this.RatingBar1.Size = new System.Drawing.Size(75, 15);
		this.RatingBar1.Stars = 3;
		this.RatingBar1.TabIndex = 2;
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 17f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.lblDuration);
		base.Controls.Add(this.RatingBar1);
		this.DoubleBuffered = true;
		this.Font = new System.Drawing.Font("Segoe UI Light", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		base.Name = "ListControlItem";
		base.Size = new System.Drawing.Size(484, 75);
		base.ResumeLayout(false);
		base.PerformLayout();
	}

	private void ListControlItem_MouseClick(object sender, MouseEventArgs e)
	{
		if (!Selected)
		{
			Selected = true;
			SelectionChanged?.Invoke(this);
		}
	}

	private void metroRadioGroup_MouseDown(object sender, MouseEventArgs e)
	{
		bState = ButtonState.ButtonDown;
		Refresh();
	}

	private void metroRadioGroup_MouseEnter(object sender, EventArgs e)
	{
		bMouse = MouseCapture.Inside;
		tmrMouseLeave.Start();
		Refresh();
	}

	private void metroRadioGroup_MouseUp(object sender, MouseEventArgs e)
	{
		bState = ButtonState.ButtonUp;
		Refresh();
	}

	private void tmrMouseLeave_Tick(object sender, EventArgs e)
	{
		Point scrPT = Control.MousePosition;
		Point ctlPT = PointToClient(scrPT);
		if ((ctlPT.X < 0) | (ctlPT.Y < 0) | (ctlPT.X > base.Width) | (ctlPT.Y > base.Height))
		{
			tmrMouseLeave.Stop();
			bMouse = MouseCapture.Outside;
			Refresh();
		}
		else
		{
			bMouse = MouseCapture.Inside;
		}
	}

	private void Paint_DrawBackground(Graphics gfx)
	{
		checked
		{
			Rectangle rect = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
			GraphicsPath p = new GraphicsPath();
			p.StartFigure();
			p.AddArc(new Rectangle(rect.Left, rect.Top, 5, 5), 180f, 90f);
			p.AddLine(rect.Left + 5, 0, rect.Right - 5, 0);
			p.AddArc(new Rectangle(rect.Right - 5, 0, 5, 5), -90f, 90f);
			p.AddLine(rect.Right, 5, rect.Right, rect.Bottom - 5);
			p.AddArc(new Rectangle(rect.Right - 5, rect.Bottom - 5, 5, 5), 0f, 90f);
			p.AddLine(rect.Right - 5, rect.Bottom, rect.Left + 5, rect.Bottom);
			p.AddArc(new Rectangle(rect.Left, rect.Height - 5, 5, 5), 90f, 90f);
			p.CloseFigure();
			Color[] ColorScheme = null;
			SolidBrush brdr = null;
			if (bState == ButtonState.Disabled)
			{
				brdr = (SolidBrush)ColorSchemes.DisabledBorder;
				ColorScheme = ColorSchemes.DisabledAllColor;
			}
			else if (mSelected)
			{
				brdr = (SolidBrush)ColorSchemes.SelectedBorder;
				if ((bState == ButtonState.ButtonUp) & (bMouse == MouseCapture.Outside))
				{
					ColorScheme = ColorSchemes.SelectedNormal;
				}
				else if ((bState == ButtonState.ButtonUp) & (bMouse == MouseCapture.Inside))
				{
					ColorScheme = ColorSchemes.SelectedHover;
				}
				else
				{
					if ((bState == ButtonState.ButtonDown) & (bMouse == MouseCapture.Outside))
					{
						return;
					}
					if ((bState == ButtonState.ButtonDown) & (bMouse == MouseCapture.Inside))
					{
						ColorScheme = ColorSchemes.SelectedPressed;
					}
				}
			}
			else
			{
				brdr = (SolidBrush)ColorSchemes.UnSelectedBorder;
				if ((bState == ButtonState.ButtonUp) & (bMouse == MouseCapture.Outside))
				{
					brdr = (SolidBrush)ColorSchemes.DisabledBorder;
					ColorScheme = ColorSchemes.UnSelectedNormal;
				}
				else if ((bState == ButtonState.ButtonUp) & (bMouse == MouseCapture.Inside))
				{
					ColorScheme = ColorSchemes.UnSelectedHover;
				}
				else
				{
					if ((bState == ButtonState.ButtonDown) & (bMouse == MouseCapture.Outside))
					{
						return;
					}
					if ((bState == ButtonState.ButtonDown) & (bMouse == MouseCapture.Inside))
					{
						ColorScheme = ColorSchemes.UnSelectedPressed;
					}
				}
			}
			LinearGradientBrush b = new LinearGradientBrush(rect, Color.White, Color.Black, LinearGradientMode.Vertical);
			ColorBlend blend = new ColorBlend();
			blend.Colors = ColorScheme;
			blend.Positions = new float[5] { 0f, 0.1f, 0.9f, 0.95f, 1f };
			b.InterpolationColors = blend;
			gfx.FillPath(b, p);
			gfx.DrawPath(new Pen(brdr), p);
			if (bMouse == MouseCapture.Outside)
			{
				rect = new Rectangle(rect.Left, base.Height - 1, rect.Width, 1);
				b = new LinearGradientBrush(rect, Color.Blue, Color.Yellow, LinearGradientMode.Horizontal);
				blend = new ColorBlend();
				blend.Colors = new Color[3]
				{
					Color.White,
					Color.LightGray,
					Color.White
				};
				blend.Positions = new float[3] { 0f, 0.5f, 1f };
				b.InterpolationColors = blend;
				gfx.FillRectangle(b, rect);
			}
		}
	}

	private void Paint_DrawButton(Graphics gfx)
	{
		Font fnt = null;
		StringFormat SF = new StringFormat
		{
			Trimming = StringTrimming.EllipsisCharacter
		};
		Rectangle workingRect = new Rectangle(40, 0, checked(RatingBar1.Left - 40 - 6), base.Height);
		fnt = new Font("Segoe UI Light", 14f);
		gfx.DrawString(layoutRectangle: new RectangleF(height: gfx.MeasureString(mSong, fnt).Height, x: 40f, y: 0f, width: workingRect.Width), s: mSong, font: fnt, brush: Brushes.Black, format: SF);
		fnt = new Font("Segoe UI Light", 10f);
		gfx.DrawString(layoutRectangle: new RectangleF(height: gfx.MeasureString(mArtist, fnt).Height, x: 42f, y: 30f, width: workingRect.Width), s: mArtist, font: fnt, brush: Brushes.Black, format: SF);
		fnt = new Font("Segoe UI Light", 10f);
		gfx.DrawString(layoutRectangle: new RectangleF(height: gfx.MeasureString(mAlbum, fnt).Height, x: 42f, y: 49f, width: workingRect.Width), s: mAlbum, font: fnt, brush: Brushes.Black, format: SF);
		if (mImage != null)
		{
			gfx.DrawImage(mImage, new Point(7, 7));
		}
		else
		{
			gfx.DrawImage(ImageList1.Images[0], new Point(7, 7));
		}
	}

	private void PaintEvent(object sender, PaintEventArgs e)
	{
		Graphics gfx = e.Graphics;
		Paint_DrawBackground(gfx);
		Paint_DrawButton(gfx);
	}
}
