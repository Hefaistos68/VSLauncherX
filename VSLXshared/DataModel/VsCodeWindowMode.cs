namespace VSLauncher.DataModel
{
	/// <summary>
	/// Specifies how Visual Studio Code opens a target window.
	/// </summary>
	public enum VsCodeWindowMode
	{
		/// <summary>
		/// Uses Visual Studio Code's default window behavior.
		/// </summary>
		Default,

		/// <summary>
		/// Opens the target in a new window (<c>--new-window</c>).
		/// </summary>
		NewWindow,

		/// <summary>
		/// Reuses the most recently active window (<c>--reuse-window</c>).
		/// </summary>
		ReuseWindow
	}
}
