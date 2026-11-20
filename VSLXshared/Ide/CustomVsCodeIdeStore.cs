using System.Diagnostics;

using Newtonsoft.Json;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Describes the inspection metadata extracted from a candidate custom VS Code-based IDE executable.
	/// </summary>
	public sealed record CustomVsCodeIdeInspection
	{
		/// <summary>
		/// Gets the canonical full path to the inspected executable.
		/// </summary>
		public string ExecutablePath { get; init; } = string.Empty;

		/// <summary>
		/// Gets the suggested full display name for the IDE.
		/// </summary>
		public string SuggestedDisplayName { get; init; } = string.Empty;

		/// <summary>
		/// Gets the suggested compact name suitable for buttons and menu labels.
		/// </summary>
		public string SuggestedShortName { get; init; } = string.Empty;

		/// <summary>
		/// Gets the suggested <c>%AppData%</c> user data directory for MRU discovery.
		/// </summary>
		public string SuggestedUserDataDirectory { get; init; } = string.Empty;

		/// <summary>
		/// Gets the detected product version string, or an empty string if unavailable.
		/// </summary>
		public string Version { get; init; } = string.Empty;

		/// <summary>
		/// Gets a value indicating whether both <c>resources\app\product.json</c> and <c>resources\app\out\cli.js</c> were verified.
		/// </summary>
		public bool IsVerifiedVsCodeFork { get; init; }
	}

	/// <summary>
	/// Provides thread-safe, atomic JSON persistence and executable inspection for manually registered VS Code-based IDEs.
	/// </summary>
	public sealed class CustomVsCodeIdeStore
	{
		private const string StoreFolderName = "VSLauncher";
		private const string StoreFileName   = "vscode-custom-ides.json";

		private readonly IVsCodeEnvironment environment;
		private readonly string storeFilePath;
		private readonly object syncLock = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomVsCodeIdeStore"/> class.
		/// </summary>
		/// <param name="environment">Optional host environment abstraction used for filesystem and folder lookups.</param>
		/// <param name="storeFilePath">Optional explicit path to the JSON store file.</param>
		public CustomVsCodeIdeStore(IVsCodeEnvironment? environment = null, string? storeFilePath = null)
		{
			this.environment   = environment ?? WindowsVsCodeEnvironment.Instance;
			this.storeFilePath = string.IsNullOrWhiteSpace(storeFilePath)
				? ResolveDefaultStorePath(this.environment)
				: storeFilePath;
		}

		/// <summary>
		/// Gets the resolved path of the JSON file backing this store.
		/// </summary>
		public string StoreFilePath
		{
			get
			{
				return this.storeFilePath;
			}
		}

		/// <summary>
		/// Loads all valid, deduplicated custom IDE entries from the store file.
		/// </summary>
		/// <returns>A read-only list of persisted entries, or an empty list if the file is absent or corrupt.</returns>
		public IReadOnlyList<CustomVsCodeIdeEntry> Load()
		{
			lock (this.syncLock)
			{
				return LoadCore();
			}
		}

		/// <summary>
		/// Persists the supplied entries atomically after normalizing and deduplicating by canonical executable path.
		/// </summary>
		/// <param name="entries">The entries to persist.</param>
		public void Save(IEnumerable<CustomVsCodeIdeEntry> entries)
		{
			ArgumentNullException.ThrowIfNull(entries);

			lock (this.syncLock)
			{
				SaveCore(NormalizeAndDeduplicate(entries));
			}
		}

		/// <summary>
		/// Adds a custom IDE entry if no existing entry references the same canonical executable path.
		/// </summary>
		/// <param name="entry">The entry to add.</param>
		/// <returns><c>true</c> if the entry was added and saved; <c>false</c> if invalid or already present.</returns>
		public bool TryAdd(CustomVsCodeIdeEntry entry)
		{
			ArgumentNullException.ThrowIfNull(entry);

			CustomVsCodeIdeEntry? normalizedEntry = NormalizeEntry(entry);

			if (normalizedEntry is null)
			{
				return false;
			}

			lock (this.syncLock)
			{
				List<CustomVsCodeIdeEntry> current = LoadCore();

				if (current.Any(existing => existing.ExecutablePath.Equals(normalizedEntry.ExecutablePath, StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}

				current.Add(normalizedEntry);
				SaveCore(current);

				return true;
			}
		}

		/// <summary>
		/// Removes any stored entry matching <paramref name="executablePath"/> case-insensitively by canonical path.
		/// </summary>
		/// <param name="executablePath">The executable path to remove.</param>
		/// <returns><c>true</c> if at least one entry was removed and the store updated; otherwise <c>false</c>.</returns>
		public bool RemoveByPath(string executablePath)
		{
			string? normalizedTarget = TryNormalizePath(executablePath);

			if (normalizedTarget is null)
			{
				return false;
			}

			lock (this.syncLock)
			{
				List<CustomVsCodeIdeEntry> current = LoadCore();
				int removedCount                   = current.RemoveAll(
					e => e.ExecutablePath.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase));

				if (removedCount == 0)
				{
					return false;
				}

				SaveCore(current);

				return true;
			}
		}

		/// <summary>
		/// Inspects an existing <c>.exe</c> file to extract product metadata, version, and suggested user data directory.
		/// </summary>
		/// <param name="executablePath">The candidate executable path.</param>
		/// <returns>An inspection result when <paramref name="executablePath"/> is an existing <c>.exe</c>; otherwise <c>null</c>.</returns>
		public CustomVsCodeIdeInspection? TryInspectExecutable(string executablePath)
		{
			string? normalizedPath = TryNormalizePath(executablePath);

			if (normalizedPath is null
				|| !Path.GetExtension(normalizedPath).Equals(".exe", StringComparison.OrdinalIgnoreCase)
				|| !this.environment.FileExists(normalizedPath))
			{
				return null;
			}

			string? installDir = Path.GetDirectoryName(normalizedPath);

			if (string.IsNullOrWhiteSpace(installDir))
			{
				return null;
			}

			VsCodeProductDescriptor? descriptor = VsCodeProductDescriptor.TryCreateFromProductJson(
				this.environment,
				installDir,
				normalizedPath);
			string version = VsCodeIdeProvider.ResolveVersion(this.environment, normalizedPath);
			string appData = this.environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			if (descriptor is not null)
			{
				return new CustomVsCodeIdeInspection
				{
					ExecutablePath             = normalizedPath,
					SuggestedDisplayName       = descriptor.DisplayName,
					SuggestedShortName         = descriptor.ShortName,
					SuggestedUserDataDirectory = ResolveSuggestedUserDataDirectory(appData, descriptor.UserDataFolderName),
					Version                    = version,
					IsVerifiedVsCodeFork       = true
				};
			}

			string exeStem = Path.GetFileNameWithoutExtension(normalizedPath);

			return new CustomVsCodeIdeInspection
			{
				ExecutablePath             = normalizedPath,
				SuggestedDisplayName       = exeStem,
				SuggestedShortName         = exeStem,
				SuggestedUserDataDirectory = ResolveSuggestedUserDataDirectory(appData, exeStem),
				Version                    = version,
				IsVerifiedVsCodeFork       = false
			};
		}

		/// <summary>
		/// Loads and normalizes entries via <see cref="IVsCodeEnvironment"/> without acquiring <see cref="syncLock"/>.
		/// </summary>
		private List<CustomVsCodeIdeEntry> LoadCore()
		{
			if (!this.environment.FileExists(this.storeFilePath))
			{
				return new List<CustomVsCodeIdeEntry>();
			}

			try
			{
				string json = this.environment.ReadAllText(this.storeFilePath);

				if (string.IsNullOrWhiteSpace(json))
				{
					return new List<CustomVsCodeIdeEntry>();
				}

				List<CustomVsCodeIdeEntry>? raw = JsonConvert.DeserializeObject<List<CustomVsCodeIdeEntry>>(json);

				return NormalizeAndDeduplicate(raw);
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
			{
				Debug.WriteLine(ex);

				return new List<CustomVsCodeIdeEntry>();
			}
		}

		/// <summary>
		/// Writes <paramref name="entries"/> to a sibling temporary file and atomically replaces <see cref="storeFilePath"/>.
		/// </summary>
		private void SaveCore(List<CustomVsCodeIdeEntry> entries)
		{
			if (string.IsNullOrWhiteSpace(this.storeFilePath))
			{
				throw new InvalidOperationException("Custom IDE store path could not be resolved.");
			}

			string? parentDir = Path.GetDirectoryName(this.storeFilePath);

			if (!string.IsNullOrWhiteSpace(parentDir))
			{
				Directory.CreateDirectory(parentDir);
			}

			string tempPath = $"{this.storeFilePath}.{Guid.NewGuid():N}.tmp";
			string json     = JsonConvert.SerializeObject(entries, Formatting.Indented);

			try
			{
				File.WriteAllText(tempPath, json);
				File.Move(tempPath, this.storeFilePath, overwrite: true);
			}
			finally
			{
				TryDeleteFile(tempPath);
			}
		}

		/// <summary>
		/// Resolves the shared custom IDE store path under <c>%AppData%\VSLauncher\vscode-custom-ides.json</c>.
		/// </summary>
		private static string ResolveDefaultStorePath(IVsCodeEnvironment environment)
		{
			string? appData = environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			if (string.IsNullOrWhiteSpace(appData))
			{
				return string.Empty;
			}

			return Path.Combine(appData, StoreFolderName, StoreFileName);
		}

		/// <summary>
		/// Resolves the suggested <c>%AppData%</c> folder for user data storage when both inputs are populated.
		/// </summary>
		private static string ResolveSuggestedUserDataDirectory(string appData, string folderName)
		{
			if (string.IsNullOrWhiteSpace(appData) || string.IsNullOrWhiteSpace(folderName))
			{
				return string.Empty;
			}

			return Path.Combine(appData, folderName);
		}

		/// <summary>
		/// Normalizes and deduplicates a sequence of candidate entries by canonical executable path.
		/// </summary>
		private static List<CustomVsCodeIdeEntry> NormalizeAndDeduplicate(IEnumerable<CustomVsCodeIdeEntry>? entries)
		{
			var result    = new List<CustomVsCodeIdeEntry>();
			var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (entries is null)
			{
				return result;
			}

			foreach (CustomVsCodeIdeEntry? entry in entries)
			{
				CustomVsCodeIdeEntry? normalized = NormalizeEntry(entry);

				if (normalized is null || !seenPaths.Add(normalized.ExecutablePath))
				{
					continue;
				}

				result.Add(normalized);
			}

			return result;
		}

		/// <summary>
		/// Normalizes a single <see cref="CustomVsCodeIdeEntry"/>, returning <c>null</c> when its executable path is invalid.
		/// </summary>
		private static CustomVsCodeIdeEntry? NormalizeEntry(CustomVsCodeIdeEntry? entry)
		{
			if (entry is null)
			{
				return null;
			}

			string? normalizedExe = TryNormalizePath(entry.ExecutablePath);

			if (normalizedExe is null)
			{
				return null;
			}

			string? displayName       = string.IsNullOrWhiteSpace(entry.DisplayName) ? null : entry.DisplayName.Trim();
			string? userDataDirectory = string.IsNullOrWhiteSpace(entry.UserDataDirectory)
				? null
				: TryNormalizePath(entry.UserDataDirectory) ?? entry.UserDataDirectory.Trim();

			return new CustomVsCodeIdeEntry
			{
				ExecutablePath    = normalizedExe,
				DisplayName       = displayName,
				UserDataDirectory = userDataDirectory
			};
		}

		/// <summary>
		/// Canonicalizes a filesystem path via <see cref="Path.GetFullPath(string)"/>, returning <c>null</c> on invalid input.
		/// </summary>
		private static string? TryNormalizePath(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			try
			{
				return Path.GetFullPath(path.Trim());
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				Debug.WriteLine(ex);

				return null;
			}
		}

		/// <summary>
		/// Deletes a leftover temporary file on a best-effort basis after a failed save.
		/// </summary>
		private static void TryDeleteFile(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
