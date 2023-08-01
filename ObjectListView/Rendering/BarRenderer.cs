/*
 * Renderers - A collection of useful renderers that are used to owner draw a cell in an ObjectListView
 *
 * Author: Phillip Piper
 * Date: 27/09/2008 9:15 AM
 *
 * Change log: 
 * v2.9
 * 2015-08-22   JPP  - Allow selected row back/fore colours to be specified for each row
 * 2015-06-23   JPP  - Added ColumnButtonRenderer plus general support for Buttons
 * 2015-06-22   JPP  - Added BaseRenderer.ConfigureItem() and ConfigureSubItem() to easily allow
 *                     other renderers to be chained for use within a primary renderer.
 *                   - Lots of tightening of hit tests and edit rectangles
 * 2015-05-15   JPP  - Handle renderering an Image when that Image is returned as an aspect.
 * v2.8
 * 2014-09-26   JPP  - Dispose of animation timer in a more robust fashion.
 * 2014-05-20   JPP  - Handle rendering disabled rows
 * v2.7
 * 2013-04-29   JPP  - Fixed bug where Images were not vertically aligned
 * v2.6
 * 2012-10-26   JPP  - Hit detection will no longer report check box hits on columns without checkboxes.
 * 2012-07-13   JPP  - [Breaking change] Added preferedSize parameter to IRenderer.GetEditRectangle().
 * v2.5.1
 * 2012-07-14   JPP  - Added CellPadding to various places. Replaced DescribedTaskRenderer.CellPadding.
 * 2012-07-11   JPP  - Added CellVerticalAlignment to various places allow cell contents to be vertically
 *                     aligned (rather than always being centered).
 * v2.5
 * 2010-08-24   JPP  - CheckBoxRenderer handles hot boxes and correctly vertically centers the box.
 * 2010-06-23   JPP  - Major rework of HighlightTextRenderer. Now uses TextMatchFilter directly.
 *                     Draw highlighting underneath text to improve legibility. Works with new
 *                     TextMatchFilter capabilities.
 * v2.4
 * 2009-10-30   JPP  - Plugged possible resource leak by using using() with CreateGraphics()
 * v2.3
 * 2009-09-28   JPP  - Added DescribedTaskRenderer
 * 2009-09-01   JPP  - Correctly handle an ImageRenderer's handling of an aspect that holds
 *                     the image to be displayed at Byte[].
 * 2009-08-29   JPP  - Fixed bug where some of a cell's background was not erased. 
 * 2009-08-15   JPP  - Correctly MeasureText() using the appropriate graphic context
 *                   - Handle translucent selection setting
 * v2.2.1
 * 2009-07-24   JPP  - Try to honour CanWrap setting when GDI rendering text.
 * 2009-07-11   JPP  - Correctly calculate edit rectangle for subitems of a tree view
 *                     (previously subitems were indented in the same way as the primary column)
 * v2.2
 * 2009-06-06   JPP  - Tweaked text rendering so that column 0 isn't ellipsed unnecessarily.
 * 2009-05-05   JPP  - Added Unfocused foreground and background colors 
 *                     (thanks to Christophe Hosten)
 * 2009-04-21   JPP  - Fixed off-by-1 error when calculating text widths. This caused
 *                     middle and right aligned columns to always wrap one character
 *                     when printed using ListViewPrinter (SF#2776634).
 * 2009-04-11   JPP  - Correctly renderer checkboxes when RowHeight is non-standard
 * 2009-04-06   JPP  - Allow for item indent when calculating edit rectangle
 * v2.1
 * 2009-02-24   JPP  - Work properly with ListViewPrinter again
 * 2009-01-26   JPP  - AUSTRALIA DAY (why aren't I on holidays!)
 *                   - Major overhaul of renderers. Now uses IRenderer interface.
 *                   - ImagesRenderer and FlagsRenderer<T> are now defunct.
 *                     The names are retained for backward compatibility.
 * 2009-01-23   JPP  - Align bitmap AND text according to column alignment (previously
 *                     only text was aligned and bitmap was always to the left).
 * 2009-01-21   JPP  - Changed to use TextRenderer rather than native GDI routines.
 * 2009-01-20   JPP  - Draw images directly from image list if possible. 30% faster!
 *                   - Tweaked some spacings to look more like native ListView
 *                   - Text highlight for non FullRowSelect is now the right color
 *                     when the control doesn't have focus.
 *                   - Commented out experimental animations. Still needs work.
 * 2009-01-19   JPP  - Changed to draw text using GDI routines. Looks more like
 *                     native control this way. Set UseGdiTextRendering to false to 
 *                     revert to previous behavior.
 * 2009-01-15   JPP  - Draw background correctly when control is disabled
 *                   - Render checkboxes using CheckBoxRenderer
 * v2.0.1
 * 2008-12-29   JPP  - Render text correctly when HideSelection is true.
 * 2008-12-26   JPP  - BaseRenderer now works correctly in all Views
 * 2008-12-23   JPP  - Fixed two small bugs in BarRenderer
 * v2.0
 * 2008-10-26   JPP  - Don't owner draw when in Design mode
 * 2008-09-27   JPP  - Separated from ObjectListView.cs
 * 
 * Copyright (C) 2006-2014 Phillip Piper
 * 
 * TO DO:
 * - Hit detection on renderers doesn't change the controls standard selection behavior
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip.piper@gmail.com.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// Render our Aspect as a progress bar
	/// </summary>
	public class BarRenderer : BaseRenderer {
        #region Constructors

        /// <summary>
        /// Make a BarRenderer
        /// </summary>
        public BarRenderer()
            : base() {}

        /// <summary>
        /// Make a BarRenderer for the given range of data values
        /// </summary>
        public BarRenderer(int minimum, int maximum)
            : this() {
            this.MinimumValue = minimum;
            this.MaximumValue = maximum;
        }

        /// <summary>
        /// Make a BarRenderer using a custom bar scheme
        /// </summary>
        public BarRenderer(Pen pen, Brush brush)
            : this() {
            this.Pen = pen;
            this.Brush = brush;
            this.UseStandardBar = false;
        }

        /// <summary>
        /// Make a BarRenderer using a custom bar scheme
        /// </summary>
        public BarRenderer(int minimum, int maximum, Pen pen, Brush brush)
            : this(minimum, maximum) {
            this.Pen = pen;
            this.Brush = brush;
            this.UseStandardBar = false;
        }

        /// <summary>
        /// Make a BarRenderer that uses a horizontal gradient
        /// </summary>
        public BarRenderer(Pen pen, Color start, Color end)
            : this() {
            this.Pen = pen;
            this.SetGradient(start, end);
        }

        /// <summary>
        /// Make a BarRenderer that uses a horizontal gradient
        /// </summary>
        public BarRenderer(int minimum, int maximum, Pen pen, Color start, Color end)
            : this(minimum, maximum) {
            this.Pen = pen;
            this.SetGradient(start, end);
        }

        #endregion

        #region Configuration Properties

        /// <summary>
        /// Should this bar be drawn in the system style?
        /// </summary>
        [Category("ObjectListView"),
         Description("Should this bar be drawn in the system style?"),
         DefaultValue(true)]
        public bool UseStandardBar {
            get { return useStandardBar; }
            set { useStandardBar = value; }
        }

        private bool useStandardBar = true;

        /// <summary>
        /// How many pixels in from our cell border will this bar be drawn
        /// </summary>
        [Category("ObjectListView"),
         Description("How many pixels in from our cell border will this bar be drawn"),
         DefaultValue(2)]
        public int Padding {
            get { return padding; }
            set { padding = value; }
        }

        private int padding = 2;

        /// <summary>
        /// What color will be used to fill the interior of the control before the 
        /// progress bar is drawn?
        /// </summary>
        [Category("ObjectListView"),
         Description("The color of the interior of the bar"),
         DefaultValue(typeof (Color), "AliceBlue")]
        public Color BackgroundColor {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        private Color backgroundColor = Color.AliceBlue;

        /// <summary>
        /// What color should the frame of the progress bar be?
        /// </summary>
        [Category("ObjectListView"),
         Description("What color should the frame of the progress bar be"),
         DefaultValue(typeof (Color), "Black")]
        public Color FrameColor {
            get { return frameColor; }
            set { frameColor = value; }
        }

        private Color frameColor = Color.Black;

        /// <summary>
        /// How many pixels wide should the frame of the progress bar be?
        /// </summary>
        [Category("ObjectListView"),
         Description("How many pixels wide should the frame of the progress bar be"),
         DefaultValue(1.0f)]
        public float FrameWidth {
            get { return frameWidth; }
            set { frameWidth = value; }
        }

        private float frameWidth = 1.0f;

        /// <summary>
        /// What color should the 'filled in' part of the progress bar be?
        /// </summary>
        /// <remarks>This is only used if GradientStartColor is Color.Empty</remarks>
        [Category("ObjectListView"),
         Description("What color should the 'filled in' part of the progress bar be"),
         DefaultValue(typeof (Color), "BlueViolet")]
        public Color FillColor {
            get { return fillColor; }
            set { fillColor = value; }
        }

        private Color fillColor = Color.BlueViolet;

        /// <summary>
        /// Use a gradient to fill the progress bar starting with this color
        /// </summary>
        [Category("ObjectListView"),
         Description("Use a gradient to fill the progress bar starting with this color"),
         DefaultValue(typeof (Color), "CornflowerBlue")]
        public Color GradientStartColor {
            get { return startColor; }
            set { startColor = value; }
        }

        private Color startColor = Color.CornflowerBlue;

        /// <summary>
        /// Use a gradient to fill the progress bar ending with this color
        /// </summary>
        [Category("ObjectListView"),
         Description("Use a gradient to fill the progress bar ending with this color"),
         DefaultValue(typeof (Color), "DarkBlue")]
        public Color GradientEndColor {
            get { return endColor; }
            set { endColor = value; }
        }

        private Color endColor = Color.DarkBlue;

        /// <summary>
        /// Regardless of how wide the column become the progress bar will never be wider than this
        /// </summary>
        [Category("Behavior"),
         Description("The progress bar will never be wider than this"),
         DefaultValue(100)]
        public int MaximumWidth {
            get { return maximumWidth; }
            set { maximumWidth = value; }
        }

        private int maximumWidth = 100;

        /// <summary>
        /// Regardless of how high the cell is  the progress bar will never be taller than this
        /// </summary>
        [Category("Behavior"),
         Description("The progress bar will never be taller than this"),
         DefaultValue(16)]
        public int MaximumHeight {
            get { return maximumHeight; }
            set { maximumHeight = value; }
        }

        private int maximumHeight = 16;

        /// <summary>
        /// The minimum data value expected. Values less than this will given an empty bar
        /// </summary>
        [Category("Behavior"),
         Description("The minimum data value expected. Values less than this will given an empty bar"),
         DefaultValue(0.0)]
        public double MinimumValue {
            get { return minimumValue; }
            set { minimumValue = value; }
        }

        private double minimumValue = 0.0;

        /// <summary>
        /// The maximum value for the range. Values greater than this will give a full bar
        /// </summary>
        [Category("Behavior"),
         Description("The maximum value for the range. Values greater than this will give a full bar"),
         DefaultValue(100.0)]
        public double MaximumValue {
            get { return maximumValue; }
            set { maximumValue = value; }
        }

        private double maximumValue = 100.0;

        #endregion

        #region Public Properties (non-IDE)

        /// <summary>
        /// The Pen that will draw the frame surrounding this bar
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Pen Pen {
            get {
                if (this.pen == null && !this.FrameColor.IsEmpty)
                    return new Pen(this.FrameColor, this.FrameWidth);
                else
                    return this.pen;
            }
            set { this.pen = value; }
        }

        private Pen pen;

        /// <summary>
        /// The brush that will be used to fill the bar
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Brush Brush {
            get {
                if (this.brush == null && !this.FillColor.IsEmpty)
                    return new SolidBrush(this.FillColor);
                else
                    return this.brush;
            }
            set { this.brush = value; }
        }

        private Brush brush;

        /// <summary>
        /// The brush that will be used to fill the background of the bar
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Brush BackgroundBrush {
            get {
                if (this.backgroundBrush == null && !this.BackgroundColor.IsEmpty)
                    return new SolidBrush(this.BackgroundColor);
                else
                    return this.backgroundBrush;
            }
            set { this.backgroundBrush = value; }
        }

        private Brush backgroundBrush;

        #endregion

        /// <summary>
        /// Draw this progress bar using a gradient
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void SetGradient(Color start, Color end) {
            this.GradientStartColor = start;
            this.GradientEndColor = end;
        }

        /// <summary>
        /// Draw our aspect
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);

            r = this.ApplyCellPadding(r);

            Rectangle frameRect = Rectangle.Inflate(r, 0 - this.Padding, 0 - this.Padding);
            frameRect.Width = Math.Min(frameRect.Width, this.MaximumWidth);
            frameRect.Height = Math.Min(frameRect.Height, this.MaximumHeight);
            frameRect = this.AlignRectangle(r, frameRect);

            // Convert our aspect to a numeric value
            IConvertible convertable = this.Aspect as IConvertible;
            if (convertable == null)
                return;
            double aspectValue = convertable.ToDouble(NumberFormatInfo.InvariantInfo);

            Rectangle fillRect = Rectangle.Inflate(frameRect, -1, -1);
            if (aspectValue <= this.MinimumValue)
                fillRect.Width = 0;
            else if (aspectValue < this.MaximumValue)
                fillRect.Width = (int) (fillRect.Width * (aspectValue - this.MinimumValue) / this.MaximumValue);

            // MS-themed progress bars don't work when printing
            if (this.UseStandardBar && ProgressBarRenderer.IsSupported && !this.IsPrinting) {
                ProgressBarRenderer.DrawHorizontalBar(g, frameRect);
                ProgressBarRenderer.DrawHorizontalChunks(g, fillRect);
            } else {
                g.FillRectangle(this.BackgroundBrush, frameRect);
                if (fillRect.Width > 0) {
                    // FillRectangle fills inside the given rectangle, so expand it a little
                    fillRect.Width++;
                    fillRect.Height++;
                    if (this.GradientStartColor == Color.Empty)
                        g.FillRectangle(this.Brush, fillRect);
                    else {
                        using (LinearGradientBrush gradient = new LinearGradientBrush(frameRect, this.GradientStartColor, this.GradientEndColor, LinearGradientMode.Horizontal)) {
                            g.FillRectangle(gradient, fillRect);
                        }
                    }
                }
                g.DrawRectangle(this.Pen, frameRect);
            }
        }

        /// <summary>
        /// Draw our aspect
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);

            r = this.ApplyCellPadding(r);

            Rectangle frameRect = Rectangle.Inflate(r, 0 - this.Padding, 0 - this.Padding);
            frameRect.Width = Math.Min(frameRect.Width, this.MaximumWidth);
            frameRect.Height = Math.Min(frameRect.Height, this.MaximumHeight);
            frameRect = this.AlignRectangle(r, frameRect);

            // Convert our aspect to a numeric value
            IConvertible convertable = this.Aspect as IConvertible;
            if (convertable == null)
                return;
            double aspectValue = convertable.ToDouble(NumberFormatInfo.InvariantInfo);

            Rectangle fillRect = Rectangle.Inflate(frameRect, -1, -1);
            if (aspectValue <= this.MinimumValue)
                fillRect.Width = 0;
            else if (aspectValue < this.MaximumValue)
                fillRect.Width = (int) (fillRect.Width * (aspectValue - this.MinimumValue) / this.MaximumValue);

            // MS-themed progress bars don't work when printing
            if (this.UseStandardBar && ProgressBarRenderer.IsSupported && !this.IsPrinting) {
                ProgressBarRenderer.DrawHorizontalBar(g, frameRect);
                ProgressBarRenderer.DrawHorizontalChunks(g, fillRect);
            } else {
                g.FillRectangle(this.BackgroundBrush, frameRect);
                if (fillRect.Width > 0) {
                    // FillRectangle fills inside the given rectangle, so expand it a little
                    fillRect.Width++;
                    fillRect.Height++;
                    if (this.GradientStartColor == Color.Empty)
                        g.FillRectangle(this.Brush, fillRect);
                    else {
                        using (LinearGradientBrush gradient = new LinearGradientBrush(frameRect, this.GradientStartColor, this.GradientEndColor, LinearGradientMode.Horizontal)) {
                            g.FillRectangle(gradient, fillRect);
                        }
                    }
                }
                g.DrawRectangle(this.Pen, frameRect);
            }
        }

        /// <summary>
        /// Handle the GetEditRectangle request
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="preferredSize"> </param>
        /// <returns></returns>
        protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex, Size preferredSize) {
            return this.CalculatePaddedAlignedBounds(g, cellBounds, preferredSize);
        }
    }
}
