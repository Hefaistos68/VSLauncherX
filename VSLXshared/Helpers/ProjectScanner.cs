using VSLauncher.DataModel;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// Recursively scans directories for Visual Studio solutions/projects and Visual Studio Code workspaces/projects.
	/// </summary>
	public class ProjectScanner
	{
		private static readonly HashSet<string> SolutionAndWorkspaceExtensions = new(StringComparer.OrdinalIgnoreCase)
		{
			".sln",
			".slnx",
			".code-workspace"
		};

		private static readonly HashSet<string> ProjectExtensions = new(StringComparer.OrdinalIgnoreCase)
		{
			".csproj",
			".vbproj",
			".vcxproj",
			".vcproj",
			".fsproj",
			".esproj",
			".tsproj"
		};

		private static readonly HashSet<string> NoiseDirectories = new(StringComparer.OrdinalIgnoreCase)
		{
			"node_modules",
			"bin",
			"obj",
			"dist",
			"build",
			"target",
			"venv",
			"__pycache__",
			"packages",
			"TestResults"
		};

		private static readonly (string FileName, VsCodeProjectKind Kind)[] ManifestMarkers =
		{
			("package.json", VsCodeProjectKind.NodeJs),
			("pyproject.toml", VsCodeProjectKind.Python),
			("Cargo.toml", VsCodeProjectKind.Rust),
			("go.mod", VsCodeProjectKind.Go),
			("pom.xml", VsCodeProjectKind.Java),
			("build.gradle", VsCodeProjectKind.Java),
			("build.gradle.kts", VsCodeProjectKind.Java),
			("CMakeLists.txt", VsCodeProjectKind.CMake),
			("pubspec.yaml", VsCodeProjectKind.Flutter),
			("composer.json", VsCodeProjectKind.Php)
		};

		/// <summary>
		/// Scans <paramref name="folderPath"/> for Visual Studio and Visual Studio Code projects.
		/// </summary>
		/// <param name="folderPath">The root directory path to scan.</param>
		/// <param name="onlySolutions">If <c>true</c>, includes only <c>.sln</c>, <c>.slnx</c>, and <c>.code-workspace</c> files.</param>
		/// <param name="flat">If <c>true</c>, collects all discovered leaf items directly under the returned <see cref="VsFolder"/>.</param>
		/// <returns>A populated <see cref="VsFolder"/> representing the scanned root directory.</returns>
		public VsFolder Scan(string folderPath, bool onlySolutions, bool flat)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

			var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			return ScanDirectory(folderPath, onlySolutions, flat, visited);
		}

		/// <summary>
		/// Detects the highest-priority Visual Studio Code project manifest or <c>.vscode</c> folder marker in <paramref name="directoryPath"/>.
		/// </summary>
		/// <param name="directoryPath">The directory path to inspect.</param>
		/// <param name="manifestFile">Receives the matched manifest file name, or <c>null</c> for a <c>.vscode</c> folder or when no marker is found.</param>
		/// <returns>The detected <see cref="VsCodeProjectKind"/>, or <c>null</c> if no marker exists.</returns>
		public static VsCodeProjectKind? DetectDirectoryManifest(string directoryPath, out string? manifestFile)
		{
			manifestFile = null;

			if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
			{
				return null;
			}

			foreach ((string fileName, VsCodeProjectKind kind) in ManifestMarkers)
			{
				if (File.Exists(Path.Combine(directoryPath, fileName)))
				{
					manifestFile = fileName;

					return kind;
				}
			}

			if (Directory.Exists(Path.Combine(directoryPath, ".vscode")))
			{
				return VsCodeProjectKind.Folder;
			}

			return null;
		}

		/// <summary>
		/// Scans a single directory while guarding against symlink cycles and inaccessible paths.
		/// </summary>
		/// <param name="folderPath">The directory path to scan.</param>
		/// <param name="onlySolutions">Whether to restrict discovery to solutions and workspaces.</param>
		/// <param name="flat">Whether to flatten discovered items into the root folder.</param>
		/// <param name="visited">Set of canonical paths already visited.</param>
		/// <returns>A <see cref="VsFolder"/> containing discovered items.</returns>
		private VsFolder ScanDirectory(string folderPath, bool onlySolutions, bool flat, HashSet<string> visited)
		{
			string? canonicalPath = TryGetCanonicalPath(folderPath);
			string displayPath    = canonicalPath ?? folderPath;
			string folderName     = ExtractFolderName(displayPath);
			var currentFolder     = new VsFolder(folderName, displayPath);

			if (string.IsNullOrEmpty(canonicalPath) || !visited.Add(canonicalPath) || !Directory.Exists(canonicalPath))
			{
				return currentFolder;
			}

			bool hasCodeWorkspace = CollectMatchingFiles(canonicalPath, onlySolutions, currentFolder);
			bool hasLeafMarker    = TryAddLeafVsCodeProject(canonicalPath, folderName, onlySolutions, hasCodeWorkspace, currentFolder);

			if (!hasLeafMarker)
			{
				ScanSubdirectories(canonicalPath, onlySolutions, flat, visited, currentFolder);
			}

			return currentFolder;
		}

		/// <summary>
		/// Collects matching solution, workspace, and project files directly inside <paramref name="directoryPath"/>.
		/// </summary>
		/// <param name="directoryPath">The directory to inspect.</param>
		/// <param name="onlySolutions">Whether to include only solutions and workspaces.</param>
		/// <param name="destination">The folder receiving matching items.</param>
		/// <returns><c>true</c> if at least one <c>.code-workspace</c> file was found in <paramref name="directoryPath"/>.</returns>
		private static bool CollectMatchingFiles(string directoryPath, bool onlySolutions, VsFolder destination)
		{
			bool hasCodeWorkspace = false;

			try
			{
				foreach (string file in Directory.EnumerateFiles(directoryPath))
				{
					string fileName = Path.GetFileName(file);

					if (string.IsNullOrEmpty(fileName) || fileName.StartsWith('.'))
					{
						continue;
					}

					if (IsFileOfInterest(file, onlySolutions, out bool isCodeWorkspace))
					{
						hasCodeWorkspace |= isCodeWorkspace;

						destination.Items.Add(CreateItemFromFile(file));
					}
				}
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or PathTooLongException)
			{
			}

			return hasCodeWorkspace;
		}

		/// <summary>
		/// Checks for a directory-level VS Code marker when allowed and adds a leaf <see cref="VsCodeProject"/> if present.
		/// </summary>
		/// <param name="directoryPath">The directory to inspect.</param>
		/// <param name="folderName">The display name for the directory.</param>
		/// <param name="onlySolutions">Whether scanning is restricted to solutions and workspaces.</param>
		/// <param name="hasCodeWorkspace">Whether the directory already contains a <c>.code-workspace</c> file.</param>
		/// <param name="destination">The folder receiving the detected project.</param>
		/// <returns><c>true</c> if a leaf VS Code marker was detected and recursion should stop.</returns>
		private static bool TryAddLeafVsCodeProject(
			string directoryPath,
			string folderName,
			bool onlySolutions,
			bool hasCodeWorkspace,
			VsFolder destination)
		{
			if (onlySolutions || hasCodeWorkspace)
			{
				return false;
			}

			VsCodeProjectKind? detectedKind = DetectDirectoryManifest(directoryPath, out string? manifestFile);

			if (!detectedKind.HasValue)
			{
				return false;
			}

			var project = new VsCodeProject(folderName, directoryPath, detectedKind.Value, manifestFile);

			destination.Items.Add(project);

			return true;
		}

		/// <summary>
		/// Recursively scans eligible subdirectories of <paramref name="directoryPath"/>.
		/// </summary>
		/// <param name="directoryPath">The parent directory path.</param>
		/// <param name="onlySolutions">Whether to restrict discovery to solutions and workspaces.</param>
		/// <param name="flat">Whether to flatten discovered items.</param>
		/// <param name="visited">Set of canonical paths already visited.</param>
		/// <param name="destination">The parent folder receiving subdirectory results.</param>
		private void ScanSubdirectories(
			string directoryPath,
			bool onlySolutions,
			bool flat,
			HashSet<string> visited,
			VsFolder destination)
		{
			try
			{
				foreach (string subDir in Directory.EnumerateDirectories(directoryPath))
				{
					if (ShouldSkipSubdirectory(subDir))
					{
						continue;
					}

					VsFolder subFolder = ScanDirectory(subDir, onlySolutions, flat, visited);

					AddSubdirectoryResult(subFolder, flat, destination);
				}
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or PathTooLongException)
			{
			}
		}

		/// <summary>
		/// Determines whether a subdirectory should be skipped due to dot-prefix, noise list membership, hidden/reparse attributes, or access errors.
		/// </summary>
		/// <param name="directoryPath">The subdirectory path.</param>
		/// <returns><c>true</c> if the subdirectory should be skipped; otherwise <c>false</c>.</returns>
		private static bool ShouldSkipSubdirectory(string directoryPath)
		{
			string dirName = Path.GetFileName(directoryPath);

			if (string.IsNullOrEmpty(dirName) || dirName.StartsWith('.') || NoiseDirectories.Contains(dirName))
			{
				return true;
			}

			try
			{
				FileAttributes attributes = File.GetAttributes(directoryPath);

				return attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.ReparsePoint);
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException or PathTooLongException)
			{
				return true;
			}
		}

		/// <summary>
		/// Adds a scanned subdirectory's results to <paramref name="destination"/> according to flat and leaf-unwrapping rules.
		/// </summary>
		/// <param name="subFolder">The scanned subdirectory folder.</param>
		/// <param name="flat">Whether to flatten items directly into <paramref name="destination"/>.</param>
		/// <param name="destination">The parent destination folder.</param>
		private static void AddSubdirectoryResult(VsFolder subFolder, bool flat, VsFolder destination)
		{
			if (subFolder.Items.Count == 0)
			{
				return;
			}

			if (flat)
			{
				destination.Items.AddRange(subFolder.Items);

				return;
			}

			if (IsSingleDirectoryVsCodeProject(subFolder))
			{
				destination.Items.Add(subFolder.Items[0]);

				return;
			}

			destination.Items.Add(subFolder);
		}

		/// <summary>
		/// Determines whether <paramref name="subFolder"/> contains solely a single directory-based <see cref="VsCodeProject"/> representing the folder itself.
		/// </summary>
		/// <param name="subFolder">The scanned subdirectory folder.</param>
		/// <returns><c>true</c> if the project should be emitted directly without a redundant wrapper folder.</returns>
		private static bool IsSingleDirectoryVsCodeProject(VsFolder subFolder)
		{
			return subFolder.Items.Count == 1
				&& subFolder.Items[0] is VsCodeProject project
				&& project.IsDirectoryTarget
				&& string.Equals(project.Path, subFolder.Path, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Determines whether a file extension should be imported under the current filter settings.
		/// </summary>
		/// <param name="filePath">The file path to evaluate.</param>
		/// <param name="onlySolutions">Whether to include only solutions and workspaces.</param>
		/// <param name="isCodeWorkspace">Outputs <c>true</c> if the file has a <c>.code-workspace</c> extension.</param>
		/// <returns><c>true</c> if the file should be imported; otherwise <c>false</c>.</returns>
		private static bool IsFileOfInterest(string filePath, bool onlySolutions, out bool isCodeWorkspace)
		{
			string extension = Path.GetExtension(filePath);

			isCodeWorkspace = string.Equals(extension, ".code-workspace", StringComparison.OrdinalIgnoreCase);

			if (SolutionAndWorkspaceExtensions.Contains(extension))
			{
				return true;
			}

			return !onlySolutions && ProjectExtensions.Contains(extension);
		}

		/// <summary>
		/// Creates a <see cref="VsItem"/> from a matching file path and populates its last-modified timestamp.
		/// </summary>
		/// <param name="filePath">The full file path.</param>
		/// <returns>The constructed <see cref="VsItem"/>.</returns>
		private static VsItem CreateItemFromFile(string filePath)
		{
			VsItem item = ImportHelper.GetItemFromExtension(Path.GetFileNameWithoutExtension(filePath), filePath);

			try
			{
				item.LastModified = new FileInfo(filePath).LastWriteTime;
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
			{
			}

			return item;
		}

		/// <summary>
		/// Extracts a non-empty folder display name from a directory path.
		/// </summary>
		/// <param name="directoryPath">The directory path.</param>
		/// <returns>The terminal directory segment, or <paramref name="directoryPath"/> for root paths.</returns>
		private static string ExtractFolderName(string directoryPath)
		{
			string trimmed    = Path.TrimEndingDirectorySeparator(directoryPath);
			string folderName = Path.GetFileName(trimmed);

			return string.IsNullOrEmpty(folderName) ? directoryPath : folderName;
		}

		/// <summary>
		/// Resolves a normalized full path without trailing non-root directory separators, or <c>null</c> when invalid.
		/// </summary>
		/// <param name="path">The candidate filesystem path.</param>
		/// <returns>The canonical path, or <c>null</c> on invalid input.</returns>
		private static string? TryGetCanonicalPath(string path)
		{
			try
			{
				return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				return null;
			}
		}
	}
}
