using System.Drawing;
using System.Windows.Forms;

namespace BrightIdeasSoftware
{
	/// <summary>
	/// This renderer draws just a checkbox to match the check state of our model object.
	/// </summary>
	public class CheckStateRenderer : BaseRenderer
	{
		/// <summary>
		/// Draw our cell
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(DrawListViewItemEventArgs e, Graphics g, Rectangle r)
		{
			this.DrawBackground(g, r);
			if (this.Column == null)
				return;
			r = this.ApplyCellPadding(r);
			CheckState state = this.Column.GetCheckState(this.RowObject);
			if (this.IsPrinting)
			{
				// Renderers don't work onto printer DCs, so we have to draw the image ourselves
				string key = ObjectListView.CHECKED_KEY;
				if (state == CheckState.Unchecked)
					key = ObjectListView.UNCHECKED_KEY;
				if (state == CheckState.Indeterminate)
					key = ObjectListView.INDETERMINATE_KEY;
				this.DrawAlignedImage(g, r, this.ImageListOrDefault.Images[key]);
			}
			else
			{
				r = this.CalculateCheckBoxBounds(g, r);
				CheckBoxRenderer.DrawCheckBox(g, r.Location, this.GetCheckBoxState(state));
			}
		}

		/// <summary>
		/// Draw our cell
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public override void Render(DrawListViewSubItemEventArgs e, Graphics g, Rectangle r)
		{
			this.DrawBackground(g, r);
			if (this.Column == null)
				return;
			r = this.ApplyCellPadding(r);
			CheckState state = this.Column.GetCheckState(this.RowObject);
			if (this.IsPrinting)
			{
				// Renderers don't work onto printer DCs, so we have to draw the image ourselves
				string key = ObjectListView.CHECKED_KEY;
				if (state == CheckState.Unchecked)
					key = ObjectListView.UNCHECKED_KEY;
				if (state == CheckState.Indeterminate)
					key = ObjectListView.INDETERMINATE_KEY;
				this.DrawAlignedImage(g, r, this.ImageListOrDefault.Images[key]);
			}
			else
			{
				r = this.CalculateCheckBoxBounds(g, r);
				CheckBoxRenderer.DrawCheckBox(g, r.Location, this.GetCheckBoxState(state));
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
		protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds, OLVListItem item, int subItemIndex, Size preferredSize)
		{
			return this.CalculatePaddedAlignedBounds(g, cellBounds, preferredSize);
		}

		/// <summary>
		/// Handle the HitTest request
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hti"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
		{
			Rectangle r = this.CalculateCheckBoxBounds(g, this.Bounds);
			if (r.Contains(x, y))
				hti.HitTestLocation = HitTestLocation.CheckBox;
		}
	}
}
