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
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// This renderer draws a functioning button in its cell
	/// </summary>
	public class ColumnButtonRenderer : BaseRenderer {

        #region Properties

        /// <summary>
        /// Gets or sets how each button will be sized
        /// </summary>
        [Category("ObjectListView"),
        Description("How each button will be sized"),
        DefaultValue(OLVColumn.ButtonSizingMode.TextBounds)]
        public OLVColumn.ButtonSizingMode SizingMode
        {
            get { return this.sizingMode; }
            set { this.sizingMode = value; }
        }
        private OLVColumn.ButtonSizingMode sizingMode = OLVColumn.ButtonSizingMode.TextBounds;

        /// <summary>
        /// Gets or sets the size of the button when the SizingMode is FixedBounds
        /// </summary>
        /// <remarks>If this is not set, the bounds of the cell will be used</remarks>
        [Category("ObjectListView"),
        Description("The size of the button when the SizingMode is FixedBounds"),
        DefaultValue(null)]
        public Size? ButtonSize
        {
            get { return this.buttonSize; }
            set { this.buttonSize = value; }
        }
        private Size? buttonSize;

        /// <summary>
        /// Gets or sets the extra space that surrounds the cell when the SizingMode is TextBounds
        /// </summary>
        [Category("ObjectListView"),
        Description("The extra space that surrounds the cell when the SizingMode is TextBounds")]
        public Size? ButtonPadding
        {
            get { return this.buttonPadding; }
            set { this.buttonPadding = value; }
        }
        private Size? buttonPadding = new Size(10, 10);

		/// <summary>
		/// Gets the button padding or default.
		/// </summary>
		private Size ButtonPaddingOrDefault {
            get { return this.ButtonPadding ?? new Size(10, 10); }
        }

        /// <summary>
        /// Gets or sets the maximum width that a button can occupy.
        /// -1 means there is no maximum width.
        /// </summary>
        /// <remarks>This is only considered when the SizingMode is TextBounds</remarks>
        [Category("ObjectListView"),
        Description("The maximum width that a button can occupy when the SizingMode is TextBounds"),
        DefaultValue(-1)]
        public int MaxButtonWidth
        {
            get { return this.maxButtonWidth; }
            set { this.maxButtonWidth = value; }
        }
        private int maxButtonWidth = -1;

        /// <summary>
        /// Gets or sets the minimum width that a button can occupy.
        /// -1 means there is no minimum width.
        /// </summary>
        /// <remarks>This is only considered when the SizingMode is TextBounds</remarks>
        [Category("ObjectListView"),
         Description("The minimum width that a button can be when the SizingMode is TextBounds"),
         DefaultValue(-1)]
        public int MinButtonWidth {
            get { return this.minButtonWidth; }
            set { this.minButtonWidth = value; }
        }
        private int minButtonWidth = -1;

        #endregion

        #region Rendering

        /// <summary>
        /// Calculate the size of the contents
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        protected override Size CalculateContentSize(Graphics g, Rectangle r) {
            if (this.SizingMode == OLVColumn.ButtonSizingMode.CellBounds)
                return r.Size;

            if (this.SizingMode == OLVColumn.ButtonSizingMode.FixedBounds)
                return this.ButtonSize ?? r.Size;

            // Ok, SizingMode must be TextBounds. So figure out the size of the text
            Size textSize = this.CalculateTextSize(g, this.GetText(), r.Width);

            // Allow for padding and max width
            textSize.Height += this.ButtonPaddingOrDefault.Height * 2;
            textSize.Width += this.ButtonPaddingOrDefault.Width * 2;
            if (this.MaxButtonWidth != -1 && textSize.Width > this.MaxButtonWidth)
                textSize.Width = this.MaxButtonWidth;
            if (textSize.Width < this.MinButtonWidth)
                textSize.Width = this.MinButtonWidth;

            return textSize;
        }

        /// <summary>
        /// Draw the button
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        protected override void DrawImageAndText(Graphics g, Rectangle r) {
            TextFormatFlags textFormatFlags = TextFormatFlags.HorizontalCenter |
                                              TextFormatFlags.VerticalCenter |
                                              TextFormatFlags.EndEllipsis |
                                              TextFormatFlags.NoPadding |
                                              TextFormatFlags.SingleLine |
                                              TextFormatFlags.PreserveGraphicsTranslateTransform;
            if (this.ListView.RightToLeftLayout)
                textFormatFlags |= TextFormatFlags.RightToLeft;

            string buttonText = GetText();
            if (!String.IsNullOrEmpty(buttonText))
                ButtonRenderer.DrawButton(g, r, buttonText, this.Font, textFormatFlags, false, CalculatePushButtonState());
        }

        /// <summary>
        /// What part of the control is under the given point?
        /// </summary>
        /// <param name="g"></param>
        /// <param name="hti"></param>
        /// <param name="bounds"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected override void StandardHitTest(Graphics g, OlvListViewHitTestInfo hti, Rectangle bounds, int x, int y) {
            Rectangle r = ApplyCellPadding(bounds);
            if (r.Contains(x, y))
                hti.HitTestLocation = HitTestLocation.Button;
        }

        /// <summary>
        /// What is the state of the button?
        /// </summary>
        /// <returns></returns>
        protected PushButtonState CalculatePushButtonState() {
            if (!this.ListItem.Enabled && !this.Column.EnableButtonWhenItemIsDisabled)
                return PushButtonState.Disabled;

            if (this.IsButtonHot)
                return ObjectListView.IsLeftMouseDown ? PushButtonState.Pressed : PushButtonState.Hot;

            return PushButtonState.Normal;
        }

        /// <summary>
        /// Is the mouse over the button?
        /// </summary>
        protected bool IsButtonHot {
            get {
                return this.IsCellHot && this.ListView.HotCellHitLocation == HitTestLocation.Button;
            }
        }

        #endregion
    }
}
