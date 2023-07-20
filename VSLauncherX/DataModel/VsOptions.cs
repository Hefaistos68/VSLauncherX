namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs options.
	/// </summary>
	public class VsOptions
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="VsOptions"/> class.
		/// </summary>
		public VsOptions()
		{
			this.RunBefore = null;
			this.RunAfter = null;
			this.RunAsAdmin = false;
		}

		/// <summary>
		/// Gets or sets the run before.
		/// </summary>
		public VsItem? RunBefore { get; set; }

		/// <summary>
		/// Gets or sets the run after.
		/// </summary>
		public VsItem? RunAfter { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether run as admin.
		/// </summary>
		public bool RunAsAdmin { get; set; }
    }
}
