namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs item list.
	/// </summary>
	public class VsItemList : List<VsItem>
	{
		private VsItem? parent;
		private bool isChanged = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="VsItemList"/> class.
		/// </summary>
		/// <param name="parentItem">The parent item.</param>
		public VsItemList(VsItem parentItem)
		{
			this.parent = parentItem;
		}

		/// <summary>
		/// Gets or sets a value indicating whether changed.
		/// </summary>
		public bool Changed { get => this.isChanged; set { this.isChanged = value; if (this.parent != null) this.parent.Changed = value; } }

		/// <summary>
		/// Adds the.
		/// </summary>
		/// <param name="item">The item.</param>
		public new void Add(VsItem item)
		{
			base.Add(item);
			this.Changed = true;
		}

		/// <summary>
		/// Removes the.
		/// </summary>
		/// <param name="item">The item.</param>
		public new void Remove(VsItem item)
		{
			base.Remove(item);
			this.Changed = true;
		}

		public void Reparent(VsItem newParent)
		{
			if (this.parent != null) 
			{
				throw new Exception();
			}

			this.parent = newParent; 
		}
	}
}
