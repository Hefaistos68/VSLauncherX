namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs project.
	/// </summary>
	public class VsProject : VsItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsProject"/> class.
		/// </summary>
		public VsProject()
		{
			this.ProjectType = eProjectType.None;
		}

		public VsProject(string name, string path, eProjectType prType) : base(name, path, null)
		{
			this.ProjectType = prType;
		}

		/// <summary>
		/// Gets or sets the project type.
		/// </summary>
		public eProjectType ProjectType { get; set; }
	}
}
