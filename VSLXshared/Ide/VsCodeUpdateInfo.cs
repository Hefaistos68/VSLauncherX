namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents the result of a Visual Studio Code update check for a single installed edition.
	/// </summary>
	public sealed class VsCodeUpdateInfo
	{
		/// <summary>
		/// Gets the stable identifier of the checked <see cref="VsCodeInstance"/>.
		/// </summary>
		public string InstanceIdentifier { get; init; } = string.Empty;

		/// <summary>
		/// Gets a value indicating whether a newer VS Code build is available to install.
		/// </summary>
		public bool IsUpdateAvailable { get; init; }

		/// <summary>
		/// Gets the installed product version detected at check time.
		/// </summary>
		public string CurrentVersion { get; init; } = string.Empty;

		/// <summary>
		/// Gets the installed VS Code commit hash detected at check time, if available.
		/// </summary>
		public string CurrentCommit { get; init; } = string.Empty;

		/// <summary>
		/// Gets the latest available product version reported by the update service.
		/// </summary>
		public string LatestVersion { get; init; } = string.Empty;

		/// <summary>
		/// Gets the latest available commit hash reported by the update service.
		/// </summary>
		public string LatestCommit { get; init; } = string.Empty;

		/// <summary>
		/// Gets the official HTTPS installer download URL when an update is available.
		/// </summary>
		public string DownloadUrl { get; init; } = string.Empty;

		/// <summary>
		/// Gets the expected lowercase hexadecimal SHA-256 hash of the installer binary, when supplied by the update service.
		/// </summary>
		public string Sha256Hash { get; init; } = string.Empty;

		/// <summary>
		/// Gets a value indicating whether the target installation uses the machine-wide System installer requiring elevation.
		/// </summary>
		public bool IsSystemInstall { get; init; }

		/// <summary>
		/// Gets the UTC timestamp when the update check was performed.
		/// </summary>
		public DateTimeOffset CheckedAtUtc { get; init; }

		/// <summary>
		/// Gets a value indicating whether the update check completed against the update service or a valid cached result.
		/// </summary>
		public bool CheckSucceeded { get; init; }
	}
}
