using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomControls
{
	/// <summary>
	/// The vsl list control item.
	/// </summary>
	public partial class VslListControlItem : UserControl
	{
		private bool mSelected;
		private System.Drawing.Image mImage;

		private string stringOne;
		private string stringTwo;
		private string stringThree;

		private Timer timerMouseLeave;

		private ButtonState bState;

		private MouseCapture bMouse;
		internal ImageList imageList;
		internal VslButtonBar buttonBar;
		private readonly string defaultUIFontName = "Segoe UI Light";

		public delegate void SelectionChangedEventHandler(object sender);

		/// <summary>
		/// The mouse capture.
		/// </summary>
		private enum MouseCapture
		{
			Outside,
			Inside
		}

		/// <summary>
		/// The button state.
		/// </summary>
		private enum ButtonState
		{
			ButtonUp,
			ButtonDown,
			Disabled
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VslListControlItem"/> class.
		/// </summary>
		public VslListControlItem()
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

			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets a value indicating whether selected.
		/// </summary>
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

		public event SelectionChangedEventHandler SelectionChanged;
		/// <summary>
		/// Gets or sets the tmr mouse leave.
		/// </summary>
		internal virtual Timer tmrMouseLeave
		{
			get
			{
				return timerMouseLeave;
			}
			set
			{
				EventHandler value2 = tmrMouseLeave_Tick;
				Timer timer = timerMouseLeave;
				if (timer != null)
				{
					timer.Tick -= value2;
				}
				timerMouseLeave = value;
				timer = timerMouseLeave;
				if (timer != null)
				{
					timer.Tick += value2;
				}
			}
		}

		/// <summary>
		/// Lists the control item_ mouse click.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void ListControlItem_MouseClick(object sender, MouseEventArgs e)
		{
			if (!Selected)
			{
				Selected = true;
				SelectionChanged?.Invoke(this);
			}
		}

		/// <summary>
		/// metros the radio group_ mouse down.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void metroRadioGroup_MouseDown(object sender, MouseEventArgs e)
		{
			bState = ButtonState.ButtonDown;
			Refresh();
		}

		/// <summary>
		/// metros the radio group_ mouse enter.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void metroRadioGroup_MouseEnter(object sender, EventArgs e)
		{
			bMouse = MouseCapture.Inside;
			tmrMouseLeave.Start();
			Refresh();
		}

		/// <summary>
		/// metros the radio group_ mouse up.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void metroRadioGroup_MouseUp(object sender, MouseEventArgs e)
		{
			bState = ButtonState.ButtonUp;
			Refresh();
		}
		/// <summary>
		/// tmrs the mouse leave_ tick.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
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


		/// <summary>
		/// Paint_S the draw background.
		/// </summary>
		/// <param name="gfx">The gfx.</param>
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

		/// <summary>
		/// Paint_S the draw button.
		/// </summary>
		/// <param name="gfx">The gfx.</param>
		private void Paint_DrawButton(Graphics gfx)
		{
			Font fnt = null;
			StringFormat SF = new StringFormat
			{
				Trimming = StringTrimming.EllipsisCharacter
			};
			Rectangle workingRect = new Rectangle(40, 0, checked(buttonBar.Left - 40 - 6), base.Height);
			fnt = new Font(defaultUIFontName, 14f);
			gfx.DrawString(layoutRectangle: new RectangleF(height: gfx.MeasureString(stringOne, fnt).Height, x: 40f, y: 0f, width: workingRect.Width), s: stringOne, font: fnt, brush: Brushes.Black, format: SF);
			fnt = new Font(defaultUIFontName, 10f);
			gfx.DrawString(layoutRectangle: new RectangleF(height: gfx.MeasureString(stringTwo, fnt).Height, x: 42f, y: 30f, width: workingRect.Width), s: stringTwo, font: fnt, brush: Brushes.Black, format: SF);
			fnt = new Font(defaultUIFontName, 10f);
			gfx.DrawString(layoutRectangle: new RectangleF(height: gfx.MeasureString(stringThree, fnt).Height, x: 42f, y: 49f, width: workingRect.Width), s: stringThree, font: fnt, brush: Brushes.Black, format: SF);
			if (mImage != null)
			{
				gfx.DrawImage(mImage, new Point(7, 7));
			}
			else
			{
				gfx.DrawImage(imageList.Images[0], new Point(7, 7));
			}
		}

		/// <summary>
		/// Paints the event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The e.</param>
		private void PaintEvent(object sender, PaintEventArgs e)
		{
			Graphics gfx = e.Graphics;
			Paint_DrawBackground(gfx);
			Paint_DrawButton(gfx);
		}

	}
}
