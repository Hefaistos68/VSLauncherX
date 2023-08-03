namespace VSLauncher.DataModel
{
	/// <summary>
	/// The launch info.
	/// </summary>
	public class LaunchInfo
	{
		/// <summary>
		/// Gets or sets the target, a Visual Studio executable
		/// </summary>
		public string? Target { get; set; }

		/// <summary>
		/// Gets or sets the solution, project or Folder to launch
		/// </summary>
		public VsFolder? Solution { get; set; }
	}
}
