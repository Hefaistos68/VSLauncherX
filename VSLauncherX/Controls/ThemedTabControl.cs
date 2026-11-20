using System.Drawing;
using System.Windows.Forms;

using ThemePalette = VSLauncher.Helpers.ThemeHelper.ThemePalette;

namespace VSLauncher.Controls
{
	/// <summary>
	/// A <see cref="TabControl"/> that switches to full double-buffered owner painting in dark mode so the tab strip background,
	/// tab headers, and page border render without native light-theme artifacts.
	/// </summary>
	public class ThemedTabControl : TabControl
	{
		private const int WmEraseBkgnd = 0x0014;

		private ThemePalette? currentTheme;

		/// <summary>
		/// Applies the specified theme palette to this <see cref="ThemedTabControl"/>.
		/// </summary>
		/// <param name="palette">The active theme palette.</param>
		internal void ApplyTheme(ThemePalette palette)
		{
			this.currentTheme = palette;
			BackColor         = palette.Surface;
			ForeColor         = palette.Text;
			DrawMode          = TabDrawMode.Normal;

			if (palette.IsDark)
			{
				SetStyle(
					ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
					true);
			}
			else
			{
				SetStyle(ControlStyles.UserPaint, false);
			}

			UpdateStyles();
			Invalidate(true);
		}

		/// <inheritdoc/>
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WmEraseBkgnd && this.currentTheme is { IsDark: true })
			{
				m.Result = (IntPtr)1;

				return;
			}

			base.WndProc(ref m);
		}

		/// <inheritdoc/>
		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			base.OnSelectedIndexChanged(e);

			if (this.currentTheme is { IsDark: true })
			{
				Invalidate();
			}
		}

		/// <inheritdoc/>
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			if (this.currentTheme is { IsDark: true })
			{
				return;
			}

			base.OnPaintBackground(pevent);
		}

		/// <inheritdoc/>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (this.currentTheme is not { IsDark: true } palette)
			{
				base.OnPaint(e);

				return;
			}

			using var surfaceBrush = new SolidBrush(palette.Surface);

			e.Graphics.FillRectangle(surfaceBrush, ClientRectangle);

			DrawPageBorder(e.Graphics, palette);

			for (int i = 0; i < TabCount; i++)
			{
				if (i != SelectedIndex)
				{
					DrawTabHeader(e.Graphics, i, isSelected: false, palette);
				}
			}

			if (SelectedIndex >= 0 && SelectedIndex < TabCount)
			{
				DrawTabHeader(e.Graphics, SelectedIndex, isSelected: true, palette);
			}
		}

		/// <summary>
		/// Draws a subtle themed border around the tab page content area.
		/// </summary>
		/// <param name="graphics">The paint graphics surface.</param>
		/// <param name="palette">The active dark theme palette.</param>
		private void DrawPageBorder(Graphics graphics, ThemePalette palette)
		{
			Rectangle display = DisplayRectangle;
			int left          = Math.Max(0, display.Left - 1);
			int top           = Math.Max(0, display.Top - 1);
			int right         = Math.Min(ClientRectangle.Width - 1, display.Right);
			int bottom        = Math.Min(ClientRectangle.Height - 1, display.Bottom);
			int width         = Math.Max(0, right - left);
			int height        = Math.Max(0, bottom - top);

			if (width <= 0 || height <= 0)
			{
				return;
			}

			using var borderPen = new Pen(palette.Border);

			graphics.DrawRectangle(borderPen, left, top, width, height);
		}

		/// <summary>
		/// Paints a single tab header in dark mode.
		/// </summary>
		/// <param name="graphics">The paint graphics surface.</param>
		/// <param name="index">The zero-based tab index.</param>
		/// <param name="isSelected">Whether this tab is currently active.</param>
		/// <param name="palette">The active dark theme palette.</param>
		private void DrawTabHeader(Graphics graphics, int index, bool isSelected, ThemePalette palette)
		{
			Rectangle bounds = GetTabRect(index);

			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return;
			}

			TabPage tabPage = TabPages[index];
			Color backColor = isSelected ? palette.Selection : palette.SurfaceAlt;
			Color textColor = !tabPage.Enabled
				? palette.DisabledText
				: (isSelected ? palette.SelectionText : palette.Text);

			Rectangle fillBounds = isSelected
				? new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height + 1)
				: bounds;

			using var backBrush = new SolidBrush(backColor);
			using var borderPen = new Pen(palette.Border);

			graphics.FillRectangle(backBrush, fillBounds);

			int right  = bounds.Right - 1;
			int bottom = bounds.Bottom - 1;

			graphics.DrawLine(borderPen, bounds.Left, bounds.Top, right, bounds.Top);
			graphics.DrawLine(borderPen, bounds.Left, bounds.Top, bounds.Left, bottom);
			graphics.DrawLine(borderPen, right, bounds.Top, right, bottom);

			if (!isSelected)
			{
				graphics.DrawLine(borderPen, bounds.Left, bottom, right, bottom);
			}

			TextRenderer.DrawText(
				graphics,
				tabPage.Text,
				Font,
				bounds,
				textColor,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
		}
	}
}
