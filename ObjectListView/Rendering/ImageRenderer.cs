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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using Timer = System.Threading.Timer;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// Render an image that comes from our data source.
	/// </summary>
	/// <remarks>The image can be sourced from:
	/// <list type="bullet">
	/// <item><description>a byte-array (normally when the image to be shown is
	/// stored as a value in a database)</description></item>
	/// <item><description>an int, which is treated as an index into the image list</description></item>
	/// <item><description>a string, which is treated first as a file name, and failing that as an index into the image list</description></item>
	/// <item><description>an ICollection of ints or strings, which will be drawn as consecutive images</description></item>
	/// </list>
	/// <para>If an image is an animated GIF, it's state is stored in the SubItem object.</para>
	/// <para>By default, the image renderer does not render animations (it begins life with animations paused).
	/// To enable animations, you must call Unpause().</para>
	/// <para>In the current implementation (2009-09), each column showing animated gifs must have a 
	/// different instance of ImageRenderer assigned to it. You cannot share the same instance of
	/// an image renderer between two animated gif columns. If you do, only the last column will be
	/// animated.</para>
	/// </remarks>
	public class ImageRenderer : BaseRenderer {
        /// <summary>
        /// Make an empty image renderer
        /// </summary>
        public ImageRenderer() {
            this.stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Make an empty image renderer that begins life ready for animations
        /// </summary>
        public ImageRenderer(bool startAnimations)
            : this() {
            this.Paused = !startAnimations;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        protected override void Dispose(bool disposing) {
            Paused = true;
            base.Dispose(disposing);
        }

        #region Properties

        /// <summary>
        /// Should the animations in this renderer be paused?
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Paused {
            get { return isPaused; }
            set {
                if (this.isPaused == value)
                    return;

                this.isPaused = value;
                if (this.isPaused) {
                    this.StopTickler();
                    this.stopwatch.Stop();
                } else {
                    this.Tickler.Change(1, Timeout.Infinite);
                    this.stopwatch.Start();
                }
            }
        }

        private bool isPaused = true;

        private void StopTickler() {
            if (this.tickler == null)
                return;

            this.tickler.Dispose();
            this.tickler = null;
        }

        /// <summary>
        /// Gets a timer that can be used to trigger redraws on animations
        /// </summary>
        protected Timer Tickler {
            get {
                if (this.tickler == null)
                    this.tickler = new System.Threading.Timer(new TimerCallback(this.OnTimer), null, Timeout.Infinite, Timeout.Infinite);
                return this.tickler;
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Pause any animations
        /// </summary>
        public void Pause() {
            this.Paused = true;
        }

        /// <summary>
        /// Unpause any animations
        /// </summary>
        public void Unpause() {
            this.Paused = false;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draw our image
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);

            if (this.Aspect == null || this.Aspect == System.DBNull.Value)
                return;
            r = this.ApplyCellPadding(r);

            if (this.Aspect is System.Byte[]) {
                this.DrawAlignedImage(g, r, this.GetImageFromAspect());
            } else {
                ICollection imageSelectors = this.Aspect as ICollection;
                if (imageSelectors == null)
                    this.DrawAlignedImage(g, r, this.GetImageFromAspect());
                else
                    this.DrawImages(g, r, imageSelectors);
            }
        }

        /// <summary>
        /// Draw our image
        /// </summary>
        /// <param name="g"></param>
        /// <param name="r"></param>
        public override void Render(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r) {
            this.DrawBackground(g, r);

            if (this.Aspect == null || this.Aspect == System.DBNull.Value)
                return;
            r = this.ApplyCellPadding(r);

            if (this.Aspect is System.Byte[]) {
                this.DrawAlignedImage(g, r, this.GetImageFromAspect());
            } else {
                ICollection imageSelectors = this.Aspect as ICollection;
                if (imageSelectors == null)
                    this.DrawAlignedImage(g, r, this.GetImageFromAspect());
                else
                    this.DrawImages(g, r, imageSelectors);
            }
        }

        /// <summary>
        /// Translate our Aspect into an image.
        /// </summary>
        /// <remarks>The strategy is:<list type="bullet">
        /// <item><description>If its a byte array, we treat it as an in-memory image</description></item>
        /// <item><description>If it's an int, we use that as an index into our image list</description></item>
        /// <item><description>If it's a string, we try to load a file by that name. If we can't, 
        /// we use the string as an index into our image list.</description></item>
        ///</list></remarks>
        /// <returns>An image</returns>
        protected Image GetImageFromAspect() {
            // If we've already figured out the image, don't do it again
            if (this.OLVSubItem != null && this.OLVSubItem.ImageSelector is Image) {
                if (this.OLVSubItem.AnimationState == null)
                    return (Image) this.OLVSubItem.ImageSelector;
                else
                    return this.OLVSubItem.AnimationState.image;
            }

            // Try to convert our Aspect into an Image
            // If its a byte array, we treat it as an in-memory image
            // If it's an int, we use that as an index into our image list
            // If it's a string, we try to find a file by that name.
            //    If we can't, we use the string as an index into our image list.
            Image image = this.Aspect as Image;
            if (image != null) {
                // Don't do anything else
            } else if (this.Aspect is System.Byte[]) {
                using (MemoryStream stream = new MemoryStream((System.Byte[]) this.Aspect)) {
                    try {
                        image = Image.FromStream(stream);
                    }
                    catch (ArgumentException) {
                        // ignore
                    }
                }
            } else if (this.Aspect is Int32) {
                image = this.GetImage(this.Aspect);
            } else {
                String str = this.Aspect as String;
                if (!String.IsNullOrEmpty(str)) {
                    try {
                        image = Image.FromFile(str);
                    }
                    catch (FileNotFoundException) {
                        image = this.GetImage(this.Aspect);
                    }
                    catch (OutOfMemoryException) {
                        image = this.GetImage(this.Aspect);
                    }
                }
            }

            // If this image is an animation, initialize the animation process
            if (this.OLVSubItem != null && AnimationState.IsAnimation(image)) {
                this.OLVSubItem.AnimationState = new AnimationState(image);
            }

            // Cache the image so we don't repeat this dreary process
            if (this.OLVSubItem != null)
                this.OLVSubItem.ImageSelector = image;

            return image;
        }

        #endregion

        #region Events

        /// <summary>
        /// This is the method that is invoked by the timer. It basically switches control to the listview thread.
        /// </summary>
        /// <param name="state">not used</param>
        public void OnTimer(Object state) {

            if (this.IsListViewDead)
                return;

            if (this.Paused)
                return;

            if (this.ListView.InvokeRequired)
                this.ListView.Invoke((MethodInvoker) delegate { this.OnTimer(state); });
            else
                this.OnTimerInThread();
        }

        private bool IsListViewDead {
            get {
                // Apply a whole heap of sanity checks, which basically ensure that the ListView is still alive
                return this.ListView == null ||
                       this.ListView.Disposing ||
                       this.ListView.IsDisposed ||
                       !this.ListView.IsHandleCreated;
            }
        }

        /// <summary>
        /// This is the OnTimer callback, but invoked in the same thread as the creator of the ListView.
        /// This method can use all of ListViews methods without creating a CrossThread exception.
        /// </summary>
        protected void OnTimerInThread() {
            // MAINTAINER NOTE: This method must renew the tickler. If it doesn't the animations will stop.

            // If this listview has been destroyed, we can't do anything, so we return without
            // renewing the tickler, effectively killing all animations on this renderer

            if (this.IsListViewDead)
                return;

            if (this.Paused)
                return;

            // If we're not in Detail view or our column has been removed from the list,
            // we can't do anything at the moment, but we still renew the tickler because the view may change later.
            if (this.ListView.View != System.Windows.Forms.View.Details || this.Column == null || this.Column.Index < 0) {
                this.Tickler.Change(1000, Timeout.Infinite);
                return;
            }

            long elapsedMilliseconds = this.stopwatch.ElapsedMilliseconds;
            int subItemIndex = this.Column.Index;
            long nextCheckAt = elapsedMilliseconds + 1000; // wait at most one second before checking again
            Rectangle updateRect = new Rectangle(); // what part of the view must be updated to draw the changed gifs?

            // Run through all the subitems in the view for our column, and for each one that
            // has an animation attached to it, see if the frame needs updating.

            for (int i = 0; i < this.ListView.GetItemCount(); i++) {
                OLVListItem lvi = this.ListView.GetItem(i);

                // Get the animation state from the subitem. If there isn't an animation state, skip this row.
                OLVListSubItem lvsi = lvi.GetSubItem(subItemIndex);
                AnimationState state = lvsi.AnimationState;
                if (state == null || !state.IsValid)
                    continue;

                // Has this frame of the animation expired?
                if (elapsedMilliseconds >= state.currentFrameExpiresAt) {
                    state.AdvanceFrame(elapsedMilliseconds);

                    // Track the area of the view that needs to be redrawn to show the changed images
                    if (updateRect.IsEmpty)
                        updateRect = lvsi.Bounds;
                    else
                        updateRect = Rectangle.Union(updateRect, lvsi.Bounds);
                }

                // Remember the minimum time at which a frame is next due to change
                nextCheckAt = Math.Min(nextCheckAt, state.currentFrameExpiresAt);
            }

            // Update the part of the listview where frames have changed
            if (!updateRect.IsEmpty)
                this.ListView.Invalidate(updateRect);

            // Renew the tickler in time for the next frame change
            this.Tickler.Change(nextCheckAt - elapsedMilliseconds, Timeout.Infinite);
        }

        #endregion

        /// <summary>
        /// Instances of this class kept track of the animation state of a single image.
        /// </summary>
        internal class AnimationState {
            private const int PropertyTagTypeShort = 3;
            private const int PropertyTagTypeLong = 4;
            private const int PropertyTagFrameDelay = 0x5100;
            private const int PropertyTagLoopCount = 0x5101;

            /// <summary>
            /// Is the given image an animation
            /// </summary>
            /// <param name="image">The image to be tested</param>
            /// <returns>Is the image an animation?</returns>
            public static bool IsAnimation(Image image) {
                if (image == null)
                    return false;
                else
                    return (new List<Guid>(image.FrameDimensionsList)).Contains(FrameDimension.Time.Guid);
            }

            /// <summary>
            /// Create an AnimationState in a quiet state
            /// </summary>
            public AnimationState() {
                this.imageDuration = new List<int>();
            }

            /// <summary>
            /// Create an animation state for the given image, which may or may not
            /// be an animation
            /// </summary>
            /// <param name="image">The image to be rendered</param>
            public AnimationState(Image image)
                : this() {
                if (!AnimationState.IsAnimation(image))
                    return;

                // How many frames in the animation?
                this.image = image;
                this.frameCount = this.image.GetFrameCount(FrameDimension.Time);

                // Find the delay between each frame.
                // The delays are stored an array of 4-byte ints. Each int is the
                // number of 1/100th of a second that should elapsed before the frame expires
                foreach (PropertyItem pi in this.image.PropertyItems) {
                    if (pi.Id == PropertyTagFrameDelay) {
                        for (int i = 0; i < pi.Len; i += 4) {
                            //TODO: There must be a better way to convert 4-bytes to an int
                            int delay = (pi.Value[i + 3] << 24) + (pi.Value[i + 2] << 16) + (pi.Value[i + 1] << 8) + pi.Value[i];
                            this.imageDuration.Add(delay * 10); // store delays as milliseconds
                        }
                        break;
                    }
                }

                // There should be as many frame durations as frames
                Debug.Assert(this.imageDuration.Count == this.frameCount, "There should be as many frame durations as there are frames.");
            }

            /// <summary>
            /// Does this state represent a valid animation
            /// </summary>
            public bool IsValid {
                get { return (this.image != null && this.frameCount > 0); }
            }

            /// <summary>
            /// Advance our images current frame and calculate when it will expire
            /// </summary>
            public void AdvanceFrame(long millisecondsNow) {
                this.currentFrame = (this.currentFrame + 1) % this.frameCount;
                this.currentFrameExpiresAt = millisecondsNow + this.imageDuration[this.currentFrame];
                this.image.SelectActiveFrame(FrameDimension.Time, this.currentFrame);
            }

            internal int currentFrame;
            internal long currentFrameExpiresAt;
            internal Image image;
            internal List<int> imageDuration;
            internal int frameCount;
        }

        #region Private variables

        private System.Threading.Timer tickler; // timer used to tickle the animations
        private Stopwatch stopwatch; // clock used to time the animation frame changes

        #endregion
    }
}
