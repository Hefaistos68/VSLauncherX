using System.Diagnostics;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLXshared.Tests.Helpers
{
	/// <summary>
	/// Unit tests for multi-IDE folder scanning, manifest priority, pruning, and tree shaping in <see cref="ProjectScanner"/>.
	/// </summary>
	[TestFixture]
	public class ProjectScannerTests
	{
		private string tempDir = string.Empty;

		/// <summary>
		/// Creates an isolated temporary directory before each test.
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			tempDir = Path.Combine(Path.GetTempPath(), "VSLXsharedScannerTests_" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(tempDir);
		}

		/// <summary>
		/// Deletes the temporary directory and resets any hidden attributes after each test.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
			{
				foreach (string dir in Directory.EnumerateDirectories(tempDir, "*", SearchOption.AllDirectories))
				{
					FileAttributes attrs = File.GetAttributes(dir);

					if (attrs.HasFlag(FileAttributes.Hidden))
					{
						File.SetAttributes(dir, attrs & ~FileAttributes.Hidden);
					}
				}

				Directory.Delete(tempDir, recursive: true);
			}
		}

		/// <summary>
		/// Verifies that a directory with <c>package.json</c> emits a single leaf <see cref="VsCodeProject"/> and stops recursing into nested projects or <c>node_modules</c>.
		/// </summary>
		[Test]
		public void Scan_WhenDirectoryHasPackageJsonAndNestedPackageJson_EmitsRootOnlyAndStopsRecursing()
		{
			string appDir      = CreateDir(tempDir, "my-app");
			string nested      = CreateDir(appDir, "sub-pkg");
			string nodeModules = CreateDir(appDir, "node_modules", "dep");

			File.WriteAllText(Path.Combine(appDir, "package.json"), "{}");
			File.WriteAllText(Path.Combine(nested, "package.json"), "{}");
			File.WriteAllText(Path.Combine(nodeModules, "package.json"), "{}");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

			// Tree shape: leaf VsCodeProject directly under parent (no my-app > my-app nesting)
			var app = root.Items.Single().ShouldBeOfType<VsCodeProject>();

			app.Name.ShouldBe("my-app");
			app.Path.ShouldBe(appDir);
			app.ProjectKind.ShouldBe(VsCodeProjectKind.NodeJs);
			app.ManifestFile.ShouldBe("package.json");
		}

		/// <summary>
		/// Verifies that a directory with both a <c>.code-workspace</c> file and a manifest omits the duplicate manifest project while still discovering nested solutions.
		/// </summary>
		[Test]
		public void Scan_WhenDirectoryHasCodeWorkspaceAndManifestAndSubfolderSln_OmitsDuplicateManifestAndFindsNestedSln()
		{
			string repo = CreateDir(tempDir, "repo");
			string src  = CreateDir(repo, "src");

			File.WriteAllText(Path.Combine(repo, "repo.code-workspace"), "{}");
			File.WriteAllText(Path.Combine(repo, "package.json"), "{}");
			File.WriteAllText(Path.Combine(src, "App.sln"), "Microsoft Visual Studio Solution File");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

			var repoFolder = root.Items.Single().ShouldBeOfType<VsFolder>();

			repoFolder.Items.OfType<VsCodeProject>().Count().ShouldBe(1);
			repoFolder.Items.OfType<VsCodeProject>().Single().ProjectKind.ShouldBe(VsCodeProjectKind.Workspace);
			repoFolder.ContainedSolutionsCount().ShouldBe(1);
		}

		/// <summary>
		/// Verifies that <c>onlySolutions: true</c> includes only <c>.sln</c>, <c>.slnx</c>, and <c>.code-workspace</c> items.
		/// </summary>
		[Test]
		public void Scan_WhenOnlySolutionsTrue_IncludesSlnSlnxAndCodeWorkspaceOnly()
		{
			string nodeDir = CreateDir(tempDir, "web");

			File.WriteAllText(Path.Combine(tempDir, "A.sln"), "");
			File.WriteAllText(Path.Combine(tempDir, "B.slnx"), "<Solution />");
			File.WriteAllText(Path.Combine(tempDir, "C.code-workspace"), "{}");
			File.WriteAllText(Path.Combine(tempDir, "D.csproj"), "<Project />");
			File.WriteAllText(Path.Combine(nodeDir, "package.json"), "{}");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: true, flat: true);

			root.Items.Select(i => i.Name).ShouldBe(new[] { "A", "B", "C" }, ignoreOrder: true);
		}

		/// <summary>
		/// Verifies that a <c>.vscode</c> subdirectory is detected as <see cref="VsCodeProjectKind.Folder"/> without traversing into <c>.vscode</c>, noise folders are skipped, and VS-only folders emit no extra VS Code project.
		/// </summary>
		[Test]
		public void Scan_DetectsVsCodeFolderWithoutRecursingIntoDotVsCodeAndSkipsNoiseDirectories()
		{
			string dotVsCodeProj = CreateDir(tempDir, "tool");
			string binDir        = CreateDir(tempDir, "bin", "nested");
			string vsOnlyDir     = CreateDir(tempDir, "legacy");

			CreateDir(dotVsCodeProj, ".vscode");
			File.WriteAllText(Path.Combine(dotVsCodeProj, ".vscode", "package.json"), "{}");
			File.WriteAllText(Path.Combine(binDir, "Cargo.toml"), "");
			File.WriteAllText(Path.Combine(vsOnlyDir, "Legacy.sln"), "");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

			var toolProject = root.Items.OfType<VsCodeProject>().ShouldHaveSingleItem();

			toolProject.Name.ShouldBe("tool");
			toolProject.ProjectKind.ShouldBe(VsCodeProjectKind.Folder);
			toolProject.ManifestFile.ShouldBeNull();
			toolProject.Warning.ShouldBeFalse();
			root.Items.OfType<VsFolder>().Single().Name.ShouldBe("legacy");
		}

		/// <summary>
		/// Verifies the exact priority order of <see cref="ProjectScanner.DetectDirectoryManifest"/> across all supported manifest files and <c>.vscode</c>.
		/// </summary>
		[Test]
		public void DetectDirectoryManifest_FollowsExactPriorityOrder()
		{
			string projectDir = CreateDir(tempDir, "polyglot");

			CreateDir(projectDir, ".vscode");

			var expectedPriority = new (string Marker, VsCodeProjectKind Kind, string? ManifestFile, bool IsDirectory)[]
			{
				("package.json", VsCodeProjectKind.NodeJs, "package.json", false),
				("pyproject.toml", VsCodeProjectKind.Python, "pyproject.toml", false),
				("Cargo.toml", VsCodeProjectKind.Rust, "Cargo.toml", false),
				("go.mod", VsCodeProjectKind.Go, "go.mod", false),
				("pom.xml", VsCodeProjectKind.Java, "pom.xml", false),
				("build.gradle", VsCodeProjectKind.Java, "build.gradle", false),
				("build.gradle.kts", VsCodeProjectKind.Java, "build.gradle.kts", false),
				("CMakeLists.txt", VsCodeProjectKind.CMake, "CMakeLists.txt", false),
				("pubspec.yaml", VsCodeProjectKind.Flutter, "pubspec.yaml", false),
				("composer.json", VsCodeProjectKind.Php, "composer.json", false),
				(".vscode", VsCodeProjectKind.Folder, null, true)
			};

			foreach (var item in expectedPriority)
			{
				if (!item.IsDirectory)
				{
					File.WriteAllText(Path.Combine(projectDir, item.Marker), "");
				}
			}

			foreach (var expected in expectedPriority)
			{
				VsCodeProjectKind? kind = ProjectScanner.DetectDirectoryManifest(projectDir, out string? manifestFile);

				kind.ShouldBe(expected.Kind);
				manifestFile.ShouldBe(expected.ManifestFile);

				string markerPath = Path.Combine(projectDir, expected.Marker);

				if (expected.IsDirectory)
				{
					Directory.Delete(markerPath, recursive: true);
				}
				else
				{
					File.Delete(markerPath);
				}
			}

			ProjectScanner.DetectDirectoryManifest(projectDir, out string? remainingManifest).ShouldBeNull();
			remainingManifest.ShouldBeNull();
		}

		/// <summary>
		/// Verifies that all configured noise directories, dot-directories, and hidden subdirectories are skipped during scanning.
		/// </summary>
		[Test]
		public void Scan_SkipsAllNoiseDotAndHiddenDirectories()
		{
			string[] skippedDirs =
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
				"TestResults",
				".git",
				".hidden"
			};

			foreach (string dirName in skippedDirs)
			{
				string skipped = CreateDir(tempDir, dirName);

				File.WriteAllText(Path.Combine(skipped, "Ignored.sln"), "");
				File.WriteAllText(Path.Combine(skipped, "package.json"), "{}");
			}

			string hiddenDir = CreateDir(tempDir, "secret");

			File.WriteAllText(Path.Combine(hiddenDir, "Secret.sln"), "");
			File.SetAttributes(hiddenDir, File.GetAttributes(hiddenDir) | FileAttributes.Hidden);

			string validDir = CreateDir(tempDir, "valid");

			File.WriteAllText(Path.Combine(validDir, "Valid.sln"), "");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: true);

			root.Items.Select(i => i.Name).ShouldBe(new[] { "Valid" });
		}

		/// <summary>
		/// Verifies that tree mode preserves intermediate parent folders and keeps directory wrapper folders when a directory has both VS files and a manifest.
		/// </summary>
		[Test]
		public void Scan_TreeShape_PreservesParentFoldersAndMixedDirectoryWrapper()
		{
			string parentGroup = CreateDir(tempDir, "group");
			string leafApp     = CreateDir(parentGroup, "web");
			string mixedApp    = CreateDir(tempDir, "mixed");

			File.WriteAllText(Path.Combine(leafApp, "package.json"), "{}");
			File.WriteAllText(Path.Combine(mixedApp, "Mixed.sln"), "");
			File.WriteAllText(Path.Combine(mixedApp, "Cargo.toml"), "");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

			VsFolder groupFolder = root.Items.OfType<VsFolder>().Single(f => f.Name == "group");
			VsCodeProject web    = groupFolder.Items.Single().ShouldBeOfType<VsCodeProject>();

			web.Name.ShouldBe("web");
			web.ProjectKind.ShouldBe(VsCodeProjectKind.NodeJs);

			VsFolder mixedFolder = root.Items.OfType<VsFolder>().Single(f => f.Name == "mixed");

			mixedFolder.Items.OfType<VsSolution>().Single().Name.ShouldBe("Mixed");
			mixedFolder.Items.OfType<VsCodeProject>().Single().ProjectKind.ShouldBe(VsCodeProjectKind.Rust);
		}

		/// <summary>
		/// Verifies that all Visual Studio project extensions are included when <c>onlySolutions</c> is false and ignored when true, and dot-files are skipped.
		/// </summary>
		[Test]
		public void Scan_WhenOnlySolutionsFalse_IncludesAllVisualStudioProjectExtensionsAndSkipsDotFiles()
		{
			string[] extensions = { ".csproj", ".vbproj", ".vcxproj", ".vcproj", ".fsproj", ".esproj", ".tsproj" };

			foreach (string ext in extensions)
			{
				File.WriteAllText(Path.Combine(tempDir, "Proj" + ext.TrimStart('.') + ext), "<Project />");
			}

			File.WriteAllText(Path.Combine(tempDir, ".hidden.sln"), "");

			var scanner      = new ProjectScanner();
			VsFolder full    = scanner.Scan(tempDir, onlySolutions: false, flat: true);
			VsFolder slnOnly = scanner.Scan(tempDir, onlySolutions: true, flat: true);

			full.Items.OfType<VsProject>().Count().ShouldBe(extensions.Length);
			slnOnly.Items.ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that scanning a non-existent directory returns an empty <see cref="VsFolder"/> without throwing.
		/// </summary>
		[Test]
		public void Scan_WhenDirectoryDoesNotExist_ReturnsEmptyFolder()
		{
			string missing = Path.Combine(tempDir, "does-not-exist");
			var scanner    = new ProjectScanner();

			VsFolder result = scanner.Scan(missing, onlySolutions: false, flat: false);

			result.Name.ShouldBe("does-not-exist");
			result.Path.ShouldBe(missing);
			result.Items.ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that <c>flat: true</c> collects nested Visual Studio and VS Code projects directly under the returned root folder.
		/// </summary>
		[Test]
		public void Scan_WhenFlatTrue_CollectsAllLeafItemsUnderRoot()
		{
			string nestedSlnDir = CreateDir(tempDir, "a", "b");
			string rustDir      = CreateDir(tempDir, "rust-cli");

			File.WriteAllText(Path.Combine(nestedSlnDir, "Deep.sln"), "");
			File.WriteAllText(Path.Combine(rustDir, "Cargo.toml"), "");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: true);

			root.Items.OfType<VsFolder>().ShouldBeEmpty();
			root.Items.Select(i => i.Name).ShouldBe(new[] { "Deep", "rust-cli" }, ignoreOrder: true);
		}

		/// <summary>
		/// Verifies that <c>onlySolutions: true</c> does not treat a manifest file as a pruning boundary for nested solutions.
		/// </summary>
		[Test]
		public void Scan_WhenOnlySolutionsTrueAndFolderHasPackageJson_StillFindsNestedSolution()
		{
			string appDir    = CreateDir(tempDir, "app");
			string nestedDir = CreateDir(appDir, "tools");

			File.WriteAllText(Path.Combine(appDir, "package.json"), "{}");
			File.WriteAllText(Path.Combine(nestedDir, "Tool.sln"), "");

			var scanner   = new ProjectScanner();
			VsFolder root = scanner.Scan(tempDir, onlySolutions: true, flat: true);

			root.Items.Select(i => i.Name).ShouldBe(new[] { "Tool" });
		}

		/// <summary>
		/// Verifies that directory reparse points (such as NTFS junctions forming a cycle) are skipped safely.
		/// </summary>
		[Test]
		public void Scan_WhenReparsePointJunctionFormsCycle_SkipsJunctionSafely()
		{
			string appDir       = CreateDir(tempDir, "app");
			string junctionPath = Path.Combine(appDir, "loop");

			File.WriteAllText(Path.Combine(appDir, "App.sln"), "");

			var startInfo = new ProcessStartInfo
			{
				FileName               = "cmd.exe",
				Arguments              = $"/c mklink /J \"{junctionPath}\" \"{tempDir}\"",
				UseShellExecute        = false,
				CreateNoWindow         = true,
				RedirectStandardOutput = true,
				RedirectStandardError  = true
			};

			using (Process? proc = Process.Start(startInfo))
			{
				proc.ShouldNotBeNull();
				proc.WaitForExit(5000);
				proc.ExitCode.ShouldBe(0);
			}

			try
			{
				var scanner   = new ProjectScanner();
				VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

				VsFolder appFolder = root.Items.ShouldHaveSingleItem().ShouldBeOfType<VsFolder>();

				appFolder.Name.ShouldBe("app");
				appFolder.Items.ShouldHaveSingleItem().ShouldBeOfType<VsSolution>().Name.ShouldBe("App");
			}
			finally
			{
				if (Directory.Exists(junctionPath))
				{
					Directory.Delete(junctionPath, recursive: false);
				}
			}
		}

		/// <summary>
		/// Creates a nested directory under <paramref name="root"/> and returns its full path.
		/// </summary>
		private static string CreateDir(string root, params string[] segments)
		{
			string fullPath = Path.Combine(new[] { root }.Concat(segments).ToArray());

			Directory.CreateDirectory(fullPath);

			return fullPath;
		}
	}
}
