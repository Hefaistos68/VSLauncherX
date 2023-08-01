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
using System.Globalization;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// A MultiImageRenderer draws the same image a number of times based on our data value
	/// </summary>
	/// <remarks><para>The stars in the Rating column of iTunes is a good example of this type of renderer.</para></remarks>
	public class MultiImageRenderer : BaseRenderer {
        /// <summary>
        /// Make a quiet renderer
        /// </summary>
        public MultiImageRenderer()
            : base() {}

        /// <summary>
        /// Make an image renderer that will draw the indicated image, at most maxImages times.
        /// </summary>
        /// <param name="imageSelector"></param>
        /// <param name="maxImages"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        public MultiImageRenderer(Object imageSelector, int maxImages, int minValue, int maxValue)
            : this() {
            this.ImageSelector = imageSelector;
            this.MaxNumberImages = maxImages;
            this.MinimumValue = minValue;
            this.MaximumValue = maxValue;
        }

        #region Configuration Properties

        /// <summary>
        /// The index of the image that should be drawn
        /// </summary>
        [Category("Behavior"),
         Description("The index of the image that should be drawn"),
         DefaultValue(-1)]
        public int ImageIndex {
            get {
                if (imageSelector is Int32)
                    return (Int32) imageSelector;
                else
                    return -1;
            }
            set { imageSelector = value; }
        }

        /// <summary>
        /// The name of the image that should be drawn
        /// </summary>
        [Category("Behavior"),
         Description("The index of the image that should be drawn"),
         DefaultValue(null)]
        public string ImageName {
            get { return imageSelector as String; }
            set { imageSelector = value; }
        }

        /// <summary>
        /// The image selector that will give the image to be drawn
        /// </summary>
        /// <remarks>Like all image selectors, this can be an int, string or Image</remarks>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Object ImageSelector {
            get { return imageSelector; }
            set { imageSelector = value; }
        }

        private Object imageSelector;

        /// <summary>
        /// What is the maximum number of images that this renderer should draw?
        /// </summary>
        [Category("Behavior"),
         Description("The maximum number of images that this renderer should draw"),
         DefaultValue(10)]
        public int MaxNumberImages {
            get { return maxNumberImages; }
            set { maxNumberImages = value; }
        }

        private int maxNumberImages = 10;

        /// <summary>
        /// Values less than or equal to this will have 0 images drawn
        /// </summary>
        [Category("Behavior"),
         Description("Values less than or equal to this will have 0 images drawn"),
         DefaultValue(0)]
        public int MinimumValue {
            get { return minimumValue; }
            set { minimumValue = value; }
        }

        private int minimumValue = 0;

        /// <summary>
        /// Values greater than or equal to this will have MaxNumberImages images drawn
        /// </summary>
        [Category("Behavior"),
         Description("Values greater than or equal to this will have MaxNumberImages images drawn"),
         DefaultValue(100)]
        public int MaximumValue {
            get { return maximumValue; }
            set { maximumValue = value; }
        }

        private int maximumValue = 100;

        #endregion

        /// <summary>
        /// Draw our data value
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);
            r = this.ApplyCellPadding(r);

            Image image = this.GetImage(this.ImageSelector);
            if (image == null)
                return;

            // Convert our aspect to a numeric value
            IConvertible convertable = this.Aspect as IConvertible;
            if (convertable == null)
                return;
            double aspectValue = convertable.ToDouble(NumberFormatInfo.InvariantInfo);

            // Calculate how many images we need to draw to represent our aspect value
            int numberOfImages;
            if (aspectValue <= this.MinimumValue)
                numberOfImages = 0;
            else if (aspectValue < this.MaximumValue)
                numberOfImages = 1 + (int) (this.MaxNumberImages * (aspectValue - this.MinimumValue) / this.MaximumValue);
            else
                numberOfImages = this.MaxNumberImages;

            // If we need to shrink the image, what will its on-screen dimensions be?
            int imageScaledWidth = image.Width;
            int imageScaledHeight = image.Height;
            if (r.Height < image.Height) {
                imageScaledWidth = (int) ((float) image.Width * (float) r.Height / (float) image.Height);
                imageScaledHeight = r.Height;
            }
            // Calculate where the images should be drawn
            Rectangle imageBounds = r;
            imageBounds.Width = (this.MaxNumberImages * (imageScaledWidth + this.Spacing)) - this.Spacing;
            imageBounds.Height = imageScaledHeight;
            imageBounds = this.AlignRectangle(r, imageBounds);

            // Finally, draw the images
            Rectangle singleImageRect = new Rectangle(imageBounds.X, imageBounds.Y, imageScaledWidth, imageScaledHeight);
            Color backgroundColor = GetBackgroundColor();
            for (int i = 0; i < numberOfImages; i++) {
                if (this.ListItem.Enabled) {
                    this.DrawImage(g, singleImageRect, this.ImageSelector);
                }  else
                    ControlPaint.DrawImageDisabled(g, image, singleImageRect.X, singleImageRect.Y, backgroundColor);
                singleImageRect.X += (imageScaledWidth + this.Spacing);
            }
        }

        /// <summary>
        /// Draw our data value
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);
            r = this.ApplyCellPadding(r);

            Image image = this.GetImage(this.ImageSelector);
            if (image == null)
                return;

            // Convert our aspect to a numeric value
            IConvertible convertable = this.Aspect as IConvertible;
            if (convertable == null)
                return;
            double aspectValue = convertable.ToDouble(NumberFormatInfo.InvariantInfo);

            // Calculate how many images we need to draw to represent our aspect value
            int numberOfImages;
            if (aspectValue <= this.MinimumValue)
                numberOfImages = 0;
            else if (aspectValue < this.MaximumValue)
                numberOfImages = 1 + (int) (this.MaxNumberImages * (aspectValue - this.MinimumValue) / this.MaximumValue);
            else
                numberOfImages = this.MaxNumberImages;

            // If we need to shrink the image, what will its on-screen dimensions be?
            int imageScaledWidth = image.Width;
            int imageScaledHeight = image.Height;
            if (r.Height < image.Height) {
                imageScaledWidth = (int) ((float) image.Width * (float) r.Height / (float) image.Height);
                imageScaledHeight = r.Height;
            }
            // Calculate where the images should be drawn
            Rectangle imageBounds = r;
            imageBounds.Width = (this.MaxNumberImages * (imageScaledWidth + this.Spacing)) - this.Spacing;
            imageBounds.Height = imageScaledHeight;
            imageBounds = this.AlignRectangle(r, imageBounds);

            // Finally, draw the images
            Rectangle singleImageRect = new Rectangle(imageBounds.X, imageBounds.Y, imageScaledWidth, imageScaledHeight);
            Color backgroundColor = GetBackgroundColor();
            for (int i = 0; i < numberOfImages; i++) {
                if (this.ListItem.Enabled) {
                    this.DrawImage(g, singleImageRect, this.ImageSelector);
                }  else
                    ControlPaint.DrawImageDisabled(g, image, singleImageRect.X, singleImageRect.Y, backgroundColor);
                singleImageRect.X += (imageScaledWidth + this.Spacing);
            }
        }
    }
}
