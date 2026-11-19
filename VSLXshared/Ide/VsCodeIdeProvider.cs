using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Data.Sqlite;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Provides Visual Studio Code discovery, <c>state.vscdb</c> MRU loading, and CLI / <c>cli.js --wait</c> launch preparation.
	/// </summary>
	public class VsCodeIdeProvider : IIdeProvider
	{
		private const string VsCodeDownloadUrl        = "https://code.visualstudio.com/download";
		private const string RecentlyOpenedKey        = "history.recentlyOpenedPathsList";
		private const string StableExeName            = "Code.exe";
		private const string InsidersExeName          = "Code - Insiders.exe";
		private const string StableIdentifierUser     = "vscode-stable-user";
		private const string StableIdentifierSystem   = "vscode-stable-system";
		private const string InsidersIdentifierUser   = "vscode-insiders-user";
		private const string InsidersIdentifierSystem = "vscode-insiders-system";

		private readonly IVsCodeEnvironment environment;
		private readonly IReadOnlyList<VsCodeInstance> instances;

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeIdeProvider"/> class using local Windows discovery.
		/// </summary>
		public VsCodeIdeProvider() : this(WindowsVsCodeEnvironment.Instance)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeIdeProvider"/> class using the supplied environment abstraction.
		/// </summary>
		/// <param name="environment">The host environment abstraction.</param>
		public VsCodeIdeProvider(IVsCodeEnvironment environment)
		{
			ArgumentNullException.ThrowIfNull(environment);

			this.environment = environment;
			this.instances   = DiscoverInstances();
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
		public string DisplayName
		{
			get
			{
				return "Visual Studio Code";
			}
		}

		/// <inheritdoc/>
		public string DownloadUrl
		{
			get
			{
				return VsCodeDownloadUrl;
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<IIdeInstance> Instances
		{
			get
			{
				return this.instances;
			}
		}

		/// <inheritdoc/>
		public bool IsInstalled
		{
			get
			{
				return this.instances.Count > 0;
			}
		}

		/// <inheritdoc/>
		public IIdeInstance? DefaultInstance
		{
			get
			{
				return this.instances.FirstOrDefault(i => !i.IsInsiders) ?? this.instances.FirstOrDefault();
			}
		}

		/// <inheritdoc/>
		public IIdeInstance? FindByIdentifier(string? identifier)
		{
			if (string.IsNullOrWhiteSpace(identifier))
			{
				return null;
			}

			return this.instances.FirstOrDefault(i => i.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));
		}

		/// <inheritdoc/>
		public IIdeInstance? FindByVersion(string? version)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				return null;
			}

			return this.instances.FirstOrDefault(i => i.Version.StartsWith(version, StringComparison.OrdinalIgnoreCase));
		}

		/// <inheritdoc/>
		public VsItemList GetRecentProjects(bool onlyDefaultProfiles)
		{
			_ = onlyDefaultProfiles;

			var groups               = new VsItemList(null);
			var seenUserDataDirs     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (VsCodeInstance instance in this.instances)
			{
				if (string.IsNullOrWhiteSpace(instance.UserDataDirectory))
				{
					continue;
				}

				string normalizedDataDir = TryGetCanonicalPath(instance.UserDataDirectory) ?? instance.UserDataDirectory;

				if (!seenUserDataDirs.Add(normalizedDataDir))
				{
					continue;
				}

				VsFolder? group = LoadRecentProjectsForInstance(instance);

				if (group is not null && group.Items.Count > 0)
				{
					groups.Add(group);
				}
			}

			return groups;
		}

		/// <inheritdoc/>
		public ProcessStartInfo? BuildStartInfo(VsItem item, IIdeInstance instance, bool forceAdmin = false, bool waitForExit = false)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(instance);

			if (string.IsNullOrWhiteSpace(item.Path) || string.IsNullOrWhiteSpace(instance.Location))
			{
				return null;
			}

			LaunchSpecification? spec = ExtractLaunchSpecification(item);

			if (spec is null || string.IsNullOrWhiteSpace(spec.TargetPath))
			{
				return null;
			}

			bool runAsAdmin = forceAdmin || item.RunAsAdmin;
			bool shouldWait = waitForExit || item.WaitForCompletion || (item.RunAfter?.WaitForCompletion == true);
			string baseArgs = BuildCliArguments(spec, shouldWait);

			if (shouldWait && !runAsAdmin)
			{
				ProcessStartInfo? nodeStartInfo = TryBuildCliNodeStartInfo(instance, baseArgs);

				if (nodeStartInfo is not null)
				{
					return nodeStartInfo;
				}
			}

			var directStartInfo = new ProcessStartInfo
			{
				FileName        = instance.Location,
				Arguments       = baseArgs,
				Verb            = runAsAdmin ? "runas" : string.Empty,
				UseShellExecute = runAsAdmin,
				ErrorDialog     = runAsAdmin
			};

			if (!runAsAdmin)
			{
				directStartInfo.Environment.Remove("ELECTRON_RUN_AS_NODE");
			}

			return directStartInfo;
		}

		/// <summary>
		/// Quotes a file or directory path for Windows command-line parsing without allowing a trailing backslash to escape the closing quote.
		/// </summary>
		/// <param name="path">The file or directory path.</param>
		/// <returns>The safely quoted argument string.</returns>
		internal static string QuotePathArgument(string path)
		{
			string trimmed = Path.TrimEndingDirectorySeparator(path.Trim());

			if (trimmed.EndsWith('\\'))
			{
				trimmed += "\\";
			}

			return $"\"{trimmed}\"";
		}

		/// <summary>
		/// Locates VS Code's internal <c>resources\app\out\cli.js</c> relative to an executable path when present.
		/// </summary>
		/// <param name="environment">The environment abstraction used for file existence probing.</param>
		/// <param name="executablePath">The path to <c>Code.exe</c> or <c>Code - Insiders.exe</c>.</param>
		/// <returns>The full path to <c>cli.js</c>, or <c>null</c> if unavailable.</returns>
		internal static string? FindCliScriptPath(IVsCodeEnvironment environment, string executablePath)
		{
			string? installDir = Path.GetDirectoryName(executablePath);

			if (string.IsNullOrWhiteSpace(installDir))
			{
				return null;
			}

			string cliPath = Path.Combine(installDir, "resources", "app", "out", "cli.js");

			return environment.FileExists(cliPath) ? cliPath : null;
		}

		/// <summary>
		/// Probes all six Windows discovery sources in priority order and materializes <see cref="VsCodeInstance"/> entries.
		/// </summary>
		/// <returns>A read-only list of discovered installations.</returns>
		private IReadOnlyList<VsCodeInstance> DiscoverInstances()
		{
			string localAppData   = this.environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string programFiles   = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string programFiles86 = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			string appData        = this.environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var state             = new DiscoveryState(appData);

			TryAddStandardCandidate(state, localAppData, @"Programs\Microsoft VS Code", StableExeName, isInsiders: false, InstallScope.User);
			TryAddStandardCandidate(state, programFiles, "Microsoft VS Code", StableExeName, isInsiders: false, InstallScope.System);
			TryAddStandardCandidate(state, programFiles86, "Microsoft VS Code", StableExeName, isInsiders: false, InstallScope.System);
			TryAddStandardCandidate(state, localAppData, @"Programs\Microsoft VS Code Insiders", InsidersExeName, isInsiders: true, InstallScope.User);
			TryAddStandardCandidate(state, programFiles, "Microsoft VS Code Insiders", InsidersExeName, isInsiders: true, InstallScope.System);

			DiscoverFromRegistry(state);
			DiscoverFromPath(state);

			bool hasBothStableScopes   = HasBothScopes(state.Candidates, isInsiders: false);
			bool hasBothInsidersScopes = HasBothScopes(state.Candidates, isInsiders: true);
			var result                 = new List<VsCodeInstance>(state.Candidates.Count);

			foreach (DiscoveredCandidate candidate in state.Candidates)
			{
				bool hasBothScopes = candidate.IsInsiders ? hasBothInsidersScopes : hasBothStableScopes;
				string displayName = ResolveDisplayName(candidate.IsInsiders, candidate.Scope, hasBothScopes);

				result.Add(new VsCodeInstance(
					displayName,
					candidate.Version,
					candidate.CanonicalPath,
					candidate.Identifier,
					candidate.UserDataDirectory,
					candidate.IsInsiders,
					this.environment));
			}

			return result;
		}

		/// <summary>
		/// Attempts to register a standard User or System installation candidate if its root folder and executable exist.
		/// </summary>
		private void TryAddStandardCandidate(
			DiscoveryState state,
			string? rootFolder,
			string relativeSubDir,
			string exeName,
			bool isInsiders,
			InstallScope scope)
		{
			if (string.IsNullOrWhiteSpace(rootFolder))
			{
				return;
			}

			string candidatePath = Path.Combine(rootFolder, relativeSubDir, exeName);

			TryAddCandidate(state, candidatePath, isInsiders, scope);
		}

		/// <summary>
		/// Probes Windows Uninstall registry entries for custom VS Code installations.
		/// </summary>
		private void DiscoverFromRegistry(DiscoveryState state)
		{
			IReadOnlyList<VsCodeUninstallEntry> entries = this.environment.GetUninstallEntries();

			foreach (VsCodeUninstallEntry entry in entries)
			{
				if (!entry.DisplayName.StartsWith("Microsoft Visual Studio Code", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				string stablePath   = Path.Combine(entry.InstallLocation, StableExeName);
				string insidersPath = Path.Combine(entry.InstallLocation, InsidersExeName);

				TryAddCandidate(state, stablePath, isInsiders: false, InstallScope.Custom);
				TryAddCandidate(state, insidersPath, isInsiders: true, InstallScope.Custom);
			}
		}

		/// <summary>
		/// Probes directories on <c>PATH</c> for <c>code.cmd</c> / <c>code-insiders.cmd</c> and resolves their sibling executables.
		/// </summary>
		private void DiscoverFromPath(DiscoveryState state)
		{
			string? pathValue = this.environment.GetEnvironmentVariable("PATH");

			if (string.IsNullOrWhiteSpace(pathValue))
			{
				return;
			}

			string[] pathEntries = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

			foreach (string rawEntry in pathEntries)
			{
				string dir = rawEntry.Trim().Trim('"');

				if (string.IsNullOrWhiteSpace(dir))
				{
					continue;
				}

				TryAddFromPathCommand(state, dir, "code.cmd", StableExeName, isInsiders: false);
				TryAddFromPathCommand(state, dir, "code-insiders.cmd", InsidersExeName, isInsiders: true);
			}
		}

		/// <summary>
		/// Checks a single <c>PATH</c> directory for a CLI wrapper script and adds its parent directory executable if present.
		/// </summary>
		private void TryAddFromPathCommand(
			DiscoveryState state,
			string binDirectory,
			string cmdFileName,
			string exeFileName,
			bool isInsiders)
		{
			try
			{
				string cmdPath = Path.Combine(binDirectory, cmdFileName);

				if (!this.environment.FileExists(cmdPath))
				{
					return;
				}

				string siblingExePath = Path.Combine(binDirectory, "..", exeFileName);

				TryAddCandidate(state, siblingExePath, isInsiders, InstallScope.Custom);
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Validates, canonicalizes, deduplicates, and records a discovered executable candidate.
		/// </summary>
		private void TryAddCandidate(
			DiscoveryState state,
			string candidatePath,
			bool isInsiders,
			InstallScope scope)
		{
			string? canonicalPath = TryGetCanonicalPath(candidatePath);

			if (string.IsNullOrWhiteSpace(canonicalPath) || !this.environment.FileExists(canonicalPath))
			{
				return;
			}

			if (!state.SeenPaths.Add(canonicalPath))
			{
				return;
			}

			string identifier = AssignIdentifier(isInsiders, scope, canonicalPath, state.UsedIdentifiers);
			string version    = ResolveVersion(canonicalPath);
			string userData   = string.IsNullOrWhiteSpace(state.AppData)
				? string.Empty
				: Path.Combine(state.AppData, isInsiders ? "Code - Insiders" : "Code");

			state.Candidates.Add(new DiscoveredCandidate(canonicalPath, version, userData, isInsiders, scope, identifier));
		}

		/// <summary>
		/// Assigns a deterministic identifier matching the edition and scope, falling back to a path hash for custom installs.
		/// </summary>
		private static string AssignIdentifier(
			bool isInsiders,
			InstallScope scope,
			string canonicalPath,
			HashSet<string> usedIdentifiers)
		{
			string? preferred = scope switch
			{
				InstallScope.User   => isInsiders ? InsidersIdentifierUser : StableIdentifierUser,
				InstallScope.System => isInsiders ? InsidersIdentifierSystem : StableIdentifierSystem,
				_                   => null
			};

			if (!string.IsNullOrEmpty(preferred) && usedIdentifiers.Add(preferred))
			{
				return preferred;
			}

			string customId = $"vscode-custom-{ComputePathHash(canonicalPath)}";

			usedIdentifiers.Add(customId);

			return customId;
		}

		/// <summary>
		/// Resolves the product version from executable file metadata or falls back to <c>resources\app\package.json</c>.
		/// </summary>
		private string ResolveVersion(string executablePath)
		{
			string? fileVersion = this.environment.GetFileVersion(executablePath);

			if (!string.IsNullOrWhiteSpace(fileVersion))
			{
				return fileVersion.Trim();
			}

			string? installDir = Path.GetDirectoryName(executablePath);

			if (string.IsNullOrWhiteSpace(installDir))
			{
				return string.Empty;
			}

			string packageJsonPath = Path.Combine(installDir, "resources", "app", "package.json");

			if (!this.environment.FileExists(packageJsonPath))
			{
				return string.Empty;
			}

			try
			{
				string json  = this.environment.ReadAllText(packageJsonPath);
				JObject root = JObject.Parse(json);
				string? ver  = TryGetStringProperty(root, "version");

				return string.IsNullOrWhiteSpace(ver) ? string.Empty : ver.Trim();
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);

				return string.Empty;
			}
		}

		/// <summary>
		/// Determines whether both User and System installations exist for a given VS Code edition.
		/// </summary>
		private static bool HasBothScopes(List<DiscoveredCandidate> candidates, bool isInsiders)
		{
			bool hasUser   = candidates.Any(c => c.IsInsiders == isInsiders && c.Scope == InstallScope.User);
			bool hasSystem = candidates.Any(c => c.IsInsiders == isInsiders && c.Scope == InstallScope.System);

			return hasUser && hasSystem;
		}

		/// <summary>
		/// Computes the display name for a discovered installation according to whether both User and System scopes coexist.
		/// </summary>
		private static string ResolveDisplayName(bool isInsiders, InstallScope scope, bool hasBothScopes)
		{
			if (hasBothScopes && scope == InstallScope.User)
			{
				return isInsiders ? "VS Code Insiders (User)" : "VS Code (User)";
			}

			if (hasBothScopes && scope == InstallScope.System)
			{
				return isInsiders ? "VS Code Insiders (System)" : "VS Code (System)";
			}

			return isInsiders ? "Visual Studio Code - Insiders" : "Visual Studio Code";
		}

		/// <summary>
		/// Loads recent local projects and workspaces for a single VS Code edition from <c>state.vscdb</c>.
		/// </summary>
		private VsFolder? LoadRecentProjectsForInstance(VsCodeInstance instance)
		{
			string dbPath = Path.Combine(instance.UserDataDirectory, "User", "globalStorage", "state.vscdb");

			if (!this.environment.FileExists(dbPath))
			{
				return null;
			}

			string? mruJson = TryReadRecentlyOpenedJson(dbPath);

			if (string.IsNullOrWhiteSpace(mruJson))
			{
				return null;
			}

			var group = new VsFolder(instance.Name, string.Empty)
			{
				ItemType = ItemTypeEnum.VisualStudio,
				Icon     = instance.AppIcon
			};

			PopulateMruGroupFromJson(group, mruJson, instance.Identifier);

			return group.Items.Count > 0 ? group : null;
		}

		/// <summary>
		/// Reads the <c>history.recentlyOpenedPathsList</c> JSON payload from <c>state.vscdb</c>, falling back to a temporary copy on lock or I/O failure.
		/// </summary>
		private string? TryReadRecentlyOpenedJson(string dbPath)
		{
			try
			{
				return ReadRecentlyOpenedJsonFromDatabase(dbPath);
			}
			catch (Exception ex) when (ex is SqliteException or IOException)
			{
				Debug.WriteLine(ex);

				return ReadRecentlyOpenedJsonViaTempCopy(dbPath);
			}
		}

		/// <summary>
		/// Copies <c>state.vscdb</c> and optional WAL/SHM sidecars into an isolated temporary directory, reads MRU JSON, and removes the temporary directory.
		/// </summary>
		private string? ReadRecentlyOpenedJsonViaTempCopy(string dbPath)
		{
			string tempDir    = Path.Combine(this.environment.GetTempPath(), "VSLX_vscode_mru_" + Guid.NewGuid().ToString("N"));
			string tempDbPath = Path.Combine(tempDir, Path.GetFileName(dbPath));

			try
			{
				Directory.CreateDirectory(tempDir);

				CopyFileShared(dbPath, tempDbPath);
				CopySidecarIfExists(dbPath + "-wal", tempDbPath + "-wal");
				TryCopyOptionalSidecar(dbPath + "-shm", tempDbPath + "-shm");

				return ReadTempCopyWithWalRecovery(tempDbPath);
			}
			catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);

				return null;
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Reads MRU JSON from a disposable temporary copy, allowing SQLite to regenerate an unlocked <c>-shm</c> index from <c>-wal</c> if <c>-shm</c> was locked or unavailable.
		/// </summary>
		private static string? ReadTempCopyWithWalRecovery(string tempDbPath)
		{
			try
			{
				return ReadRecentlyOpenedJsonFromDatabase(tempDbPath, SqliteOpenMode.ReadOnly);
			}
			catch (SqliteException ex)
			{
				Debug.WriteLine(ex);

				return ReadRecentlyOpenedJsonFromDatabase(tempDbPath, SqliteOpenMode.ReadWrite);
			}
		}

		/// <summary>
		/// Executes the SQLite query against <c>ItemTable</c> using the requested open mode and clears the connection pool before closing.
		/// </summary>
		private static string? ReadRecentlyOpenedJsonFromDatabase(string dbPath, SqliteOpenMode openMode = SqliteOpenMode.ReadOnly)
		{
			string connectionString = new SqliteConnectionStringBuilder
			{
				DataSource     = dbPath,
				Mode           = openMode,
				Pooling        = false,
				DefaultTimeout = 1
			}.ToString();

			using var connection = new SqliteConnection(connectionString);

			try
			{
				connection.Open();

				using SqliteCommand command = connection.CreateCommand();

				command.CommandText = "SELECT value FROM ItemTable WHERE key = $key;";
				command.Parameters.AddWithValue("$key", RecentlyOpenedKey);

				object? scalar = command.ExecuteScalar();

				return scalar switch
				{
					string text  => text,
					byte[] bytes => Encoding.UTF8.GetString(bytes),
					_            => null
				};
			}
			finally
			{
				SqliteConnection.ClearPool(connection);
			}
		}

		/// <summary>
		/// Parses the <c>entries</c> array from VS Code MRU JSON and appends local workspace/folder items to <paramref name="group"/>.
		/// </summary>
		private static void PopulateMruGroupFromJson(VsFolder group, string mruJson, string editionIdentifier)
		{
			try
			{
				JObject root    = JObject.Parse(mruJson);
				JArray? entries = root["entries"] as JArray;

				if (entries is null)
				{
					return;
				}

				foreach (JToken token in entries)
				{
					VsCodeProject? project = TryCreateMruProject(token as JObject, editionIdentifier);

					if (project is not null)
					{
						group.Items.Add(project);
					}
				}
			}
			catch (JsonException ex)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Converts a single VS Code MRU JSON entry into a <see cref="VsCodeProject"/> if it represents a local workspace or folder.
		/// </summary>
		private static VsCodeProject? TryCreateMruProject(JObject? entryObj, string editionIdentifier)
		{
			if (entryObj is null)
			{
				return null;
			}

			string? remoteAuthority = TryGetStringProperty(entryObj, "remoteAuthority");

			if (!string.IsNullOrWhiteSpace(remoteAuthority))
			{
				return null;
			}

			if (entryObj["workspace"] is JObject workspaceObj)
			{
				string? workspaceConfigUri = TryGetStringProperty(workspaceObj, "configPath");

				if (!string.IsNullOrWhiteSpace(workspaceConfigUri))
				{
					return TryCreateWorkspaceMruProject(workspaceConfigUri, editionIdentifier);
				}
			}

			string? folderUri = TryGetStringProperty(entryObj, "folderUri");

			if (!string.IsNullOrWhiteSpace(folderUri))
			{
				return TryCreateFolderMruProject(folderUri, editionIdentifier);
			}

			return null;
		}

		/// <summary>
		/// Safely reads a string property from a <see cref="JObject"/> without throwing when the token is a non-string type.
		/// </summary>
		private static string? TryGetStringProperty(JObject obj, string propertyName)
		{
			return obj[propertyName] is JValue { Type: JTokenType.String, Value: string text } ? text : null;
		}

		/// <summary>
		/// Decodes a <c>.code-workspace</c> <c>file://</c> URI into a <see cref="VsCodeProject"/>.
		/// </summary>
		private static VsCodeProject? TryCreateWorkspaceMruProject(string workspaceConfigUri, string editionIdentifier)
		{
			string? decodedPath = TryDecodeLocalFileUri(workspaceConfigUri);

			if (string.IsNullOrWhiteSpace(decodedPath))
			{
				return null;
			}

			string name = Path.GetFileNameWithoutExtension(decodedPath);

			if (string.IsNullOrWhiteSpace(name))
			{
				name = Path.GetFileName(decodedPath);
			}

			var project = new VsCodeProject(name, decodedPath, VsCodeProjectKind.Workspace)
			{
				VsVersion = editionIdentifier
			};

			project.Warning = !File.Exists(decodedPath);

			return project;
		}

		/// <summary>
		/// Decodes a folder <c>file://</c> URI into a <see cref="VsCodeProject"/> and refines its kind from directory manifests when accessible.
		/// </summary>
		private static VsCodeProject? TryCreateFolderMruProject(string folderUri, string editionIdentifier)
		{
			string? decodedPath = TryDecodeLocalFileUri(folderUri);

			if (string.IsNullOrWhiteSpace(decodedPath))
			{
				return null;
			}

			string dirName = Path.GetFileName(Path.TrimEndingDirectorySeparator(decodedPath));

			if (string.IsNullOrWhiteSpace(dirName))
			{
				dirName = decodedPath;
			}

			bool exists            = Directory.Exists(decodedPath);
			string? manifestFile   = null;
			VsCodeProjectKind kind = exists
				? (ProjectScanner.DetectDirectoryManifest(decodedPath, out manifestFile) ?? VsCodeProjectKind.Folder)
				: VsCodeProjectKind.Folder;

			var project = new VsCodeProject(dirName, decodedPath, kind, manifestFile)
			{
				VsVersion = editionIdentifier
			};

			project.Warning = !exists;

			return project;
		}

		/// <summary>
		/// Decodes a Windows <c>file://</c> URI into a normalized local or UNC filesystem path, returning <c>null</c> for remote or invalid URIs.
		/// </summary>
		private static string? TryDecodeLocalFileUri(string rawUri)
		{
			if (!Uri.TryCreate(rawUri, UriKind.Absolute, out Uri? uri) || !uri.IsFile)
			{
				return null;
			}

			string rawPath = uri.IsUnc
				? uri.LocalPath
				: DecodeLocalAbsolutePath(uri);

			if (string.IsNullOrWhiteSpace(rawPath))
			{
				return null;
			}

			return NormalizeDecodedWindowsPath(rawPath);
		}

		/// <summary>
		/// Unescapes a local <c>file:///</c> URI path and strips any leading slash preceding a Windows drive letter.
		/// </summary>
		private static string DecodeLocalAbsolutePath(Uri uri)
		{
			string unescaped = Uri.UnescapeDataString(uri.AbsolutePath);

			if (unescaped.Length >= 3 && unescaped[0] == '/' && char.IsLetter(unescaped[1]) && unescaped[2] == ':')
			{
				return unescaped.Substring(1);
			}

			return unescaped;
		}

		/// <summary>
		/// Normalizes path separators, drive-letter casing, and trailing separators for a decoded Windows path.
		/// </summary>
		private static string? NormalizeDecodedWindowsPath(string path)
		{
			string windowsPath = path.Replace('/', '\\');

			try
			{
				string fullPath = Path.GetFullPath(windowsPath);

				fullPath = Path.TrimEndingDirectorySeparator(fullPath);

				if (fullPath.Length >= 2 && char.IsLetter(fullPath[0]) && fullPath[1] == ':')
				{
					fullPath = char.ToUpperInvariant(fullPath[0]) + fullPath.Substring(1);
				}

				return fullPath;
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				return null;
			}
		}

		/// <summary>
		/// Extracts the target path and effective VS Code launch flags while stripping Visual Studio-only options.
		/// </summary>
		private static LaunchSpecification? ExtractLaunchSpecification(VsItem item)
		{
			if (item is VsCodeProject codeProject)
			{
				return new LaunchSpecification(
					codeProject.Path!,
					codeProject.Instance,
					codeProject.WindowMode,
					codeProject.DisableExtensions,
					codeProject.Commands);
			}

			if (item is VsFolder folder)
			{
				VsCodeLaunchOptions? options = folder.VsCodeOptions;

				return new LaunchSpecification(
					folder.Path!,
					options?.Profile,
					options?.WindowMode ?? VsCodeWindowMode.Default,
					options?.DisableExtensions ?? false,
					options?.Commands);
			}

			if (item.IdeType == IdeType.VisualStudio)
			{
				if (item.ItemType != ItemTypeEnum.Solution && item.ItemType != ItemTypeEnum.Project)
				{
					return null;
				}

				string? containingDir = Path.GetDirectoryName(item.Path);
				string targetPath     = !string.IsNullOrWhiteSpace(containingDir) ? containingDir : item.Path!;

				return new LaunchSpecification(targetPath, null, VsCodeWindowMode.Default, false, null);
			}

			if (item.ItemType == ItemTypeEnum.Solution || item.ItemType == ItemTypeEnum.Project)
			{
				return new LaunchSpecification(item.Path!, item.Instance, VsCodeWindowMode.Default, false, item.Commands);
			}

			return null;
		}

		/// <summary>
		/// Builds the ordered VS Code CLI argument string from a resolved launch specification.
		/// </summary>
		private static string BuildCliArguments(LaunchSpecification spec, bool shouldWait)
		{
			var args = new List<string> { QuotePathArgument(spec.TargetPath) };

			if (!string.IsNullOrWhiteSpace(spec.Profile))
			{
				string trimmedProfile = spec.Profile.Trim();

				if (!string.Equals(trimmedProfile, "<default>", StringComparison.OrdinalIgnoreCase))
				{
					args.Add($"--profile \"{trimmedProfile}\"");
				}
			}

			if (spec.WindowMode == VsCodeWindowMode.NewWindow)
			{
				args.Add("--new-window");
			}
			else if (spec.WindowMode == VsCodeWindowMode.ReuseWindow)
			{
				args.Add("--reuse-window");
			}

			if (spec.DisableExtensions)
			{
				args.Add("--disable-extensions");
			}

			if (shouldWait)
			{
				args.Add("--wait");
			}

			if (!string.IsNullOrWhiteSpace(spec.Commands))
			{
				args.Add(spec.Commands.Trim());
			}

			return string.Join(' ', args);
		}

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> invoking <c>cli.js</c> via <c>ELECTRON_RUN_AS_NODE=1</c> when <c>cli.js</c> exists.
		/// </summary>
		private ProcessStartInfo? TryBuildCliNodeStartInfo(IIdeInstance instance, string baseArgs)
		{
			string? cliJsPath = ResolveCliScriptPath(instance);

			if (string.IsNullOrWhiteSpace(cliJsPath))
			{
				return null;
			}

			var nodeStartInfo = new ProcessStartInfo
			{
				FileName        = instance.Location,
				Arguments       = $"{QuotePathArgument(cliJsPath)} {baseArgs}",
				UseShellExecute = false,
				Verb            = string.Empty,
				ErrorDialog     = false
			};

			nodeStartInfo.Environment["ELECTRON_RUN_AS_NODE"] = "1";

			return nodeStartInfo;
		}

		/// <summary>
		/// Resolves <c>cli.js</c> using the target instance's environment when available, falling back to the provider's environment.
		/// </summary>
		private string? ResolveCliScriptPath(IIdeInstance instance)
		{
			if (instance is VsCodeInstance vsCodeInstance)
			{
				string? fromInstance = vsCodeInstance.GetCliScriptPath();

				if (!string.IsNullOrWhiteSpace(fromInstance) || !ReferenceEquals(vsCodeInstance.Environment, WindowsVsCodeEnvironment.Instance))
				{
					return fromInstance;
				}
			}

			return FindCliScriptPath(this.environment, instance.Location);
		}

		/// <summary>
		/// Returns the canonical full path for a candidate path, or <c>null</c> if invalid.
		/// </summary>
		private static string? TryGetCanonicalPath(string path)
		{
			try
			{
				return Path.GetFullPath(path);
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				return null;
			}
		}

		/// <summary>
		/// Computes a short deterministic lowercase hexadecimal hash for a canonical executable path.
		/// </summary>
		private static string ComputePathHash(string canonicalPath)
		{
			byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPath.ToUpperInvariant()));

			return Convert.ToHexString(hashBytes, 0, 4).ToLowerInvariant();
		}

		/// <summary>
		/// Copies a potentially shared/locked source file to a destination path using permissive read sharing.
		/// </summary>
		private static void CopyFileShared(string sourcePath, string destinationPath)
		{
			using var source      = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

			source.CopyTo(destination);
		}

		/// <summary>
		/// Copies an optional SQLite sidecar file when it exists alongside the database.
		/// </summary>
		private static void CopySidecarIfExists(string sourcePath, string destinationPath)
		{
			if (File.Exists(sourcePath))
			{
				CopyFileShared(sourcePath, destinationPath);
			}
		}

		/// <summary>
		/// Attempts to copy an optional sidecar file such as <c>-shm</c>, cleaning any partial copy if a lock prevents reading it.
		/// </summary>
		private static void TryCopyOptionalSidecar(string sourcePath, string destinationPath)
		{
			if (!File.Exists(sourcePath))
			{
				return;
			}

			try
			{
				CopyFileShared(sourcePath, destinationPath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
				TryDeleteFile(destinationPath);
			}
		}

		/// <summary>
		/// Best-effort deletion of a single temporary file.
		/// </summary>
		private static void TryDeleteFile(string filePath)
		{
			try
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Best-effort deletion of a temporary directory created during MRU fallback reading.
		/// </summary>
		private static void TryDeleteDirectory(string directoryPath)
		{
			try
			{
				if (Directory.Exists(directoryPath))
				{
					Directory.Delete(directoryPath, recursive: true);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Classifies how a VS Code installation was discovered.
		/// </summary>
		private enum InstallScope
		{
			User,
			System,
			Custom
		}

		/// <summary>
		/// Mutable state tracked while probing discovery sources in priority order.
		/// </summary>
		private sealed class DiscoveryState
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="DiscoveryState"/> class.
			/// </summary>
			/// <param name="appData">The resolved roaming AppData path.</param>
			public DiscoveryState(string appData)
			{
				this.AppData = appData;
			}

			/// <summary>
			/// Gets the discovered candidates collected so far.
			/// </summary>
			public List<DiscoveredCandidate> Candidates { get; } = new List<DiscoveredCandidate>();

			/// <summary>
			/// Gets the canonical executable paths already visited.
			/// </summary>
			public HashSet<string> SeenPaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			/// <summary>
			/// Gets the identifiers already assigned.
			/// </summary>
			public HashSet<string> UsedIdentifiers { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			/// <summary>
			/// Gets the roaming AppData path used to construct edition user data directories.
			/// </summary>
			public string AppData { get; }
		}

		/// <summary>
		/// Intermediate candidate metadata collected prior to final display-name resolution.
		/// </summary>
		private sealed record DiscoveredCandidate(
			string CanonicalPath,
			string Version,
			string UserDataDirectory,
			bool IsInsiders,
			InstallScope Scope,
			string Identifier);

		/// <summary>
		/// Normalized VS Code launch target and option flags extracted from an item.
		/// </summary>
		private sealed record LaunchSpecification(
			string TargetPath,
			string? Profile,
			VsCodeWindowMode WindowMode,
			bool DisableExtensions,
			string? Commands);
	}
}
