using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;

namespace VSLXshared.Tests.DataModel
{
	/// <summary>
	/// Unit tests for workspace-backed folder detection, cloning, and project counting in <see cref="VsFolder"/>.
	/// </summary>
	[TestFixture]
	public class VsFolderWorkspaceTests
	{
		private string tempDir = string.Empty;

		/// <summary>
		/// Creates an isolated temporary directory before each test.
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			tempDir = Path.Combine(Path.GetTempPath(), "VSLXsharedFolderTests_" + Guid.NewGuid().ToString("N"));

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
		/// Verifies that <see cref="VsFolder.IsWorkspaceBacked"/> returns true only for disk-backed folders with a workspace and folder item type.
		/// </summary>
		[Test]
		public void IsWorkspaceBacked_TrueOnlyForDiskBackedFolderWithCodeWorkspaceAndFolderItemType()
		{
			string wsFile = Path.Combine(tempDir, "app.code-workspace");

			File.WriteAllText(wsFile, "{}");

			var diskFolder    = new VsFolder("app", tempDir);
			var virtualFolder = new VsFolder("group", string.Empty);
			var mruOpenFolder = new VsFolder("app", tempDir) { ItemType = ItemTypeEnum.Project };

			diskFolder.IsWorkspaceBacked().ShouldBeTrue();
			virtualFolder.IsWorkspaceBacked().ShouldBeFalse();
			mruOpenFolder.IsWorkspaceBacked().ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that <see cref="VsFolder.IsWorkspaceBacked"/> returns true when a disk-backed folder contains a workspace <see cref="VsCodeProject"/> child.
		/// </summary>
		[Test]
		public void IsWorkspaceBacked_WhenFolderContainsWorkspaceChild_ReturnsTrue()
		{
			var folder = new VsFolder("app", tempDir);
			var child  = new VsCodeProject("app", Path.Combine(tempDir, "nested.code-workspace"), VsCodeProjectKind.Workspace);

			folder.Items.Add(child);

			folder.IsWorkspaceBacked().ShouldBeTrue();
		}

		/// <summary>
		/// Verifies that <see cref="VsFolder.IsWorkspaceBacked"/> returns false when the folder directory has no workspace file or child.
		/// </summary>
		[Test]
		public void IsWorkspaceBacked_WhenDirectoryHasNoWorkspaceAndNoWorkspaceChild_ReturnsFalse()
		{
			var folder       = new VsFolder("empty", tempDir);
			var missingDir   = new VsFolder("missing", Path.Combine(tempDir, "nonexistent"));

			folder.Items.Add(new VsCodeProject("node", tempDir, VsCodeProjectKind.NodeJs));

			folder.IsWorkspaceBacked().ShouldBeFalse();
			missingDir.IsWorkspaceBacked().ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that <see cref="VsFolder.Clone(bool)"/> deep-copies <see cref="VsFolder.VsCodeOptions"/>.
		/// </summary>
		[Test]
		public void Clone_WithItems_DeepCopiesVsCodeOptions()
		{
			var folder = new VsFolder("repo", tempDir)
			{
				VsCodeOptions = new VsCodeLaunchOptions
				{
					Edition           = "vscode-insiders-user",
					Profile           = "Dev",
					WindowMode        = VsCodeWindowMode.NewWindow,
					DisableExtensions = true,
					Commands          = "--verbose"
				}
			};

			folder.Items.Add(new VsCodeProject("web", tempDir, VsCodeProjectKind.NodeJs));

			VsFolder clone = folder.Clone(true);

			clone.VsCodeOptions.ShouldNotBeNull();
			clone.VsCodeOptions.ShouldNotBeSameAs(folder.VsCodeOptions);

			clone.VsCodeOptions.Profile = "Changed";

			folder.VsCodeOptions.Profile.ShouldBe("Dev");
			clone.VsCodeOptions.Edition.ShouldBe("vscode-insiders-user");
			clone.VsCodeOptions.WindowMode.ShouldBe(VsCodeWindowMode.NewWindow);
			clone.VsCodeOptions.DisableExtensions.ShouldBeTrue();
			clone.VsCodeOptions.Commands.ShouldBe("--verbose");
			clone.Items.Count.ShouldBe(1);
		}

		/// <summary>
		/// Verifies that <see cref="VsFolder.ContainedProjectsCount"/> counts both <see cref="VsProject"/> and <see cref="VsCodeProject"/> items recursively.
		/// </summary>
		[Test]
		public void ContainedProjectsCount_IncludesVsCodeProjectsAndNestedFolders()
		{
			var root  = new VsFolder("root", string.Empty);
			var child = new VsFolder("nested", string.Empty);

			root.Items.Add(new VsProject("core", Path.Combine(tempDir, "core.csproj"), ProjectTypeEnum.CSProject));
			root.Items.Add(new VsCodeProject("ws", Path.Combine(tempDir, "app.code-workspace"), VsCodeProjectKind.Workspace));
			child.Items.Add(new VsCodeProject("api", tempDir, VsCodeProjectKind.Python));
			root.Items.Add(child);

			root.ContainedProjectsCount().ShouldBe(3);
		}
	}
}
