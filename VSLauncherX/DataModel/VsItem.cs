using Newtonsoft.Json;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The item type, required for serialization mostly
	/// </summary>
	public enum ItemTypeEnum
	{
		Solution,
		Project,
		Other,
		Folder,
		VisualStudio
	}

	/// <summary>
	/// The vs item.
	/// </summary>
	public class VsItem : VsOptions
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="VsItem"/> class.
		/// </summary>
		public VsItem() : base()
		{
			this.ItemType = ItemTypeEnum.Other;
		}

		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A VsItem.</returns>
		public VsItem Clone()
		{
			return (VsItem)this.MemberwiseClone();
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="VsItem"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public VsItem(string name, string path, DateTime? modified) : this()
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Path = path ?? throw new ArgumentNullException(nameof(path));
			this.LastModified = modified ?? DateTime.Now;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Gets or sets the path.
		/// </summary>
		public string? Path { get; set; }

		/// <summary>
		/// Gets or sets the commands.
		/// </summary>
		public string? Commands { get; set; }

		/// <summary>
		/// Gets or sets the instance.
		/// </summary>
		public string? Instance { get; set; }

		/// <summary>
		/// Gets the last modified.
		/// </summary>
		[JsonIgnore]
		public DateTime LastModified { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether changed.
		/// </summary>
		[JsonIgnore]
		public bool Changed { get; set; }

		/// <summary>
		/// Gets or sets the item type.
		/// </summary>
		public ItemTypeEnum ItemType { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether checked.
		/// </summary>
		[JsonIgnore]
		public bool Checked { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this solution is possibly inaccessible
		/// </summary>
		[JsonIgnore]
		public bool Warning { get; set; }

		/// <summary>
		/// Refreshes the item
		/// </summary>
		public virtual void Refresh()
		{
		}
    }
}
