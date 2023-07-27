using Newtonsoft.Json;

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
		[JsonProperty("FolderItems")]
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
					if (i is VsFolder f)
					{
						var c = f.Checked;
						if (c == true)
							n1++;
						else if (c == false)
							n2++;
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
					if(i is VsFolder f)
					{
						f.Checked = value;
					}
					else
					{
						i.Checked = value ?? false;
					}
				}
			}
		}

		/// <summary>
		/// Reutrns the number of solutions this item holds.
		/// </summary>
		/// <returns>An int.</returns>
		public int ContainedSolutionsCount()
		{
			int n = 0;

			foreach (var i in this.Items)
			{
				if (i is VsFolder f)
				{
					n += f.ContainedSolutionsCount();
				}
				else if (i is VsSolution)
				{
					n++;
				}
			}

			return n;
		}
		/// <summary>
		/// Reutrns the number of projects this item holds.
		/// </summary>
		/// <returns>An int.</returns>
		public int ContainedProjectsCount()
		{
			int n = 0;

			foreach (var i in this.Items)
			{
				if (i is VsFolder f)
				{
					n += f.ContainedProjectsCount();
				}
				else if (i is VsProject)
				{
					n++;
				}
			}

			return n;
		}

		/// <summary>
		/// Tos the string.
		/// </summary>
		/// <returns>A string? .</returns>
		public override string? ToString()
		{
			return base.ToString();
		}

		/// <summary>
		/// Finds the parent of the given item, recurses through all subitems
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>A VsFolder.</returns>
		internal VsFolder? FindParent(object item)
		{
			if (this.Items.Contains(item))
				return this;

			foreach(var i in this.Items)
			{
				if (i is VsFolder f)
				{
					var p = f.FindParent(item);
					if (p != null)
					{
						return p;
					}
				}
			}

			return null;
		}
	}
}
