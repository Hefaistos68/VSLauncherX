namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents the result of a Visual Studio Code or VS Code-based IDE update check for a single installed edition.
	/// </summary>
	public sealed class VsCodeUpdateInfo
	{
		private VsCodeUpdateCheckStatus? updateCheckStatus;

		/// <summary>
		/// Gets the stable identifier of the checked <see cref="VsCodeInstance"/>.
		/// </summary>
		public string InstanceIdentifier { get; init; } = string.Empty;

		/// <summary>
		/// Gets a value indicating whether a newer build is available to install.
		/// </summary>
		public bool IsUpdateAvailable { get; init; }

		/// <summary>
		/// Gets the installed product version detected at check time.
		/// </summary>
		public string CurrentVersion { get; init; } = string.Empty;

		/// <summary>
		/// Gets the installed commit hash detected at check time, if available.
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
		/// Gets a value indicating whether the update check completed against the update service, a valid cached result, or an explicit unsupported determination.
		/// </summary>
		public bool CheckSucceeded { get; init; }

		/// <summary>
		/// Gets the outcome status of the update check, defaulting consistently from <see cref="CheckSucceeded"/> and <see cref="IsUpdateAvailable"/> when not explicitly specified.
		/// </summary>
		public VsCodeUpdateCheckStatus UpdateCheckStatus
		{
			get
			{
				if (this.updateCheckStatus.HasValue)
				{
					return this.updateCheckStatus.Value;
				}

				if (!this.CheckSucceeded)
				{
					return VsCodeUpdateCheckStatus.Failed;
				}

				return this.IsUpdateAvailable
					? VsCodeUpdateCheckStatus.UpdateAvailable
					: VsCodeUpdateCheckStatus.UpToDate;
			}
			init
			{
				this.updateCheckStatus = value;
			}
		}

		/// <summary>
		/// Gets the Windows installer family used when executing an automated update.
		/// </summary>
		public VsCodeInstallerKind InstallerKind { get; init; } = VsCodeInstallerKind.InnoSetupVsCode;
	}
}
