namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs solution.
	/// </summary>
	public class VsSolution : VsItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsSolution"/> class.
		/// </summary>
		public VsSolution()
		{
			this.SolutionType =	eSolutionType.None;
		}

		public VsSolution(string name, string path) : base(name, path, null)
		{
		}

		public VsSolution(string name, string path, eSolutionType type) : base(name, path, null)
		{
			this.SolutionType = type;
		}

		/// <summary>
		/// Gets or sets the solution type.
		/// </summary>
		public eSolutionType SolutionType { get; set; }
	}
}
