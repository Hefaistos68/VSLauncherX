namespace VSLauncher.Ide
{
	/// <summary>
	/// Specifies the remote update-checking strategy supported by a VS Code-based IDE.
	/// </summary>
	public enum VsCodeUpdateStrategyKind
	{
		/// <summary>
		/// Queries a VS Code-compatible update API endpoint (<c>/api/update/&lt;platform&gt;/&lt;quality&gt;/&lt;commit&gt;</c>).
		/// </summary>
		VsCodeApi = 0,

		/// <summary>
		/// Queries the GitHub Releases API (<c>/repos/&lt;owner&gt;/&lt;repo&gt;/releases/latest</c>).
		/// </summary>
		GitHubReleases = 1,

		/// <summary>
		/// Automatic update checking is not supported for this IDE.
		/// </summary>
		Unsupported = 2
	}

	/// <summary>
	/// Specifies the Windows installer family and silent command-line behavior for a VS Code-based IDE.
	/// </summary>
	public enum VsCodeInstallerKind
	{
		/// <summary>
		/// Official VS Code Inno Setup installer supporting <c>/MERGETASKS=!runcode</c>.
		/// </summary>
		InnoSetupVsCode = 0,

		/// <summary>
		/// Standard Inno Setup installer supporting <c>/SILENT /NORESTART /CLOSEAPPLICATIONS</c>.
		/// </summary>
		InnoSetupGeneric = 1,

		/// <summary>
		/// NSIS installer supporting <c>/S</c> silent execution.
		/// </summary>
		NsisSilent = 2,

		/// <summary>
		/// External download flow without automated silent installer execution.
		/// </summary>
		ExternalDownload = 3
	}

	/// <summary>
	/// Represents the outcome status of an update check for a VS Code-based IDE.
	/// </summary>
	public enum VsCodeUpdateCheckStatus
	{
		/// <summary>
		/// The installed version matches the latest available release.
		/// </summary>
		UpToDate = 0,

		/// <summary>
		/// A newer release is available to download and install.
		/// </summary>
		UpdateAvailable = 1,

		/// <summary>
		/// Automatic update checking is unsupported for this IDE.
		/// </summary>
		Unsupported = 2,

		/// <summary>
		/// The update check failed due to a network, HTTP, or payload validation error.
		/// </summary>
		Failed = 3
	}
}
