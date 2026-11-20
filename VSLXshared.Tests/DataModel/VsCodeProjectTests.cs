using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;

namespace VSLXshared.Tests.DataModel
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeProject"/> and <see cref="VsCodeLaunchOptions"/>.
	/// </summary>
	[TestFixture]
	public class VsCodeProjectTests
	{
		private string tempDir = string.Empty;

		/// <summary>
		/// Creates an isolated temporary directory before each test.
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			tempDir = Path.Combine(Path.GetTempPath(), "VSLXsharedTests_" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(tempDir);
		}

		/// <summary>
		/// Deletes the temporary directory after each test.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, recursive: true);
			}
		}

		/// <summary>
		/// Verifies that refreshing an existing workspace file clears the warning flag and marks it as a non-directory target.
		/// </summary>
		[Test]
		public void Refresh_WhenWorkspaceFileExists_ClearsWarningAndMarksNonDirectoryTarget()
		{
			string file = Path.Combine(tempDir, "demo.code-workspace");

			File.WriteAllText(file, "{}");

			var project = new VsCodeProject("demo", file, VsCodeProjectKind.Workspace);

			project.IdeType.ShouldBe(IdeType.VsCode);
			project.ItemType.ShouldBe(ItemTypeEnum.Project);
			project.IsDirectoryTarget.ShouldBeFalse();
			project.Warning.ShouldBeFalse();
			project.TypeAsName().ShouldBe("VS Code Workspace");
		}

		/// <summary>
		/// Verifies that refreshing an existing directory with a manifest file clears the warning flag and marks it as a directory target.
		/// </summary>
		[Test]
		public void Refresh_WhenManifestDirectoryExists_ClearsWarningAndMarksDirectoryTarget()
		{
			string manifest = Path.Combine(tempDir, "package.json");

			File.WriteAllText(manifest, "{}");

			var project = new VsCodeProject("web", tempDir, VsCodeProjectKind.NodeJs, "package.json");

			project.IsDirectoryTarget.ShouldBeTrue();
			project.Warning.ShouldBeFalse();
			project.TypeAsName().ShouldBe("Node.js");
		}

		/// <summary>
		/// Verifies that refreshing a missing workspace file or missing manifest sets the warning flag.
		/// </summary>
		[Test]
		public void Refresh_WhenTargetOrManifestMissing_SetsWarning()
		{
			string missingWorkspace = Path.Combine(tempDir, "missing.code-workspace");

			var workspaceProject = new VsCodeProject("missing", missingWorkspace, VsCodeProjectKind.Workspace);
			var missingManifest  = new VsCodeProject("web", tempDir, VsCodeProjectKind.NodeJs, "package.json");

			workspaceProject.Warning.ShouldBeTrue();
			workspaceProject.IsDirectoryTarget.ShouldBeFalse();
			missingManifest.Warning.ShouldBeTrue();
			missingManifest.IsDirectoryTarget.ShouldBeTrue();
		}

		/// <summary>
		/// Verifies that refreshing a plain directory target without a manifest clears the warning flag.
		/// </summary>
		[Test]
		public void Refresh_WhenFolderWithoutManifestExists_ClearsWarningAndMarksDirectoryTarget()
		{
			var project = new VsCodeProject("notes", tempDir, VsCodeProjectKind.Folder);

			project.IsDirectoryTarget.ShouldBeTrue();
			project.Warning.ShouldBeFalse();
			project.TypeAsName().ShouldBe("VS Code Folder");
		}

		/// <summary>
		/// Verifies display names returned by <see cref="VsCodeProject.TypeAsName"/> for all project kinds.
		/// </summary>
		/// <param name="kind">The VS Code project kind.</param>
		/// <param name="expectedName">The expected display name.</param>
		[TestCase(VsCodeProjectKind.Workspace, "VS Code Workspace")]
		[TestCase(VsCodeProjectKind.Folder, "VS Code Folder")]
		[TestCase(VsCodeProjectKind.NodeJs, "Node.js")]
		[TestCase(VsCodeProjectKind.Python, "Python")]
		[TestCase(VsCodeProjectKind.Rust, "Rust")]
		[TestCase(VsCodeProjectKind.Go, "Go")]
		[TestCase(VsCodeProjectKind.Java, "Java")]
		[TestCase(VsCodeProjectKind.CMake, "CMake")]
		[TestCase(VsCodeProjectKind.Flutter, "Flutter")]
		[TestCase(VsCodeProjectKind.Php, "PHP")]
		[TestCase(VsCodeProjectKind.Generic, "VS Code Folder")]
		public void TypeAsName_ReturnsExpectedLabelForEveryProjectKind(VsCodeProjectKind kind, string expectedName)
		{
			var project = new VsCodeProject("item", tempDir, kind);

			project.TypeAsName().ShouldBe(expectedName);
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeLaunchOptions.Clone"/> produces a distinct copy with identical option values.
		/// </summary>
		[Test]
		public void Clone_VsCodeLaunchOptions_CreatesIndependentCopy()
		{
			var original = new VsCodeLaunchOptions
			{
				Edition           = "vscode-insiders-user",
				Profile           = "Work",
				WindowMode        = VsCodeWindowMode.ReuseWindow,
				DisableExtensions = true,
				Commands          = "--log debug"
			};

			VsCodeLaunchOptions copy = original.Clone();

			copy.Profile = "Personal";

			copy.ShouldNotBeSameAs(original);
			copy.Edition.ShouldBe("vscode-insiders-user");
			copy.WindowMode.ShouldBe(VsCodeWindowMode.ReuseWindow);
			copy.DisableExtensions.ShouldBeTrue();
			copy.Commands.ShouldBe("--log debug");
			original.Profile.ShouldBe("Work");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeProject"/> cloning preserves VS Code specific fields independently.
		/// </summary>
		[Test]
		public void Clone_VsCodeProject_PreservesVsCodeFields()
		{
			var original = new VsCodeProject("api", tempDir, VsCodeProjectKind.Python, "pyproject.toml")
			{
				VsVersion         = "vscode-stable-user",
				WindowMode        = VsCodeWindowMode.NewWindow,
				DisableExtensions = true
			};

			var clone = original.Clone().ShouldBeOfType<VsCodeProject>();

			clone.WindowMode = VsCodeWindowMode.ReuseWindow;

			clone.ShouldNotBeSameAs(original);
			clone.IdeType.ShouldBe(IdeType.VsCode);
			clone.ProjectKind.ShouldBe(VsCodeProjectKind.Python);
			clone.ManifestFile.ShouldBe("pyproject.toml");
			clone.VsVersion.ShouldBe("vscode-stable-user");
			clone.DisableExtensions.ShouldBeTrue();
			original.WindowMode.ShouldBe(VsCodeWindowMode.NewWindow);
		}
	}
}
