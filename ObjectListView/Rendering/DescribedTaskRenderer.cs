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

		/// <summary>
		/// Configures the sub item.
		/// </summary>
		/// <param name="e">The e.</param>
		/// <param name="cellBounds">The cell bounds.</param>
		/// <param name="model">The model.</param>
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
