using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VSLauncher.DataModel;

using VSLXshared;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents a discovered Visual Studio Code or VS Code-based IDE installation and provides profile discovery and quick-launch actions.
	/// </summary>
	public class VsCodeInstance : IIdeInstance
	{
		private const string DefaultProfileName = "<default>";

		private readonly IVsCodeEnvironment environment;
		private Icon? cachedAppIcon;

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeInstance"/> class.
		/// </summary>
		/// <param name="name">The display name of the VS Code edition.</param>
		/// <param name="version">The product version string.</param>
		/// <param name="location">The full path to <c>Code.exe</c> or <c>Code - Insiders.exe</c>.</param>
		/// <param name="identifier">The stable unique identifier for this edition and scope.</param>
		/// <param name="userDataDirectory">The user data root directory containing <c>User\globalStorage</c>.</param>
		/// <param name="isInsiders"><c>true</c> for VS Code Insiders; <c>false</c> for VS Code Stable.</param>
		/// <param name="environment">Optional environment abstraction for testing.</param>
		public VsCodeInstance(
			string name,
			string version,
			string location,
			string identifier,
			string userDataDirectory,
			bool isInsiders,
			IVsCodeEnvironment? environment = null)
			: this(
				name,
				version,
				location,
				identifier,
				userDataDirectory,
				isInsiders ? VsCodeProductDescriptor.VsCodeInsiders : VsCodeProductDescriptor.VsCodeStable,
				environment)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeInstance"/> class from a <see cref="VsCodeProductDescriptor"/>.
		/// </summary>
		/// <param name="name">The display name of the IDE installation.</param>
		/// <param name="version">The product version string.</param>
		/// <param name="location">The full path to the IDE executable.</param>
		/// <param name="identifier">The stable unique identifier for this edition and scope.</param>
		/// <param name="userDataDirectory">The user data root directory containing <c>User\globalStorage</c>.</param>
		/// <param name="descriptor">The product metadata and capabilities descriptor.</param>
		/// <param name="environment">Optional environment abstraction for testing.</param>
		public VsCodeInstance(
			string name,
			string version,
			string location,
			string identifier,
			string userDataDirectory,
			VsCodeProductDescriptor descriptor,
			IVsCodeEnvironment? environment = null)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentException.ThrowIfNullOrWhiteSpace(location);
			ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
			ArgumentNullException.ThrowIfNull(descriptor);

			this.Name              = name;
			this.Version           = version ?? string.Empty;
			this.Location          = location;
			this.Identifier        = identifier;
			this.UserDataDirectory = userDataDirectory ?? string.Empty;
			this.Descriptor        = descriptor;
			this.environment       = environment ?? WindowsVsCodeEnvironment.Instance;
		}

		/// <inheritdoc/>
		public IdeType IdeType
		{
			get
			{
				return IdeType.VsCode;
			}
		}

		/// <inheritdoc/>
		public string Name { get; }

		/// <summary>
		/// Gets the product descriptor backing this installation.
		/// </summary>
		public VsCodeProductDescriptor Descriptor { get; }

		/// <inheritdoc/>
		public string ShortName
		{
			get
			{
				return !string.IsNullOrWhiteSpace(this.Descriptor.ShortName) ? this.Descriptor.ShortName : this.Name;
			}
		}

		/// <summary>
		/// Gets the product flavor identifier (for example, <c>vscode-stable</c> or <c>antigravity-ide</c>).
		/// </summary>
		public string FlavorId
		{
			get
			{
				return this.Descriptor.FlavorId;
			}
		}

		/// <summary>
		/// Gets the official download page URL for this product, if available.
		/// </summary>
		public string DownloadUrl
		{
			get
			{
				return this.Descriptor.DownloadUrl;
			}
		}

		/// <summary>
		/// Gets the remote update-checking strategy supported by this product.
		/// </summary>
		public VsCodeUpdateStrategyKind UpdateStrategy
		{
			get
			{
				return this.Descriptor.UpdateStrategy;
			}
		}

		/// <summary>
		/// Gets the Windows installer family used when applying automated updates.
		/// </summary>
		public VsCodeInstallerKind InstallerKind
		{
			get
			{
				return this.Descriptor.InstallerKind;
			}
		}

		/// <summary>
		/// Gets the <c>owner/repo</c> slug used when <see cref="UpdateStrategy"/> is <see cref="VsCodeUpdateStrategyKind.GitHubReleases"/>.
		/// </summary>
		public string GitHubRepo
		{
			get
			{
				return this.Descriptor.GitHubRepo;
			}
		}

		/// <summary>
		/// Gets the allowlisted HTTPS hosts trusted to serve installer downloads for this product.
		/// </summary>
		public IReadOnlyList<string> TrustedDownloadHosts
		{
			get
			{
				return this.Descriptor.TrustedDownloadHosts;
			}
		}

		/// <inheritdoc/>
		public string Version { get; private set; }

		/// <inheritdoc/>
		public string Location { get; }

		/// <inheritdoc/>
		public string Identifier { get; }

		/// <summary>
		/// Gets the VS Code user data root directory for this edition.
		/// </summary>
		public string UserDataDirectory { get; }

		/// <summary>
		/// Gets a value indicating whether this installation is an Insiders/preview edition.
		/// </summary>
		public bool IsInsiders
		{
			get
			{
				return this.Descriptor.IsInsiders;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this installation is a machine-wide system install.
		/// </summary>
		public bool IsSystemInstall { get; init; }

		/// <summary>
		/// Gets a value indicating whether this installation was added manually via custom IDE settings.
		/// </summary>
		public bool IsCustom { get; init; }

		/// <inheritdoc/>
		public Icon AppIcon
		{
			get
			{
				if (this.cachedAppIcon is not null)
				{
					return this.cachedAppIcon;
				}

				try
				{
					this.cachedAppIcon = Icon.ExtractAssociatedIcon(this.Location) ?? Resources.DefaultIcon;
				}
				catch
				{
					this.cachedAppIcon = Resources.DefaultIcon;
				}

				return this.cachedAppIcon;
			}
		}

		/// <summary>
		/// Gets the environment abstraction bound to this instance.
		/// </summary>
		internal IVsCodeEnvironment Environment
		{
			get
			{
				return this.environment;
			}
		}

		/// <inheritdoc/>
		public List<string> GetProfiles()
		{
			var profiles = new List<string> { DefaultProfileName };

			if (string.IsNullOrWhiteSpace(this.UserDataDirectory))
			{
				return profiles;
			}

			string storagePath = Path.Combine(this.UserDataDirectory, "User", "globalStorage", "storage.json");

			if (!this.environment.FileExists(storagePath))
			{
				return profiles;
			}

			try
			{
				string json        = this.environment.ReadAllText(storagePath);
				JObject root       = JObject.Parse(json);
				JArray? profileArr = root["userDataProfiles"] as JArray;

				if (profileArr is null)
				{
					return profiles;
				}

				CollectDistinctProfileNames(profileArr, profiles);
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}

			return profiles;
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> that launches this VS Code edition with no extra arguments.
		/// </summary>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public ProcessStartInfo BuildStartInfo()
		{
			return BuildQuickStartInfo(string.Empty, runAsAdmin: false);
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> that launches this VS Code edition elevated as administrator.
		/// </summary>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public ProcessStartInfo BuildAdminStartInfo()
		{
			return BuildQuickStartInfo(string.Empty, runAsAdmin: true);
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> that launches this VS Code edition with <c>--new-window</c>.
		/// </summary>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public ProcessStartInfo BuildNewWindowStartInfo()
		{
			return BuildQuickStartInfo("--new-window", runAsAdmin: false);
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> that launches this VS Code edition with <c>--disable-extensions</c>.
		/// </summary>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public ProcessStartInfo BuildWithoutExtensionsStartInfo()
		{
			return BuildQuickStartInfo("--disable-extensions", runAsAdmin: false);
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> that opens the specified file or folder directly in this VS Code edition.
		/// </summary>
		/// <param name="targetPath">The file or directory path to open.</param>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		public ProcessStartInfo BuildOpenPathStartInfo(string targetPath)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

			return BuildQuickStartInfo(VsCodeIdeProvider.QuotePathArgument(targetPath), runAsAdmin: false);
		}

		/// <summary>
		/// Launches this VS Code edition directly with no arguments.
		/// </summary>
		public void Execute()
		{
			Execute(BuildStartInfo());
		}

		/// <summary>
		/// Launches this VS Code edition elevated as administrator.
		/// </summary>
		public void ExecuteAsAdmin()
		{
			Execute(BuildAdminStartInfo());
		}

		/// <summary>
		/// Launches this VS Code edition in a new window.
		/// </summary>
		public void ExecuteNewWindow()
		{
			Execute(BuildNewWindowStartInfo());
		}

		/// <summary>
		/// Launches this VS Code edition with all extensions disabled.
		/// </summary>
		public void ExecuteWithoutExtensions()
		{
			Execute(BuildWithoutExtensionsStartInfo());
		}

		/// <summary>
		/// Opens the specified file or folder directly in this VS Code edition.
		/// </summary>
		/// <param name="targetPath">The file or directory path to open.</param>
		public void OpenPath(string targetPath)
		{
			Execute(BuildOpenPathStartInfo(targetPath));
		}

		/// <summary>
		/// Re-reads the installed product version from disk and updates <see cref="Version"/>, falling back to <paramref name="fallbackVersion"/> if disk lookup is unavailable.
		/// </summary>
		/// <param name="fallbackVersion">Optional version string to apply when disk metadata cannot be read.</param>
		/// <returns>The updated <see cref="Version"/> string.</returns>
		public string RefreshVersion(string? fallbackVersion = null)
		{
			string resolved = VsCodeIdeProvider.ResolveVersion(this.environment, this.Location);

			if (!string.IsNullOrWhiteSpace(resolved))
			{
				this.Version = resolved;

				return this.Version;
			}

			if (!string.IsNullOrWhiteSpace(fallbackVersion))
			{
				this.Version = fallbackVersion.Trim();
			}

			return this.Version;
		}

		/// <inheritdoc/>
		public override string? ToString()
		{
			return string.IsNullOrEmpty(this.Version) ? this.Name : $"{this.Name} ({this.Version})";
		}

		/// <summary>
		/// Resolves the path to VS Code's internal <c>cli.js</c> entrypoint if present on this installation.
		/// </summary>
		/// <returns>The full path to <c>cli.js</c>, or <c>null</c> if absent.</returns>
		internal string? GetCliScriptPath()
		{
			return VsCodeIdeProvider.FindCliScriptPath(this.environment, this.Location);
		}

		/// <summary>
		/// Appends unique, non-empty profile names from the <c>userDataProfiles</c> JSON array.
		/// </summary>
		/// <param name="profileArray">The parsed <c>userDataProfiles</c> array.</param>
		/// <param name="profiles">The destination list already seeded with <c>&lt;default&gt;</c>.</param>
		private static void CollectDistinctProfileNames(JArray profileArray, List<string> profiles)
		{
			var seen = new HashSet<string>(profiles, StringComparer.OrdinalIgnoreCase);

			foreach (JToken token in profileArray)
			{
				if (token is not JObject profileObj)
				{
					continue;
				}

				if (profileObj["name"] is not JValue { Type: JTokenType.String, Value: string name } || string.IsNullOrWhiteSpace(name))
				{
					continue;
				}

				string trimmed = name.Trim();

				if (seen.Add(trimmed))
				{
					profiles.Add(trimmed);
				}
			}
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> for a quick-launch action targeting this executable.
		/// </summary>
		/// <param name="arguments">The command-line arguments string.</param>
		/// <param name="runAsAdmin">Whether UAC elevation should be requested.</param>
		/// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
		private ProcessStartInfo BuildQuickStartInfo(string arguments, bool runAsAdmin)
		{
			var startInfo = new ProcessStartInfo
			{
				FileName        = this.Location,
				Arguments       = arguments,
				Verb            = runAsAdmin ? "runas" : string.Empty,
				UseShellExecute = runAsAdmin,
				ErrorDialog     = runAsAdmin
			};

			if (!runAsAdmin)
			{
				startInfo.Environment.Remove("ELECTRON_RUN_AS_NODE");
			}

			return startInfo;
		}

		/// <summary>
		/// Starts a process from the supplied <see cref="ProcessStartInfo"/> and surfaces launch failures to the user.
		/// </summary>
		/// <param name="startInfo">The start info to execute.</param>
		private static void Execute(ProcessStartInfo startInfo)
		{
			try
			{
				using var process = Process.Start(startInfo);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
