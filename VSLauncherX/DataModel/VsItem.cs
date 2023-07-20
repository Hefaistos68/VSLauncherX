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
		public VsItem(string name, string path) : this()
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Path = path ?? throw new ArgumentNullException(nameof(path));
			this.LastModified = DateTime.Now;
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
		public DateTime LastModified { get; private set; }
    }
}
