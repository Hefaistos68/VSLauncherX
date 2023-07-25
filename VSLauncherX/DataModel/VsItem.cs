namespace VSLauncher.DataModel
{
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
		/// Gets the last modified.
		/// </summary>
		public DateTime LastModified { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether changed.
		/// </summary>
		public bool Changed { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether checked.
		/// </summary>
		public bool Checked { get; set; }
    }
}
