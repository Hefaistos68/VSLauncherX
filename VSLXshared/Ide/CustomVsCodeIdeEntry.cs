namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents a manually registered VS Code-based IDE persisted in the shared custom IDE store.
	/// </summary>
	public sealed record CustomVsCodeIdeEntry
	{
		/// <summary>
		/// Gets the canonical full path to the IDE executable (<c>.exe</c>).
		/// </summary>
		public string ExecutablePath { get; init; } = string.Empty;

		/// <summary>
		/// Gets the optional custom display name configured by the user.
		/// </summary>
		public string? DisplayName { get; init; }

		/// <summary>
		/// Gets the optional user data directory override used to read MRU workspaces.
		/// </summary>
		public string? UserDataDirectory { get; init; }
	}
}
