using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Data.Sqlite;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Provides Visual Studio Code and VS Code-based fork discovery, <c>state.vscdb</c> MRU loading, and CLI / <c>cli.js --wait</c> launch preparation.
	/// </summary>
	public class VsCodeIdeProvider : IIdeProvider
	{
		private const string VsCodeDownloadUrl = "https://code.visualstudio.com/download";
		private const string RecentlyOpenedKey = "history.recentlyOpenedPathsList";

		private static readonly Regex NonSlugCharsRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private readonly IVsCodeEnvironment environment;
		private readonly CustomVsCodeIdeStore customStore;
		private volatile IReadOnlyList<VsCodeInstance> instances;

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
		/// <param name="customStore">Optional custom IDE store used to load manually registered IDEs.</param>
		public VsCodeIdeProvider(IVsCodeEnvironment environment, CustomVsCodeIdeStore? customStore = null)
		{
			ArgumentNullException.ThrowIfNull(environment);

			this.environment = environment;
			this.customStore = customStore ?? new CustomVsCodeIdeStore(environment);
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
				IReadOnlyList<VsCodeInstance> current = this.instances;

				return current.FirstOrDefault(i => i.Identifier.StartsWith("vscode-stable-", StringComparison.OrdinalIgnoreCase))
					?? current.FirstOrDefault(i => i.Identifier.StartsWith("vscode-insiders-", StringComparison.OrdinalIgnoreCase))
					?? current.FirstOrDefault();
			}
		}

		/// <summary>
		/// Re-runs discovery across standard paths, generic program directories, registry, <c>PATH</c>, and the custom IDE store, replacing the cached instance list atomically.
		/// </summary>
		public void Refresh()
		{
			this.instances = DiscoverInstances();
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
		/// Probes all five Windows discovery sources in priority order and materializes <see cref="VsCodeInstance"/> entries.
		/// </summary>
		/// <returns>A read-only list of discovered installations.</returns>
		private IReadOnlyList<VsCodeInstance> DiscoverInstances()
		{
			string localAppData   = this.environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string programFiles   = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string programFiles86 = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			string appData        = this.environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var state             = new DiscoveryState(appData, programFiles, programFiles86);

			DiscoverKnownStandardCandidates(state, localAppData, programFiles, programFiles86);
			DiscoverGenericProgramDirectories(state, localAppData, programFiles, programFiles86);
			DiscoverFromRegistry(state);
			DiscoverFromPath(state);
			DiscoverFromCustomStore(state);

			return BuildInstances(state.Candidates);
		}

		/// <summary>
		/// Probes standard per-user and machine-wide directories for every product in <see cref="VsCodeProductDescriptor.KnownProducts"/>.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="localAppData">The resolved <c>%LocalAppData%</c> path.</param>
		/// <param name="programFiles">The resolved <c>%ProgramFiles%</c> path.</param>
		/// <param name="programFiles86">The resolved <c>%ProgramFiles(x86)%</c> path.</param>
		private void DiscoverKnownStandardCandidates(
			DiscoveryState state,
			string? localAppData,
			string? programFiles,
			string? programFiles86)
		{
			foreach (VsCodeProductDescriptor descriptor in VsCodeProductDescriptor.KnownProducts)
			{
				AddStandardCandidatesForDescriptor(state, descriptor, localAppData, programFiles, programFiles86);
			}
		}

		/// <summary>
		/// Probes User and System standard subdirectories for a single <see cref="VsCodeProductDescriptor"/>.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="descriptor">The product descriptor to probe.</param>
		/// <param name="localAppData">The resolved <c>%LocalAppData%</c> path.</param>
		/// <param name="programFiles">The resolved <c>%ProgramFiles%</c> path.</param>
		/// <param name="programFiles86">The resolved <c>%ProgramFiles(x86)%</c> path.</param>
		private void AddStandardCandidatesForDescriptor(
			DiscoveryState state,
			VsCodeProductDescriptor descriptor,
			string? localAppData,
			string? programFiles,
			string? programFiles86)
		{
			foreach (string relativeDir in descriptor.UserRelativeDirs)
			{
				TryAddStandardDirectoryCandidates(state, localAppData, relativeDir, descriptor, InstallScope.User);
			}

			foreach (string relativeDir in descriptor.SystemRelativeDirs)
			{
				TryAddStandardDirectoryCandidates(state, programFiles, relativeDir, descriptor, InstallScope.System);
				TryAddStandardDirectoryCandidates(state, programFiles86, relativeDir, descriptor, InstallScope.System);
			}
		}

		/// <summary>
		/// Probes known executable filenames inside a standard root-relative installation directory.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="rootFolder">The base special folder path.</param>
		/// <param name="relativeSubDir">The product-specific subdirectory under <paramref name="rootFolder"/>.</param>
		/// <param name="descriptor">The product descriptor.</param>
		/// <param name="scope">The installation scope.</param>
		private void TryAddStandardDirectoryCandidates(
			DiscoveryState state,
			string? rootFolder,
			string relativeSubDir,
			VsCodeProductDescriptor descriptor,
			InstallScope scope)
		{
			if (string.IsNullOrWhiteSpace(rootFolder))
			{
				return;
			}

			string installDir = Path.Combine(rootFolder, relativeSubDir);

			TryAddDescriptorExecutablesInDir(state, installDir, descriptor, scope);
		}

		/// <summary>
		/// Probes immediate subdirectories of standard Windows program roots for VS Code-based forks containing both <c>product.json</c> and <c>cli.js</c>.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="localAppData">The resolved <c>%LocalAppData%</c> path.</param>
		/// <param name="programFiles">The resolved <c>%ProgramFiles%</c> path.</param>
		/// <param name="programFiles86">The resolved <c>%ProgramFiles(x86)%</c> path.</param>
		private void DiscoverGenericProgramDirectories(
			DiscoveryState state,
			string? localAppData,
			string? programFiles,
			string? programFiles86)
		{
			if (!string.IsNullOrWhiteSpace(localAppData))
			{
				ProbeProgramRootDirectory(state, Path.Combine(localAppData, "Programs"), InstallScope.User);
			}

			ProbeProgramRootDirectory(state, programFiles, InstallScope.System);
			ProbeProgramRootDirectory(state, programFiles86, InstallScope.System);
		}

		/// <summary>
		/// Enumerates immediate subdirectories under <paramref name="rootDirectory"/> and probes each for a valid VS Code layout.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="rootDirectory">The parent program directory to inspect.</param>
		/// <param name="scope">The scope assigned to discovered installations under <paramref name="rootDirectory"/>.</param>
		private void ProbeProgramRootDirectory(DiscoveryState state, string? rootDirectory, InstallScope scope)
		{
			if (string.IsNullOrWhiteSpace(rootDirectory))
			{
				return;
			}

			IReadOnlyList<string> subDirs = GetDirectoriesSafe(rootDirectory);

			foreach (string subDir in subDirs.OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrWhiteSpace(subDir))
				{
					TryAddGenericInstallDirectory(state, subDir, scope);
				}
			}
		}

		/// <summary>
		/// Validates that <paramref name="installDir"/> contains both <c>resources\app\product.json</c> and <c>resources\app\out\cli.js</c>, resolves its main executable, and records it.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="installDir">The candidate installation directory.</param>
		/// <param name="scope">The installation scope.</param>
		/// <returns><c>true</c> if a candidate was added; otherwise <c>false</c>.</returns>
		private bool TryAddGenericInstallDirectory(DiscoveryState state, string installDir, InstallScope scope)
		{
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(installDir, "resources", "app", "out", "cli.js");

			if (!this.environment.FileExists(productJsonPath) || !this.environment.FileExists(cliJsPath))
			{
				return false;
			}

			JObject? productJson = TryReadProductJson(this.environment, productJsonPath);

			if (productJson is null)
			{
				return false;
			}

			string? executablePath = FindMainExecutableInDirectory(installDir, productJson);

			if (string.IsNullOrWhiteSpace(executablePath))
			{
				return false;
			}

			VsCodeProductDescriptor? descriptor = ResolveDescriptorFromProductJson(installDir, executablePath);

			if (descriptor is null)
			{
				return false;
			}

			return TryAddCandidate(state, executablePath, descriptor, scope, isCustom: false);
		}

		/// <summary>
		/// Probes Windows Uninstall registry entries for known VS Code products and generic forks with <c>product.json</c> and <c>cli.js</c>.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		private void DiscoverFromRegistry(DiscoveryState state)
		{
			IReadOnlyList<VsCodeUninstallEntry> entries = this.environment.GetUninstallEntries() ?? Array.Empty<VsCodeUninstallEntry>();

			foreach (VsCodeUninstallEntry entry in entries)
			{
				ProcessUninstallEntry(state, entry);
			}
		}

		/// <summary>
		/// Inspects a single Windows Uninstall entry by known display-name prefix or verified VS Code directory layout.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="entry">The uninstall registry entry.</param>
		private void ProcessUninstallEntry(DiscoveryState state, VsCodeUninstallEntry? entry)
		{
			if (entry is null || string.IsNullOrWhiteSpace(entry.InstallLocation))
			{
				return;
			}

			IReadOnlyList<VsCodeProductDescriptor> matchingDescriptors = FindMatchingDescriptorsByUninstallPrefix(entry.DisplayName);
			bool addedKnown                                            = false;

			foreach (VsCodeProductDescriptor descriptor in matchingDescriptors)
			{
				if (TryAddDescriptorExecutablesInDir(state, entry.InstallLocation, descriptor, InstallScope.Custom))
				{
					addedKnown = true;
				}
			}

			if (!addedKnown)
			{
				TryAddGenericInstallDirectory(state, entry.InstallLocation, InstallScope.Custom);
			}
		}

		/// <summary>
		/// Probes directories on <c>PATH</c> for known CLI wrapper scripts and resolves their parent executables.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
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

				if (!string.IsNullOrWhiteSpace(dir))
				{
					ProbePathDirectory(state, dir);
				}
			}
		}

		/// <summary>
		/// Checks a single <c>PATH</c> directory for any known product's CLI wrapper scripts.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="binDirectory">The directory entry from <c>PATH</c>.</param>
		private void ProbePathDirectory(DiscoveryState state, string binDirectory)
		{
			foreach (VsCodeProductDescriptor descriptor in VsCodeProductDescriptor.KnownProducts)
			{
				foreach (string cmdFileName in descriptor.PathCommandNames)
				{
					TryAddFromPathCommand(state, binDirectory, cmdFileName, descriptor);
				}
			}
		}

		/// <summary>
		/// Checks a single <c>PATH</c> directory for a CLI wrapper script and adds its parent directory executable if present.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="binDirectory">The directory containing CLI scripts.</param>
		/// <param name="cmdFileName">The wrapper script filename.</param>
		/// <param name="descriptor">The associated product descriptor.</param>
		private void TryAddFromPathCommand(
			DiscoveryState state,
			string binDirectory,
			string cmdFileName,
			VsCodeProductDescriptor descriptor)
		{
			try
			{
				string cmdPath = Path.Combine(binDirectory, cmdFileName);

				if (!this.environment.FileExists(cmdPath))
				{
					return;
				}

				foreach (string exeFileName in descriptor.ExecutableNames)
				{
					string siblingExePath = Path.Combine(binDirectory, "..", exeFileName);

					if (TryAddCandidate(state, siblingExePath, descriptor, InstallScope.Custom, isCustom: false))
					{
						return;
					}

					string appParentExePath = Path.Combine(binDirectory, "..", "..", "..", exeFileName);

					if (TryAddCandidate(state, appParentExePath, descriptor, InstallScope.Custom, isCustom: false))
					{
						return;
					}
				}
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Loads manually registered entries from <see cref="CustomVsCodeIdeStore"/> without mutating missing entries, deduplicates against auto-discovered installs, and applies user overrides.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		private void DiscoverFromCustomStore(DiscoveryState state)
		{
			IReadOnlyList<CustomVsCodeIdeEntry> entries = this.customStore.Load() ?? Array.Empty<CustomVsCodeIdeEntry>();

			foreach (CustomVsCodeIdeEntry entry in entries)
			{
				AddCustomStoreEntry(state, entry);
			}
		}

		/// <summary>
		/// Validates, inspects, and records a single entry loaded from <see cref="CustomVsCodeIdeStore"/>.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="entry">The persisted custom IDE entry.</param>
		private void AddCustomStoreEntry(DiscoveryState state, CustomVsCodeIdeEntry? entry)
		{
			if (entry is null || string.IsNullOrWhiteSpace(entry.ExecutablePath))
			{
				return;
			}

			string? canonicalPath = TryGetCanonicalPath(entry.ExecutablePath);

			if (string.IsNullOrWhiteSpace(canonicalPath) || !this.environment.FileExists(canonicalPath))
			{
				return;
			}

			string installDir                  = Path.GetDirectoryName(canonicalPath) ?? string.Empty;
			VsCodeProductDescriptor descriptor = ResolveCustomStoreDescriptor(installDir, canonicalPath, entry);
			string? userDataOverride           = string.IsNullOrWhiteSpace(entry.UserDataDirectory)
				? null
				: TryGetCanonicalPath(entry.UserDataDirectory.Trim()) ?? entry.UserDataDirectory.Trim();

			TryAddCandidate(state, canonicalPath, descriptor, InstallScope.Custom, isCustom: true, userDataOverride: userDataOverride);
		}

		/// <summary>
		/// Resolves a descriptor for a custom store executable using <c>product.json</c> when present and applying user display-name overrides.
		/// </summary>
		/// <param name="installDir">The directory containing the executable.</param>
		/// <param name="canonicalPath">The canonical executable path.</param>
		/// <param name="entry">The persisted custom IDE entry.</param>
		/// <returns>The effective product descriptor.</returns>
		private VsCodeProductDescriptor ResolveCustomStoreDescriptor(
			string installDir,
			string canonicalPath,
			CustomVsCodeIdeEntry entry)
		{
			VsCodeProductDescriptor? parsed        = string.IsNullOrWhiteSpace(installDir)
				? null
				: ResolveDescriptorFromProductJson(installDir, canonicalPath);
			VsCodeProductDescriptor baseDescriptor = parsed ?? CreateFallbackCustomDescriptor(canonicalPath, entry.DisplayName);

			if (string.IsNullOrWhiteSpace(entry.DisplayName))
			{
				return baseDescriptor;
			}

			string customName = entry.DisplayName.Trim();

			return baseDescriptor with
			{
				DisplayName = customName,
				ShortName   = customName
			};
		}

		/// <summary>
		/// Probes all executable filenames defined on <paramref name="descriptor"/> within <paramref name="installDir"/>.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="installDir">The installation directory to probe.</param>
		/// <param name="descriptor">The product descriptor.</param>
		/// <param name="scope">The installation scope.</param>
		/// <returns><c>true</c> if at least one candidate was added; otherwise <c>false</c>.</returns>
		private bool TryAddDescriptorExecutablesInDir(
			DiscoveryState state,
			string installDir,
			VsCodeProductDescriptor descriptor,
			InstallScope scope)
		{
			bool added = false;

			foreach (string exeName in descriptor.ExecutableNames)
			{
				string candidatePath = Path.Combine(installDir, exeName);

				if (TryAddCandidate(state, candidatePath, descriptor, scope, isCustom: false))
				{
					added = true;
				}
			}

			return added;
		}

		/// <summary>
		/// Validates, canonicalizes, deduplicates, and records a discovered executable candidate.
		/// </summary>
		/// <param name="state">The shared discovery state.</param>
		/// <param name="candidatePath">The candidate executable path.</param>
		/// <param name="descriptor">The product descriptor.</param>
		/// <param name="scope">The installation scope.</param>
		/// <param name="isCustom">Whether the entry originated from <see cref="CustomVsCodeIdeStore"/>.</param>
		/// <param name="userDataOverride">Optional explicit user data directory override.</param>
		/// <returns><c>true</c> if the candidate was added; otherwise <c>false</c>.</returns>
		private bool TryAddCandidate(
			DiscoveryState state,
			string candidatePath,
			VsCodeProductDescriptor descriptor,
			InstallScope scope,
			bool isCustom,
			string? userDataOverride = null)
		{
			string? canonicalPath = TryGetCanonicalPath(candidatePath);

			if (string.IsNullOrWhiteSpace(canonicalPath) || !this.environment.FileExists(canonicalPath))
			{
				return false;
			}

			if (!state.SeenPaths.Add(canonicalPath))
			{
				return false;
			}

			string identifier    = AssignIdentifier(descriptor, scope, canonicalPath, state.UsedIdentifiers);
			string version       = ResolveVersion(canonicalPath);
			string userData      = ResolveCandidateUserDataDirectory(state.AppData, descriptor, userDataOverride);
			bool isSystemInstall = scope == InstallScope.System
				|| IsPathUnderDirectory(canonicalPath, state.ProgramFiles)
				|| IsPathUnderDirectory(canonicalPath, state.ProgramFiles86);

			state.Candidates.Add(new DiscoveredCandidate(
				canonicalPath,
				version,
				userData,
				descriptor,
				scope,
				identifier)
			{
				IsSystemInstall = isSystemInstall,
				IsCustom        = isCustom
			});

			return true;
		}

		/// <summary>
		/// Materializes final <see cref="VsCodeInstance"/> objects with per-flavor User/System display-name disambiguation.
		/// </summary>
		/// <param name="candidates">The ordered list of discovered candidates.</param>
		/// <returns>A read-only list of materialized instances.</returns>
		private IReadOnlyList<VsCodeInstance> BuildInstances(List<DiscoveredCandidate> candidates)
		{
			var result = new List<VsCodeInstance>(candidates.Count);

			foreach (DiscoveredCandidate candidate in candidates)
			{
				bool hasBothScopes = candidate.Scope != InstallScope.Custom
					&& HasBothScopesForFlavor(candidates, candidate.Descriptor.FlavorId);
				string displayName = ResolveDisplayName(candidate.Descriptor, candidate.Scope, hasBothScopes);

				result.Add(new VsCodeInstance(
					displayName,
					candidate.Version,
					candidate.CanonicalPath,
					candidate.Identifier,
					candidate.UserDataDirectory,
					candidate.Descriptor,
					this.environment)
				{
					IsSystemInstall = candidate.IsSystemInstall,
					IsCustom        = candidate.IsCustom
				});
			}

			return result;
		}

		/// <summary>
		/// Assigns a deterministic identifier matching the flavor and scope, falling back to a path hash for custom or duplicate installs.
		/// </summary>
		private static string AssignIdentifier(
			VsCodeProductDescriptor descriptor,
			InstallScope scope,
			string canonicalPath,
			HashSet<string> usedIdentifiers)
		{
			string? preferred = scope switch
			{
				InstallScope.User   => $"{descriptor.FlavorId}-user",
				InstallScope.System => $"{descriptor.FlavorId}-system",
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
		/// Resolves a product descriptor from <c>product.json</c> and <c>cli.js</c>, matching a known built-in product when applicable.
		/// </summary>
		private VsCodeProductDescriptor? ResolveDescriptorFromProductJson(string installDir, string executablePath)
		{
			VsCodeProductDescriptor? parsed = VsCodeProductDescriptor.TryCreateFromProductJson(
				this.environment,
				installDir,
				executablePath);

			if (parsed is null)
			{
				return null;
			}

			return MatchKnownDescriptor(parsed) ?? parsed;
		}

		/// <summary>
		/// Matches a parsed <c>product.json</c> descriptor against <see cref="VsCodeProductDescriptor.KnownProducts"/> by flavor, display name, or short name.
		/// </summary>
		private static VsCodeProductDescriptor? MatchKnownDescriptor(VsCodeProductDescriptor parsed)
		{
			foreach (VsCodeProductDescriptor known in VsCodeProductDescriptor.KnownProducts)
			{
				if (known.FlavorId.Equals(parsed.FlavorId, StringComparison.OrdinalIgnoreCase)
					|| known.DisplayName.Equals(parsed.DisplayName, StringComparison.OrdinalIgnoreCase)
					|| known.ShortName.Equals(parsed.ShortName, StringComparison.OrdinalIgnoreCase))
				{
					return known;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds known descriptors whose <see cref="VsCodeProductDescriptor.UninstallNamePrefixes"/> match <paramref name="displayName"/>, ordered longest prefix first.
		/// </summary>
		private static IReadOnlyList<VsCodeProductDescriptor> FindMatchingDescriptorsByUninstallPrefix(string? displayName)
		{
			if (string.IsNullOrWhiteSpace(displayName))
			{
				return Array.Empty<VsCodeProductDescriptor>();
			}

			var matches = new List<(VsCodeProductDescriptor Descriptor, int PrefixLength)>();

			foreach (VsCodeProductDescriptor descriptor in VsCodeProductDescriptor.KnownProducts)
			{
				int longestPrefix = 0;

				foreach (string prefix in descriptor.UninstallNamePrefixes)
				{
					if (!string.IsNullOrWhiteSpace(prefix)
						&& displayName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
						&& prefix.Length > longestPrefix)
					{
						longestPrefix = prefix.Length;
					}
				}

				if (longestPrefix > 0)
				{
					matches.Add((descriptor, longestPrefix));
				}
			}

			return matches
				.OrderByDescending(m => m.PrefixLength)
				.Select(m => m.Descriptor)
				.ToList();
		}

		/// <summary>
		/// Selects the primary application executable in a verified VS Code-based installation directory, excluding uninstallers.
		/// </summary>
		private string? FindMainExecutableInDirectory(string installDir, JObject productJson)
		{
			IReadOnlyList<string> allFiles = GetFilesSafe(installDir, "*.exe");

			if (allFiles.Count == 0)
			{
				return ProbeFallbackExecutableNames(installDir, productJson);
			}

			List<string> candidates = FilterExecutableCandidates(allFiles);

			if (candidates.Count == 0)
			{
				return null;
			}

			return FindPreferredExecutable(installDir, candidates, productJson) ?? candidates[0];
		}

		/// <summary>
		/// Filters out null entries and uninstaller executables (<c>unins*.exe</c> / <c>Uninstall*.exe</c>) and sorts deterministically by filename.
		/// </summary>
		private static List<string> FilterExecutableCandidates(IReadOnlyList<string> allFiles)
		{
			var candidates = new List<string>();

			foreach (string file in allFiles)
			{
				if (string.IsNullOrWhiteSpace(file))
				{
					continue;
				}

				string fileName = Path.GetFileName(file);

				if (!IsUninstallerExecutable(fileName))
				{
					candidates.Add(file);
				}
			}

			candidates.Sort((left, right) => string.Compare(
				Path.GetFileName(left),
				Path.GetFileName(right),
				StringComparison.OrdinalIgnoreCase));

			return candidates;
		}

		/// <summary>
		/// Selects the candidate executable matching <c>nameShort</c>, <c>win32DirName</c>, directory name, <c>nameLong</c>, <c>applicationName</c>, or known executable names.
		/// </summary>
		private static string? FindPreferredExecutable(string installDir, List<string> candidates, JObject productJson)
		{
			foreach (string stem in GetPreferredExecutableStems(installDir, productJson))
			{
				string? match = candidates.FirstOrDefault(
					c => Path.GetFileNameWithoutExtension(c).Equals(stem, StringComparison.OrdinalIgnoreCase));

				if (!string.IsNullOrWhiteSpace(match))
				{
					return match;
				}
			}

			return FindKnownExecutableMatch(candidates);
		}

		/// <summary>
		/// Probes expected executable names directly via <see cref="IVsCodeEnvironment.FileExists"/> when directory enumeration yields no entries.
		/// </summary>
		private string? ProbeFallbackExecutableNames(string installDir, JObject productJson)
		{
			foreach (string stem in GetPreferredExecutableStems(installDir, productJson))
			{
				string candidatePath = Path.Combine(installDir, $"{stem}.exe");

				if (this.environment.FileExists(candidatePath))
				{
					return candidatePath;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns ordered candidate executable name stems derived from <c>product.json</c> and the installation folder name.
		/// </summary>
		private static IEnumerable<string> GetPreferredExecutableStems(string installDir, JObject productJson)
		{
			string dirName = Path.GetFileName(Path.TrimEndingDirectorySeparator(installDir));
			string?[] rawStems =
			[
				TryGetStringProperty(productJson, "nameShort")?.Trim(),
				TryGetStringProperty(productJson, "win32DirName")?.Trim(),
				dirName,
				TryGetStringProperty(productJson, "nameLong")?.Trim(),
				TryGetStringProperty(productJson, "applicationName")?.Trim()
			];

			foreach (string? stem in rawStems)
			{
				if (!string.IsNullOrWhiteSpace(stem) && !IsUninstallerExecutable(stem))
				{
					yield return stem;
				}
			}
		}

		/// <summary>
		/// Finds a candidate whose filename matches any executable defined in <see cref="VsCodeProductDescriptor.KnownProducts"/>.
		/// </summary>
		private static string? FindKnownExecutableMatch(List<string> candidates)
		{
			foreach (VsCodeProductDescriptor descriptor in VsCodeProductDescriptor.KnownProducts)
			{
				foreach (string exeName in descriptor.ExecutableNames)
				{
					string? match = candidates.FirstOrDefault(
						c => Path.GetFileName(c).Equals(exeName, StringComparison.OrdinalIgnoreCase));

					if (!string.IsNullOrWhiteSpace(match))
					{
						return match;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns <c>true</c> if a filename corresponds to an uninstaller executable (<c>unins*.exe</c> / <c>Uninstall*.exe</c>).
		/// </summary>
		private static bool IsUninstallerExecutable(string? fileName)
		{
			return string.IsNullOrWhiteSpace(fileName)
				|| fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Safely enumerates subdirectories via <see cref="IVsCodeEnvironment.GetDirectories"/>, guarding against null returns from test mocks.
		/// </summary>
		private IReadOnlyList<string> GetDirectoriesSafe(string? path)
		{
			return this.environment.GetDirectories(path) ?? Array.Empty<string>();
		}

		/// <summary>
		/// Safely enumerates files via <see cref="IVsCodeEnvironment.GetFiles"/>, guarding against null returns from test mocks.
		/// </summary>
		private IReadOnlyList<string> GetFilesSafe(string? path, string searchPattern)
		{
			return this.environment.GetFiles(path, searchPattern) ?? Array.Empty<string>();
		}

		/// <summary>
		/// Attempts to read and parse <c>product.json</c> via <paramref name="environment"/>.
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
		/// Creates a fallback <see cref="VsCodeProductDescriptor"/> for a custom IDE executable lacking <c>product.json</c> metadata.
		/// </summary>
		private static VsCodeProductDescriptor CreateFallbackCustomDescriptor(string canonicalPath, string? customDisplayName)
		{
			string exeName       = Path.GetFileName(canonicalPath) ?? string.Empty;
			string exeStem       = Path.GetFileNameWithoutExtension(canonicalPath) ?? "vscode-custom";
			string effectiveName = !string.IsNullOrWhiteSpace(customDisplayName) ? customDisplayName.Trim() : exeStem;

			return new VsCodeProductDescriptor
			{
				FlavorId           = SlugifyFlavorId(effectiveName),
				DisplayName        = effectiveName,
				ShortName          = effectiveName,
				ExecutableNames    = string.IsNullOrWhiteSpace(exeName) ? Array.Empty<string>() : [exeName],
				UserDataFolderName = exeStem,
				UpdateStrategy     = VsCodeUpdateStrategyKind.Unsupported,
				InstallerKind      = VsCodeInstallerKind.InnoSetupGeneric,
				IsInsiders         = false
			};
		}

		/// <summary>
		/// Resolves the effective user data directory for a discovered candidate.
		/// </summary>
		private static string ResolveCandidateUserDataDirectory(
			string appData,
			VsCodeProductDescriptor descriptor,
			string? userDataOverride)
		{
			if (!string.IsNullOrWhiteSpace(userDataOverride))
			{
				return userDataOverride;
			}

			if (string.IsNullOrWhiteSpace(appData) || string.IsNullOrWhiteSpace(descriptor.UserDataFolderName))
			{
				return string.Empty;
			}

			return Path.Combine(appData, descriptor.UserDataFolderName);
		}

		/// <summary>
		/// Determines whether <paramref name="filePath"/> resides within <paramref name="rootDirectory"/>.
		/// </summary>
		private static bool IsPathUnderDirectory(string filePath, string? rootDirectory)
		{
			if (string.IsNullOrWhiteSpace(rootDirectory))
			{
				return false;
			}

			string? canonicalRoot = TryGetCanonicalPath(rootDirectory);

			if (string.IsNullOrWhiteSpace(canonicalRoot))
			{
				return false;
			}

			string normalizedPrefix = Path.TrimEndingDirectorySeparator(canonicalRoot) + Path.DirectorySeparatorChar;

			return filePath.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Converts a display name into a lowercase ASCII alphanumeric slug separated by hyphens.
		/// </summary>
		private static string SlugifyFlavorId(string value)
		{
			string slug = NonSlugCharsRegex.Replace(value.ToLowerInvariant(), "-").Trim('-');

			return string.IsNullOrEmpty(slug) ? "vscode-custom" : slug;
		}

		/// <summary>
		/// Resolves the product version from executable file metadata or falls back to <c>resources\app\package.json</c>.
		/// </summary>
		/// <param name="environment">The environment abstraction used for version and file reads.</param>
		/// <param name="executablePath">The path to <c>Code.exe</c> or <c>Code - Insiders.exe</c>.</param>
		/// <returns>The resolved product version string, or an empty string if unavailable.</returns>
		internal static string ResolveVersion(IVsCodeEnvironment environment, string executablePath)
		{
			string? fileVersion = environment.GetFileVersion(executablePath);

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

			if (!environment.FileExists(packageJsonPath))
			{
				return string.Empty;
			}

			try
			{
				string json  = environment.ReadAllText(packageJsonPath);
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
		/// Resolves the product version from executable file metadata or falls back to <c>resources\app\package.json</c>.
		/// </summary>
		private string ResolveVersion(string executablePath)
		{
			return ResolveVersion(this.environment, executablePath);
		}

		/// <summary>
		/// Determines whether both User and System installations exist for a given product flavor.
		/// </summary>
		private static bool HasBothScopesForFlavor(List<DiscoveredCandidate> candidates, string flavorId)
		{
			bool hasUser   = candidates.Any(c => c.Descriptor.FlavorId.Equals(flavorId, StringComparison.OrdinalIgnoreCase) && c.Scope == InstallScope.User);
			bool hasSystem = candidates.Any(c => c.Descriptor.FlavorId.Equals(flavorId, StringComparison.OrdinalIgnoreCase) && c.Scope == InstallScope.System);

			return hasUser && hasSystem;
		}

		/// <summary>
		/// Computes the display name for a discovered installation according to whether both User and System scopes coexist for its flavor.
		/// </summary>
		private static string ResolveDisplayName(VsCodeProductDescriptor descriptor, InstallScope scope, bool hasBothScopes)
		{
			string shortName = !string.IsNullOrWhiteSpace(descriptor.ShortName) ? descriptor.ShortName : descriptor.DisplayName;

			if (hasBothScopes && scope == InstallScope.User)
			{
				return $"{shortName} (User)";
			}

			if (hasBothScopes && scope == InstallScope.System)
			{
				return $"{shortName} (System)";
			}

			return descriptor.DisplayName;
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
			/// <param name="programFiles">The resolved Program Files path.</param>
			/// <param name="programFiles86">The resolved Program Files (x86) path.</param>
			public DiscoveryState(string appData, string? programFiles, string? programFiles86)
			{
				this.AppData        = appData;
				this.ProgramFiles   = programFiles;
				this.ProgramFiles86 = programFiles86;
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

			/// <summary>
			/// Gets the resolved Program Files root directory.
			/// </summary>
			public string? ProgramFiles { get; }

			/// <summary>
			/// Gets the resolved Program Files (x86) root directory.
			/// </summary>
			public string? ProgramFiles86 { get; }
		}

		/// <summary>
		/// Intermediate candidate metadata collected prior to final display-name resolution.
		/// </summary>
		private sealed record DiscoveredCandidate(
			string CanonicalPath,
			string Version,
			string UserDataDirectory,
			VsCodeProductDescriptor Descriptor,
			InstallScope Scope,
			string Identifier)
		{
			/// <summary>
			/// Gets a value indicating whether this installation resides in a machine-wide system location.
			/// </summary>
			public bool IsSystemInstall { get; init; }

			/// <summary>
			/// Gets a value indicating whether this installation originated from <see cref="CustomVsCodeIdeStore"/>.
			/// </summary>
			public bool IsCustom { get; init; }
		}

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
