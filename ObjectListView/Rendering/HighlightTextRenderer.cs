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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// This renderer highlights substrings that match a given text filter. 
	/// </summary>
	public class HighlightTextRenderer : BaseRenderer, IFilterAwareRenderer {
        #region Life and death

        /// <summary>
        /// Create a HighlightTextRenderer
        /// </summary>
        public HighlightTextRenderer() {
            this.FramePen = Pens.DarkGreen;
            this.FillBrush = Brushes.Yellow;
        }

        /// <summary>
        /// Create a HighlightTextRenderer
        /// </summary>
        /// <param name="filter"></param>
        public HighlightTextRenderer(TextMatchFilter filter)
            : this() {
            this.Filter = filter;
        }

        /// <summary>
        /// Create a HighlightTextRenderer
        /// </summary>
        /// <param name="text"></param>
        [Obsolete("Use HighlightTextRenderer(TextMatchFilter) instead", true)]
        public HighlightTextRenderer(string text) {}

        #endregion

        #region Configuration properties

        /// <summary>
        /// Gets or set how rounded will be the corners of the text match frame
        /// </summary>
        [Category("Appearance"),
         DefaultValue(3.0f),
         Description("How rounded will be the corners of the text match frame?")]
        public float CornerRoundness {
            get { return cornerRoundness; }
            set { cornerRoundness = value; }
        }

        private float cornerRoundness = 3.0f;

        /// <summary>
        /// Gets or set the brush will be used to paint behind the matched substrings.
        /// Set this to null to not fill the frame.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Brush FillBrush {
            get { return fillBrush; }
            set { fillBrush = value; }
        }

        private Brush fillBrush;

        /// <summary>
        /// Gets or sets the filter that is filtering the ObjectListView and for
        /// which this renderer should highlight text
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextMatchFilter Filter {
            get { return filter; }
            set { filter = value; }
        }

        private TextMatchFilter filter;

        /// <summary>
        /// When a filter changes, keep track of the text matching filters
        /// </summary>
        IModelFilter IFilterAwareRenderer.Filter
        {
            get { return filter; }
            set { RegisterNewFilter(value); }
        }

        internal void RegisterNewFilter(IModelFilter newFilter) {
            TextMatchFilter textFilter = newFilter as TextMatchFilter;
            if (textFilter != null)
            {
                Filter = textFilter;
                return;
            }
            CompositeFilter composite = newFilter as CompositeFilter;
            if (composite != null)
            {
                foreach (TextMatchFilter textSubFilter in composite.TextFilters)
                {
                    Filter = textSubFilter;
                    return;
                }
            }
            Filter = null; 
        } 

        /// <summary>
        /// Gets or set the pen will be used to frame the matched substrings.
        /// Set this to null to not draw a frame.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Pen FramePen {
            get { return framePen; }
            set { framePen = value; }
        }

        private Pen framePen;

        /// <summary>
        /// Gets or sets whether the frame around a text match will have rounded corners
        /// </summary>
        [Category("Appearance"),
         DefaultValue(true),
         Description("Will the frame around a text match will have rounded corners?")]
        public bool UseRoundedRectangle {
            get { return useRoundedRectangle; }
            set { useRoundedRectangle = value; }
        }

        private bool useRoundedRectangle = true;

        #endregion

        #region Compatibility properties

        /// <summary>
        /// Gets or set the text that will be highlighted
        /// </summary>
        [Obsolete("Set the Filter directly rather than just the text", true)]
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TextToHighlight {
            get { return String.Empty; }
            set { }
        }

        /// <summary>
        /// Gets or sets the manner in which substring will be compared.
        /// </summary>
        /// <remarks>
        /// Use this to control if substring matches are case sensitive or insensitive.</remarks>
        [Obsolete("Set the Filter directly rather than just this setting", true)]
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public StringComparison StringComparison {
            get { return StringComparison.CurrentCultureIgnoreCase; }
            set { }
        }

        #endregion

        #region IRenderer interface overrides

        /// <summary>
        /// Handle a HitTest request after all state information has been initialized
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="preferredSize"> </param>
        /// <returns></returns>
        protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex, Size preferredSize) {
            return this.StandardGetEditRectangle(g, cellBounds, preferredSize);
        }

        #endregion

        #region Rendering

        // This class has two implement two highlighting schemes: one for GDI, another for GDI+.
        // Naturally, GDI+ makes the task easier, but we have to provide something for GDI
        // since that it is what is normally used.

        /// <summary>
        /// Draw text using GDI
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="txt"></param>
        protected override void DrawTextGdi(Graphics g, Rectangle r, string txt) {
            if (this.ShouldDrawHighlighting)
                this.DrawGdiTextHighlighting(g, r, txt);

            base.DrawTextGdi(g, r, txt);
        }

        /// <summary>
        /// Draw the highlighted text using GDI
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="txt"></param>
        protected virtual void DrawGdiTextHighlighting(Graphics g, Rectangle r, string txt) {

            // TextRenderer puts horizontal padding around the strings, so we need to take
            // that into account when measuring strings
            const int paddingAdjustment = 6;

            // Cache the font
            Font f = this.Font;

            foreach (CharacterRange range in this.Filter.FindAllMatchedRanges(txt)) {
                // Measure the text that comes before our substring
                Size precedingTextSize = Size.Empty;
                if (range.First > 0) {
                    string precedingText = txt.Substring(0, range.First);
                    precedingTextSize = TextRenderer.MeasureText(g, precedingText, f, r.Size, NormalTextFormatFlags);
                    precedingTextSize.Width -= paddingAdjustment;
                }

                // Measure the length of our substring (may be different each time due to case differences)
                string highlightText = txt.Substring(range.First, range.Length);
                Size textToHighlightSize = TextRenderer.MeasureText(g, highlightText, f, r.Size, NormalTextFormatFlags);
                textToHighlightSize.Width -= paddingAdjustment;

                float textToHighlightLeft = r.X + precedingTextSize.Width + 1;
                float textToHighlightTop = this.AlignVertically(r, textToHighlightSize.Height);

                // Draw a filled frame around our substring
                this.DrawSubstringFrame(g, textToHighlightLeft, textToHighlightTop, textToHighlightSize.Width, textToHighlightSize.Height);
            }
        }

        /// <summary>
        /// Draw an indication around the given frame that shows a text match
        /// </summary>
        /// <param name="g"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected virtual void DrawSubstringFrame(Graphics g, float x, float y, float width, float height) {
            if (this.UseRoundedRectangle) {
                using (GraphicsPath path = this.GetRoundedRect(x, y, width, height, 3.0f)) {
                    if (this.FillBrush != null)
                        g.FillPath(this.FillBrush, path);
                    if (this.FramePen != null)
                        g.DrawPath(this.FramePen, path);
                }
            } else {
                if (this.FillBrush != null)
                    g.FillRectangle(this.FillBrush, x, y, width, height);
                if (this.FramePen != null)
                    g.DrawRectangle(this.FramePen, x, y, width, height);
            }
        }

        /// <summary>
        /// Draw the text using GDI+
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="txt"></param>
        protected override void DrawTextGdiPlus(Graphics g, Rectangle r, string txt) {
            if (this.ShouldDrawHighlighting)
                this.DrawGdiPlusTextHighlighting(g, r, txt);

            base.DrawTextGdiPlus(g, r, txt);
        }

        /// <summary>
        /// Draw the highlighted text using GDI+
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="txt"></param>
        protected virtual void DrawGdiPlusTextHighlighting(Graphics g, Rectangle r, string txt) {
            // Find the substrings we want to highlight
            List<CharacterRange> ranges = new List<CharacterRange>(this.Filter.FindAllMatchedRanges(txt));

            if (ranges.Count == 0)
                return;

            using (StringFormat fmt = this.StringFormatForGdiPlus) {
                RectangleF rf = r;
                fmt.SetMeasurableCharacterRanges(ranges.ToArray());
                Region[] stringRegions = g.MeasureCharacterRanges(txt, this.Font, rf, fmt);

                foreach (Region region in stringRegions) {
                    RectangleF bounds = region.GetBounds(g);
                    this.DrawSubstringFrame(g, bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height);
                }
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets whether the renderer should actually draw highlighting
        /// </summary>
        protected bool ShouldDrawHighlighting {
            get { return this.Column == null || (this.Column.Searchable && this.Filter != null && this.Filter.HasComponents); }
        }

        /// <summary>
        /// Return a GraphicPath that is a round cornered rectangle
        /// </summary>
        /// <returns>A round cornered rectangle path</returns>
        /// <remarks>If I could rely on people using C# 3.0+, this should be
        /// an extension method of GraphicsPath.</remarks>        
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="diameter"></param>
        protected GraphicsPath GetRoundedRect(float x, float y, float width, float height, float diameter) {
            return GetRoundedRect(new RectangleF(x, y, width, height), diameter);
        }

        /// <summary>
        /// Return a GraphicPath that is a round cornered rectangle
        /// </summary>
        /// <param name="rect">The rectangle</param>
        /// <param name="diameter">The diameter of the corners</param>
        /// <returns>A round cornered rectangle path</returns>
        /// <remarks>If I could rely on people using C# 3.0+, this should be
        /// an extension method of GraphicsPath.</remarks>
        protected GraphicsPath GetRoundedRect(RectangleF rect, float diameter) {
            GraphicsPath path = new GraphicsPath();

            if (diameter > 0) {
                RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
                path.AddArc(arc, 180, 90);
                arc.X = rect.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = rect.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = rect.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();
            } else {
                path.AddRectangle(rect);
            }

            return path;
        }

        #endregion
    }
}
