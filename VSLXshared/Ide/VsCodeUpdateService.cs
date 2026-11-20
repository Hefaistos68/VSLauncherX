using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Checks daily for updates across VS Code and supported VS Code-based IDEs, persists per-edition check state,
	/// and downloads/verifies official Windows installers for in-place updates.
	/// </summary>
	public sealed class VsCodeUpdateService : IDisposable
	{
		/// <summary>
		/// The default minimum interval between remote update API checks for a given IDE edition.
		/// </summary>
		public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(24);

		private const string UpdateApiBaseUrl           = "https://update.code.visualstudio.com/api/update";
		private const string GitHubApiBaseUrl           = "https://api.github.com/repos";
		private const string InstallerSilentArguments   = "/SILENT /NORESTART /CLOSEAPPLICATIONS /MERGETASKS=!runcode";
		private const string GenericInnoSilentArguments = "/SILENT /NORESTART /CLOSEAPPLICATIONS";
		private const string NsisSilentArguments        = "/S";
		private const string CacheFileName              = "vscode-update-cache.json";
		private const int DownloadBufferSize            = 81920;

		private static readonly TimeSpan MetadataRequestTimeout = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan ClockSkewTolerance     = TimeSpan.FromMinutes(5);
		private static readonly Regex GitHubRepoRegex           = new Regex("^[A-Za-z0-9._-]+/[A-Za-z0-9._-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private static readonly HashSet<string> TrustedDownloadHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"vscode.download.prss.microsoft.com",
			"update.code.visualstudio.com",
			"az764295.vo.msecnd.net"
		};

		private static readonly HashSet<string> DefaultGitHubDownloadHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"github.com",
			"objects.githubusercontent.com",
			"release-assets.githubusercontent.com"
		};

		private readonly HttpClient httpClient;
		private readonly IVsCodeEnvironment environment;
		private readonly Func<DateTimeOffset> utcNowProvider;
		private readonly string cacheFilePath;
		private readonly Architecture osArchitecture;
		private readonly object cacheLock = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeUpdateService"/> class using default Windows environment and HTTP settings.
		/// </summary>
		public VsCodeUpdateService()
			: this(httpHandler: null, environment: null, utcNowProvider: null, cacheFilePath: null, osArchitecture: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeUpdateService"/> class with injectable dependencies for testing.
		/// </summary>
		/// <param name="httpHandler">Optional HTTP message handler override.</param>
		/// <param name="environment">Optional VS Code host environment abstraction.</param>
		/// <param name="utcNowProvider">Optional UTC clock provider.</param>
		/// <param name="cacheFilePath">Optional path to the daily update cache JSON file.</param>
		/// <param name="osArchitecture">Optional OS architecture override.</param>
		public VsCodeUpdateService(
			HttpMessageHandler? httpHandler = null,
			IVsCodeEnvironment? environment = null,
			Func<DateTimeOffset>? utcNowProvider = null,
			string? cacheFilePath = null,
			Architecture? osArchitecture = null)
		{
			this.environment    = environment ?? WindowsVsCodeEnvironment.Instance;
			this.utcNowProvider = utcNowProvider ?? (() => DateTimeOffset.UtcNow);
			this.osArchitecture = osArchitecture ?? RuntimeInformation.OSArchitecture;
			this.cacheFilePath  = string.IsNullOrWhiteSpace(cacheFilePath)
				? ResolveDefaultCachePath(this.environment)
				: cacheFilePath;

			this.httpClient = httpHandler is null
				? new HttpClient()
				: new HttpClient(httpHandler, disposeHandler: false);

			this.httpClient.Timeout = TimeSpan.FromMinutes(10);
			this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VSLauncherX");
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			this.httpClient.Dispose();
		}

		/// <summary>
		/// Checks whether an update is available for <paramref name="instance"/>, honoring the 24-hour persisted cache unless <paramref name="forceRefresh"/> is set.
		/// </summary>
		/// <param name="instance">The discovered IDE installation to check.</param>
		/// <param name="forceRefresh">When <c>true</c>, bypasses the daily cache and queries the update endpoint immediately.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A <see cref="VsCodeUpdateInfo"/> describing update availability and outcome status.</returns>
		public async Task<VsCodeUpdateInfo> CheckForUpdateAsync(
			VsCodeInstance instance,
			bool forceRefresh = false,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(instance);

			DateTimeOffset nowUtc                    = this.utcNowProvider();
			LocalInstallationInfo localInfo          = ReadLocalInstallationInfo(instance);
			VsCodeUpdateStrategyKind activeStrategy  = ResolveEffectiveStrategy(instance, localInfo);

			if (activeStrategy == VsCodeUpdateStrategyKind.Unsupported)
			{
				return CreateUnsupportedInfo(instance, localInfo, nowUtc);
			}

			if (!forceRefresh)
			{
				VsCodeUpdateInfo? cached = TryResolveFreshCachedUpdate(instance, localInfo, nowUtc);

				if (cached is not null)
				{
					return cached;
				}
			}

			VsCodeUpdateInfo remoteResult = await QueryRemoteUpdateAsync(
				instance,
				localInfo,
				activeStrategy,
				nowUtc,
				cancellationToken).ConfigureAwait(false);

			if (remoteResult.CheckSucceeded)
			{
				SaveCacheEntry(instance.Identifier, remoteResult);
			}

			return remoteResult;
		}

		/// <summary>
		/// Checks every supplied <see cref="VsCodeInstance"/> for updates and returns a case-insensitive map keyed by <see cref="VsCodeInstance.Identifier"/>.
		/// </summary>
		/// <param name="instances">The installed IDE instances to inspect.</param>
		/// <param name="forceRefresh">When <c>true</c>, bypasses the 24-hour cache for all checked instances.</param>
		/// <param name="cancellationToken">A token to cancel the check sequence.</param>
		/// <returns>A dictionary mapping each instance identifier to its <see cref="VsCodeUpdateInfo"/> result.</returns>
		public async Task<IReadOnlyDictionary<string, VsCodeUpdateInfo>> CheckAllForUpdatesAsync(
			IEnumerable<VsCodeInstance> instances,
			bool forceRefresh = false,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(instances);

			var results = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);

			foreach (VsCodeInstance instance in instances)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (instance is null)
				{
					continue;
				}

				VsCodeUpdateInfo info = await CheckForUpdateAsync(instance, forceRefresh, cancellationToken).ConfigureAwait(false);

				results[instance.Identifier] = info;
			}

			return results;
		}

		/// <summary>
		/// Downloads the official VS Code installer described by <paramref name="updateInfo"/> to a temporary file and verifies its SHA-256 hash when provided.
		/// </summary>
		/// <param name="updateInfo">The update metadata containing the download URL and expected hash.</param>
		/// <param name="cancellationToken">A token to cancel the download.</param>
		/// <returns>The full path to the downloaded and verified installer executable.</returns>
		public Task<string> DownloadInstallerAsync(VsCodeUpdateInfo updateInfo, CancellationToken cancellationToken = default)
		{
			return DownloadInstallerAsync(updateInfo, instance: null, cancellationToken);
		}

		/// <summary>
		/// Downloads the installer described by <paramref name="updateInfo"/> for <paramref name="instance"/> to a temporary file and verifies its SHA-256 hash when provided.
		/// </summary>
		/// <param name="updateInfo">The update metadata containing the download URL and expected hash.</param>
		/// <param name="instance">Optional target IDE instance whose trusted download hosts and <c>updateUrl</c> should be honored.</param>
		/// <param name="cancellationToken">A token to cancel the download.</param>
		/// <returns>The full path to the downloaded and verified installer executable.</returns>
		public async Task<string> DownloadInstallerAsync(
			VsCodeUpdateInfo updateInfo,
			VsCodeInstance? instance,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(updateInfo);

			if (!updateInfo.IsUpdateAvailable || !IsTrustedDownloadUrl(updateInfo.DownloadUrl, instance))
			{
				throw new InvalidOperationException("VS Code update download URL is missing or untrusted.");
			}

			string tempDir  = this.environment.GetTempPath();
			string filePath = Path.Combine(tempDir, BuildTempInstallerFileName(updateInfo));

			Directory.CreateDirectory(tempDir);

			try
			{
				string actualSha256 = await DownloadFileAndComputeSha256Async(updateInfo.DownloadUrl, filePath, cancellationToken).ConfigureAwait(false);

				if (!string.IsNullOrWhiteSpace(updateInfo.Sha256Hash)
					&& !actualSha256.Equals(updateInfo.Sha256Hash.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					TryDeleteInstallerFile(filePath);

					throw new InvalidDataException("Downloaded VS Code installer failed SHA-256 verification.");
				}

				return filePath;
			}
			catch
			{
				TryDeleteInstallerFile(filePath);

				throw;
			}
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> configured to run the official VS Code Inno Setup installer in silent update mode.
		/// </summary>
		/// <param name="installerPath">The path to the downloaded installer executable.</param>
		/// <param name="isSystemInstall"><c>true</c> for machine-wide System installs requiring UAC elevation; otherwise <c>false</c>.</param>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public static ProcessStartInfo BuildInstallerStartInfo(string installerPath, bool isSystemInstall)
		{
			return BuildInstallerStartInfo(installerPath, isSystemInstall, VsCodeInstallerKind.InnoSetupVsCode);
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> configured to run a downloaded IDE installer using silent arguments appropriate for <paramref name="installerKind"/>.
		/// </summary>
		/// <param name="installerPath">The path to the downloaded installer executable.</param>
		/// <param name="isSystemInstall"><c>true</c> for machine-wide System installs requiring UAC elevation; otherwise <c>false</c>.</param>
		/// <param name="installerKind">The installer family determining silent command-line arguments.</param>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public static ProcessStartInfo BuildInstallerStartInfo(
			string installerPath,
			bool isSystemInstall,
			VsCodeInstallerKind installerKind)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);

			return new ProcessStartInfo
			{
				FileName        = installerPath,
				Arguments       = ResolveInstallerArguments(installerKind),
				UseShellExecute = true,
				Verb            = isSystemInstall ? "runas" : string.Empty,
				ErrorDialog     = true
			};
		}

		/// <summary>
		/// Validates whether an <c>updateUrl</c> value is a non-placeholder, public HTTPS DNS endpoint suitable for remote update queries.
		/// </summary>
		/// <param name="url">The candidate endpoint URL.</param>
		/// <returns><c>true</c> if the URL uses HTTPS with a non-reserved DNS hostname; otherwise <c>false</c>.</returns>
		public static bool IsValidUpdateEndpointUrl(string? url)
		{
			return VsCodeProductDescriptor.IsValidUpdateEndpointUrl(url);
		}

		/// <summary>
		/// Determines whether a download URL uses HTTPS and points to a trusted Microsoft VS Code download host.
		/// </summary>
		/// <param name="url">The candidate download URL.</param>
		/// <returns><c>true</c> if the URL is HTTPS on an allowlisted Microsoft host; otherwise <c>false</c>.</returns>
		public static bool IsTrustedDownloadUrl(string? url)
		{
			return IsTrustedDownloadUrl(url, instance: null);
		}

		/// <summary>
		/// Determines whether a download URL uses HTTPS and points to a host trusted for <paramref name="instance"/> (or Microsoft VS Code when <paramref name="instance"/> is <c>null</c>).
		/// </summary>
		/// <param name="url">The candidate download URL.</param>
		/// <param name="instance">Optional IDE instance supplying product trusted hosts and <c>product.json</c> <c>updateUrl</c>.</param>
		/// <returns><c>true</c> if the URL is a valid public HTTPS endpoint on a trusted host; otherwise <c>false</c>.</returns>
		public static bool IsTrustedDownloadUrl(string? url, VsCodeInstance? instance)
		{
			if (!IsValidUpdateEndpointUrl(url) || !Uri.TryCreate(url!.Trim(), UriKind.Absolute, out Uri? uri))
			{
				return false;
			}

			string host = uri.Host.TrimEnd('.').ToLowerInvariant();

			if (instance is null)
			{
				return TrustedDownloadHosts.Contains(host);
			}

			return IsHostTrustedForInstance(host, instance);
		}

		/// <summary>
		/// Compares two semantic version strings by their numeric dot-separated components, ignoring prerelease suffixes.
		/// </summary>
		/// <param name="left">The left version string.</param>
		/// <param name="right">The right version string.</param>
		/// <returns>A negative value if <paramref name="left"/> is older, zero if equal, or a positive value if newer.</returns>
		public static int CompareVersions(string? left, string? right)
		{
			int[] leftParts  = ParseNumericVersionParts(left);
			int[] rightParts = ParseNumericVersionParts(right);
			int maxLength    = Math.Max(leftParts.Length, rightParts.Length);

			for (int i = 0; i < maxLength; i++)
			{
				int leftValue  = i < leftParts.Length ? leftParts[i] : 0;
				int rightValue = i < rightParts.Length ? rightParts[i] : 0;

				if (leftValue != rightValue)
				{
					return leftValue.CompareTo(rightValue);
				}
			}

			return 0;
		}

		/// <summary>
		/// Deletes a temporary installer file on a best-effort basis without throwing on lock or I/O errors.
		/// </summary>
		/// <param name="installerPath">The installer file path to remove.</param>
		public static void TryDeleteInstallerFile(string? installerPath)
		{
			if (string.IsNullOrWhiteSpace(installerPath))
			{
				return;
			}

			try
			{
				if (File.Exists(installerPath))
				{
					File.Delete(installerPath);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Resolves silent command-line arguments corresponding to <paramref name="installerKind"/>.
		/// </summary>
		private static string ResolveInstallerArguments(VsCodeInstallerKind installerKind)
		{
			return installerKind switch
			{
				VsCodeInstallerKind.InnoSetupVsCode  => InstallerSilentArguments,
				VsCodeInstallerKind.InnoSetupGeneric => GenericInnoSilentArguments,
				VsCodeInstallerKind.NsisSilent       => NsisSilentArguments,
				_                                    => string.Empty
			};
		}

		/// <summary>
		/// Resolves the default daily cache file path under <c>%AppData%\VSLauncher</c>.
		/// </summary>
		private static string ResolveDefaultCachePath(IVsCodeEnvironment environment)
		{
			string appData = environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			return Path.Combine(appData, "VSLauncher", CacheFileName);
		}

		/// <summary>
		/// Reads local <c>product.json</c> and <c>package.json</c> metadata to determine commit hash, channel, scope, platform target, and <c>updateUrl</c>.
		/// </summary>
		private LocalInstallationInfo ReadLocalInstallationInfo(VsCodeInstance instance)
		{
			string? installDir       = Path.GetDirectoryName(instance.Location);
			JObject? productObj      = TryReadJsonObject(this.environment, installDir, "product.json");
			JObject? packageObj      = TryReadJsonObject(this.environment, installDir, "package.json");

			string rawCommit         = TryGetStringProperty(productObj, "commit") ?? string.Empty;
			string commit            = IsValidCommitToken(rawCommit) ? rawCommit.Trim() : string.Empty;
			string quality           = ResolveQuality(productObj, instance.IsInsiders);
			bool isSystem            = ResolveIsSystemInstall(productObj, instance);
			string version           = ResolveLocalVersion(packageObj, instance);
			string platform          = ResolvePlatformTarget(this.osArchitecture, isSystem);
			string productUpdateUrl  = TryGetStringProperty(productObj, "updateUrl")?.Trim() ?? string.Empty;

			return new LocalInstallationInfo(version, commit, quality, platform, isSystem, productUpdateUrl);
		}

		/// <summary>
		/// Attempts to read and parse a JSON file under <c>resources\app</c> inside an IDE installation directory.
		/// </summary>
		private static JObject? TryReadJsonObject(IVsCodeEnvironment targetEnvironment, string? installDir, string fileName)
		{
			if (string.IsNullOrWhiteSpace(installDir))
			{
				return null;
			}

			string filePath = Path.Combine(installDir, "resources", "app", fileName);

			if (!targetEnvironment.FileExists(filePath))
			{
				return null;
			}

			try
			{
				string content = targetEnvironment.ReadAllText(filePath);

				return JObject.Parse(content);
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);

				return null;
			}
		}

		/// <summary>
		/// Determines the update quality channel (<c>stable</c> or <c>insider</c>).
		/// </summary>
		private static string ResolveQuality(JObject? productObj, bool isInsiders)
		{
			string? productQuality = TryGetStringProperty(productObj, "quality")?.Trim().ToLowerInvariant();

			if (productQuality is "stable" or "insider")
			{
				return productQuality;
			}

			return isInsiders ? "insider" : "stable";
		}

		/// <summary>
		/// Determines whether the installation is a machine-wide System setup rather than a per-user setup.
		/// </summary>
		private bool ResolveIsSystemInstall(JObject? productObj, VsCodeInstance instance)
		{
			string? target = TryGetStringProperty(productObj, "target")?.Trim();

			if (string.Equals(target, "system", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (string.Equals(target, "user", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (instance.IsSystemInstall || instance.Identifier.EndsWith("-system", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (instance.Identifier.EndsWith("-user", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return IsUnderProgramFiles(instance.Location);
		}

		/// <summary>
		/// Checks whether an executable path resides inside Program Files or Program Files (x86).
		/// </summary>
		private bool IsUnderProgramFiles(string executablePath)
		{
			string programFiles   = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string programFiles86 = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

			return IsSubPath(executablePath, programFiles) || IsSubPath(executablePath, programFiles86);
		}

		/// <summary>
		/// Determines whether <paramref name="candidatePath"/> is located inside <paramref name="rootFolder"/>.
		/// </summary>
		private static bool IsSubPath(string candidatePath, string? rootFolder)
		{
			if (string.IsNullOrWhiteSpace(rootFolder))
			{
				return false;
			}

			string normalizedRoot = Path.TrimEndingDirectorySeparator(rootFolder) + Path.DirectorySeparatorChar;

			return candidatePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Resolves the best-available installed version from <c>package.json</c>, file version info, or the instance metadata.
		/// </summary>
		private string ResolveLocalVersion(JObject? packageObj, VsCodeInstance instance)
		{
			string? packageVersion = TryGetStringProperty(packageObj, "version")?.Trim();

			if (!string.IsNullOrWhiteSpace(packageVersion))
			{
				return packageVersion;
			}

			string? fileVersion = this.environment.GetFileVersion(instance.Location)?.Trim();

			if (!string.IsNullOrWhiteSpace(fileVersion))
			{
				return fileVersion;
			}

			return instance.Version;
		}

		/// <summary>
		/// Maps OS architecture and install scope to the VS Code update API platform identifier.
		/// </summary>
		private static string ResolvePlatformTarget(Architecture architecture, bool isSystemInstall)
		{
			string archPrefix = architecture switch
			{
				Architecture.Arm64 => "win32-arm64",
				Architecture.X86   => "win32",
				_                  => "win32-x64"
			};

			return isSystemInstall ? archPrefix : $"{archPrefix}-user";
		}

		/// <summary>
		/// Resolves the effective update-checking strategy for <paramref name="instance"/>, rejecting placeholder or invalid <c>updateUrl</c> endpoints.
		/// </summary>
		private static VsCodeUpdateStrategyKind ResolveEffectiveStrategy(VsCodeInstance instance, LocalInstallationInfo localInfo)
		{
			switch (instance.UpdateStrategy)
			{
				case VsCodeUpdateStrategyKind.VsCodeApi:
					return ResolveVsCodeApiBaseUrl(instance, localInfo) is not null
						? VsCodeUpdateStrategyKind.VsCodeApi
						: VsCodeUpdateStrategyKind.Unsupported;

				case VsCodeUpdateStrategyKind.GitHubReleases:
					return IsValidGitHubRepo(instance.GitHubRepo)
						? VsCodeUpdateStrategyKind.GitHubReleases
						: VsCodeUpdateStrategyKind.Unsupported;

				default:
					return VsCodeUpdateStrategyKind.Unsupported;
			}
		}

		/// <summary>
		/// Resolves and validates the VS Code update API base URL for official VS Code builds or fork <c>product.json</c> <c>updateUrl</c> endpoints.
		/// </summary>
		private static string? ResolveVsCodeApiBaseUrl(VsCodeInstance instance, LocalInstallationInfo localInfo)
		{
			if (IsOfficialMicrosoftFlavor(instance.FlavorId))
			{
				return UpdateApiBaseUrl;
			}

			if (!IsValidUpdateEndpointUrl(localInfo.ProductUpdateUrl))
			{
				return null;
			}

			string trimmed = localInfo.ProductUpdateUrl.Trim().TrimEnd('/');

			return trimmed.EndsWith("/api/update", StringComparison.OrdinalIgnoreCase)
				? trimmed
				: $"{trimmed}/api/update";
		}

		/// <summary>
		/// Returns whether <paramref name="flavorId"/> represents an official Microsoft VS Code edition.
		/// </summary>
		private static bool IsOfficialMicrosoftFlavor(string flavorId)
		{
			return flavorId.Equals("vscode-stable", StringComparison.OrdinalIgnoreCase)
				|| flavorId.Equals("vscode-insiders", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks whether <paramref name="gitHubRepo"/> is a safe <c>owner/repo</c> slug.
		/// </summary>
		private static bool IsValidGitHubRepo(string? gitHubRepo)
		{
			return !string.IsNullOrWhiteSpace(gitHubRepo) && GitHubRepoRegex.IsMatch(gitHubRepo.Trim());
		}

		/// <summary>
		/// Checks whether a normalized lowercase hostname is trusted for <paramref name="instance"/> via its allowlisted hosts or validated <c>product.json</c> <c>updateUrl</c>.
		/// </summary>
		private static bool IsHostTrustedForInstance(string host, VsCodeInstance instance)
		{
			foreach (string trustedHost in instance.TrustedDownloadHosts)
			{
				string normalized = trustedHost.Trim().TrimEnd('.').ToLowerInvariant();

				if (!string.IsNullOrEmpty(normalized) && host.Equals(normalized, StringComparison.Ordinal))
				{
					return true;
				}
			}

			if (instance.UpdateStrategy == VsCodeUpdateStrategyKind.GitHubReleases
				&& instance.TrustedDownloadHosts.Count == 0
				&& DefaultGitHubDownloadHosts.Contains(host))
			{
				return true;
			}

			return MatchesProductUpdateUrlHost(host, instance);
		}

		/// <summary>
		/// Checks whether <paramref name="host"/> matches or is a subdomain of a validated <c>product.json</c> <c>updateUrl</c> host for <paramref name="instance"/>.
		/// </summary>
		private static bool MatchesProductUpdateUrlHost(string host, VsCodeInstance instance)
		{
			string? installDir  = Path.GetDirectoryName(instance.Location);
			JObject? productObj = TryReadJsonObject(instance.Environment, installDir, "product.json");
			string? updateUrl   = TryGetStringProperty(productObj, "updateUrl")?.Trim();

			if (!IsValidUpdateEndpointUrl(updateUrl) || !Uri.TryCreate(updateUrl, UriKind.Absolute, out Uri? updateUri))
			{
				return false;
			}

			string updateHost = updateUri.Host.TrimEnd('.').ToLowerInvariant();

			if (host.Equals(updateHost, StringComparison.Ordinal))
			{
				return true;
			}

			return updateHost.Contains('.') && host.EndsWith($".{updateHost}", StringComparison.Ordinal);
		}

		/// <summary>
		/// Attempts to satisfy an update check from the persisted daily cache when the entry is within 24 hours and still matches the local install.
		/// </summary>
		private VsCodeUpdateInfo? TryResolveFreshCachedUpdate(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset nowUtc)
		{
			Dictionary<string, VsCodeUpdateInfo> cache = LoadCache();

			if (!cache.TryGetValue(instance.Identifier, out VsCodeUpdateInfo? cached)
				|| !IsCacheEntryFresh(cached, nowUtc)
				|| cached.UpdateCheckStatus == VsCodeUpdateCheckStatus.Unsupported)
			{
				return null;
			}

			if (HasLocalUpdatedToCachedTarget(localInfo, cached))
			{
				VsCodeUpdateInfo refreshed = CreateUpToDateInfo(instance, localInfo, cached.CheckedAtUtc);

				SaveCacheEntry(instance.Identifier, refreshed);

				return refreshed;
			}

			if (cached.IsUpdateAvailable && !IsTrustedDownloadUrl(cached.DownloadUrl, instance))
			{
				return null;
			}

			return IsLocalMatchingCachedSnapshot(localInfo, cached) ? cached : null;
		}

		/// <summary>
		/// Checks whether a cached entry succeeded and falls within the 24-hour freshness window.
		/// </summary>
		private static bool IsCacheEntryFresh(VsCodeUpdateInfo cached, DateTimeOffset nowUtc)
		{
			if (!cached.CheckSucceeded)
			{
				return false;
			}

			if (cached.CheckedAtUtc > nowUtc + ClockSkewTolerance)
			{
				return false;
			}

			return (nowUtc - cached.CheckedAtUtc) < DefaultCheckInterval;
		}

		/// <summary>
		/// Determines whether the local installation now matches the target commit or version of a previously cached available update.
		/// </summary>
		private static bool HasLocalUpdatedToCachedTarget(LocalInstallationInfo localInfo, VsCodeUpdateInfo cached)
		{
			if (!cached.IsUpdateAvailable)
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(localInfo.Commit) && !string.IsNullOrWhiteSpace(cached.LatestCommit))
			{
				return localInfo.Commit.Equals(cached.LatestCommit, StringComparison.OrdinalIgnoreCase);
			}

			if (!string.IsNullOrWhiteSpace(localInfo.Version) && !string.IsNullOrWhiteSpace(cached.LatestVersion))
			{
				return CompareVersions(localInfo.Version, cached.LatestVersion) >= 0;
			}

			return false;
		}

		/// <summary>
		/// Checks whether the local commit/version still matches the installation state captured when the cache entry was stored.
		/// </summary>
		private static bool IsLocalMatchingCachedSnapshot(LocalInstallationInfo localInfo, VsCodeUpdateInfo cached)
		{
			if (!string.IsNullOrWhiteSpace(localInfo.Commit) || !string.IsNullOrWhiteSpace(cached.CurrentCommit))
			{
				return string.Equals(localInfo.Commit, cached.CurrentCommit, StringComparison.OrdinalIgnoreCase);
			}

			return string.Equals(localInfo.Version, cached.CurrentVersion, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Dispatches the remote update check according to <paramref name="strategy"/>.
		/// </summary>
		private Task<VsCodeUpdateInfo> QueryRemoteUpdateAsync(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			VsCodeUpdateStrategyKind strategy,
			DateTimeOffset nowUtc,
			CancellationToken cancellationToken)
		{
			return strategy switch
			{
				VsCodeUpdateStrategyKind.VsCodeApi      => QueryVsCodeApiAsync(instance, localInfo, nowUtc, cancellationToken),
				VsCodeUpdateStrategyKind.GitHubReleases => QueryGitHubReleaseAsync(instance, localInfo, nowUtc, cancellationToken),
				_                                       => Task.FromResult(CreateUnsupportedInfo(instance, localInfo, nowUtc))
			};
		}

		/// <summary>
		/// Queries a VS Code-compatible update API endpoint and converts the response into a <see cref="VsCodeUpdateInfo"/>.
		/// </summary>
		private async Task<VsCodeUpdateInfo> QueryVsCodeApiAsync(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset nowUtc,
			CancellationToken cancellationToken)
		{
			string? baseUrl = ResolveVsCodeApiBaseUrl(instance, localInfo);

			if (string.IsNullOrWhiteSpace(baseUrl))
			{
				return CreateUnsupportedInfo(instance, localInfo, nowUtc);
			}

			bool queriedByCommit = !string.IsNullOrWhiteSpace(localInfo.Commit);
			string commitSegment = queriedByCommit ? Uri.EscapeDataString(localInfo.Commit) : "latest";
			string requestUrl    = $"{baseUrl}/{localInfo.Platform}/{localInfo.Quality}/{commitSegment}";

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			timeoutCts.CancelAfter(MetadataRequestTimeout);

			try
			{
				using HttpResponseMessage response = await this.httpClient.GetAsync(requestUrl, timeoutCts.Token).ConfigureAwait(false);

				if (response.StatusCode == HttpStatusCode.NoContent)
				{
					return CreateUpToDateInfo(instance, localInfo, nowUtc);
				}

				if (response.StatusCode != HttpStatusCode.OK)
				{
					return CreateFailedInfo(instance, localInfo, nowUtc);
				}

				string json = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);

				return ParseVsCodeApiPayload(instance, localInfo, json, queriedByCommit, nowUtc);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
			{
				Debug.WriteLine(ex);

				return CreateFailedInfo(instance, localInfo, nowUtc);
			}
		}

		/// <summary>
		/// Queries the GitHub Releases <c>/releases/latest</c> endpoint and parses matching Windows installer metadata.
		/// </summary>
		private async Task<VsCodeUpdateInfo> QueryGitHubReleaseAsync(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset nowUtc,
			CancellationToken cancellationToken)
		{
			if (!IsValidGitHubRepo(instance.GitHubRepo))
			{
				return CreateUnsupportedInfo(instance, localInfo, nowUtc);
			}

			string requestUrl    = $"{GitHubApiBaseUrl}/{instance.GitHubRepo.Trim()}/releases/latest";
			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			timeoutCts.CancelAfter(MetadataRequestTimeout);

			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

				using HttpResponseMessage response = await this.httpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);

				if (response.StatusCode != HttpStatusCode.OK)
				{
					return CreateFailedInfo(instance, localInfo, nowUtc);
				}

				string json = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);

				return ParseGitHubReleasePayload(instance, localInfo, json, nowUtc);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
			{
				Debug.WriteLine(ex);

				return CreateFailedInfo(instance, localInfo, nowUtc);
			}
		}

		/// <summary>
		/// Parses an HTTP 200 VS Code update API JSON response and validates its download URL and version/commit progression.
		/// </summary>
		private static VsCodeUpdateInfo ParseVsCodeApiPayload(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			string json,
			bool queriedByCommit,
			DateTimeOffset nowUtc)
		{
			JObject root         = JObject.Parse(json);
			string downloadUrl   = TryGetStringProperty(root, "url")?.Trim() ?? string.Empty;
			string latestCommit  = TryGetStringProperty(root, "version")?.Trim() ?? string.Empty;
			string latestVersion = TryGetStringProperty(root, "productVersion")?.Trim()
				?? TryGetStringProperty(root, "name")?.Trim()
				?? string.Empty;
			string sha256Hash    = TryGetStringProperty(root, "sha256hash")?.Trim() ?? string.Empty;

			if (!IsTrustedDownloadUrl(downloadUrl, instance))
			{
				return CreateFailedInfo(instance, localInfo, nowUtc);
			}

			bool isNewer = DetermineIsNewer(localInfo, latestVersion, latestCommit, queriedByCommit);

			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instance.Identifier,
				IsUpdateAvailable  = isNewer,
				UpdateCheckStatus  = isNewer ? VsCodeUpdateCheckStatus.UpdateAvailable : VsCodeUpdateCheckStatus.UpToDate,
				InstallerKind      = instance.InstallerKind,
				CurrentVersion     = localInfo.Version,
				CurrentCommit      = localInfo.Commit,
				LatestVersion      = string.IsNullOrWhiteSpace(latestVersion) ? localInfo.Version : latestVersion,
				LatestCommit       = latestCommit,
				DownloadUrl        = isNewer ? downloadUrl : string.Empty,
				Sha256Hash         = isNewer ? sha256Hash : string.Empty,
				IsSystemInstall    = localInfo.IsSystemInstall,
				CheckedAtUtc       = nowUtc,
				CheckSucceeded     = true
			};
		}

		/// <summary>
		/// Parses a GitHub <c>releases/latest</c> JSON response, compares release versions, and selects the matching Windows installer asset.
		/// </summary>
		private VsCodeUpdateInfo ParseGitHubReleasePayload(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			string json,
			DateTimeOffset nowUtc)
		{
			JObject root         = JObject.Parse(json);
			string tagName       = TryGetStringProperty(root, "tag_name")?.Trim() ?? string.Empty;
			string latestVersion = NormalizeVersionTag(tagName);

			if (string.IsNullOrWhiteSpace(latestVersion))
			{
				return CreateFailedInfo(instance, localInfo, nowUtc);
			}

			if (CompareVersions(latestVersion, localInfo.Version) <= 0)
			{
				return CreateUpToDateInfo(instance, localInfo, nowUtc, latestVersion);
			}

			GitHubAssetCandidate? asset = SelectBestGitHubAsset(
				root["assets"] as JArray,
				instance,
				localInfo.IsSystemInstall,
				this.osArchitecture);

			if (asset is null)
			{
				return CreateFailedInfo(instance, localInfo, nowUtc);
			}

			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instance.Identifier,
				IsUpdateAvailable  = true,
				UpdateCheckStatus  = VsCodeUpdateCheckStatus.UpdateAvailable,
				InstallerKind      = instance.InstallerKind,
				CurrentVersion     = localInfo.Version,
				CurrentCommit      = localInfo.Commit,
				LatestVersion      = latestVersion,
				LatestCommit       = string.Empty,
				DownloadUrl        = asset.DownloadUrl,
				Sha256Hash         = asset.Sha256Hash,
				IsSystemInstall    = localInfo.IsSystemInstall,
				CheckedAtUtc       = nowUtc,
				CheckSucceeded     = true
			};
		}

		/// <summary>
		/// Selects the best-matching trusted Windows <c>.exe</c> installer asset from a GitHub release <c>assets</c> array.
		/// </summary>
		private static GitHubAssetCandidate? SelectBestGitHubAsset(
			JArray? assets,
			VsCodeInstance instance,
			bool isSystemInstall,
			Architecture architecture)
		{
			if (assets is null)
			{
				return null;
			}

			GitHubAssetCandidate? bestCandidate = null;

			foreach (JToken token in assets)
			{
				if (token is not JObject assetObj)
				{
					continue;
				}

				if (!TryScoreGitHubAsset(assetObj, instance, isSystemInstall, architecture, out GitHubAssetCandidate? candidate))
				{
					continue;
				}

				if (bestCandidate is null || candidate!.Score > bestCandidate.Score)
				{
					bestCandidate = candidate;
				}
			}

			return bestCandidate;
		}

		/// <summary>
		/// Validates and scores a single GitHub release asset object for Windows architecture and install scope compatibility.
		/// </summary>
		private static bool TryScoreGitHubAsset(
			JObject assetObj,
			VsCodeInstance instance,
			bool isSystemInstall,
			Architecture architecture,
			out GitHubAssetCandidate? candidate)
		{
			candidate = null;

			string name        = TryGetStringProperty(assetObj, "name")?.Trim() ?? string.Empty;
			string downloadUrl = TryGetStringProperty(assetObj, "browser_download_url")?.Trim() ?? string.Empty;

			if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !IsTrustedDownloadUrl(downloadUrl, instance))
			{
				return false;
			}

			string lowerName = name.ToLowerInvariant();

			if (!TryScoreArchitecture(lowerName, architecture, out int archScore))
			{
				return false;
			}

			int totalScore    = archScore + ComputeScopeScore(lowerName, isSystemInstall);
			string sha256Hash = ExtractGitHubAssetSha256(assetObj);

			if (lowerName.Contains("setup", StringComparison.Ordinal) || lowerName.Contains("installer", StringComparison.Ordinal))
			{
				totalScore += 2;
			}

			candidate = new GitHubAssetCandidate(downloadUrl, sha256Hash, totalScore);

			return true;
		}

		/// <summary>
		/// Evaluates whether a lowercase installer filename matches <paramref name="architecture"/> and assigns a priority score.
		/// </summary>
		private static bool TryScoreArchitecture(string lowerName, Architecture architecture, out int score)
		{
			bool hasArm64 = lowerName.Contains("arm64", StringComparison.Ordinal) || lowerName.Contains("aarch64", StringComparison.Ordinal);
			bool hasX64   = lowerName.Contains("x64", StringComparison.Ordinal)
				|| lowerName.Contains("amd64", StringComparison.Ordinal)
				|| lowerName.Contains("x86_64", StringComparison.Ordinal)
				|| lowerName.Contains("win64", StringComparison.Ordinal);
			bool hasX86   = lowerName.Contains("ia32", StringComparison.Ordinal)
				|| (lowerName.Contains("x86", StringComparison.Ordinal) && !lowerName.Contains("x86_64", StringComparison.Ordinal));

			score = 0;

			switch (architecture)
			{
				case Architecture.Arm64:
					if (!hasArm64 || hasX64 || hasX86)
					{
						return false;
					}

					score = 20;

					return true;

				case Architecture.X86:
					if (!hasX86 || hasX64 || hasArm64)
					{
						return false;
					}

					score = 20;

					return true;

				default:
					if (hasArm64 || hasX86)
					{
						return false;
					}

					score = hasX64 ? 20 : 5;

					return true;
			}
		}

		/// <summary>
		/// Scores a lowercase installer filename by whether it matches User vs. System installation scope.
		/// </summary>
		private static int ComputeScopeScore(string lowerName, bool isSystemInstall)
		{
			bool hasUserToken   = lowerName.Contains("user", StringComparison.Ordinal);
			bool hasSystemToken = lowerName.Contains("system", StringComparison.Ordinal);

			if (!isSystemInstall)
			{
				if (hasUserToken)
				{
					return 10;
				}

				return hasSystemToken ? -10 : 0;
			}

			if (hasSystemToken)
			{
				return 10;
			}

			return hasUserToken ? -10 : 5;
		}

		/// <summary>
		/// Extracts an optional lowercase SHA-256 hex digest from a GitHub release asset's <c>digest</c> field.
		/// </summary>
		private static string ExtractGitHubAssetSha256(JObject assetObj)
		{
			string? digest = TryGetStringProperty(assetObj, "digest")?.Trim();

			if (string.IsNullOrWhiteSpace(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}

			string hex = digest["sha256:".Length..].Trim();

			return hex.Length == 64 ? hex.ToLowerInvariant() : string.Empty;
		}

		/// <summary>
		/// Strips an optional leading <c>v</c> prefix from a GitHub release tag name.
		/// </summary>
		private static string NormalizeVersionTag(string? tagName)
		{
			if (string.IsNullOrWhiteSpace(tagName))
			{
				return string.Empty;
			}

			string trimmed = tagName.Trim();

			if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase) && trimmed.Length > 1 && char.IsAsciiDigit(trimmed[1]))
			{
				return trimmed[1..];
			}

			return trimmed;
		}

		/// <summary>
		/// Determines whether the remote release is newer than the locally installed build.
		/// </summary>
		private static bool DetermineIsNewer(
			LocalInstallationInfo localInfo,
			string latestVersion,
			string latestCommit,
			bool queriedByCommit)
		{
			if (queriedByCommit && !string.IsNullOrWhiteSpace(latestCommit))
			{
				return !latestCommit.Equals(localInfo.Commit, StringComparison.OrdinalIgnoreCase);
			}

			return CompareVersions(latestVersion, localInfo.Version) > 0;
		}

		/// <summary>
		/// Streams a remote file to <paramref name="destinationPath"/> while computing its lowercase SHA-256 hex digest.
		/// </summary>
		private async Task<string> DownloadFileAndComputeSha256Async(
			string downloadUrl,
			string destinationPath,
			CancellationToken cancellationToken)
		{
			using HttpResponseMessage response = await this.httpClient
				.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
				.ConfigureAwait(false);

			response.EnsureSuccessStatusCode();

			await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			await using var fileStream        = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, DownloadBufferSize, useAsync: true);
			using var hasher                  = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

			byte[] buffer = new byte[DownloadBufferSize];
			int bytesRead;

			while ((bytesRead = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
			{
				await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
				hasher.AppendData(buffer, 0, bytesRead);
			}

			await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

			return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
		}

		/// <summary>
		/// Builds a sanitized temporary filename for a downloaded installer.
		/// </summary>
		private static string BuildTempInstallerFileName(VsCodeUpdateInfo updateInfo)
		{
			string scopeTag = updateInfo.IsSystemInstall ? "System" : "User";
			string suffix   = Guid.NewGuid().ToString("N")[..12];

			return $"VSCodeUpdate_{scopeTag}_{suffix}.exe";
		}

		/// <summary>
		/// Loads the persisted per-edition update cache dictionary from disk, returning an empty dictionary on absence or corruption.
		/// </summary>
		private Dictionary<string, VsCodeUpdateInfo> LoadCache()
		{
			lock (this.cacheLock)
			{
				if (!File.Exists(this.cacheFilePath))
				{
					return new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);
				}

				try
				{
					string json = File.ReadAllText(this.cacheFilePath);
					var raw     = JsonConvert.DeserializeObject<Dictionary<string, VsCodeUpdateInfo>>(json);

					return raw is null
						? new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase)
						: new Dictionary<string, VsCodeUpdateInfo>(raw, StringComparer.OrdinalIgnoreCase);
				}
				catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
				{
					Debug.WriteLine(ex);

					return new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);
				}
			}
		}

		/// <summary>
		/// Persists a single edition's <see cref="VsCodeUpdateInfo"/> to the daily cache file atomically.
		/// </summary>
		private void SaveCacheEntry(string instanceIdentifier, VsCodeUpdateInfo entry)
		{
			lock (this.cacheLock)
			{
				try
				{
					Dictionary<string, VsCodeUpdateInfo> cache = LoadCache();

					cache[instanceIdentifier] = entry;

					string? parentDir = Path.GetDirectoryName(this.cacheFilePath);

					if (!string.IsNullOrWhiteSpace(parentDir))
					{
						Directory.CreateDirectory(parentDir);
					}

					string tempPath = $"{this.cacheFilePath}.{Guid.NewGuid():N}.tmp";
					string json     = JsonConvert.SerializeObject(cache, Formatting.Indented);

					File.WriteAllText(tempPath, json);
					File.Move(tempPath, this.cacheFilePath, overwrite: true);
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
				{
					Debug.WriteLine(ex);
				}
			}
		}

		/// <summary>
		/// Creates a succeeded <see cref="VsCodeUpdateInfo"/> representing an up-to-date installation.
		/// </summary>
		private static VsCodeUpdateInfo CreateUpToDateInfo(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset checkedAtUtc,
			string? latestVersion = null)
		{
			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instance.Identifier,
				IsUpdateAvailable  = false,
				UpdateCheckStatus  = VsCodeUpdateCheckStatus.UpToDate,
				InstallerKind      = instance.InstallerKind,
				CurrentVersion     = localInfo.Version,
				CurrentCommit      = localInfo.Commit,
				LatestVersion      = string.IsNullOrWhiteSpace(latestVersion) ? localInfo.Version : latestVersion,
				LatestCommit       = localInfo.Commit,
				IsSystemInstall    = localInfo.IsSystemInstall,
				CheckedAtUtc       = checkedAtUtc,
				CheckSucceeded     = true
			};
		}

		/// <summary>
		/// Creates a non-failing <see cref="VsCodeUpdateInfo"/> indicating that automated update checks are unsupported for the target IDE.
		/// </summary>
		private static VsCodeUpdateInfo CreateUnsupportedInfo(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset checkedAtUtc)
		{
			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instance.Identifier,
				IsUpdateAvailable  = false,
				UpdateCheckStatus  = VsCodeUpdateCheckStatus.Unsupported,
				InstallerKind      = instance.InstallerKind,
				CurrentVersion     = localInfo.Version,
				CurrentCommit      = localInfo.Commit,
				LatestVersion      = localInfo.Version,
				LatestCommit       = localInfo.Commit,
				IsSystemInstall    = localInfo.IsSystemInstall,
				CheckedAtUtc       = checkedAtUtc,
				CheckSucceeded     = true
			};
		}

		/// <summary>
		/// Creates a non-throwing failed <see cref="VsCodeUpdateInfo"/> when an update check cannot be completed.
		/// </summary>
		private static VsCodeUpdateInfo CreateFailedInfo(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset checkedAtUtc)
		{
			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instance.Identifier,
				IsUpdateAvailable  = false,
				UpdateCheckStatus  = VsCodeUpdateCheckStatus.Failed,
				InstallerKind      = instance.InstallerKind,
				CurrentVersion     = localInfo.Version,
				CurrentCommit      = localInfo.Commit,
				IsSystemInstall    = localInfo.IsSystemInstall,
				CheckedAtUtc       = checkedAtUtc,
				CheckSucceeded     = false
			};
		}

		/// <summary>
		/// Safely reads a string property from a nullable <see cref="JObject"/>.
		/// </summary>
		private static string? TryGetStringProperty(JObject? obj, string propertyName)
		{
			return obj?[propertyName] is JValue { Type: JTokenType.String, Value: string text } ? text : null;
		}

		/// <summary>
		/// Validates that a commit string is an ASCII alphanumeric token safe to place in a URL path segment.
		/// </summary>
		private static bool IsValidCommitToken(string commit)
		{
			string trimmed = commit.Trim();

			if (trimmed.Length < 7 || trimmed.Length > 64)
			{
				return false;
			}

			foreach (char ch in trimmed)
			{
				if (!char.IsAsciiLetterOrDigit(ch))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Splits the numeric prefix of a semantic version string into integer components.
		/// </summary>
		private static int[] ParseNumericVersionParts(string? version)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				return Array.Empty<int>();
			}

			string trimmed    = NormalizeVersionTag(version);
			int suffixIndex   = trimmed.IndexOfAny(new[] { '-', '+' });
			string core       = suffixIndex >= 0 ? trimmed[..suffixIndex] : trimmed;
			string[] segments = core.Split('.', StringSplitOptions.RemoveEmptyEntries);
			int[] parts       = new int[segments.Length];

			for (int i = 0; i < segments.Length; i++)
			{
				parts[i] = int.TryParse(segments[i], NumberStyles.None, CultureInfo.InvariantCulture, out int value)
					? value
					: 0;
			}

			return parts;
		}

		/// <summary>
		/// Captures local installation metadata required to resolve an update strategy and construct update API requests.
		/// </summary>
		private sealed record LocalInstallationInfo(
			string Version,
			string Commit,
			string Quality,
			string Platform,
			bool IsSystemInstall,
			string ProductUpdateUrl);

		/// <summary>
		/// Represents a candidate Windows installer asset parsed from a GitHub release payload.
		/// </summary>
		private sealed record GitHubAssetCandidate(
			string DownloadUrl,
			string Sha256Hash,
			int Score);
	}
}
