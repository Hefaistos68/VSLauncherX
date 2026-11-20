using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Checks daily for Visual Studio Code updates via Microsoft's official update service, persists per-edition daily check state,
	/// and downloads/verifies official Windows installers for in-place updates.
	/// </summary>
	public sealed class VsCodeUpdateService : IDisposable
	{
		/// <summary>
		/// The default minimum interval between remote update API checks for a given VS Code edition.
		/// </summary>
		public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(24);

		private const string UpdateApiBaseUrl          = "https://update.code.visualstudio.com/api/update";
		private const string InstallerSilentArguments  = "/SILENT /NORESTART /CLOSEAPPLICATIONS /MERGETASKS=!runcode";
		private const string CacheFileName             = "vscode-update-cache.json";
		private const int DownloadBufferSize           = 81920;

		private static readonly TimeSpan MetadataRequestTimeout = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan ClockSkewTolerance     = TimeSpan.FromMinutes(5);

		private static readonly HashSet<string> TrustedDownloadHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"vscode.download.prss.microsoft.com",
			"update.code.visualstudio.com",
			"az764295.vo.msecnd.net"
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
		/// <param name="instance">The discovered VS Code installation to check.</param>
		/// <param name="forceRefresh">When <c>true</c>, bypasses the daily cache and queries the update API immediately.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A <see cref="VsCodeUpdateInfo"/> describing update availability.</returns>
		public async Task<VsCodeUpdateInfo> CheckForUpdateAsync(
			VsCodeInstance instance,
			bool forceRefresh = false,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(instance);

			DateTimeOffset nowUtc            = this.utcNowProvider();
			LocalInstallationInfo localInfo  = ReadLocalInstallationInfo(instance);

			if (!forceRefresh)
			{
				VsCodeUpdateInfo? cached = TryResolveFreshCachedUpdate(instance.Identifier, localInfo, nowUtc);

				if (cached is not null)
				{
					return cached;
				}
			}

			VsCodeUpdateInfo remoteResult = await QueryRemoteUpdateAsync(instance, localInfo, nowUtc, cancellationToken).ConfigureAwait(false);

			if (remoteResult.CheckSucceeded)
			{
				SaveCacheEntry(instance.Identifier, remoteResult);
			}

			return remoteResult;
		}

		/// <summary>
		/// Downloads the official VS Code installer described by <paramref name="updateInfo"/> to a temporary file and verifies its SHA-256 hash when provided.
		/// </summary>
		/// <param name="updateInfo">The update metadata containing the download URL and expected hash.</param>
		/// <param name="cancellationToken">A token to cancel the download.</param>
		/// <returns>The full path to the downloaded and verified installer executable.</returns>
		public async Task<string> DownloadInstallerAsync(VsCodeUpdateInfo updateInfo, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(updateInfo);

			if (!updateInfo.IsUpdateAvailable || !IsTrustedDownloadUrl(updateInfo.DownloadUrl))
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
			ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);

			return new ProcessStartInfo
			{
				FileName        = installerPath,
				Arguments       = InstallerSilentArguments,
				UseShellExecute = true,
				Verb            = isSystemInstall ? "runas" : string.Empty,
				ErrorDialog     = true
			};
		}

		/// <summary>
		/// Determines whether a download URL uses HTTPS and points to a trusted Microsoft VS Code download host.
		/// </summary>
		/// <param name="url">The candidate download URL.</param>
		/// <returns><c>true</c> if the URL is HTTPS on an allowlisted Microsoft host; otherwise <c>false</c>.</returns>
		public static bool IsTrustedDownloadUrl(string? url)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
			{
				return false;
			}

			return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
				&& TrustedDownloadHosts.Contains(uri.Host);
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
		/// Resolves the default daily cache file path under <c>%AppData%\VSLauncher</c>.
		/// </summary>
		private static string ResolveDefaultCachePath(IVsCodeEnvironment environment)
		{
			string appData = environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			return Path.Combine(appData, "VSLauncher", CacheFileName);
		}

		/// <summary>
		/// Reads local <c>product.json</c> and <c>package.json</c> metadata to determine commit hash, channel, scope, and platform target.
		/// </summary>
		private LocalInstallationInfo ReadLocalInstallationInfo(VsCodeInstance instance)
		{
			string? installDir  = Path.GetDirectoryName(instance.Location);
			JObject? productObj = TryReadJsonObject(installDir, "product.json");
			JObject? packageObj = TryReadJsonObject(installDir, "package.json");

			string rawCommit    = TryGetStringProperty(productObj, "commit") ?? string.Empty;
			string commit       = IsValidCommitToken(rawCommit) ? rawCommit.Trim() : string.Empty;
			string quality      = ResolveQuality(productObj, instance.IsInsiders);
			bool isSystem       = ResolveIsSystemInstall(productObj, instance);
			string version      = ResolveLocalVersion(packageObj, instance);
			string platform     = ResolvePlatformTarget(this.osArchitecture, isSystem);

			return new LocalInstallationInfo(version, commit, quality, platform, isSystem);
		}

		/// <summary>
		/// Attempts to read and parse a JSON file under <c>resources\app</c> inside a VS Code installation directory.
		/// </summary>
		private JObject? TryReadJsonObject(string? installDir, string fileName)
		{
			if (string.IsNullOrWhiteSpace(installDir))
			{
				return null;
			}

			string filePath = Path.Combine(installDir, "resources", "app", fileName);

			if (!this.environment.FileExists(filePath))
			{
				return null;
			}

			try
			{
				string content = this.environment.ReadAllText(filePath);

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

			if (instance.Identifier.EndsWith("-system", StringComparison.OrdinalIgnoreCase))
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
		/// Maps OS architecture and install scope to the official VS Code update API platform identifier.
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
		/// Attempts to satisfy an update check from the persisted daily cache when the entry is within 24 hours and still matches the local install.
		/// </summary>
		private VsCodeUpdateInfo? TryResolveFreshCachedUpdate(
			string instanceIdentifier,
			LocalInstallationInfo localInfo,
			DateTimeOffset nowUtc)
		{
			Dictionary<string, VsCodeUpdateInfo> cache = LoadCache();

			if (!cache.TryGetValue(instanceIdentifier, out VsCodeUpdateInfo? cached) || !IsCacheEntryFresh(cached, nowUtc))
			{
				return null;
			}

			if (HasLocalUpdatedToCachedTarget(localInfo, cached))
			{
				VsCodeUpdateInfo refreshed = CreateUpToDateInfo(instanceIdentifier, localInfo, cached.CheckedAtUtc);

				SaveCacheEntry(instanceIdentifier, refreshed);

				return refreshed;
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
		/// Determines whether the local installation now matches the target commit of a previously cached available update.
		/// </summary>
		private static bool HasLocalUpdatedToCachedTarget(LocalInstallationInfo localInfo, VsCodeUpdateInfo cached)
		{
			return cached.IsUpdateAvailable
				&& !string.IsNullOrWhiteSpace(localInfo.Commit)
				&& !string.IsNullOrWhiteSpace(cached.LatestCommit)
				&& localInfo.Commit.Equals(cached.LatestCommit, StringComparison.OrdinalIgnoreCase);
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
		/// Queries Microsoft's VS Code update API and converts the response into a <see cref="VsCodeUpdateInfo"/>.
		/// </summary>
		private async Task<VsCodeUpdateInfo> QueryRemoteUpdateAsync(
			VsCodeInstance instance,
			LocalInstallationInfo localInfo,
			DateTimeOffset nowUtc,
			CancellationToken cancellationToken)
		{
			bool queriedByCommit = !string.IsNullOrWhiteSpace(localInfo.Commit);
			string commitSegment = queriedByCommit ? Uri.EscapeDataString(localInfo.Commit) : "latest";
			string requestUrl    = $"{UpdateApiBaseUrl}/{localInfo.Platform}/{localInfo.Quality}/{commitSegment}";

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			timeoutCts.CancelAfter(MetadataRequestTimeout);

			try
			{
				using HttpResponseMessage response = await this.httpClient.GetAsync(requestUrl, timeoutCts.Token).ConfigureAwait(false);

				if (response.StatusCode == HttpStatusCode.NoContent)
				{
					return CreateUpToDateInfo(instance.Identifier, localInfo, nowUtc);
				}

				if (response.StatusCode != HttpStatusCode.OK)
				{
					return CreateFailedInfo(instance.Identifier, localInfo, nowUtc);
				}

				string json = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);

				return ParseUpdatePayload(instance.Identifier, localInfo, json, queriedByCommit, nowUtc);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
			{
				Debug.WriteLine(ex);

				return CreateFailedInfo(instance.Identifier, localInfo, nowUtc);
			}
		}

		/// <summary>
		/// Parses an HTTP 200 update JSON response and validates its download URL and version/commit progression.
		/// </summary>
		private static VsCodeUpdateInfo ParseUpdatePayload(
			string instanceIdentifier,
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

			if (!IsTrustedDownloadUrl(downloadUrl))
			{
				return CreateFailedInfo(instanceIdentifier, localInfo, nowUtc);
			}

			bool isNewer = DetermineIsNewer(localInfo, latestVersion, latestCommit, queriedByCommit);

			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instanceIdentifier,
				IsUpdateAvailable  = isNewer,
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
		/// Builds a sanitized temporary filename for a downloaded VS Code installer.
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
			string instanceIdentifier,
			LocalInstallationInfo localInfo,
			DateTimeOffset checkedAtUtc)
		{
			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instanceIdentifier,
				IsUpdateAvailable  = false,
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
			string instanceIdentifier,
			LocalInstallationInfo localInfo,
			DateTimeOffset checkedAtUtc)
		{
			return new VsCodeUpdateInfo
			{
				InstanceIdentifier = instanceIdentifier,
				IsUpdateAvailable  = false,
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

			string trimmed    = version.Trim();
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
		/// Captures local installation metadata required to construct a VS Code update API request.
		/// </summary>
		private sealed record LocalInstallationInfo(
			string Version,
			string Commit,
			string Quality,
			string Platform,
			bool IsSystemInstall);
	}
}
