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

namespace BrightIdeasSoftware
{
	/// <summary>
	/// This renderer draws an image, a single line title, and then multi-line description
	/// under the title.
	/// </summary>
	/// <remarks>
	/// <para>This class works best with FullRowSelect = true.</para>
	/// <para>It's not designed to work with cell editing -- it will work but will look odd.</para>
	/// <para>
	/// It's not RightToLeft friendly.
	/// </para>
	/// </remarks>
	public class DescribedTaskRenderer : BaseRenderer, IFilterAwareRenderer
    {
        private readonly StringFormat noWrapStringFormat;
        private readonly HighlightTextRenderer highlightTextRenderer = new HighlightTextRenderer();

        /// <summary>
        /// Create a DescribedTaskRenderer
        /// </summary>
        public DescribedTaskRenderer() {
            this.noWrapStringFormat = new StringFormat(StringFormatFlags.NoWrap);
            this.noWrapStringFormat.Trimming = StringTrimming.EllipsisCharacter;
            this.noWrapStringFormat.Alignment = StringAlignment.Near;
            this.noWrapStringFormat.LineAlignment = StringAlignment.Near;
            this.highlightTextRenderer.CellVerticalAlignment = StringAlignment.Near;
        }

        #region Configuration properties

        /// <summary>
        /// Should text be rendered using GDI routines? This makes the text look more
        /// like a native List view control.
        /// </summary>
        public override bool UseGdiTextRendering
        {
            get { return base.UseGdiTextRendering; }
            set
            {
                base.UseGdiTextRendering = value;
                this.highlightTextRenderer.UseGdiTextRendering = value;
            }
        }

        /// <summary>
        /// Gets or set the font that will be used to draw the title of the task
        /// </summary>
        /// <remarks>If this is null, the ListView's font will be used</remarks>
        [Category("ObjectListView"),
         Description("The font that will be used to draw the title of the task"),
         DefaultValue(null)]
        public Font TitleFont {
            get { return titleFont; }
            set { titleFont = value; }
        }

        private Font titleFont;

        /// <summary>
        /// Return a font that has been set for the title or a reasonable default
        /// </summary>
        [Browsable(false)]
        public Font TitleFontOrDefault {
            get { return this.TitleFont ?? this.ListView.Font; }
        }

        /// <summary>
        /// Gets or set the color of the title of the task
        /// </summary>
        /// <remarks>This color is used when the task is not selected or when the listview
        /// has a translucent selection mechanism.</remarks>
        [Category("ObjectListView"),
         Description("The color of the title"),
         DefaultValue(typeof (Color), "")]
        public Color TitleColor {
            get { return titleColor; }
            set { titleColor = value; }
        }

        private Color titleColor;

        /// <summary>
        /// Return the color of the title of the task or a reasonable default
        /// </summary>
        [Browsable(false)]
        public Color TitleColorOrDefault {
            get {
                if (!this.ListItem.Enabled)
                    return this.SubItem.ForeColor;
                if (this.IsItemSelected || this.TitleColor.IsEmpty)
                    return this.GetForegroundColor();
                
                return this.TitleColor;
            }
        }

        /// <summary>
        /// Gets or set the font that will be used to draw the description of the task
        /// </summary>
        /// <remarks>If this is null, the ListView's font will be used</remarks>
        [Category("ObjectListView"),
         Description("The font that will be used to draw the description of the task"),
         DefaultValue(null)]
        public Font DescriptionFont {
            get { return descriptionFont; }
            set { descriptionFont = value; }
        }

        private Font descriptionFont;

        /// <summary>
        /// Return a font that has been set for the title or a reasonable default
        /// </summary>
        [Browsable(false)]
        public Font DescriptionFontOrDefault {
            get { return this.DescriptionFont ?? this.ListView.Font; }
        }

        /// <summary>
        /// Gets or set the color of the description of the task
        /// </summary>
        /// <remarks>This color is used when the task is not selected or when the listview
        /// has a translucent selection mechanism.</remarks>
        [Category("ObjectListView"),
         Description("The color of the description"),
         DefaultValue(typeof (Color), "")]
        public Color DescriptionColor {
            get { return descriptionColor; }
            set { descriptionColor = value; }
        }
        private Color descriptionColor = Color.Empty;

        /// <summary>
        /// Return the color of the description of the task or a reasonable default
        /// </summary>
        [Browsable(false)]
        public Color DescriptionColorOrDefault {
            get {
                if (!this.ListItem.Enabled)
                    return this.SubItem.ForeColor;
                if (this.IsItemSelected && !this.ListView.UseTranslucentSelection)
                    return this.GetForegroundColor();
                return this.DescriptionColor.IsEmpty ? defaultDescriptionColor : this.DescriptionColor;
            }
        }
        private static Color defaultDescriptionColor = Color.FromArgb(45, 46, 49);

        /// <summary>
        /// Gets or sets the number of pixels that will be left between the image and the text
        /// </summary>
        [Category("ObjectListView"),
         Description("The number of pixels that that will be left between the image and the text"),
         DefaultValue(4)]
        public int ImageTextSpace
        {
            get { return imageTextSpace; }
            set { imageTextSpace = value; }
        }
        private int imageTextSpace = 4;

        /// <summary>
        /// Gets or sets the number of pixels that will be left between the title and the description
        /// </summary>
        [Category("ObjectListView"),
         Description("The number of pixels that that will be left between the title and the description"),
         DefaultValue(2)]
        public int TitleDescriptionSpace
        {
            get { return titleDescriptionSpace; }
            set { titleDescriptionSpace = value; }
        }
        private int titleDescriptionSpace = 2;

        /// <summary>
        /// Gets or sets the name of the aspect of the model object that contains the task description
        /// </summary>
        [Category("ObjectListView"),
         Description("The name of the aspect of the model object that contains the task description"),
         DefaultValue(null)]
        public string DescriptionAspectName {
            get { return descriptionAspectName; }
            set { descriptionAspectName = value; }
        }
        private string descriptionAspectName;

        #endregion

        #region Text highlighting

        /// <summary>
        /// Gets or sets the filter that is filtering the ObjectListView and for
        /// which this renderer should highlight text
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextMatchFilter Filter
        {
            get { return this.highlightTextRenderer.Filter; }
            set { this.highlightTextRenderer.Filter = value; }
        }

        /// <summary>
        /// When a filter changes, keep track of the text matching filters
        /// </summary>
        IModelFilter IFilterAwareRenderer.Filter {
            get { return this.Filter; }
            set { this.highlightTextRenderer.RegisterNewFilter(value); }
        }

        #endregion

        #region Calculating

        /// <summary>
        /// Fetch the description from the model class
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual string GetDescription(object model) 
		{
             if (String.IsNullOrEmpty(this.DescriptionAspectName))
			{
				return this.descriptionGetter.Invoke(model) as string;
			}
			else
			{
				return this.DescriptionAspectName;
			}
        }
		/// <summary>
		/// This delegate will be used to extract a value to be displayed in this column.
		/// </summary>
		/// <remarks>
		/// If this is set, AspectName is ignored.
		/// </remarks>
		[Browsable(false),
		 DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AspectGetterDelegate DescriptionGetter
		{
			set { descriptionGetter = value; }
		}
		private AspectGetterDelegate descriptionGetter;

        #endregion

        #region Rendering

        public override void ConfigureSubItem(DrawListViewSubItemEventArgs e, Rectangle cellBounds, object model) {
            base.ConfigureSubItem(e, cellBounds, model);
            this.highlightTextRenderer.ConfigureSubItem(e, cellBounds, model);
        }

        /// <summary>
        /// Draw our item
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);
            r = this.ApplyCellPadding(r);
            this.DrawDescribedTask(g, r, this.GetText(), this.GetDescription(this.RowObject), this.GetImageSelector());
        }

        /// <summary>
        /// Draw our item
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);
            r = this.ApplyCellPadding(r);
            this.DrawDescribedTask(g, r, this.GetText(), this.GetDescription(this.RowObject), this.GetImageSelector());
        }

        /// <summary>
        /// Draw the task
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="imageSelector"></param>
        protected virtual void DrawDescribedTask(Graphics g, Rectangle r, string title, string description, object imageSelector) {

            //Debug.WriteLine(String.Format("DrawDescribedTask({0}, {1}, {2}, {3})", r, title, description, imageSelector));

            // Draw the image if one's been given
            Rectangle textBounds = r;
            if (imageSelector != null) {

                int imageWidth = this.DrawImage(g, r, imageSelector);

				// g.DrawRectangle(new Pen(Color.Black), r);

                int gapToText = imageWidth + this.ImageTextSpace;
                textBounds.X += gapToText;
                textBounds.Width -= gapToText;
            }

            // Draw the title
            if (!String.IsNullOrEmpty(title)) {
                using (SolidBrush b = new SolidBrush(this.TitleColorOrDefault)) {
                    this.highlightTextRenderer.CanWrap = false;
                    this.highlightTextRenderer.Font = this.TitleFontOrDefault;
                    this.highlightTextRenderer.TextBrush = b;
                    this.highlightTextRenderer.DrawText(g, textBounds, title);
                }

                // How tall was the title?
                SizeF size = g.MeasureString(title, this.TitleFontOrDefault, textBounds.Width, this.noWrapStringFormat);
                int pixelsToDescription = this.TitleDescriptionSpace + (int)size.Height;
                textBounds.Y += pixelsToDescription;
                textBounds.Height -= pixelsToDescription;
            }

            // Draw the description
            if (!String.IsNullOrEmpty(description)) {
                using (SolidBrush b = new SolidBrush(this.DescriptionColorOrDefault)) {
                    this.highlightTextRenderer.CanWrap = true;
                    this.highlightTextRenderer.Font = this.DescriptionFontOrDefault;
                    this.highlightTextRenderer.TextBrush = b;
                    this.highlightTextRenderer.DrawText(g, textBounds, description); 
                }
            }

            //g.DrawRectangle(Pens.OrangeRed, r);
        }

        #endregion

        #region Hit Testing

        /// <summary>
        /// Handle the HitTest request
        /// </summary>
        /// <param name="g"></param>
        /// <param name="hti"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y) {
            if (this.Bounds.Contains(x, y))
                hti.HitTestLocation = HitTestLocation.Text;
        }

        #endregion
    }
}
