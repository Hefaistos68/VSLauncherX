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
					this.parent.Changed = value;
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
		/// Adds a range of items to the list.
		/// </summary>
		/// <param name="collection">The items to add.</param>
		public new void AddRange(IEnumerable<VsItem> collection)
		{
			base.AddRange(collection);
			this.Changed = true;
		}

		/// <summary>
		/// Inserts an item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index.</param>
		/// <param name="item">The item to insert.</param>
		public new void Insert(int index, VsItem item)
		{
			base.Insert(index, item);
			this.Changed = true;
		}

		/// <summary>
		/// Removes the.
		/// </summary>
		/// <param name="item">The item.</param>
		public new bool Remove(VsItem item)
		{
			bool removed = base.Remove(item);

			if (removed)
			{
				this.Changed = true;
			}

			return removed;
		}

		/// <summary>
		/// Removes the item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index.</param>
		public new void RemoveAt(int index)
		{
			base.RemoveAt(index);
			this.Changed = true;
		}

		/// <summary>
		/// Clears all items from the list.
		/// </summary>
		public new void Clear()
		{
			if (this.Count > 0)
			{
				base.Clear();
				this.Changed = true;
			}
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
