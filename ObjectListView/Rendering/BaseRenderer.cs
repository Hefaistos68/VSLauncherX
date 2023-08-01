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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// A BaseRenderer provides useful base level functionality for any custom renderer.
	/// </summary>
	/// <remarks>
	/// <para>Subclasses will normally override the Render or OptionalRender method, and use the other
	/// methods as helper functions.</para>
	/// </remarks>
	[Browsable(true),
     ToolboxItem(true)]
    public class BaseRenderer : AbstractRenderer {
        internal const TextFormatFlags NormalTextFormatFlags = TextFormatFlags.NoPrefix |
                                                               TextFormatFlags.EndEllipsis |
                                                               TextFormatFlags.PreserveGraphicsTranslateTransform;

        #region Configuration Properties

        /// <summary>
        /// Can the renderer wrap lines that do not fit completely within the cell?
        /// </summary>
        /// <remarks>Wrapping text doesn't work with the GDI renderer.</remarks>
        [Category("Appearance"),
         Description("Can the renderer wrap text that does not fit completely within the cell"),
         DefaultValue(false)]
        public bool CanWrap {
            get { return canWrap; }
            set {
                canWrap = value;
                if (canWrap)
                    this.UseGdiTextRendering = false;
            }
        }
        private bool canWrap;

        /// <summary>
        /// Gets or sets how many pixels will be left blank around this cell
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting only takes effect when the control is owner drawn.
        /// </para>
        /// <para><see cref="ObjectListView.CellPadding"/> for more details.</para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("The number of pixels that renderer will leave empty around the edge of the cell"),
         DefaultValue(null)]
        public Rectangle? CellPadding {
            get { return this.cellPadding; }
            set { this.cellPadding = value; }
        }
        private Rectangle? cellPadding;

        /// <summary>
        /// Gets the horiztonal alignment of the column
        /// </summary>
        [Browsable(false)]
        public HorizontalAlignment CellHorizontalAlignment
        {
            get { return this.Column == null ? HorizontalAlignment.Left : this.Column.TextAlign; }
        }

        /// <summary>
        /// Gets or sets how cells drawn by this renderer will be vertically aligned.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this is not set, the value from the column or control itself will be used.
        /// </para>
        /// </remarks>
        [Category("ObjectListView"),
         Description("How will cell values be vertically aligned?"),
         DefaultValue(null)]
        public virtual StringAlignment? CellVerticalAlignment {
            get { return this.cellVerticalAlignment; }
            set { this.cellVerticalAlignment = value; }
        }
        private StringAlignment? cellVerticalAlignment;

        /// <summary>
        /// Gets the optional padding that this renderer should apply before drawing.
        /// This property considers all possible sources of padding
        /// </summary>
        [Browsable(false)]
        protected virtual Rectangle? EffectiveCellPadding {
            get {
                if (this.cellPadding.HasValue)
                    return this.cellPadding.Value;

                if (this.OLVSubItem != null && this.OLVSubItem.CellPadding.HasValue)
                    return this.OLVSubItem.CellPadding.Value;

                if (this.ListItem != null && this.ListItem.CellPadding.HasValue)
                    return this.ListItem.CellPadding.Value;

                if (this.Column != null && this.Column.CellPadding.HasValue)
                    return this.Column.CellPadding.Value;

                if (this.ListView != null && this.ListView.CellPadding.HasValue)
                    return this.ListView.CellPadding.Value;

                return null;
            }
        }

        /// <summary>
        /// Gets the vertical cell alignment that should govern the rendering.
        /// This property considers all possible sources.
        /// </summary>
        [Browsable(false)]
        protected virtual StringAlignment EffectiveCellVerticalAlignment {
            get {
                if (this.cellVerticalAlignment.HasValue)
                    return this.cellVerticalAlignment.Value;

                if (this.OLVSubItem != null && this.OLVSubItem.CellVerticalAlignment.HasValue)
                    return this.OLVSubItem.CellVerticalAlignment.Value;

                if (this.ListItem != null && this.ListItem.CellVerticalAlignment.HasValue)
                    return this.ListItem.CellVerticalAlignment.Value;

                if (this.Column != null && this.Column.CellVerticalAlignment.HasValue)
                    return this.Column.CellVerticalAlignment.Value;

                if (this.ListView != null)
                    return this.ListView.CellVerticalAlignment;

                return StringAlignment.Center;
            }
        }

        /// <summary>
        /// Gets or sets the image list from which keyed images will be fetched
        /// </summary>
        [Category("Appearance"),
         Description("The image list from which keyed images will be fetched for drawing. If this is not given, the small ImageList from the ObjectListView will be used"),
         DefaultValue(null)]
        public ImageList ImageList {
            get { return imageList; }
            set { imageList = value; }
        }

        private ImageList imageList;

        /// <summary>
        /// When rendering multiple images, how many pixels should be between each image?
        /// </summary>
        [Category("Appearance"),
         Description("When rendering multiple images, how many pixels should be between each image?"),
         DefaultValue(1)]
        public int Spacing {
            get { return spacing; }
            set { spacing = value; }
        }

        private int spacing = 1;

        /// <summary>
        /// Should text be rendered using GDI routines? This makes the text look more
        /// like a native List view control.
        /// </summary>
        [Category("Appearance"),
         Description("Should text be rendered using GDI routines?"),
         DefaultValue(true)]
        public virtual bool UseGdiTextRendering {
            get {
                // Can't use GDI routines on a GDI+ printer context
                return !this.IsPrinting && useGdiTextRendering;
            }
            set { useGdiTextRendering = value; }
        }
        private bool useGdiTextRendering = true;

        #endregion

        #region State Properties

        /// <summary>
        /// Get or set the aspect of the model object that this renderer should draw
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Object Aspect {
            get {
                if (aspect == null)
                    aspect = column.GetValue(this.rowObject);
                return aspect;
            }
            set { aspect = value; }
        }

        private Object aspect;

        /// <summary>
        /// What are the bounds of the cell that is being drawn?
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle Bounds {
            get { return bounds; }
            set { bounds = value; }
        }

        private Rectangle bounds;

        /// <summary>
        /// Get or set the OLVColumn that this renderer will draw
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OLVColumn Column {
            get { return column; }
            set { column = value; }
        }

        private OLVColumn column;

        /// <summary>
        /// Get/set the event that caused this renderer to be called
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DrawListViewItemEventArgs DrawItemEvent {
            get { return drawItemEventArgs; }
            set { drawItemEventArgs = value; }
        }

        private DrawListViewItemEventArgs drawItemEventArgs;

        /// <summary>
        /// Get/set the event that caused this renderer to be called
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DrawListViewSubItemEventArgs Event {
            get { return eventArgs; }
            set { eventArgs = value; }
        }

        private DrawListViewSubItemEventArgs eventArgs;

        /// <summary>
        /// Gets or  sets the font to be used for text in this cell
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font Font {
            get {
                if (this.font != null || this.ListItem == null)
                    return this.font;

                if (this.SubItem == null || this.ListItem.UseItemStyleForSubItems)
                    return this.ListItem.Font;

                return this.SubItem.Font;
            }
            set { this.font = value; }
        }

        private Font font;

        /// <summary>
        /// Gets the image list from which keyed images will be fetched
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList ImageListOrDefault {
            get { return this.ImageList ?? this.ListView.SmallImageList; }
        }

        /// <summary>
        /// Should this renderer fill in the background before drawing?
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsDrawBackground {
            get { return !this.IsPrinting; }
        }

        /// <summary>
        /// Cache whether or not our item is selected
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsItemSelected {
            get { return isItemSelected; }
            set { isItemSelected = value; }
        }

        private bool isItemSelected;

        /// <summary>
        /// Is this renderer being used on a printer context?
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPrinting {
            get { return isPrinting; }
            set { isPrinting = value; }
        }

        private bool isPrinting;

        /// <summary>
        /// Get or set the listitem that this renderer will be drawing
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OLVListItem ListItem {
            get { return listItem; }
            set { listItem = value; }
        }

        private OLVListItem listItem;

        /// <summary>
        /// Get/set the listview for which the drawing is to be done
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObjectListView ListView {
            get { return objectListView; }
            set { objectListView = value; }
        }

        private ObjectListView objectListView;

        /// <summary>
        /// Get the specialized OLVSubItem that this renderer is drawing
        /// </summary>
        /// <remarks>This returns null for column 0.</remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OLVListSubItem OLVSubItem {
            get { return listSubItem as OLVListSubItem; }
        }

        /// <summary>
        /// Get or set the model object that this renderer should draw
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Object RowObject {
            get { return rowObject; }
            set { rowObject = value; }
        }

        private Object rowObject;

        /// <summary>
        /// Get or set the list subitem that this renderer will be drawing
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OLVListSubItem SubItem {
            get { return listSubItem; }
            set { listSubItem = value; }
        }

        private OLVListSubItem listSubItem;

        /// <summary>
        /// The brush that will be used to paint the text
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Brush TextBrush {
            get {
                if (textBrush == null)
                    return new SolidBrush(this.GetForegroundColor());
                else
                    return this.textBrush;
            }
            set { textBrush = value; }
        }

        private Brush textBrush;

        /// <summary>
        /// Will this renderer use the custom images from the parent ObjectListView
        /// to draw the checkbox images.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this is true, the renderer will use the images from the 
        /// StateImageList to represent checkboxes. 0 - unchecked, 1 - checked, 2 - indeterminate.
        /// </para>
        /// <para>If this is false (the default), then the renderer will use .NET's standard
        /// CheckBoxRenderer.</para>
        /// </remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseCustomCheckboxImages {
            get { return useCustomCheckboxImages; }
            set { useCustomCheckboxImages = value; }
        }

        private bool useCustomCheckboxImages;

        private void ClearState() {
            this.Event = null;
            this.DrawItemEvent = null;
            this.Aspect = null;
            this.Font = null;
            this.TextBrush = null;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Align the second rectangle with the first rectangle,
        /// according to the alignment of the column
        /// </summary>
        /// <param name="outer">The cell's bounds</param>
        /// <param name="inner">The rectangle to be aligned within the bounds</param>
        /// <returns>An aligned rectangle</returns>
        protected virtual Rectangle AlignRectangle(Rectangle outer, Rectangle inner) {
            Rectangle r = new Rectangle(outer.Location, inner.Size);

            // Align horizontally depending on the column alignment
            if (inner.Width < outer.Width) {
                r.X = AlignHorizontally(outer, inner);
            }

            // Align vertically too
            if (inner.Height < outer.Height) {
                r.Y = AlignVertically(outer, inner);
            }

            return r;
        }

        /// <summary>
        /// Calculate the left edge of the rectangle that aligns the outer rectangle with the inner one 
        /// according to this renderer's horizontal alignment
        /// </summary>
        /// <param name="outer"></param>
        /// <param name="inner"></param>
        /// <returns></returns>
        protected int AlignHorizontally(Rectangle outer, Rectangle inner) {
            HorizontalAlignment alignment = this.CellHorizontalAlignment;
            switch (alignment) {
                case HorizontalAlignment.Left:
                    return outer.Left + 1;
                case HorizontalAlignment.Center:
                    return outer.Left + ((outer.Width - inner.Width) / 2);
                case HorizontalAlignment.Right:
                    return outer.Right - inner.Width - 1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// Calculate the top of the rectangle that aligns the outer rectangle with the inner rectangle
        /// according to this renders vertical alignment
        /// </summary>
        /// <param name="outer"></param>
        /// <param name="inner"></param>
        /// <returns></returns>
        protected int AlignVertically(Rectangle outer, Rectangle inner) {
            return AlignVertically(outer, inner.Height);
        }

        /// <summary>
        /// Calculate the top of the rectangle that aligns the outer rectangle with a rectangle of the given height
        /// according to this renderer's vertical alignment
        /// </summary>
        /// <param name="outer"></param>
        /// <param name="innerHeight"></param>
        /// <returns></returns>
        protected int AlignVertically(Rectangle outer, int innerHeight) {
            switch (this.EffectiveCellVerticalAlignment) {
                case StringAlignment.Near:
                    return outer.Top + 1;
                case StringAlignment.Center:
                    return outer.Top + ((outer.Height - innerHeight) / 2);
                case StringAlignment.Far:
                    return outer.Bottom - innerHeight - 1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Calculate the space that our rendering will occupy and then align that space
        /// with the given rectangle, according to the Column alignment
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r">Pre-padded bounds of the cell</param>
        /// <returns></returns>
        protected virtual Rectangle CalculateAlignedRectangle(Graphics g, Rectangle r) {
            if (this.Column == null)
                return r;

            Rectangle contentRectangle = new Rectangle(Point.Empty, this.CalculateContentSize(g, r));
            return this.AlignRectangle(r, contentRectangle);
        }

        /// <summary>
        /// Calculate the size of the content of this cell.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r">Pre-padded bounds of the cell</param>
        /// <returns>The width and height of the content</returns>
        protected virtual Size CalculateContentSize(Graphics g, Rectangle r)
        {
            Size checkBoxSize = this.CalculatePrimaryCheckBoxSize(g);
            Size imageSize = this.CalculateImageSize(g, this.GetImageSelector());
            Size textSize = this.CalculateTextSize(g, this.GetText(), r.Width - (checkBoxSize.Width + imageSize.Width));

            // If the combined width is greater than the whole cell,  we just use the cell itself

            int width = Math.Min(r.Width, checkBoxSize.Width + imageSize.Width + textSize.Width);
            int componentMaxHeight = Math.Max(checkBoxSize.Height, Math.Max(imageSize.Height, textSize.Height));
            int height = Math.Min(r.Height, componentMaxHeight);

            return new Size(width, height);
        }

        /// <summary>
        /// Calculate the bounds of a checkbox given the (pre-padded) cell bounds
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cellBounds">Pre-padded cell bounds</param>
        /// <returns></returns>
        protected Rectangle CalculateCheckBoxBounds(Graphics g, Rectangle cellBounds) {
            Size checkBoxSize = this.CalculateCheckBoxSize(g);
            return this.AlignRectangle(cellBounds, new Rectangle(0, 0, checkBoxSize.Width, checkBoxSize.Height));
        }
        
        
        /// <summary>
        /// How much space will the check box for this cell occupy?
        /// </summary>
        /// <remarks>Only column 0 can have check boxes. Sub item checkboxes are
        /// treated as images</remarks>
        /// <param name="g"></param>
        /// <returns></returns>
        protected virtual Size CalculateCheckBoxSize(Graphics g)
        {
            if (UseCustomCheckboxImages && this.ListView.StateImageList != null)
                return this.ListView.StateImageList.ImageSize;

            return CheckBoxRenderer.GetGlyphSize(g, CheckBoxState.UncheckedNormal);
        }

        /// <summary>
        /// How much space will the check box for this row occupy? 
        /// If the list doesn't have checkboxes, or this isn't the primary column,
        /// this returns an empty size.
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        protected virtual Size CalculatePrimaryCheckBoxSize(Graphics g) {
            if (!this.ListView.CheckBoxes || !this.ColumnIsPrimary)
                return Size.Empty;
            
            Size size = this.CalculateCheckBoxSize(g);
            size.Width += 6;
            return size;
        }

        /// <summary>
        /// How much horizontal space will the image of this cell occupy?
        /// </summary>
        /// <param name="g"></param>
        /// <param name="imageSelector"></param>
        /// <returns></returns>
        protected virtual int CalculateImageWidth(Graphics g, object imageSelector)
        {
            return this.CalculateImageSize(g, imageSelector).Width + 2;
        }

        /// <summary>
        /// How much vertical space will the image of this cell occupy?
        /// </summary>
        /// <param name="g"></param>
        /// <param name="imageSelector"></param>
        /// <returns></returns>
        protected virtual int CalculateImageHeight(Graphics g, object imageSelector)
        {
            return this.CalculateImageSize(g, imageSelector).Height;
        }

        /// <summary>
        /// How much space will the image of this cell occupy?
        /// </summary>
        /// <param name="g"></param>
        /// <param name="imageSelector"></param>
        /// <returns></returns>
        protected virtual Size CalculateImageSize(Graphics g, object imageSelector)
        {
            if (imageSelector == null || imageSelector == DBNull.Value)
                return Size.Empty;

            // Check for the image in the image list (most common case)
            ImageList il = this.ImageListOrDefault;
            if (il != null)
            {
                int selectorAsInt = -1;

                if (imageSelector is Int32)
                    selectorAsInt = (Int32)imageSelector;
                else
                {
                    String selectorAsString = imageSelector as String;
                    if (selectorAsString != null)
                        selectorAsInt = il.Images.IndexOfKey(selectorAsString);
                }
                if (selectorAsInt >= 0)
                    return il.ImageSize;
            }

            // Is the selector actually an image?
            Image image = imageSelector as Image;
            if (image != null)
                return image.Size;

            return Size.Empty;
        }

        /// <summary>
        /// How much horizontal space will the text of this cell occupy?
        /// </summary>
        /// <param name="g"></param>
        /// <param name="txt"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        protected virtual int CalculateTextWidth(Graphics g, string txt, int width)
        {
            if (String.IsNullOrEmpty(txt))
                return 0;

            return CalculateTextSize(g, txt, width).Width;
        }

        /// <summary>
        /// How much space will the text of this cell occupy?
        /// </summary>
        /// <param name="g"></param>
        /// <param name="txt"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        protected virtual Size CalculateTextSize(Graphics g, string txt, int width)
        {
            if (String.IsNullOrEmpty(txt))
                return Size.Empty;

            if (this.UseGdiTextRendering)
            {
                Size proposedSize = new Size(width, Int32.MaxValue);
                return TextRenderer.MeasureText(g, txt, this.Font, proposedSize, NormalTextFormatFlags);
            }
            
            // Using GDI+ renderering
            using (StringFormat fmt = new StringFormat()) {
                fmt.Trimming = StringTrimming.EllipsisCharacter;
                SizeF sizeF = g.MeasureString(txt, this.Font, width, fmt);
                return new Size(1 + (int)sizeF.Width, 1 + (int)sizeF.Height);
            }
        }

        /// <summary>
        /// Return the Color that is the background color for this item's cell
        /// </summary>
        /// <returns>The background color of the subitem</returns>
        public virtual Color GetBackgroundColor() {
            if (!this.ListView.Enabled)
                return SystemColors.Control;

            if (this.IsItemSelected && !this.ListView.UseTranslucentSelection && this.ListView.FullRowSelect)
                return this.GetSelectedBackgroundColor();

            if (this.SubItem == null || this.ListItem.UseItemStyleForSubItems)
                return this.ListItem.BackColor;

            return this.SubItem.BackColor;
        }

        /// <summary>
        /// Return the color of the background color when the item is selected
        /// </summary>
        /// <returns>The background color of the subitem</returns>
        public virtual Color GetSelectedBackgroundColor() {
            if (this.ListView.Focused) 
                return this.ListItem.SelectedBackColor ?? this.ListView.SelectedBackColorOrDefault;

            if (!this.ListView.HideSelection)
                return this.ListView.UnfocusedSelectedBackColorOrDefault;

            return this.ListItem.BackColor;
        }

        /// <summary>
        /// Return the color to be used for text in this cell
        /// </summary>
        /// <returns>The text color of the subitem</returns>
        public virtual Color GetForegroundColor() {
            if (this.IsItemSelected && 
                !this.ListView.UseTranslucentSelection &&
                (this.ColumnIsPrimary || this.ListView.FullRowSelect)) 
                return this.GetSelectedForegroundColor();

            return this.SubItem == null || this.ListItem.UseItemStyleForSubItems ? this.ListItem.ForeColor : this.SubItem.ForeColor;
        }

        /// <summary>
        /// Return the color of the foreground color when the item is selected
        /// </summary>
        /// <returns>The foreground color of the subitem</returns>
        public virtual Color GetSelectedForegroundColor()
        {
            if (this.ListView.Focused)
                return this.ListItem.SelectedForeColor ?? this.ListView.SelectedForeColorOrDefault;

            if (!this.ListView.HideSelection)
                return this.ListView.UnfocusedSelectedForeColorOrDefault;

            return this.SubItem == null || this.ListItem.UseItemStyleForSubItems ? this.ListItem.ForeColor : this.SubItem.ForeColor;
        }

        /// <summary>
        /// Return the image that should be drawn against this subitem
        /// </summary>
        /// <returns>An Image or null if no image should be drawn.</returns>
        protected virtual Image GetImage() {
            return this.GetImage(this.GetImageSelector());
        }

        /// <summary>
        /// Return the actual image that should be drawn when keyed by the given image selector.
        /// An image selector can be: <list type="bullet">
        /// <item><description>an int, giving the index into the image list</description></item>
        /// <item><description>a string, giving the image key into the image list</description></item>
        /// <item><description>an Image, being the image itself</description></item>
        /// </list>
        /// </summary>
        /// <param name="imageSelector">The value that indicates the image to be used</param>
        /// <returns>An Image or null</returns>
        protected virtual Image GetImage(Object imageSelector) {
            if (imageSelector == null || imageSelector == DBNull.Value)
                return null;

            ImageList il = this.ImageListOrDefault;
            if (il != null) {
                if (imageSelector is Int32) {
                    Int32 index = (Int32) imageSelector;
                    if (index < 0 || index >= il.Images.Count)
                        return null;

                    return il.Images[index];
                }

                String str = imageSelector as String;
                if (str != null) {
                    if (il.Images.ContainsKey(str))
                        return il.Images[str];

                    return null;
                }
            }

            return imageSelector as Image;
        }

        /// <summary>
        /// </summary>
        protected virtual Object GetImageSelector() {
            return this.ColumnIsPrimary ? this.ListItem.ImageSelector : this.OLVSubItem.ImageSelector;
        }

        /// <summary>
        /// Return the string that should be drawn within this
        /// </summary>
        /// <returns></returns>
        protected virtual string GetText() {
            return this.SubItem == null ? this.ListItem.Text : this.SubItem.Text;
        }

        /// <summary>
        /// Return the Color that is the background color for this item's text
        /// </summary>
        /// <returns>The background color of the subitem's text</returns>
        [Obsolete("Use GetBackgroundColor() instead")]
        protected virtual Color GetTextBackgroundColor() {
            return Color.Red; // just so it shows up if it is used
        }

        #endregion

        #region IRenderer members

        /// <summary>
        /// Render the whole item in a non-details view.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="g"></param>
        /// <param name="itemBounds"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public override bool RenderItem(DrawListViewItemEventArgs e, Graphics g, Rectangle itemBounds, object model) {
            this.ConfigureItem(e, itemBounds, model);
            return this.OptionalRender(e, g, itemBounds);
        }

        /// <summary>
        /// Prepare this renderer to draw in response to the given event
        /// </summary>
        /// <param name="e"></param>
        /// <param name="itemBounds"></param>
        /// <param name="model"></param>
        /// <remarks>Use this if you want to chain a second renderer within a primary renderer.</remarks>
        public virtual void ConfigureItem(DrawListViewItemEventArgs e, Rectangle itemBounds, object model)
        {
            this.ClearState();

            this.DrawItemEvent = e;
            this.ListItem = (OLVListItem)e.Item;
            this.SubItem = null;
            this.ListView = (ObjectListView)this.ListItem.ListView;
            this.Column = this.ListView.GetColumn(0);
            this.RowObject = model;
            this.Bounds = itemBounds;
            this.IsItemSelected = this.ListItem.Selected && this.ListItem.Enabled;
        }

        /// <summary>
        /// Render one cell
        /// </summary>
        /// <param name="e"></param>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public override bool RenderSubItem(DrawListViewSubItemEventArgs e, Graphics g, Rectangle cellBounds, object model) {
            this.ConfigureSubItem(e, cellBounds, model);
            return this.OptionalRender(e, g, cellBounds);
        }

        /// <summary>
        /// Prepare this renderer to draw in response to the given event
        /// </summary>
        /// <param name="e"></param>
        /// <param name="cellBounds"></param>
        /// <param name="model"></param>
        /// <remarks>Use this if you want to chain a second renderer within a primary renderer.</remarks>
        public virtual void ConfigureSubItem(DrawListViewSubItemEventArgs e, Rectangle cellBounds, object model) {
            this.ClearState();

            this.Event = e;
            this.ListItem = (OLVListItem)e.Item;
            this.SubItem = (OLVListSubItem)e.SubItem;
            this.ListView = (ObjectListView)this.ListItem.ListView;
            this.Column = (OLVColumn)e.Header;
            this.RowObject = model;
            this.Bounds = cellBounds;
            this.IsItemSelected = this.ListItem.Selected && this.ListItem.Enabled;
        }

        /// <summary>
        /// Calculate which part of this cell was hit
        /// </summary>
        /// <param name="hti"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public override void HitTest(OlvListViewHitTestInfo hti, int x, int y) {
            this.ClearState();

            this.ListView = hti.ListView;
            this.ListItem = hti.Item;
            this.SubItem = hti.SubItem;
            this.Column = hti.Column;
            this.RowObject = hti.RowObject;
            this.IsItemSelected = this.ListItem.Selected && this.ListItem.Enabled;
            if (this.SubItem == null)
                this.Bounds = this.ListItem.Bounds;
            else
                this.Bounds = this.ListItem.GetSubItemBounds(this.Column.Index);

            using (Graphics g = this.ListView.CreateGraphics()) {
                this.HandleHitTest(g, hti, x, y);
            }
        }

        /// <summary>
        /// Calculate the edit rectangle
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="preferredSize"> </param>
        /// <returns></returns>
        public override Rectangle GetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex, Size preferredSize) {
            this.ClearState();

            this.ListView = (ObjectListView) item.ListView;
            this.ListItem = item;
            this.SubItem = item.GetSubItem(subItemIndex);
            this.Column = this.ListView.GetColumn(subItemIndex);
            this.RowObject = item.RowObject;
            this.IsItemSelected = this.ListItem.Selected && this.ListItem.Enabled;
            this.Bounds = cellBounds;

            return this.HandleGetEditRectangle(g, cellBounds, item, subItemIndex, preferredSize);
        }

        #endregion

        #region IRenderer implementation

        // Subclasses will probably want to override these methods rather than the IRenderer
        // interface methods.

        /// <summary>
        /// Draw our data into the given rectangle using the given graphics context.
        /// </summary>
        /// <remarks>
        /// <para>Subclasses should override this method.</para></remarks>
        /// <param name="g">The graphics context that should be used for drawing</param>
        /// <param name="r">The bounds of the subitem cell</param>
        /// <returns>Returns whether the rendering has already taken place.
        /// If this returns false, the default processing will take over.
        /// </returns>
        public virtual bool OptionalRender(DrawListViewItemEventArgs e, Graphics g, Rectangle r) {
            if (this.ListView.View != View.Details)
                return false;

            this.Render(e, g, r);
            return true;
        }

        /// <summary>
        /// Draw our data into the given rectangle using the given graphics context.
        /// </summary>
        /// <remarks>
        /// <para>Subclasses should override this method.</para></remarks>
        /// <param name="g">The graphics context that should be used for drawing</param>
        /// <param name="r">The bounds of the subitem cell</param>
        /// <returns>Returns whether the rendering has already taken place.
        /// If this returns false, the default processing will take over.
        /// </returns>
        public virtual bool OptionalRender(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            if (this.ListView.View != View.Details)
                return false;

            this.Render(e, g, r);
            return true;
        }

        /// <summary>
        /// Draw our data into the given rectangle using the given graphics context.
        /// </summary>
        /// <remarks>
        /// <para>Subclasses should override this method.</para></remarks>
        /// <param name="g">The graphics context that should be used for drawing</param>
        /// <param name="r">The bounds of the subitem cell</param>
        /// <returns>Returns whether the rendering has already taken place.
        /// If this returns false, the default processing will take over.
        /// </returns>
        public virtual bool OptionalSubitemRender(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            if (this.ListView.View != View.Details)
                return false;

            this.Render(e, g, r);
            return true;
        }

        /// <summary>
        /// Draw our data into the given rectangle using the given graphics context.
        /// </summary>
        /// <remarks>
        /// <para>Subclasses should override this method if they never want
        /// to fall back on the default processing</para></remarks>
        /// <param name="g">The graphics context that should be used for drawing</param>
        /// <param name="r">The bounds of the subitem cell</param>
        public virtual void Render(DrawListViewItemEventArgs e, Graphics g, Rectangle r) {
            this.StandardRender(g, r);
        }

        /// <summary>
        /// Draw our data into the given rectangle using the given graphics context.
        /// </summary>
        /// <remarks>
        /// <para>Subclasses should override this method if they never want
        /// to fall back on the default processing</para></remarks>
        /// <param name="g">The graphics context that should be used for drawing</param>
        /// <param name="r">The bounds of the subitem cell</param>
        public virtual void Render(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            this.StandardRender(g, r);
        }

        /// <summary>
        /// Do the actual work of hit testing. Subclasses should override this rather than HitTest()
        /// </summary>
        /// <param name="g"></param>
        /// <param name="hti"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected virtual void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y) {
            Rectangle r = this.CalculateAlignedRectangle(g, ApplyCellPadding(this.Bounds));
            this.StandardHitTest(g, hti, r, x, y);
        }

        /// <summary>
        /// Handle a HitTest request after all state information has been initialized
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="item"></param>
        /// <param name="subItemIndex"></param>
        /// <param name="preferredSize"> </param>
        /// <returns></returns>
        protected virtual Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex, Size preferredSize) {
            // MAINTAINER NOTE: This type testing is wrong (design-wise). The base class should return cell bounds,
            // and a more specialized class should return StandardGetEditRectangle(). But BaseRenderer is used directly
            // to draw most normal cells, as well as being directly subclassed for user implemented renderers. And this
            // method needs to return different bounds in each of those cases. We should have a StandardRenderer and make
            // BaseRenderer into an ABC -- but that would break too much existing code. And so we have this hack :(

            // If we are a standard renderer, return the position of the text, otherwise, use the whole cell.
            if (this.GetType() == typeof (BaseRenderer))
                return this.StandardGetEditRectangle(g, cellBounds, preferredSize);

            // Center the editor vertically
            if (cellBounds.Height != preferredSize.Height)
                cellBounds.Y += (cellBounds.Height - preferredSize.Height) / 2;

            return cellBounds;
        }

        #endregion

        #region Standard IRenderer implementations

        /// <summary>
        /// Draw the standard "[checkbox] [image] [text]" cell after the state properties have been initialized.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        protected void StandardRender(Graphics g, Rectangle r) {
            this.DrawBackground(g, r);

            // Adjust the first columns rectangle to match the padding used by the native mode of the ListView
            if (this.ColumnIsPrimary && this.CellHorizontalAlignment == HorizontalAlignment.Left ) {
                r.X += 3;
                r.Width -= 1;
            }
            r = this.ApplyCellPadding(r);
            this.DrawAlignedImageAndText(g, r);

            // Show where the bounds of the cell padding are (debugging)
            if (ObjectListView.ShowCellPaddingBounds)
                g.DrawRectangle(Pens.Purple, r);
        }

        /// <summary>
        /// Change the bounds of the given rectangle to take any cell padding into account
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public virtual Rectangle ApplyCellPadding(Rectangle r) {
            Rectangle? padding = this.EffectiveCellPadding;
            if (!padding.HasValue)
                return r;
            // The two subtractions below look wrong, but are correct!
            Rectangle paddingRectangle = padding.Value;
            r.Width -= paddingRectangle.Right;
            r.Height -= paddingRectangle.Bottom;
            r.Offset(paddingRectangle.Location);
            return r;
        }

        /// <summary>
        /// Perform normal hit testing relative to the given aligned content bounds
        /// </summary>
        /// <param name="g"></param>
        /// <param name="hti"></param>
        /// <param name="bounds"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected virtual void StandardHitTest(Graphics g, OlvListViewHitTestInfo hti, Rectangle alignedContentRectangle, int x, int y) {
            Rectangle r = alignedContentRectangle;

            // Match tweaking from renderer
            if (this.ColumnIsPrimary && this.CellHorizontalAlignment == HorizontalAlignment.Left && !(this is TreeListView.TreeRenderer)) {
                r.X += 3;
                r.Width -= 1;
            }
            int width = 0;

            // Did they hit a check box on the primary column?
            if (this.ColumnIsPrimary && this.ListView.CheckBoxes) {
                Size checkBoxSize = this.CalculateCheckBoxSize(g);
                int checkBoxTop = this.AlignVertically(r, checkBoxSize.Height);
                Rectangle r3 = new Rectangle(r.X, checkBoxTop, checkBoxSize.Width, checkBoxSize.Height);
                width = r3.Width + 6;
                // g.DrawRectangle(Pens.DarkGreen, r3);
                if (r3.Contains(x, y)) {
                    hti.HitTestLocation = HitTestLocation.CheckBox;
                    return;
                }
            }

            // Did they hit the image? If they hit the image of a 
            // non-primary column that has a checkbox, it counts as a 
            // checkbox hit
            r.X += width;
            r.Width -= width;
            width = this.CalculateImageWidth(g, this.GetImageSelector());
            Rectangle rTwo = r;
            rTwo.Width = width;
            // g.DrawRectangle(Pens.Red, rTwo);
            if (rTwo.Contains(x, y)) {
                if (this.Column != null && (this.Column.Index > 0 && this.Column.CheckBoxes))
                    hti.HitTestLocation = HitTestLocation.CheckBox;
                else
                    hti.HitTestLocation = HitTestLocation.Image;
                return;
            }

            // Did they hit the text?
            r.X += width;
            r.Width -= width;
            width = this.CalculateTextWidth(g, this.GetText(), r.Width);
            rTwo = r;
            rTwo.Width = width;
            // g.DrawRectangle(Pens.Blue, rTwo);
            if (rTwo.Contains(x, y)) {
                hti.HitTestLocation = HitTestLocation.Text;
                return;
            }

            hti.HitTestLocation = HitTestLocation.InCell;
        }

        /// <summary>
        /// This method calculates the bounds of the text within a standard layout
        /// (i.e. optional checkbox, optional image, text)
        /// </summary>
        /// <remarks>This method only works correctly if the state of the renderer
        /// has been fully initialized (see BaseRenderer.GetEditRectangle)</remarks>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="preferredSize"> </param>
        /// <returns></returns>
        protected virtual Rectangle StandardGetEditRectangle(Graphics g, Rectangle cellBounds, Size preferredSize) {

            Size contentSize = this.CalculateContentSize(g, cellBounds);
            int contentWidth = this.Column.CellEditUseWholeCellEffective ? cellBounds.Width : contentSize.Width;
            Rectangle editControlBounds = this.CalculatePaddedAlignedBounds(g, cellBounds, new Size(contentWidth, preferredSize.Height));

            Size checkBoxSize = this.CalculatePrimaryCheckBoxSize(g);
            int imageWidth = this.CalculateImageWidth(g, this.GetImageSelector());

            int width = checkBoxSize.Width + imageWidth;

            // Indent the primary column by the required amount
            if (this.ListItem.IndentCount > 0) {
                int indentWidth = this.ListView.SmallImageSize.Width * this.ListItem.IndentCount;
                width += indentWidth;
            }

            editControlBounds.X += width;
            editControlBounds.Width -= width;

            if (editControlBounds.Width < 50)
                editControlBounds.Width = 50;
            if (editControlBounds.Right > cellBounds.Right)
                editControlBounds.Width = cellBounds.Right - editControlBounds.Left;

            return editControlBounds;
        }

        /// <summary>
        /// Apply any padding to the given bounds, and then align a rectangle of the given
        /// size within that padded area.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="cellBounds"></param>
        /// <param name="preferredSize"></param>
        /// <returns></returns>
        protected Rectangle CalculatePaddedAlignedBounds(Graphics g, Rectangle cellBounds, Size preferredSize) {
            Rectangle r = ApplyCellPadding(cellBounds);
            r = this.AlignRectangle(r, new Rectangle(Point.Empty, preferredSize));
            return r;
        }

        #endregion

        #region Drawing routines

        /// <summary>
        /// Draw the given image aligned horizontally within the column.
        /// </summary>
        /// <remarks>
        /// Over tall images are scaled to fit. Over-wide images are
        /// truncated. This is by design!
        /// </remarks>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">Bounds of the cell</param>
        /// <param name="image">The image to be drawn</param>
        protected virtual void DrawAlignedImage(Graphics g, Rectangle r, Image image) {
            if (image == null)
                return;

            // By default, the image goes in the top left of the rectangle
            Rectangle imageBounds = new Rectangle(r.Location, image.Size);

            // If the image is too tall to be drawn in the space provided, proportionally scale it down.
            // Too wide images are not scaled.
            if (image.Height > r.Height) {
                float scaleRatio = (float) r.Height / (float) image.Height;
                imageBounds.Width = (int) ((float) image.Width * scaleRatio);
                imageBounds.Height = r.Height - 1;
            }

            // Align and draw our (possibly scaled) image
            Rectangle alignRectangle = this.AlignRectangle(r, imageBounds);
            if (this.ListItem.Enabled)
                g.DrawImage(image, alignRectangle);
            else
                ControlPaint.DrawImageDisabled(g, image, alignRectangle.X, alignRectangle.Y, GetBackgroundColor());
        }

        /// <summary>
        /// Draw our subitems image and text
        /// </summary>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">Pre-padded bounds of the cell</param>
        protected virtual void DrawAlignedImageAndText(Graphics g, Rectangle r) {
            this.DrawImageAndText(g, this.CalculateAlignedRectangle(g, r));
        }

        /// <summary>
        /// Fill in the background of this cell
        /// </summary>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">Bounds of the cell</param>
        protected virtual void DrawBackground(Graphics g, Rectangle r) {
            if (!this.IsDrawBackground)
                return;

            Color backgroundColor = this.GetBackgroundColor();

            using (Brush brush = new SolidBrush(backgroundColor)) {
                g.FillRectangle(brush, r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2);
            }
        }

        /// <summary>
        /// Draw the primary check box of this row (checkboxes in other sub items use a different method)
        /// </summary>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">The pre-aligned and padded target rectangle</param>
        protected virtual int DrawCheckBox(Graphics g, Rectangle r) {
            // The odd constants are to match checkbox placement in native mode (on XP at least)
            // TODO: Unify this with CheckStateRenderer

            // The rectangle r is already horizontally aligned. We still need to align it vertically.
            Size checkBoxSize = this.CalculateCheckBoxSize(g);
            Point checkBoxLocation = new Point(r.X, this.AlignVertically(r, checkBoxSize.Height));

            if (this.IsPrinting || this.UseCustomCheckboxImages) {
                int imageIndex = this.ListItem.StateImageIndex;
                if (this.ListView.StateImageList == null || imageIndex < 0 || imageIndex >= this.ListView.StateImageList.Images.Count)
                    return 0;

                return this.DrawImage(g, new Rectangle(checkBoxLocation, checkBoxSize), this.ListView.StateImageList.Images[imageIndex]) + 4;
            }

            CheckBoxState boxState = this.GetCheckBoxState(this.ListItem.CheckState);
            CheckBoxRenderer.DrawCheckBox(g, checkBoxLocation, boxState);
            return checkBoxSize.Width;
        }

        /// <summary>
        /// Calculate the CheckBoxState we need to correctly draw the given state
        /// </summary>
        /// <param name="checkState"></param>
        /// <returns></returns>
        protected virtual CheckBoxState GetCheckBoxState(CheckState checkState) {

            // Should the checkbox be drawn as disabled?
            if (this.IsCheckBoxDisabled) {
                switch (checkState) {
                    case CheckState.Checked:
                        return CheckBoxState.CheckedDisabled;
                    case CheckState.Unchecked:
                        return CheckBoxState.UncheckedDisabled;
                    default:
                        return CheckBoxState.MixedDisabled;
                }
            }

            // Is the cursor currently over this checkbox?
            if (this.IsCheckboxHot) {
                switch (checkState) {
                    case CheckState.Checked:
                        return CheckBoxState.CheckedHot;
                    case CheckState.Unchecked:
                        return CheckBoxState.UncheckedHot;
                    default:
                        return CheckBoxState.MixedHot;
                }
            }

            // Not hot and not disabled -- just draw it normally
            switch (checkState) {
                case CheckState.Checked:
                    return CheckBoxState.CheckedNormal;
                case CheckState.Unchecked:
                    return CheckBoxState.UncheckedNormal;
                default:
                    return CheckBoxState.MixedNormal;
            }

        }

        /// <summary>
        /// Should this checkbox be drawn as disabled?
        /// </summary>
        protected virtual bool IsCheckBoxDisabled {
            get {
                if (this.ListItem != null && !this.ListItem.Enabled)
                    return true;

                if (!this.ListView.RenderNonEditableCheckboxesAsDisabled)
                    return false;

                return (this.ListView.CellEditActivation == ObjectListView.CellEditActivateMode.None ||
                        (this.Column != null && !this.Column.IsEditable));
            }
        }

        /// <summary>
        /// Is the current item hot (i.e. under the mouse)?
        /// </summary>
        protected bool IsCellHot {
            get {
                return this.ListView != null &&
                       this.ListView.HotRowIndex == this.ListItem.Index &&
                       this.ListView.HotColumnIndex == (this.Column == null ? 0 : this.Column.Index);
            }
        }

        /// <summary>
        /// Is the mouse over a checkbox in this cell?
        /// </summary>
        protected bool IsCheckboxHot {
            get {
                return this.IsCellHot && this.ListView.HotCellHitLocation == HitTestLocation.CheckBox;
            }
        }

        /// <summary>
        /// Draw the given text and optional image in the "normal" fashion
        /// </summary>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">Bounds of the cell</param>
        /// <param name="imageSelector">The optional image to be drawn</param>
        protected virtual int DrawImage(Graphics g, Rectangle r, Object imageSelector) {
            if (imageSelector == null || imageSelector == DBNull.Value)
                return 0;

			Image image = null;
			
			if(imageSelector is Icon icon)
			{
				// convert icon to Image
				image = icon.ToBitmap();
			}
			else
			{

				// Draw from the image list (most common case)
			ImageList il = this.ImageListOrDefault;
			if (il != null) {

                // Try to translate our imageSelector into a valid ImageList index
                int selectorAsInt = -1;
                if (imageSelector is Int32) {
                    selectorAsInt = (Int32) imageSelector;
                    if (selectorAsInt >= il.Images.Count)
                        selectorAsInt = -1;
                } else {
                    String selectorAsString = imageSelector as String;
                    if (selectorAsString != null)
                        selectorAsInt = il.Images.IndexOfKey(selectorAsString);
                }

                // If we found a valid index into the ImageList, draw it.
                // We want to draw using the native DrawImageList calls, since that let's us do some nice effects
                // But the native call does not work on PrinterDCs, so if we're printing we have to skip this bit.
                if (selectorAsInt >= 0) {
                    if (!this.IsPrinting) {
                         if (il.ImageSize.Height < r.Height)
						{
							r.Y = this.AlignVertically(r, new Rectangle(Point.Empty, il.ImageSize));
						}

                        // If we are not printing, it's probable that the given Graphics object is double buffered using a BufferedGraphics object.
                        // But the ImageList.Draw method doesn't honor the Translation matrix that's probably in effect on the buffered
                        // graphics. So we have to calculate our drawing rectangle, relative to the cells natural boundaries.
                        // This effectively simulates the Translation matrix.

                        // Rectangle r2 = new Rectangle(r.X - this.Bounds.X, r.Y - this.Bounds.Y, r.Width, r.Height);
                        Rectangle r2 = new Rectangle(r.X, r.Y - this.Bounds.Y, r.Width, r.Height);
                        NativeMethods.DrawImageList(g, il, selectorAsInt, r2.X, r2.Y, this.IsItemSelected, !this.ListItem.Enabled);
                        return il.ImageSize.Width;
                    }

                    // For some reason, printing from an image list doesn't work onto a printer context
                    // So get the image from the list and FALL THROUGH to the "print an image" case
                    imageSelector = il.Images[selectorAsInt];
                }
            }
			}

			// Is the selector actually an image?
			if (image is null)
			{
				image = imageSelector as Image;
			}

			if (image == null)
                return 0; // no, give up

            if (image.Size.Height < r.Height)
                r.Y = this.AlignVertically(r, new Rectangle(Point.Empty, image.Size));

            if (this.ListItem.Enabled)
                g.DrawImageUnscaled(image, r.X, r.Y);
            else
                ControlPaint.DrawImageDisabled(g, image, r.X, r.Y, GetBackgroundColor());

            return image.Width;
        }

        /// <summary>
        /// Draw our subitems image and text
        /// </summary>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">Bounds of the cell</param>
        protected virtual void DrawImageAndText(Graphics g, Rectangle r) {
            int offset = 0;
            if (this.ListView.CheckBoxes && this.ColumnIsPrimary) {
                offset = this.DrawCheckBox(g, r) + 6;
                r.X += offset;
                r.Width -= offset;
            }

            offset = this.DrawImage(g, r, this.GetImageSelector());
            r.X += offset;
            r.Width -= offset;

            this.DrawText(g, r, this.GetText());
        }

        /// <summary>
        /// Draw the given collection of image selectors
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="imageSelectors"></param>
        protected virtual int DrawImages(Graphics g, Rectangle r, ICollection imageSelectors) {
            // Collect the non-null images
            List<Image> images = new List<Image>();
            foreach (Object selector in imageSelectors) {
                Image image = this.GetImage(selector);
                if (image != null)
                    images.Add(image);
            }

            // Figure out how much space they will occupy
            int width = 0;
            int height = 0;
            foreach (Image image in images) {
                width += (image.Width + this.Spacing);
                height = Math.Max(height, image.Height);
            }

            // Align the collection of images within the cell
            Rectangle r2 = this.AlignRectangle(r, new Rectangle(0, 0, width, height));

            // Finally, draw all the images in their correct location
            Color backgroundColor = GetBackgroundColor();
            Point pt = r2.Location;
            foreach (Image image in images) {
                if (this.ListItem.Enabled)
                    g.DrawImage(image, pt);
                else
                    ControlPaint.DrawImageDisabled(g, image, pt.X, pt.Y, backgroundColor);
                pt.X += (image.Width + this.Spacing);
            }

            // Return the width that the images occupy
            return width;
        }

        /// <summary>
        /// Draw the given text and optional image in the "normal" fashion
        /// </summary>
        /// <param name="g">Graphics context to use for drawing</param>
        /// <param name="r">Bounds of the cell</param>
        /// <param name="txt">The string to be drawn</param>
        public virtual void DrawText(Graphics g, Rectangle r, String txt) {
            if (String.IsNullOrEmpty(txt))
                return;

            if (this.UseGdiTextRendering)
                this.DrawTextGdi(g, r, txt);
            else
                this.DrawTextGdiPlus(g, r, txt);
        }

        /// <summary>
        /// Print the given text in the given rectangle using only GDI routines
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        /// <param name="txt"></param>
        /// <remarks>
        /// The native list control uses GDI routines to do its drawing, so using them
        /// here makes the owner drawn mode looks more natural.
        /// <para>This method doesn't honour the CanWrap setting on the renderer. All
        /// text is single line</para>
        /// </remarks>
        protected virtual void DrawTextGdi(Graphics g, Rectangle r, String txt) {
            Color backColor = Color.Transparent;
            if (this.IsDrawBackground && this.IsItemSelected && ColumnIsPrimary && !this.ListView.FullRowSelect)
                backColor = this.GetSelectedBackgroundColor();

            TextFormatFlags flags = NormalTextFormatFlags | this.CellVerticalAlignmentAsTextFormatFlag;

            // I think there is a bug in the TextRenderer. Setting or not setting SingleLine doesn't make 
            // any difference -- it is always single line.
            if (!this.CanWrap)
                flags |= TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, txt, this.Font, r, this.GetForegroundColor(), backColor, flags);
        }

        private bool ColumnIsPrimary {
            get { return this.Column != null && this.Column.Index == 0; }
        }

        /// <summary>
        /// Gets the cell's vertical alignment as a TextFormatFlag
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected TextFormatFlags CellVerticalAlignmentAsTextFormatFlag {
            get {
                switch (this.EffectiveCellVerticalAlignment) {
                    case StringAlignment.Near:
                        return TextFormatFlags.Top;
                    case StringAlignment.Center:
                        return TextFormatFlags.VerticalCenter;
                    case StringAlignment.Far:
                        return TextFormatFlags.Bottom;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Gets the StringFormat needed when drawing text using GDI+
        /// </summary>
        protected virtual StringFormat StringFormatForGdiPlus {
            get {
                StringFormat fmt = new StringFormat();
                fmt.LineAlignment = this.EffectiveCellVerticalAlignment;
                fmt.Trimming = StringTrimming.EllipsisCharacter;
                fmt.Alignment = this.Column == null ? StringAlignment.Near : this.Column.TextStringAlign;
                if (!this.CanWrap)
                    fmt.FormatFlags = StringFormatFlags.NoWrap;
                return fmt;
            }
        }

        /// <summary>
        /// Print the given text in the given rectangle using normal GDI+ .NET methods
        /// </summary>
        /// <remarks>Printing to a printer dc has to be done using this method.</remarks>
        protected virtual void DrawTextGdiPlus(Graphics g, Rectangle r, String txt) {
            using (StringFormat fmt = this.StringFormatForGdiPlus) {
                // Draw the background of the text as selected, if it's the primary column
                // and it's selected and it's not in FullRowSelect mode.
                Font f = this.Font;
                if (this.IsDrawBackground && this.IsItemSelected && this.ColumnIsPrimary && !this.ListView.FullRowSelect) {
                    SizeF size = g.MeasureString(txt, f, r.Width, fmt);
                    Rectangle r2 = r;
                    r2.Width = (int) size.Width + 1;
                    using (Brush brush = new SolidBrush(this.GetSelectedBackgroundColor())) {
                        g.FillRectangle(brush, r2);
                    }
                }
                RectangleF rf = r;
                g.DrawString(txt, f, this.TextBrush, rf, fmt);
            }

            // We should put a focus rectangle around the column 0 text if it's selected --
            // but we don't because:
            // - I really dislike this UI convention
            // - we are using buffered graphics, so the DrawFocusRecatangle method of the event doesn't work

            //if (this.ColumnIsPrimary) {
            //    Size size = TextRenderer.MeasureText(this.SubItem.Text, this.ListView.ListFont);
            //    if (r.Width > size.Width)
            //        r.Width = size.Width;
            //    this.Event.DrawFocusRectangle(r);
            //}
        }

        #endregion
    }
}
