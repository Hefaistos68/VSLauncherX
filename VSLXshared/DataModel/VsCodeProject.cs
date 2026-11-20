using System.Diagnostics;
using System.Security;

using Newtonsoft.Json;

using VSLauncher.Helpers;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// Represents a Visual Studio Code workspace file or project folder item.
	/// </summary>
	public class VsCodeProject : VsItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeProject"/> class.
		/// </summary>
		public VsCodeProject()
		{
			this.IdeType     = IdeType.VsCode;
			this.ItemType    = ItemTypeEnum.Project;
			this.ProjectKind = VsCodeProjectKind.Workspace;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeProject"/> class.
		/// </summary>
		/// <param name="name">The display name of the project or workspace.</param>
		/// <param name="path">The path to the <c>.code-workspace</c> file or project directory.</param>
		/// <param name="projectKind">The detected or configured VS Code project kind.</param>
		/// <param name="manifestFile">The optional manifest file name relative to <paramref name="path"/>.</param>
		public VsCodeProject(string name, string path, VsCodeProjectKind projectKind, string? manifestFile = null)
			: base(name, path, null)
		{
			this.IdeType      = IdeType.VsCode;
			this.ItemType     = ItemTypeEnum.Project;
			this.ProjectKind  = projectKind;
			this.ManifestFile = manifestFile;

			this.Refresh();
		}

		/// <summary>
		/// Gets or sets the VS Code project kind.
		/// </summary>
		public VsCodeProjectKind ProjectKind { get; set; }

		/// <summary>
		/// Gets or sets the optional manifest file name used to identify directory-based projects.
		/// </summary>
		public string? ManifestFile { get; set; }

		/// <summary>
		/// Gets or sets the window reuse mode for launching this item.
		/// </summary>
		public VsCodeWindowMode WindowMode { get; set; } = VsCodeWindowMode.Default;

		/// <summary>
		/// Gets or sets a value indicating whether extensions should be disabled when launching this item.
		/// </summary>
		public bool DisableExtensions { get; set; }

		/// <summary>
		/// Gets a value indicating whether this item targets a folder rather than a <c>.code-workspace</c> file.
		/// </summary>
		[JsonIgnore]
		public bool IsDirectoryTarget => this.ProjectKind != VsCodeProjectKind.Workspace;

		/// <inheritdoc/>
		public override void Refresh()
		{
			this.Warning = !CheckIsAccessible();

			if (!this.Warning)
			{
				this.LastModified = GetLastModifiedTime();
			}
		}

		/// <summary>
		/// Returns the human-readable project kind label.
		/// </summary>
		/// <returns>The display name corresponding to <see cref="ProjectKind"/>.</returns>
		public string TypeAsName()
		{
			return this.ProjectKind switch
			{
				VsCodeProjectKind.Workspace => "VS Code Workspace",
				VsCodeProjectKind.NodeJs    => "Node.js",
				VsCodeProjectKind.Python    => "Python",
				VsCodeProjectKind.Rust      => "Rust",
				VsCodeProjectKind.Go        => "Go",
				VsCodeProjectKind.Java      => "Java",
				VsCodeProjectKind.CMake     => "CMake",
				VsCodeProjectKind.Flutter   => "Flutter",
				VsCodeProjectKind.Php       => "PHP",
				_                           => "VS Code Folder"
			};
		}

		/// <inheritdoc/>
		public override string? ToString()
		{
			return $"{this.Name} ({this.TypeAsName()})";
		}

		/// <summary>
		/// Checks whether the target workspace file or project directory (and optional manifest) is accessible.
		/// </summary>
		/// <returns><c>true</c> if accessible; otherwise <c>false</c>.</returns>
		private bool CheckIsAccessible()
		{
			if (string.IsNullOrWhiteSpace(this.Path))
			{
				return false;
			}

			if (!this.IsDirectoryTarget)
			{
				string? directory = System.IO.Path.GetDirectoryName(this.Path);

				if (!PathHelper.PathIsValidAndCanRead(directory))
				{
					return false;
				}

				return CanReadFile(this.Path);
			}

			if (!PathHelper.PathIsValidAndCanRead(this.Path))
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(this.ManifestFile))
			{
				string manifestPath = ResolveManifestPath(this.Path, this.ManifestFile);

				return CanReadFile(manifestPath);
			}

			return true;
		}

		/// <summary>
		/// Retrieves the last-modified timestamp for the workspace file, manifest file, or directory.
		/// </summary>
		/// <returns>The last write timestamp, or <see cref="DateTime.MinValue"/> on error.</returns>
		private DateTime GetLastModifiedTime()
		{
			try
			{
				if (!this.IsDirectoryTarget)
				{
					return new FileInfo(this.Path!).LastWriteTime;
				}

				if (!string.IsNullOrWhiteSpace(this.ManifestFile))
				{
					string manifestPath = ResolveManifestPath(this.Path!, this.ManifestFile);

					return new FileInfo(manifestPath).LastWriteTime;
				}

				return new DirectoryInfo(this.Path!).LastWriteTime;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
			{
				Debug.WriteLine(ex);

				return DateTime.MinValue;
			}
		}

		/// <summary>
		/// Resolves a manifest file path relative to the project directory when not already rooted.
		/// </summary>
		/// <param name="directoryPath">The project directory path.</param>
		/// <param name="manifestFile">The manifest file name or path.</param>
		/// <returns>The combined manifest file path.</returns>
		private static string ResolveManifestPath(string directoryPath, string manifestFile)
		{
			return System.IO.Path.IsPathRooted(manifestFile)
				? manifestFile
				: System.IO.Path.Combine(directoryPath, manifestFile);
		}

		/// <summary>
		/// Verifies that the specified file exists and can be opened for reading.
		/// </summary>
		/// <param name="filePath">The file path to test.</param>
		/// <returns><c>true</c> if the file exists and is readable; otherwise <c>false</c>.</returns>
		private static bool CanReadFile(string filePath)
		{
			if (!File.Exists(filePath))
			{
				return false;
			}

			try
			{
				using var stream = File.OpenRead(filePath);

				return stream.CanRead;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
			{
				Debug.WriteLine(ex);

				return false;
			}
		}
	}
}
