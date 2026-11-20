using System.Diagnostics;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Describes metadata, Windows discovery locations, and update capabilities for a VS Code-based IDE product.
	/// </summary>
	public sealed record VsCodeProductDescriptor
	{
		private static readonly Regex NonSlugCharsRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private static readonly string[] ReservedDomains =
		[
			"localhost",
			"local",
			"example",
			"invalid",
			"test",
			"example.com",
			"example.org",
			"example.net"
		];

		/// <summary>
		/// Gets the stable built-in descriptor for Visual Studio Code (Stable).
		/// </summary>
		public static VsCodeProductDescriptor VsCodeStable { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "vscode-stable",
			DisplayName           = "Visual Studio Code",
			ShortName             = "VS Code",
			ExecutableNames       = ["Code.exe"],
			UserRelativeDirs      = [@"Programs\Microsoft VS Code"],
			SystemRelativeDirs    = ["Microsoft VS Code"],
			UninstallNamePrefixes = ["Microsoft Visual Studio Code"],
			PathCommandNames      = ["code.cmd"],
			UserDataFolderName    = "Code",
			DownloadUrl           = "https://code.visualstudio.com/download",
			UpdateStrategy        = VsCodeUpdateStrategyKind.VsCodeApi,
			InstallerKind         = VsCodeInstallerKind.InnoSetupVsCode,
			TrustedDownloadHosts  = ["vscode.download.prss.microsoft.com", "update.code.visualstudio.com", "az764295.vo.msecnd.net"],
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the stable built-in descriptor for Visual Studio Code (Insiders).
		/// </summary>
		public static VsCodeProductDescriptor VsCodeInsiders { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "vscode-insiders",
			DisplayName           = "Visual Studio Code - Insiders",
			ShortName             = "VS Code Insiders",
			ExecutableNames       = ["Code - Insiders.exe"],
			UserRelativeDirs      = [@"Programs\Microsoft VS Code Insiders"],
			SystemRelativeDirs    = ["Microsoft VS Code Insiders"],
			UninstallNamePrefixes = ["Microsoft Visual Studio Code Insiders", "Microsoft Visual Studio Code - Insiders"],
			PathCommandNames      = ["code-insiders.cmd"],
			UserDataFolderName    = "Code - Insiders",
			DownloadUrl           = "https://code.visualstudio.com/insiders",
			UpdateStrategy        = VsCodeUpdateStrategyKind.VsCodeApi,
			InstallerKind         = VsCodeInstallerKind.InnoSetupVsCode,
			TrustedDownloadHosts  = ["vscode.download.prss.microsoft.com", "update.code.visualstudio.com", "az764295.vo.msecnd.net"],
			IsInsiders            = true
		};

		/// <summary>
		/// Gets the built-in descriptor for Antigravity IDE.
		/// </summary>
		private static VsCodeProductDescriptor AntigravityIde { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "antigravity-ide",
			DisplayName           = "Antigravity IDE",
			ShortName             = "Antigravity IDE",
			ExecutableNames       = ["Antigravity IDE.exe"],
			UserRelativeDirs      = [@"Programs\Antigravity IDE"],
			SystemRelativeDirs    = ["Antigravity IDE"],
			UninstallNamePrefixes = ["Antigravity IDE"],
			PathCommandNames      = ["antigravity-ide.cmd"],
			UserDataFolderName    = "Antigravity IDE",
			UpdateStrategy        = VsCodeUpdateStrategyKind.Unsupported,
			InstallerKind         = VsCodeInstallerKind.InnoSetupGeneric,
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the built-in descriptor for Cursor.
		/// </summary>
		private static VsCodeProductDescriptor Cursor { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "cursor",
			DisplayName           = "Cursor",
			ShortName             = "Cursor",
			ExecutableNames       = ["Cursor.exe"],
			UserRelativeDirs      = [@"Programs\cursor", @"Programs\Cursor"],
			SystemRelativeDirs    = ["Cursor", "cursor"],
			UninstallNamePrefixes = ["Cursor"],
			PathCommandNames      = ["cursor.cmd"],
			UserDataFolderName    = "Cursor",
			DownloadUrl           = "https://cursor.com/downloads",
			UpdateStrategy        = VsCodeUpdateStrategyKind.VsCodeApi,
			InstallerKind         = VsCodeInstallerKind.InnoSetupGeneric,
			TrustedDownloadHosts  = ["downloader.cursor.sh", "downloads.cursor.com", "api2.cursor.sh"],
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the built-in descriptor for Windsurf.
		/// </summary>
		private static VsCodeProductDescriptor Windsurf { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "windsurf",
			DisplayName           = "Windsurf",
			ShortName             = "Windsurf",
			ExecutableNames       = ["Windsurf.exe"],
			UserRelativeDirs      = [@"Programs\Windsurf"],
			SystemRelativeDirs    = ["Windsurf"],
			UninstallNamePrefixes = ["Windsurf"],
			PathCommandNames      = ["windsurf.cmd"],
			UserDataFolderName    = "Windsurf",
			DownloadUrl           = "https://windsurf.com/download",
			UpdateStrategy        = VsCodeUpdateStrategyKind.VsCodeApi,
			InstallerKind         = VsCodeInstallerKind.InnoSetupGeneric,
			TrustedDownloadHosts  = ["windsurf-stable.codeiumdata.com", "windsurf-stable.codeium.com"],
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the built-in descriptor for VSCodium.
		/// </summary>
		private static VsCodeProductDescriptor VSCodium { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "vscodium",
			DisplayName           = "VSCodium",
			ShortName             = "VSCodium",
			ExecutableNames       = ["VSCodium.exe"],
			UserRelativeDirs      = [@"Programs\VSCodium"],
			SystemRelativeDirs    = ["VSCodium"],
			UninstallNamePrefixes = ["VSCodium"],
			PathCommandNames      = ["codium.cmd"],
			UserDataFolderName    = "VSCodium",
			DownloadUrl           = "https://vscodium.com/",
			UpdateStrategy        = VsCodeUpdateStrategyKind.GitHubReleases,
			InstallerKind         = VsCodeInstallerKind.InnoSetupGeneric,
			GitHubRepo            = "VSCodium/vscodium",
			TrustedDownloadHosts  = ["github.com", "objects.githubusercontent.com", "release-assets.githubusercontent.com"],
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the built-in descriptor for Positron.
		/// </summary>
		private static VsCodeProductDescriptor Positron { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "positron",
			DisplayName           = "Positron",
			ShortName             = "Positron",
			ExecutableNames       = ["Positron.exe"],
			UserRelativeDirs      = [@"Programs\Positron"],
			SystemRelativeDirs    = ["Positron"],
			UninstallNamePrefixes = ["Positron"],
			PathCommandNames      = ["positron.cmd"],
			UserDataFolderName    = "Positron",
			DownloadUrl           = "https://positron.posit.co/download.html",
			UpdateStrategy        = VsCodeUpdateStrategyKind.GitHubReleases,
			InstallerKind         = VsCodeInstallerKind.InnoSetupGeneric,
			GitHubRepo            = "posit-dev/positron",
			TrustedDownloadHosts  = ["github.com", "objects.githubusercontent.com", "release-assets.githubusercontent.com"],
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the built-in descriptor for Kiro.
		/// </summary>
		private static VsCodeProductDescriptor Kiro { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "kiro",
			DisplayName           = "Kiro",
			ShortName             = "Kiro",
			ExecutableNames       = ["Kiro.exe"],
			UserRelativeDirs      = [@"Programs\Kiro"],
			SystemRelativeDirs    = ["Kiro"],
			UninstallNamePrefixes = ["Kiro"],
			PathCommandNames      = ["kiro.cmd"],
			UserDataFolderName    = "Kiro",
			DownloadUrl           = "https://kiro.dev/",
			UpdateStrategy        = VsCodeUpdateStrategyKind.Unsupported,
			InstallerKind         = VsCodeInstallerKind.ExternalDownload,
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the built-in descriptor for Trae.
		/// </summary>
		private static VsCodeProductDescriptor Trae { get; } = new VsCodeProductDescriptor
		{
			FlavorId              = "trae",
			DisplayName           = "Trae",
			ShortName             = "Trae",
			ExecutableNames       = ["Trae.exe"],
			UserRelativeDirs      = [@"Programs\Trae"],
			SystemRelativeDirs    = ["Trae"],
			UninstallNamePrefixes = ["Trae"],
			PathCommandNames      = ["trae.cmd"],
			UserDataFolderName    = "Trae",
			DownloadUrl           = "https://www.trae.ai/",
			UpdateStrategy        = VsCodeUpdateStrategyKind.Unsupported,
			InstallerKind         = VsCodeInstallerKind.ExternalDownload,
			IsInsiders            = false
		};

		/// <summary>
		/// Gets the ordered catalog of built-in VS Code and fork product descriptors.
		/// </summary>
		public static IReadOnlyList<VsCodeProductDescriptor> KnownProducts { get; } =
		[
			VsCodeStable,
			VsCodeInsiders,
			AntigravityIde,
			Cursor,
			Windsurf,
			VSCodium,
			Positron,
			Kiro,
			Trae
		];

		/// <summary>
		/// Gets the deterministic flavor identifier used to construct stable instance IDs.
		/// </summary>
		public string FlavorId { get; init; } = string.Empty;

		/// <summary>
		/// Gets the full display name of the IDE product.
		/// </summary>
		public string DisplayName { get; init; } = string.Empty;

		/// <summary>
		/// Gets the compact display name suitable for buttons and context menu labels.
		/// </summary>
		public string ShortName { get; init; } = string.Empty;

		/// <summary>
		/// Gets the candidate Windows executable filenames for this product.
		/// </summary>
		public IReadOnlyList<string> ExecutableNames { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Gets the relative subdirectories under <c>%LocalAppData%</c> for per-user installations.
		/// </summary>
		public IReadOnlyList<string> UserRelativeDirs { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Gets the relative subdirectories under <c>%ProgramFiles%</c> and <c>%ProgramFiles(x86)%</c> for machine-wide installations.
		/// </summary>
		public IReadOnlyList<string> SystemRelativeDirs { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Gets the Windows Uninstall registry <c>DisplayName</c> prefixes associated with this product.
		/// </summary>
		public IReadOnlyList<string> UninstallNamePrefixes { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Gets the CLI wrapper script filenames on <c>PATH</c> associated with this product.
		/// </summary>
		public IReadOnlyList<string> PathCommandNames { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Gets the folder name under <c>%AppData%</c> where the product stores user data and profiles.
		/// </summary>
		public string UserDataFolderName { get; init; } = string.Empty;

		/// <summary>
		/// Gets the official download page URL for this product, if any.
		/// </summary>
		public string DownloadUrl { get; init; } = string.Empty;

		/// <summary>
		/// Gets the update-checking strategy supported by this product.
		/// </summary>
		public VsCodeUpdateStrategyKind UpdateStrategy { get; init; } = VsCodeUpdateStrategyKind.Unsupported;

		/// <summary>
		/// Gets the installer family used for automated updates.
		/// </summary>
		public VsCodeInstallerKind InstallerKind { get; init; } = VsCodeInstallerKind.InnoSetupGeneric;

		/// <summary>
		/// Gets the <c>owner/repo</c> identifier when <see cref="UpdateStrategy"/> is <see cref="VsCodeUpdateStrategyKind.GitHubReleases"/>.
		/// </summary>
		public string GitHubRepo { get; init; } = string.Empty;

		/// <summary>
		/// Gets the allowlisted HTTPS hosts trusted to serve installer downloads for this product.
		/// </summary>
		public IReadOnlyList<string> TrustedDownloadHosts { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Gets a value indicating whether this product represents an Insiders/preview channel.
		/// </summary>
		public bool IsInsiders { get; init; }

		/// <summary>
		/// Validates whether an <c>updateUrl</c> value is a non-placeholder, public HTTPS DNS endpoint suitable for remote update queries.
		/// </summary>
		/// <param name="url">The candidate endpoint URL.</param>
		/// <returns><c>true</c> if the URL uses HTTPS with a non-reserved DNS hostname; otherwise <c>false</c>.</returns>
		public static bool IsValidUpdateEndpointUrl(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return false;
			}

			if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri))
			{
				return false;
			}

			if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
				|| uri.HostNameType != UriHostNameType.Dns
				|| !string.IsNullOrEmpty(uri.UserInfo))
			{
				return false;
			}

			string host = uri.Host.TrimEnd('.').ToLowerInvariant();

			if (string.IsNullOrEmpty(host))
			{
				return false;
			}

			return !IsReservedHost(host);
		}

		/// <summary>
		/// Attempts to inspect <c>resources\app\product.json</c> and <c>resources\app\out\cli.js</c> inside <paramref name="installDir"/>
		/// to construct a <see cref="VsCodeProductDescriptor"/> for an auto-discovered or manually added fork.
		/// </summary>
		/// <param name="environment">The host environment abstraction.</param>
		/// <param name="installDir">The root installation directory containing the IDE executable.</param>
		/// <param name="executablePath">The full path to the IDE executable.</param>
		/// <returns>A populated <see cref="VsCodeProductDescriptor"/> when both metadata and <c>cli.js</c> exist and parse cleanly; otherwise <c>null</c>.</returns>
		public static VsCodeProductDescriptor? TryCreateFromProductJson(
			IVsCodeEnvironment environment,
			string installDir,
			string executablePath)
		{
			ArgumentNullException.ThrowIfNull(environment);

			if (string.IsNullOrWhiteSpace(installDir) || string.IsNullOrWhiteSpace(executablePath))
			{
				return null;
			}

			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(installDir, "resources", "app", "out", "cli.js");

			if (!environment.FileExists(productJsonPath) || !environment.FileExists(cliJsPath))
			{
				return null;
			}

			JObject? root = TryReadProductJson(environment, productJsonPath);

			if (root is null)
			{
				return null;
			}

			string exeName            = Path.GetFileName(executablePath)?.Trim() ?? string.Empty;
			string exeStem            = Path.GetFileNameWithoutExtension(executablePath)?.Trim() ?? string.Empty;
			string? nameShort         = TryGetStringProperty(root, "nameShort")?.Trim();
			string? nameLong          = TryGetStringProperty(root, "nameLong")?.Trim();
			string? win32DirName      = TryGetStringProperty(root, "win32DirName")?.Trim();
			string? quality           = TryGetStringProperty(root, "quality")?.Trim();
			string? updateUrl         = TryGetStringProperty(root, "updateUrl")?.Trim();
			string shortName          = FirstNonEmpty(nameShort, nameLong, exeStem);
			string displayName        = FirstNonEmpty(nameLong, nameShort, exeStem);
			string userDataFolderName = FirstNonEmpty(win32DirName, nameShort, exeStem);
			string flavorId           = Slugify(FirstNonEmpty(shortName, exeStem, "vscode-fork"));
			bool hasValidUpdateUrl    = IsValidUpdateEndpointUrl(updateUrl);

			return new VsCodeProductDescriptor
			{
				FlavorId             = flavorId,
				DisplayName          = displayName,
				ShortName            = shortName,
				ExecutableNames      = string.IsNullOrEmpty(exeName) ? Array.Empty<string>() : [exeName],
				UserDataFolderName   = userDataFolderName,
				UpdateStrategy       = hasValidUpdateUrl ? VsCodeUpdateStrategyKind.VsCodeApi : VsCodeUpdateStrategyKind.Unsupported,
				InstallerKind        = VsCodeInstallerKind.InnoSetupGeneric,
				TrustedDownloadHosts = ResolveTrustedHosts(updateUrl, hasValidUpdateUrl),
				IsInsiders           = string.Equals(quality, "insider", StringComparison.OrdinalIgnoreCase)
			};
		}

		/// <summary>
		/// Checks whether a normalized lowercase hostname matches a reserved local or RFC 2606 domain.
		/// </summary>
		private static bool IsReservedHost(string host)
		{
			foreach (string domain in ReservedDomains)
			{
				if (host.Equals(domain, StringComparison.Ordinal)
					|| host.EndsWith($".{domain}", StringComparison.Ordinal))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Attempts to read and parse <c>product.json</c> from disk via <paramref name="environment"/>.
		/// </summary>
		private static JObject? TryReadProductJson(IVsCodeEnvironment environment, string productJsonPath)
		{
			try
			{
				string json = environment.ReadAllText(productJsonPath);

				return JObject.Parse(json);
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);

				return null;
			}
		}

		/// <summary>
		/// Safely reads a string property from a <see cref="JObject"/>.
		/// </summary>
		private static string? TryGetStringProperty(JObject obj, string propertyName)
		{
			return obj[propertyName] is JValue { Type: JTokenType.String, Value: string text } ? text : null;
		}

		/// <summary>
		/// Returns the first non-null, non-whitespace candidate string.
		/// </summary>
		private static string FirstNonEmpty(params string?[] candidates)
		{
			foreach (string? candidate in candidates)
			{
				if (!string.IsNullOrWhiteSpace(candidate))
				{
					return candidate;
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Converts a display name into a lowercase ASCII alphanumeric slug separated by hyphens.
		/// </summary>
		private static string Slugify(string value)
		{
			string slug = NonSlugCharsRegex.Replace(value.ToLowerInvariant(), "-").Trim('-');

			return string.IsNullOrEmpty(slug) ? "vscode-fork" : slug;
		}

		/// <summary>
		/// Extracts the host of a validated HTTPS <c>updateUrl</c> as an initial trusted download host.
		/// </summary>
		private static IReadOnlyList<string> ResolveTrustedHosts(string? updateUrl, bool hasValidUpdateUrl)
		{
			if (!hasValidUpdateUrl || !Uri.TryCreate(updateUrl, UriKind.Absolute, out Uri? uri))
			{
				return Array.Empty<string>();
			}

			return [uri.Host];
		}
	}
}
