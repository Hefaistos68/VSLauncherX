using Newtonsoft.Json;

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
		public VsItemList(VsItem? parentItem)
		{
			this.parent = parentItem;
		}

		/// <summary>
		/// Gets or sets a value indicating whether changed.
		/// </summary>
		[JsonIgnore]
		public bool Changed
		{
			get => this.isChanged;
			set
			{
				this.isChanged = value;
				
				if (this.parent != null)
				{
					((VsFolder)this.parent).Changed = value;
				}

				if (OnChanged != null)
				{
					this.isChanged = OnChanged.Invoke(value);
				}
			}
		}

		public delegate bool OnChangedHandler(bool changed);
		public event OnChangedHandler? OnChanged;

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

		/// <summary>
		/// Reparents the list
		/// </summary>
		/// <param name="newParent">The new parent.</param>
		public void Reparent(VsItem newParent)
		{
// 			if (this.parent != null && this.parent != newParent) 
// 			{
// 				throw new Exception();
// 			}

			this.parent = newParent; 
		}
	}
}
