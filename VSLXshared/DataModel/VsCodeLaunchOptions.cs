namespace VSLauncher.DataModel
{
	/// <summary>
	/// Stores Visual Studio Code launch options applied to a folder or workspace.
	/// </summary>
	public class VsCodeLaunchOptions
	{
		/// <summary>
		/// Gets or sets the VS Code installation edition identifier.
		/// </summary>
		public string? Edition { get; set; }

		/// <summary>
		/// Gets or sets the VS Code profile name.
		/// </summary>
		public string? Profile { get; set; }

		/// <summary>
		/// Gets or sets the window reuse mode.
		/// </summary>
		public VsCodeWindowMode WindowMode { get; set; } = VsCodeWindowMode.Default;

		/// <summary>
		/// Gets or sets a value indicating whether extensions should be disabled when launching.
		/// </summary>
		public bool DisableExtensions { get; set; }

		/// <summary>
		/// Gets or sets additional command-line arguments passed to VS Code.
		/// </summary>
		public string? Commands { get; set; }

		/// <summary>
		/// Creates a deep copy of this <see cref="VsCodeLaunchOptions"/> instance.
		/// </summary>
		/// <returns>A cloned <see cref="VsCodeLaunchOptions"/> instance.</returns>
		public VsCodeLaunchOptions Clone()
		{
			return (VsCodeLaunchOptions)this.MemberwiseClone();
		}
	}
}
