using System.Diagnostics;

using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeIdeProvider.BuildStartInfo"/>, cross-IDE flag stripping, <c>cli.js --wait</c> handling, and <see cref="VsCodeInstance"/> quick-launch builders.
	/// </summary>
	[TestFixture]
	public class VsCodeIdeProviderLaunchTests
	{
		private const string CodeExePath = @"C:\Program Files\Microsoft VS Code\Code.exe";
		private const string CliJsPath   = @"C:\Program Files\Microsoft VS Code\resources\app\out\cli.js";

		/// <summary>
		/// Verifies CLI argument order for <see cref="VsCodeProject"/> with profile, window mode, disable-extensions, and custom commands.
		/// </summary>
		[Test]
		public void BuildStartInfo_ForVsCodeProject_BuildsProfileWindowModeDisableExtensionsAndCommands()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: false);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);
			var project                  = new VsCodeProject
			{
				Name              = "App",
				Path              = @"C:\src\app",
				ProjectKind       = VsCodeProjectKind.NodeJs,
				Instance          = "Work",
				WindowMode        = VsCodeWindowMode.NewWindow,
				DisableExtensions = true,
				Commands          = "--goto a.ts:1"
			};

			ProcessStartInfo? psi = provider.BuildStartInfo(project, instance);

			psi.ShouldNotBeNull();
			psi.FileName.ShouldBe(CodeExePath);
			psi.UseShellExecute.ShouldBeFalse();
			psi.Verb.ShouldBe(string.Empty);
			psi.Arguments.ShouldBe("\"C:\\src\\app\" --profile \"Work\" --new-window --disable-extensions --goto a.ts:1");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeWindowMode.ReuseWindow"/> emits <c>--reuse-window</c> and <c>&lt;default&gt;</c> profile is omitted.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenReuseWindowAndDefaultProfile_EmitsReuseWindowWithoutProfileFlag()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: false);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);
			var project                  = new VsCodeProject
			{
				Name       = "App",
				Path       = @"C:\src\app.code-workspace",
				Instance   = "<default>",
				WindowMode = VsCodeWindowMode.ReuseWindow
			};

			ProcessStartInfo? psi = provider.BuildStartInfo(project, instance);

			psi.ShouldNotBeNull();
			psi.Arguments.ShouldBe("\"C:\\src\\app.code-workspace\" --reuse-window");
		}

		/// <summary>
		/// Verifies that directory paths trim trailing backslashes before quoting while drive roots remain safely quoted.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenDirectoryHasTrailingBackslashOrIsRoot_QuotesSafelyWithoutEscapingClosingQuote()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: false);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);
			var folderProj               = new VsCodeProject { Name = "App", Path = @"C:\src\app\", ProjectKind = VsCodeProjectKind.Folder };
			var rootProj                 = new VsCodeProject { Name = "Drive", Path = @"C:\", ProjectKind = VsCodeProjectKind.Folder };

			ProcessStartInfo? folderPsi = provider.BuildStartInfo(folderProj, instance);
			ProcessStartInfo? rootPsi   = provider.BuildStartInfo(rootProj, instance);

			folderPsi.ShouldNotBeNull();
			folderPsi.Arguments.ShouldBe("\"C:\\src\\app\"");

			rootPsi.ShouldNotBeNull();
			rootPsi.Arguments.ShouldNotBe("\"C:\\\"");
			rootPsi.Arguments.ShouldBe("\"C:\\\\\"");
		}

		/// <summary>
		/// Verifies that launching a <see cref="VsFolder"/> reads <see cref="VsFolder.VsCodeOptions"/> and ignores Visual Studio <c>Instance</c> / <c>Commands</c>.
		/// </summary>
		[Test]
		public void BuildStartInfo_ForVsFolder_UsesVsCodeOptionsAndIgnoresVisualStudioFields()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: false);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);
			var folder                   = new VsFolder("Workspace", @"C:\src\ws")
			{
				Instance      = "VSExp",
				Commands      = "/SafeMode",
				VsCodeOptions = new VsCodeLaunchOptions
				{
					Profile           = "Frontend",
					WindowMode        = VsCodeWindowMode.ReuseWindow,
					DisableExtensions = true,
					Commands          = "--log trace"
				}
			};

			ProcessStartInfo? psi = provider.BuildStartInfo(folder, instance);

			psi.ShouldNotBeNull();
			psi.Arguments.ShouldBe("\"C:\\src\\ws\" --profile \"Frontend\" --reuse-window --disable-extensions --log trace");
		}

		/// <summary>
		/// Verifies that launching a Visual Studio solution via single-IDE fallback targets its containing directory and strips Visual Studio <c>Instance</c> and <c>Commands</c>.
		/// </summary>
		[Test]
		public void BuildStartInfo_ForVisualStudioSolutionFallback_TargetsContainingDirectoryAndStripsVsFlags()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: false);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);
			var solution                 = new VsSolution
			{
				Name     = "App",
				Path     = @"C:\src\MySolution\App.sln",
				Instance = "Exp",
				Commands = "/SafeMode"
			};

			ProcessStartInfo? psi = provider.BuildStartInfo(solution, instance);

			psi.ShouldNotBeNull();
			psi.FileName.ShouldBe(CodeExePath);
			psi.Arguments.ShouldBe("\"C:\\src\\MySolution\"");
		}

		/// <summary>
		/// Verifies that waiting when unelevated and <c>cli.js</c> exists uses <c>ELECTRON_RUN_AS_NODE=1</c> and prepends <c>cli.js</c> with <c>--wait</c>.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenWaitRequestedViaItemOrRunAfterAndCliJsExists_ConfiguresElectronRunAsNode()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: true);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);

			var waitSelf = new VsCodeProject
			{
				Name              = "App",
				Path              = @"C:\src\app",
				ProjectKind       = VsCodeProjectKind.Folder,
				WaitForCompletion = true
			};

			var waitRunAfter = new VsCodeProject
			{
				Name        = "App2",
				Path        = @"C:\src\app",
				ProjectKind = VsCodeProjectKind.Folder,
				RunAfter    = new VsItem { WaitForCompletion = true }
			};

			var plainProject = new VsCodeProject
			{
				Name        = "App3",
				Path        = @"C:\src\app",
				ProjectKind = VsCodeProjectKind.Folder
			};

			ProcessStartInfo? psiSelf        = provider.BuildStartInfo(waitSelf, instance);
			ProcessStartInfo? psiRunAfter    = provider.BuildStartInfo(waitRunAfter, instance);
			ProcessStartInfo? psiWaitParam   = provider.BuildStartInfo(plainProject, instance, waitForExit: true);

			foreach (ProcessStartInfo? psi in new[] { psiSelf, psiRunAfter, psiWaitParam })
			{
				psi.ShouldNotBeNull();
				psi.FileName.ShouldBe(CodeExePath);
				psi.UseShellExecute.ShouldBeFalse();
				psi.Environment["ELECTRON_RUN_AS_NODE"].ShouldBe("1");
				psi.Arguments.ShouldBe($"\"{CliJsPath}\" \"C:\\src\\app\" --wait");
			}
		}

		/// <summary>
		/// Verifies fallback to direct <c>Code.exe --wait</c> when elevated or when <c>cli.js</c> is absent.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenElevatedOrCliJsMissing_FallsBackToDirectExecutableWithWait()
		{
			var project = new VsCodeProject
			{
				Name              = "App",
				Path              = @"C:\src\app",
				ProjectKind       = VsCodeProjectKind.Folder,
				WaitForCompletion = true
			};

			Mock<IVsCodeEnvironment> noCliEnv = CreateEnvironment(cliJsExists: false);
			var noCliProvider                 = new VsCodeIdeProvider(noCliEnv.Object);
			ProcessStartInfo? noCliPsi        = noCliProvider.BuildStartInfo(project, CreateInstance(noCliEnv.Object));

			noCliPsi.ShouldNotBeNull();
			noCliPsi.FileName.ShouldBe(CodeExePath);
			noCliPsi.UseShellExecute.ShouldBeFalse();
			noCliPsi.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeFalse();
			noCliPsi.Arguments.ShouldBe("\"C:\\src\\app\" --wait");

			Mock<IVsCodeEnvironment> cliEnv = CreateEnvironment(cliJsExists: true);
			var cliProvider                 = new VsCodeIdeProvider(cliEnv.Object);
			ProcessStartInfo? adminPsi      = cliProvider.BuildStartInfo(project, CreateInstance(cliEnv.Object), forceAdmin: true);

			adminPsi.ShouldNotBeNull();
			adminPsi.FileName.ShouldBe(CodeExePath);
			adminPsi.Verb.ShouldBe("runas");
			adminPsi.UseShellExecute.ShouldBeTrue();
			adminPsi.ErrorDialog.ShouldBeTrue();
			adminPsi.Arguments.ShouldBe("\"C:\\src\\app\" --wait");
		}

		/// <summary>
		/// Verifies that non-launchable items or items without a path return <c>null</c>.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenNonLaunchableOrEmptyPath_ReturnsNull()
		{
			Mock<IVsCodeEnvironment> env = CreateEnvironment(cliJsExists: false);
			VsCodeInstance instance      = CreateInstance(env.Object);
			var provider                 = new VsCodeIdeProvider(env.Object);
			var otherItem                = new VsItem { Name = "Script", Path = @"C:\tools\prep.cmd", ItemType = ItemTypeEnum.Other };
			var emptyGroup               = new VsFolder { Name = "Group", Path = string.Empty };

			provider.BuildStartInfo(otherItem, instance).ShouldBeNull();
			provider.BuildStartInfo(emptyGroup, instance).ShouldBeNull();
		}

		/// <summary>
		/// Verifies direct quick-action <see cref="ProcessStartInfo"/> builders on <see cref="VsCodeInstance"/>.
		/// </summary>
		[Test]
		public void VsCodeInstance_DirectQuickActionBuilders_ProduceExpectedStartInfo()
		{
			string? originalElectronEnv = Environment.GetEnvironmentVariable("ELECTRON_RUN_AS_NODE");

			Environment.SetEnvironmentVariable("ELECTRON_RUN_AS_NODE", "1");

			try
			{
				VsCodeInstance instance = CreateInstance(CreateEnvironment(cliJsExists: false).Object);

				ProcessStartInfo startPsi     = instance.BuildStartInfo();
				ProcessStartInfo adminPsi     = instance.BuildAdminStartInfo();
				ProcessStartInfo newWindowPsi = instance.BuildNewWindowStartInfo();
				ProcessStartInfo noExtPsi     = instance.BuildWithoutExtensionsStartInfo();
				ProcessStartInfo openPathPsi  = instance.BuildOpenPathStartInfo(@"C:\src\demo\");

				startPsi.FileName.ShouldBe(CodeExePath);
				startPsi.Arguments.ShouldBe(string.Empty);
				startPsi.UseShellExecute.ShouldBeFalse();
				startPsi.Verb.ShouldBe(string.Empty);
				startPsi.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeFalse();

				adminPsi.FileName.ShouldBe(CodeExePath);
				adminPsi.Arguments.ShouldBe(string.Empty);
				adminPsi.Verb.ShouldBe("runas");
				adminPsi.UseShellExecute.ShouldBeTrue();

				newWindowPsi.FileName.ShouldBe(CodeExePath);
				newWindowPsi.Arguments.ShouldBe("--new-window");
				newWindowPsi.UseShellExecute.ShouldBeFalse();
				newWindowPsi.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeFalse();

				noExtPsi.FileName.ShouldBe(CodeExePath);
				noExtPsi.Arguments.ShouldBe("--disable-extensions");
				noExtPsi.UseShellExecute.ShouldBeFalse();
				noExtPsi.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeFalse();

				openPathPsi.FileName.ShouldBe(CodeExePath);
				openPathPsi.Arguments.ShouldBe("\"C:\\src\\demo\"");
				openPathPsi.UseShellExecute.ShouldBeFalse();
				openPathPsi.Environment.ContainsKey("ELECTRON_RUN_AS_NODE").ShouldBeFalse();
			}
			finally
			{
				Environment.SetEnvironmentVariable("ELECTRON_RUN_AS_NODE", originalElectronEnv);
			}
		}

		/// <summary>
		/// Creates a mock <see cref="IVsCodeEnvironment"/> with optional <c>cli.js</c> presence.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateEnvironment(bool cliJsExists)
		{
			var mock = new Mock<IVsCodeEnvironment>();

			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Returns(@"C:\Users\Dev\AppData\Local");
			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"C:\Users\Dev\AppData\Roaming");
			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(@"C:\Program Files");
			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(@"C:\Program Files (x86)");
			mock.Setup(e => e.GetUninstallEntries()).Returns(Array.Empty<VsCodeUninstallEntry>());
			mock.Setup(e => e.FileExists(CodeExePath)).Returns(true);
			mock.Setup(e => e.FileExists(CliJsPath)).Returns(cliJsExists);
			mock.Setup(e => e.GetFileVersion(CodeExePath)).Returns("1.95.0");

			return mock;
		}

		/// <summary>
		/// Creates a <see cref="VsCodeInstance"/> bound to the specified test environment.
		/// </summary>
		private static VsCodeInstance CreateInstance(IVsCodeEnvironment environment)
		{
			return new VsCodeInstance(
				"Visual Studio Code",
				"1.95.0",
				CodeExePath,
				"vscode-stable-system",
				@"C:\Users\Dev\AppData\Roaming\Code",
				isInsiders: false,
				environment);
		}
	}
}
