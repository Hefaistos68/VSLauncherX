using System.Text.Json.Serialization;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs folder.
	/// </summary>
	public class VsFolder : VsItem
	{
		private VsItemList items;

		/// <summary>
		/// Initializes a new instance of the <see cref="VsFolder"/> class.
		/// </summary>
		public VsFolder()
		{
			this.Items = new VsItemList(this);
			this.ItemType = eItemType.Folder;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsFolder"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public VsFolder(string name, string path) : base(name, path, null)
		{
			this.Items = new VsItemList(this);
			this.ItemType = eItemType.Folder;
		}

		/// <summary>
		/// Gets or sets the items.
		/// </summary>
		public VsItemList Items
		{
			get { return items; }
			set
			{
				items = value;
				items.Reparent(this);
			}
		}

		[JsonIgnore]
		public new bool? Checked
		{
			get 
			{
				int n1 = 0;
				int n2 = 0;

				if (Items.Count == 0)
				{
					return base.Checked;
				}

				foreach (var i in Items)
				{
					if (i is VsFolder)
					{
					}
					else if (i.Checked)
						n1++;
					else
						n2++;
				}

				return n1 == Items.Count ? true : n2 == Items.Count ? false : null; 
			}
			set
			{
				base.Checked = value ?? false;
				foreach (var i in Items)
				{
					i.Checked = value ?? false;
				}
			}
		}
	}
}
