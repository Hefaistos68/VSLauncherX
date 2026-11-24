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
		/// Verifies that an empty folder explicitly set to VS Code launches VS Code directly with its configured options even when no project or <c>.code-workspace</c> files exist.
		/// </summary>
		[Test]
		public void RunAll_WhenEmptyFolderSetToVsCodeWithoutWorkspaceOrProjects_LaunchesVsCodeDirectly()
		{
			string emptyDir = Path.Combine(tempDir, "empty-vscode-folder");

			Directory.CreateDirectory(emptyDir);

			var launched     = new List<ProcessStartInfo>();
			var codeStable   = CreateTestVsCodeInstance("vscode-stable-user", @"C:\Code\Code.exe");
			var codeInsiders = CreateTestVsCodeInstance("vscode-insiders-user", @"C:\CodeInsiders\Code - Insiders.exe");
			var vsProvider   = CreateCapturingVsProvider(out _);
			var folder       = new VsFolder("empty-vscode-folder", emptyDir)
			{
				IdeType       = IdeType.VsCode,
				VsCodeOptions = new VsCodeLaunchOptions
				{
					Edition           = "vscode-insiders-user",
					Profile           = "Docs",
					WindowMode        = VsCodeWindowMode.NewWindow,
					DisableExtensions = true
				}
			};

			var runner = new Runner(
				new LaunchInfo { Solution = folder },
				new IdeManager(new[] { vsProvider, CreateStubVsCodeProvider(codeStable, codeInsiders) }),
				psi => { launched.Add(psi); return null; });

			runner.BuildStartInfo(folder).ShouldNotBeNull();

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(@"C:\CodeInsiders\Code - Insiders.exe");
			launched[0].Arguments.ShouldContain($"\"{emptyDir}\"");
			launched[0].Arguments.ShouldContain("--profile \"Docs\"");
			launched[0].Arguments.ShouldContain("--new-window");
			launched[0].Arguments.ShouldContain("--disable-extensions");
			launched[0].WorkingDirectory.ShouldBe(emptyDir);
		}

		/// <summary>
		/// Verifies that an empty folder with legacy <see cref="VsFolder.VsCodeOptions"/> populated (and default <see cref="IdeType.VisualStudio"/>) still launches VS Code directly.
		/// </summary>
		[Test]
		public void RunAll_WhenLegacyEmptyFolderHasVsCodeOptions_LaunchesVsCodeDirectly()
		{
			string emptyDir = Path.Combine(tempDir, "legacy-vscode-folder");

			Directory.CreateDirectory(emptyDir);

			var launched     = new List<ProcessStartInfo>();
			var codeProvider = CreateCapturingVsCodeProvider(out IIdeInstance codeInstance);
			var vsProvider   = CreateCapturingVsProvider(out _);
			var folder       = new VsFolder("legacy-vscode-folder", emptyDir)
			{
				VsCodeOptions = new VsCodeLaunchOptions()
			};

			var runner = new Runner(
				new LaunchInfo { Solution = folder },
				new IdeManager(new[] { vsProvider, codeProvider }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(codeInstance.Location);
			launched[0].Arguments.ShouldContain($"\"{emptyDir}\"");
			launched[0].WorkingDirectory.ShouldBe(emptyDir);
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
		/// Verifies that a legacy <see cref="LaunchInfo.Target"/> path resolves the matching Visual Studio instance when <see cref="LaunchInfo.DefaultTargets"/> is null.
		/// </summary>
		[Test]
		public void RunAll_LegacyLaunchInfoTargetWithoutDefaultTargets_ResolvesMatchingVisualStudioInstance()
		{
			string slnPath = Path.Combine(tempDir, "LegacyApp.sln");

			File.WriteAllText(slnPath, string.Empty);

			var launched = new List<ProcessStartInfo>();
			var vs1      = new VisualStudioInstance("VS 2019", "16.11.0", @"C:\VS19\devenv.exe", "vs19", "2019");
			var vs2      = new VisualStudioInstance("VS 2022", "17.10.0", @"C:\VS22\devenv.exe", "vs22", "2022");
			var launchInfo = new LaunchInfo
			{
				Solution       = new VsFolder(new VsSolution { Name = "LegacyApp", Path = slnPath }),
				Target         = @"c:\vs19\devenv.exe",
				DefaultTargets = null
			};

			var runner = new Runner(
				launchInfo,
				new IdeManager(new IIdeProvider[] { new VisualStudioIdeProvider(new[] { vs1, vs2 }) }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(@"C:\VS19\devenv.exe");

			var explicitLaunched = new List<ProcessStartInfo>();
			var explicitRunner   = new Runner(
				new LaunchInfo
				{
					Solution       = new VsFolder(new VsSolution { Name = "LegacyApp", Path = slnPath }),
					Target         = @"c:\vs19\devenv.exe",
					DefaultTargets = new[] { new IdeDefaultTarget(IdeType.VisualStudio, "vs22") }
				},
				new IdeManager(new IIdeProvider[] { new VisualStudioIdeProvider(new[] { vs1, vs2 }) }),
				psi => { explicitLaunched.Add(psi); return null; });

			explicitRunner.Run();

			explicitLaunched.Count.ShouldBe(1);
			explicitLaunched[0].FileName.ShouldBe(@"C:\VS22\devenv.exe");
		}

		/// <summary>
		/// Verifies that <see cref="VsFolder"/> MRU group containers with <see cref="ItemTypeEnum.VisualStudio"/> recurse into their children.
		/// </summary>
		[Test]
		public void RunAll_VisualStudioGroupFolder_RecursesIntoChildren()
		{
			string slnPath = Path.Combine(tempDir, "MruApp.sln");
			string codeDir = Path.Combine(tempDir, "mru-web");

			File.WriteAllText(slnPath, string.Empty);
			Directory.CreateDirectory(codeDir);

			var launched     = new List<ProcessStartInfo>();
			var vsProvider   = CreateCapturingVsProvider(out IIdeInstance vsInstance);
			var codeProvider = CreateCapturingVsCodeProvider(out IIdeInstance codeInstance);
			var vsGroup      = new VsFolder("Visual Studio 2022 (17.10)", string.Empty) { ItemType = ItemTypeEnum.VisualStudio };
			var codeGroup    = new VsFolder("Visual Studio Code", string.Empty) { ItemType = ItemTypeEnum.VisualStudio };
			var rootGroup    = new VsFolder("Root", string.Empty);

			vsGroup.Items.Add(new VsSolution { Name = "MruApp", Path = slnPath });
			codeGroup.Items.Add(new VsCodeProject("mru-web", codeDir, VsCodeProjectKind.Folder));
			rootGroup.Items.Add(vsGroup);
			rootGroup.Items.Add(codeGroup);

			var runner = new Runner(
				new LaunchInfo { Solution = rootGroup },
				new IdeManager(new[] { vsProvider, codeProvider }),
				psi => { launched.Add(psi); return null; });

			runner.BuildStartInfo(vsGroup).ShouldBeNull();

			runner.Run();

			launched.Count.ShouldBe(2);
			launched[0].FileName.ShouldBe(vsInstance.Location);
			launched[0].Arguments.ShouldContain($"\"{slnPath}\"");
			launched[1].FileName.ShouldBe(codeInstance.Location);
			launched[1].Arguments.ShouldContain($"\"{codeDir}\"");
		}

		/// <summary>
		/// Verifies that an MRU open-folder <see cref="VsFolder"/> with <see cref="ItemTypeEnum.Project"/> launches directly rather than being treated as an empty container.
		/// </summary>
		[Test]
		public void RunAll_OpenFolderWithProjectItemType_LaunchesDirectly()
		{
			string repoDir = Path.Combine(tempDir, "open-repo");

			Directory.CreateDirectory(repoDir);

			var launched   = new List<ProcessStartInfo>();
			var vsProvider = CreateCapturingVsProvider(out IIdeInstance vsInstance);
			var openFolder = new VsFolder("open-repo", repoDir) { ItemType = ItemTypeEnum.Project };

			var runner = new Runner(
				new LaunchInfo { Solution = openFolder },
				new IdeManager(new[] { vsProvider }),
				psi => { launched.Add(psi); return null; });

			runner.Run();

			launched.Count.ShouldBe(1);
			launched[0].FileName.ShouldBe(vsInstance.Location);
			launched[0].Arguments.ShouldContain($"\"{repoDir}\"");
			launched[0].WorkingDirectory.ShouldBe(repoDir);
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
		/// Verifies that <see cref="Runner.ShouldMoveProcessToMonitor"/> skips monitor polling when <c>ELECTRON_RUN_AS_NODE</c> is set, while permitting normal GUI launches with a preferred monitor.
		/// </summary>
		[Test]
		public void ShouldMoveProcessToMonitor_SkipsHeadlessNodeLaunchAndAllowsGuiLaunch()
		{
			string cliJsPath = Path.Combine(tempDir, "resources", "app", "out", "cli.js");
			string codeExe   = Path.Combine(tempDir, "Code.exe");

			Mock<IVsCodeEnvironment> envMock = CreateDefaultVsCodeEnvironmentMock();

			envMock.Setup(e => e.FileExists(cliJsPath)).Returns(true);

			var provider = new VsCodeIdeProvider(envMock.Object);
			var instance = new VsCodeInstance("Visual Studio Code", "1.95.0", codeExe, "vscode-stable-user", Path.Combine(tempDir, "userdata"), false, envMock.Object);

			var waitItem = new VsCodeProject("app", tempDir, VsCodeProjectKind.Folder)
			{
				PreferredMonitor  = 0,
				WaitForCompletion = true
			};

			var guiItem = new VsCodeProject("app", tempDir, VsCodeProjectKind.Folder)
			{
				PreferredMonitor  = 0,
				WaitForCompletion = false
			};

			var itemWithoutMonitor = new VsCodeProject("app", tempDir, VsCodeProjectKind.Folder)
			{
				PreferredMonitor = null
			};

			ProcessStartInfo nodeStartInfo  = provider.BuildStartInfo(waitItem, instance)!;
			ProcessStartInfo guiStartInfo   = provider.BuildStartInfo(guiItem, instance)!;
			ProcessStartInfo adminStartInfo = provider.BuildStartInfo(waitItem, instance, forceAdmin: true)!;

			nodeStartInfo.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeTrue();
			guiStartInfo.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeFalse();

			Runner.ShouldMoveProcessToMonitor(waitItem, nodeStartInfo).ShouldBeFalse();
			Runner.ShouldMoveProcessToMonitor(guiItem, guiStartInfo).ShouldBeTrue();
			Runner.ShouldMoveProcessToMonitor(waitItem, adminStartInfo).ShouldBeTrue();
			Runner.ShouldMoveProcessToMonitor(itemWithoutMonitor, guiStartInfo).ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that <see cref="Runner.Run"/> launches both <c>.sln</c> (with <c>RequiredVersion</c>) and <c>.slnx</c> items
		/// using the sidebar-selected Visual Studio version when <see cref="VsItem.VsVersion"/> is unset.
		/// </summary>
		[Test]
		public void RunAll_DefaultVisualStudioSlnAndSlnx_LaunchSidebarSelectedVersionOverRequiredVersion()
		{
			string slnPath  = Path.Combine(tempDir, "App.sln");
			string slnxPath = Path.Combine(tempDir, "App.slnx");

			File.WriteAllText(slnPath, string.Empty);
			File.WriteAllText(slnxPath, string.Empty);

			var vsCommunity = new VisualStudioInstance("Visual Studio Community 2026", "18.12.12405.243", @"C:\VS26\Community\devenv.exe", "6025944c", "2026");
			var vsInsiders  = new VisualStudioInstance("Visual Studio Community 2026 Insiders", "18.13.12412.241", @"C:\VS26\Insiders\devenv.exe", "c5d6d819", "2026");
			var vsProvider  = new VisualStudioIdeProvider(new[] { vsCommunity, vsInsiders });
			var ideManager  = new IdeManager(new IIdeProvider[] { vsProvider });

			var slnItem  = new VsSolution { Name = "AppSln", Path = slnPath, RequiredVersion = "18", VsVersion = string.Empty };
			var slnxItem = new VsSolution { Name = "AppSlnx", Path = slnxPath, RequiredVersion = string.Empty, VsVersion = string.Empty };

			(VsItem Item, string TargetId, string ExpectedExe)[] cases =
			{
				(slnItem, "c5d6d819", vsInsiders.Location),
				(slnItem, "6025944c", vsCommunity.Location),
				(slnxItem, "c5d6d819", vsInsiders.Location),
				(slnxItem, "6025944c", vsCommunity.Location)
			};

			foreach ((VsItem item, string targetId, string expectedExe) in cases)
			{
				var launched   = new List<ProcessStartInfo>();
				var launchInfo = new LaunchInfo
				{
					Solution       = new VsFolder(item.Clone()),
					DefaultTargets = new[] { new IdeDefaultTarget(IdeType.VisualStudio, targetId) }
				};
				var runner = new Runner(launchInfo, ideManager, psi => { launched.Add(psi); return null; });

				runner.Run();

				launched.Count.ShouldBe(1);
				launched[0].FileName.ShouldBe(expectedExe);
			}
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
