using System.Diagnostics;

using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Ide;

namespace BackgroundLaunch.Tests
{
	/// <summary>
	/// Unit tests for <see cref="Runner"/> multi-IDE resolution, workspace-backed folders, mixed groups, and cross-IDE fallback.
	/// </summary>
	[TestFixture]
	public class RunnerTests
	{
		private string tempDir = string.Empty;
		private string originalCurrentDirectory = string.Empty;

		/// <summary>
		/// Creates an isolated temporary directory and saves the current working directory before each test.
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			originalCurrentDirectory = Environment.CurrentDirectory;
			tempDir                  = Path.Combine(Path.GetTempPath(), "BackgroundLaunchTests_" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(tempDir);
		}

		/// <summary>
		/// Restores the original working directory and removes the temporary directory after each test.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			if (!string.IsNullOrEmpty(originalCurrentDirectory) && Directory.Exists(originalCurrentDirectory))
			{
				Environment.CurrentDirectory = originalCurrentDirectory;
			}

			if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, recursive: true);
			}
		}

		/// <summary>
		/// Verifies that a nested workspace-backed folder launches in VS Code without recursing into its child items.
		/// </summary>
		[Test]
		public void RunAll_NestedWorkspaceBackedFolder_OpensInVsCodeAndDoesNotRecurseIntoChildren()
		{
			string wsDir      = CreateWorkspaceDir(tempDir, "ws-app");
			var launched      = new List<ProcessStartInfo>();
			var codeProvider  = CreateCapturingVsCodeProvider(out IIdeInstance codeInstance);
			var vsProvider    = CreateCapturingVsProvider(out _);

			var outerGroup = new VsFolder("Outer", string.Empty);
			var wsFolder   = new VsFolder("ws-app", wsDir);

			wsFolder.Items.Add(new VsSolution { Name = "ChildSln", Path = Path.Combine(wsDir, "Child.sln") });
			outerGroup.Items.Add(wsFolder);

			var runner = new Runner(
				new LaunchInfo { Solution = outerGroup },
				new IdeManager(new[] { vsProvider, codeProvider }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(codeInstance.Location);
			launched[0].Arguments.ShouldContain($"\"{wsDir}\"");
			launched[0].WorkingDirectory.ShouldBe(wsDir);
		}

		/// <summary>
		/// Verifies that a mixed-IDE group skips VS Code items when VS Code is uninstalled and still launches Visual Studio children.
		/// </summary>
		[Test]
		public void RunAll_MixedIdeGroupWhenVsCodeMissing_SkipsVsCodeChildAndLaunchesVisualStudioChild()
		{
			string slnPath = Path.Combine(tempDir, "App.sln");

			File.WriteAllText(slnPath, string.Empty);

			var launched          = new List<ProcessStartInfo>();
			var vsProvider        = CreateCapturingVsProvider(out IIdeInstance vsInstance);
			var emptyCodeProvider = new Mock<IIdeProvider>();

			emptyCodeProvider.SetupGet(p => p.IdeType).Returns(IdeType.VsCode);
			emptyCodeProvider.SetupGet(p => p.IsInstalled).Returns(false);

			var group = new VsFolder("Mixed", string.Empty);

			group.Items.Add(new VsCodeProject("web", tempDir, VsCodeProjectKind.Folder));
			group.Items.Add(new VsSolution { Name = "App", Path = slnPath });

			var runner = new Runner(
				new LaunchInfo { Solution = group },
				new IdeManager(new[] { vsProvider, emptyCodeProvider.Object }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(vsInstance.Location);
		}

		/// <summary>
		/// Verifies that a mixed-IDE group resolves per-IDE <see cref="LaunchInfo.DefaultTargets"/> and assigns directory working paths.
		/// </summary>
		[Test]
		public void RunAll_MixedIdeGroupUsesPerIdeDefaultTargetsAndSetsDirectoryWorkingPath()
		{
			string slnPath = Path.Combine(tempDir, "App.sln");

			File.WriteAllText(slnPath, string.Empty);

			string webDir = Path.Combine(tempDir, "web");

			Directory.CreateDirectory(webDir);

			var launched     = new List<ProcessStartInfo>();
			var vs1          = new VisualStudioInstance("VS 2019", "16.11.0", @"C:\VS19\devenv.exe", "vs19", "2019");
			var vs2          = new VisualStudioInstance("VS 2022", "17.10.0", @"C:\VS22\devenv.exe", "vs22", "2022");
			var codeStable   = CreateTestVsCodeInstance("vscode-stable-user", @"C:\Code\Code.exe");
			var codeInsiders = CreateTestVsCodeInstance("vscode-insiders-user", @"C:\CodeInsiders\Code - Insiders.exe");

			var group = new VsFolder("Mixed", string.Empty);

			group.Items.Add(new VsSolution { Name = "App", Path = slnPath });
			group.Items.Add(new VsCodeProject("web", webDir, VsCodeProjectKind.NodeJs));

			var launchInfo = new LaunchInfo
			{
				Solution       = group,
				DefaultTargets = new[]
				{
					new IdeDefaultTarget(IdeType.VisualStudio, "vs19"),
					new IdeDefaultTarget(IdeType.VsCode, "vscode-insiders-user")
				}
			};

			var runner = new Runner(
				launchInfo,
				new IdeManager(new IIdeProvider[]
				{
					new VisualStudioIdeProvider(new[] { vs1, vs2 }),
					CreateStubVsCodeProvider(codeStable, codeInsiders)
				}),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(2);
			launched[0].FileName.ShouldBe(@"C:\VS19\devenv.exe");
			launched[0].WorkingDirectory.ShouldBe(tempDir);
			launched[1].FileName.ShouldBe(@"C:\CodeInsiders\Code - Insiders.exe");
			launched[1].WorkingDirectory.ShouldBe(webDir);
		}

		/// <summary>
		/// Verifies that <c>waitForExit: true</c> is forwarded to <see cref="IIdeProvider.BuildStartInfo"/> when <see cref="VsOptions.RunAfter"/> has <see cref="VsOptions.WaitForCompletion"/> enabled.
		/// </summary>
		[Test]
		public void RunAll_WhenRunAfterWaitsForCompletion_PassesWaitForExitTrueToProvider()
		{
			string wsDir       = CreateWorkspaceDir(tempDir, "ws-wait");
			var codeInstance   = CreateTestVsCodeInstance("vscode-stable-user", @"C:\Code\Code.exe");
			var providerMock   = new Mock<IIdeProvider>();
			var launched       = new List<ProcessStartInfo>();

			providerMock.SetupGet(p => p.IdeType).Returns(IdeType.VsCode);
			providerMock.SetupGet(p => p.IsInstalled).Returns(true);
			providerMock.SetupGet(p => p.DefaultInstance).Returns(codeInstance);
			providerMock.SetupGet(p => p.Instances).Returns(new[] { codeInstance });
			providerMock
				.Setup(p => p.BuildStartInfo(It.IsAny<VsItem>(), It.IsAny<IIdeInstance>(), It.IsAny<bool>(), It.IsAny<bool>()))
				.Returns<VsItem, IIdeInstance, bool, bool>((item, inst, _, waitForExit) => new ProcessStartInfo
				{
					FileName  = inst.Location,
					Arguments = waitForExit ? "--wait" : string.Empty
				});

			var leafProject = new VsCodeProject("leaf", tempDir, VsCodeProjectKind.Folder)
			{
				RunAfter = new VsItem { Name = "After", Path = string.Empty, WaitForCompletion = true }
			};
			var wsFolder = new VsFolder("ws-wait", wsDir)
			{
				RunAfter = new VsItem { Name = "After", Path = string.Empty, WaitForCompletion = true }
			};

			var runner = new Runner(
				new LaunchInfo { Solution = new VsFolder(leafProject) },
				new IdeManager(new[] { providerMock.Object }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			var wsRunner = new Runner(
				new LaunchInfo { Solution = wsFolder },
				new IdeManager(new[] { providerMock.Object }),
				psi => { launched.Add(psi); return null; });

			wsRunner.Run();

			providerMock.Verify(p => p.BuildStartInfo(leafProject, codeInstance, false, true), Times.Once);
			providerMock.Verify(p => p.BuildStartInfo(wsFolder, codeInstance, false, true), Times.Once);
			launched.Count.ShouldBe(2);
			launched[0].Arguments.ShouldBe("--wait");
			launched[1].Arguments.ShouldBe("--wait");
		}

		/// <summary>
		/// Verifies single-IDE fallback when only VS Code is installed: launching a Visual Studio solution opens its containing directory and strips Visual Studio flags.
		/// </summary>
		[Test]
		public void RunAll_CrossIdeSingleIdeFallback_OpensContainingDirectoryWithoutVsFlags()
		{
			string slnDir  = Path.Combine(tempDir, "MySolution");
			string slnPath = Path.Combine(slnDir, "App.sln");

			Directory.CreateDirectory(slnDir);
			File.WriteAllText(slnPath, string.Empty);

			string codeExePath = @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\Code.exe";
			var envMock        = CreateDefaultVsCodeEnvironmentMock();

			envMock.Setup(e => e.FileExists(codeExePath)).Returns(true);
			envMock.Setup(e => e.GetFileVersion(codeExePath)).Returns("1.95.0");

			var launched = new List<ProcessStartInfo>();
			var solution = new VsSolution
			{
				Name     = "App",
				Path     = slnPath,
				Instance = "Exp",
				Commands = "/SafeMode"
			};
			var runner = new Runner(
				new LaunchInfo { Solution = new VsFolder(solution) },
				new IdeManager(new IIdeProvider[]
				{
					new VisualStudioIdeProvider(Array.Empty<VisualStudioInstance>()),
					new VsCodeIdeProvider(envMock.Object)
				}),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(codeExePath);
			launched[0].Arguments.ShouldBe($"\"{slnDir}\"");
			launched[0].Arguments.ShouldNotContain("--profile");
			launched[0].Arguments.ShouldNotContain("/SafeMode");
			launched[0].Arguments.ShouldNotContain("/RootSuffix");
			launched[0].WorkingDirectory.ShouldBe(slnDir);
		}

		/// <summary>
		/// Verifies that a workspace-backed folder falls back to recursing into children when VS Code is not installed.
		/// </summary>
		[Test]
		public void RunAll_WorkspaceBackedFolderWhenVsCodeMissing_RecursesIntoVisualStudioChildren()
		{
			string wsDir   = CreateWorkspaceDir(tempDir, "ws-fallback");
			string slnPath = Path.Combine(wsDir, "Child.sln");

			File.WriteAllText(slnPath, string.Empty);

			var launched          = new List<ProcessStartInfo>();
			var vsProvider        = CreateCapturingVsProvider(out IIdeInstance vsInstance);
			var emptyCodeProvider = new Mock<IIdeProvider>();

			emptyCodeProvider.SetupGet(p => p.IdeType).Returns(IdeType.VsCode);
			emptyCodeProvider.SetupGet(p => p.IsInstalled).Returns(false);

			var wsFolder = new VsFolder("ws-fallback", wsDir);

			wsFolder.Items.Add(new VsSolution { Name = "Child", Path = slnPath });

			var runner = new Runner(
				new LaunchInfo { Solution = wsFolder },
				new IdeManager(new[] { vsProvider, emptyCodeProvider.Object }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(vsInstance.Location);
			launched[0].Arguments.ShouldContain($"\"{slnPath}\"");
		}

		/// <summary>
		/// Verifies that a non-workspace disk folder returns <c>null</c> from <see cref="Runner.BuildStartInfo"/> and recurses into children during <see cref="Runner.RunAll"/>.
		/// </summary>
		[Test]
		public void BuildStartInfo_NonWorkspaceFolderWithExistingPath_ReturnsNullAndRunAllRecurses()
		{
			string childDir = Path.Combine(tempDir, "child-project");

			Directory.CreateDirectory(childDir);

			var launched     = new List<ProcessStartInfo>();
			var codeProvider = CreateCapturingVsCodeProvider(out IIdeInstance codeInstance);
			var vsProvider   = CreateCapturingVsProvider(out _);
			var group        = new VsFolder("DiskGroup", tempDir);

			group.Items.Add(new VsCodeProject("child-project", childDir, VsCodeProjectKind.Folder));

			var runner = new Runner(
				new LaunchInfo { Solution = group },
				new IdeManager(new[] { vsProvider, codeProvider }),
				psi => { launched.Add(psi); return null; });

			runner.BuildStartInfo(group).ShouldBeNull();

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(codeInstance.Location);
			launched[0].Arguments.ShouldContain($"\"{childDir}\"");
		}

		/// <summary>
		/// Verifies that <c>RunBefore</c> and <c>RunAfter</c> items of <see cref="ItemTypeEnum.Other"/> launch in order around the main item.
		/// </summary>
		[Test]
		public void RunAll_RunBeforeAndRunAfterItems_LaunchInExpectedOrder()
		{
			string scriptDir  = Path.Combine(tempDir, "scripts");
			string beforePath = Path.Combine(scriptDir, "before.cmd");
			string afterPath  = Path.Combine(scriptDir, "after.cmd");

			Directory.CreateDirectory(scriptDir);
			File.WriteAllText(beforePath, string.Empty);
			File.WriteAllText(afterPath, string.Empty);

			var launched     = new List<ProcessStartInfo>();
			var codeProvider = CreateCapturingVsCodeProvider(out IIdeInstance codeInstance);
			var project      = new VsCodeProject("web", tempDir, VsCodeProjectKind.Folder)
			{
				RunBefore = new VsItem { Name = "Before", Path = beforePath, Commands = "--prep", ItemType = ItemTypeEnum.Other },
				RunAfter  = new VsItem { Name = "After", Path = afterPath, Commands = "--done", ItemType = ItemTypeEnum.Other }
			};

			var runner = new Runner(
				new LaunchInfo { Solution = new VsFolder(project) },
				new IdeManager(new[] { codeProvider }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(3);
			launched[0].FileName.ShouldBe(beforePath);
			launched[0].Arguments.ShouldBe("--prep");
			launched[0].UseShellExecute.ShouldBeTrue();
			launched[0].WorkingDirectory.ShouldBe(scriptDir);

			launched[1].FileName.ShouldBe(codeInstance.Location);
			launched[1].WorkingDirectory.ShouldBe(tempDir);

			launched[2].FileName.ShouldBe(afterPath);
			launched[2].Arguments.ShouldBe("--done");
			launched[2].WorkingDirectory.ShouldBe(scriptDir);
		}

		/// <summary>
		/// Verifies <see cref="Runner.ResolveWorkingDirectory"/> for directory, file, and empty paths.
		/// </summary>
		[Test]
		public void ResolveWorkingDirectory_ReturnsDirectoryPathOrParentDirectory()
		{
			string filePath = Path.Combine(tempDir, "App.sln");

			Runner.ResolveWorkingDirectory(tempDir).ShouldBe(tempDir);
			Runner.ResolveWorkingDirectory(filePath).ShouldBe(tempDir);
			Runner.ResolveWorkingDirectory(string.Empty).ShouldBeNull();
			Runner.ResolveWorkingDirectory(null).ShouldBeNull();
		}

		/// <summary>
		/// Creates a subdirectory containing an empty <c>.code-workspace</c> file.
		/// </summary>
		private static string CreateWorkspaceDir(string rootTempDir, string folderName)
		{
			string wsDir = Path.Combine(rootTempDir, folderName);

			Directory.CreateDirectory(wsDir);
			File.WriteAllText(Path.Combine(wsDir, $"{folderName}.code-workspace"), "{}");

			return wsDir;
		}

		/// <summary>
		/// Creates a Visual Studio provider with a single synthetic instance.
		/// </summary>
		private static IIdeProvider CreateCapturingVsProvider(out IIdeInstance vsInstance)
		{
			var instance = new VisualStudioInstance("Visual Studio 2022", "17.10.0", @"C:\VS22\devenv.exe", "vs22", "2022");

			vsInstance = instance;

			return new VisualStudioIdeProvider(new[] { instance });
		}

		/// <summary>
		/// Creates a VS Code provider stub backed by real <see cref="VsCodeIdeProvider"/> start-info construction.
		/// </summary>
		private IIdeProvider CreateCapturingVsCodeProvider(out IIdeInstance codeInstance)
		{
			VsCodeInstance instance = CreateTestVsCodeInstance("vscode-stable-user", @"C:\Code\Code.exe");

			codeInstance = instance;

			return CreateStubVsCodeProvider(instance);
		}

		/// <summary>
		/// Creates a synthetic <see cref="VsCodeInstance"/> bound to a mocked environment.
		/// </summary>
		private VsCodeInstance CreateTestVsCodeInstance(string identifier, string location)
		{
			Mock<IVsCodeEnvironment> envMock = CreateDefaultVsCodeEnvironmentMock();
			bool insiders                    = identifier.Contains("insiders", StringComparison.OrdinalIgnoreCase);

			return new VsCodeInstance(
				insiders ? "Visual Studio Code - Insiders" : "Visual Studio Code",
				"1.95.0",
				location,
				identifier,
				Path.Combine(tempDir, "userdata-" + identifier),
				insiders,
				envMock.Object);
		}

		/// <summary>
		/// Creates an <see cref="IIdeProvider"/> stub for the supplied <see cref="VsCodeInstance"/> items delegating start-info construction to <see cref="VsCodeIdeProvider"/>.
		/// </summary>
		private static IIdeProvider CreateStubVsCodeProvider(params VsCodeInstance[] instances)
		{
			Mock<IVsCodeEnvironment> envMock = CreateDefaultVsCodeEnvironmentMock();
			var realProvider                 = new VsCodeIdeProvider(envMock.Object);
			var mock                         = new Mock<IIdeProvider>();

			mock.SetupGet(p => p.IdeType).Returns(IdeType.VsCode);
			mock.SetupGet(p => p.IsInstalled).Returns(instances.Length > 0);
			mock.SetupGet(p => p.Instances).Returns(instances);
			mock.SetupGet(p => p.DefaultInstance).Returns(instances.FirstOrDefault(i => !i.IsInsiders) ?? instances.FirstOrDefault());
			mock.Setup(p => p.FindByIdentifier(It.IsAny<string?>()))
				.Returns<string?>(id => string.IsNullOrWhiteSpace(id)
					? null
					: instances.FirstOrDefault(i => i.Identifier.Equals(id, StringComparison.OrdinalIgnoreCase)));
			mock.Setup(p => p.BuildStartInfo(It.IsAny<VsItem>(), It.IsAny<IIdeInstance>(), It.IsAny<bool>(), It.IsAny<bool>()))
				.Returns<VsItem, IIdeInstance, bool, bool>((item, instance, forceAdmin, waitForExit) =>
					realProvider.BuildStartInfo(item, instance, forceAdmin, waitForExit));

			return mock.Object;
		}

		/// <summary>
		/// Creates a baseline mocked <see cref="IVsCodeEnvironment"/> with empty discovery results.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateDefaultVsCodeEnvironmentMock()
		{
			var envMock = new Mock<IVsCodeEnvironment>();

			envMock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Returns(@"C:\Users\Dev\AppData\Local");
			envMock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"C:\Users\Dev\AppData\Roaming");
			envMock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(@"C:\Program Files");
			envMock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(@"C:\Program Files (x86)");
			envMock.Setup(e => e.GetUninstallEntries()).Returns(Array.Empty<VsCodeUninstallEntry>());

			return envMock;
		}
	}
}
