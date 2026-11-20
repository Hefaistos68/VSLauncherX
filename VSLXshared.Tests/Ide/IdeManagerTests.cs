using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="IdeManager"/> provider management, resolution precedence, and single-IDE fallback.
	/// </summary>
	[TestFixture]
	public class IdeManagerTests
	{
		/// <summary>
		/// Verifies the 4-step Visual Studio resolution precedence: explicit ID, solution required version, sidebar default, and provider default.
		/// </summary>
		[Test]
		public void ResolveInstance_FollowsExplicitIdThenSolutionRequiredVersionThenDefaultTargetThenProviderDefault()
		{
			var vs2019     = CreateMockInstance(IdeType.VisualStudio, "vs19", "16.11");
			var vs2022     = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
			var vsProvider = CreateMockProvider(IdeType.VisualStudio, vs2022, vs2019, vs2022);

			vsProvider.Setup(p => p.FindByVersion("16")).Returns(vs2019);

			var manager   = new IdeManager(new[] { vsProvider.Object });
			var pinned    = new VsSolution { VsVersion = "vs19", RequiredVersion = "17" };
			var byReq     = new VsSolution { RequiredVersion = "16" };
			var bySidebar = new VsProject();
			var defaults  = new Dictionary<IdeType, string> { [IdeType.VisualStudio] = "vs19" };

			manager.ResolveInstance(pinned, defaults)!.Identifier.ShouldBe("vs19");
			manager.ResolveInstance(byReq, defaults)!.Identifier.ShouldBe("vs19");
			manager.ResolveInstance(bySidebar, defaults)!.Identifier.ShouldBe("vs19");
			manager.ResolveInstance(bySidebar, null)!.Identifier.ShouldBe("vs22");
		}

		/// <summary>
		/// Verifies that a solution's <see cref="VsSolution.RequiredVersion"/> takes precedence over a different sidebar default identifier.
		/// </summary>
		[Test]
		public void ResolveInstance_SolutionRequiredVersionTakesPrecedenceOverDefaultTarget()
		{
			var vs2019     = CreateMockInstance(IdeType.VisualStudio, "vs19", "16.11");
			var vs2022     = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
			var vsProvider = CreateMockProvider(IdeType.VisualStudio, vs2022, vs2019, vs2022);

			vsProvider.Setup(p => p.FindByVersion("16")).Returns(vs2019);

			var manager  = new IdeManager(new[] { vsProvider.Object });
			var byReq    = new VsSolution { RequiredVersion = "16" };
			var defaults = new Dictionary<IdeType, string> { [IdeType.VisualStudio] = "vs22" };

			manager.ResolveInstance(byReq, defaults)!.Identifier.ShouldBe("vs19");
		}

		/// <summary>
		/// Verifies single-IDE fallback when Visual Studio is not installed and a Visual Studio solution resolves to VS Code.
		/// </summary>
		[Test]
		public void ResolveInstance_WhenOnlyVsCodeInstalled_FallsBackVisualStudioSolutionToVsCode()
		{
			var vsProvider   = CreateMockProvider(IdeType.VisualStudio, defaultInstance: null);
			var code         = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
			var codeProvider = CreateMockProvider(IdeType.VsCode, code, code);
			var manager      = new IdeManager(new[] { vsProvider.Object, codeProvider.Object });
			var sln          = new VsSolution { Path = @"C:\src\App.sln" };

			IIdeInstance? resolved = manager.ResolveInstance(sln);

			resolved.ShouldNotBeNull();
			resolved.IdeType.ShouldBe(IdeType.VsCode);
			resolved.Identifier.ShouldBe("vscode-stable-user");
		}

		/// <summary>
		/// Verifies that single-IDE fallback honors the VS Code default target identifier before the VS Code provider default.
		/// </summary>
		[Test]
		public void ResolveInstance_WhenOnlyVsCodeInstalled_SingleIdeFallbackUsesDefaultTargetBeforeProviderDefault()
		{
			var vsProvider   = CreateMockProvider(IdeType.VisualStudio, defaultInstance: null);
			var codeStable   = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
			var codeInsiders = CreateMockInstance(IdeType.VsCode, "vscode-insiders-user", "1.96.0");
			var codeProvider = CreateMockProvider(IdeType.VsCode, codeStable, codeStable, codeInsiders);
			var manager      = new IdeManager(new[] { vsProvider.Object, codeProvider.Object });
			var sln          = new VsSolution { Path = @"C:\src\App.sln" };
			var defaults     = new Dictionary<IdeType, string> { [IdeType.VsCode] = "vscode-insiders-user" };

			IIdeInstance? resolved = manager.ResolveInstance(sln, defaults);

			resolved.ShouldNotBeNull();
			resolved.Identifier.ShouldBe("vscode-insiders-user");
		}

		/// <summary>
		/// Verifies that a workspace-backed folder resolves to the VS Code edition configured in <see cref="VsFolder.VsCodeOptions"/> when VS Code is installed.
		/// </summary>
		[Test]
		public void ResolveInstance_WhenWorkspaceBackedFolderAndVsCodeInstalled_ResolvesVsCodeEdition()
		{
			string wsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(wsDir);
			File.WriteAllText(Path.Combine(wsDir, "app.code-workspace"), "{}");

			try
			{
				var vs           = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
				var codeStable   = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
				var codeInsiders = CreateMockInstance(IdeType.VsCode, "vscode-insiders-user", "1.96.0");
				var manager      = new IdeManager(new[]
				{
					CreateMockProvider(IdeType.VisualStudio, vs, vs).Object,
					CreateMockProvider(IdeType.VsCode, codeStable, codeStable, codeInsiders).Object
				});
				var folder = new VsFolder("app", wsDir) { VsCodeOptions = new VsCodeLaunchOptions { Edition = "vscode-insiders-user" } };

				manager.ResolveInstance(folder)!.Identifier.ShouldBe("vscode-insiders-user");
			}
			finally
			{
				Directory.Delete(wsDir, true);
			}
		}

		/// <summary>
		/// Verifies that a workspace-backed folder without an explicit VS Code edition falls back to the VS Code default target or provider default,
		/// and targets Visual Studio when VS Code is not installed.
		/// </summary>
		[Test]
		public void ResolveInstance_WhenWorkspaceBackedFolderHasNoEditionOrNoVsCode_ResolvesExpectedDefault()
		{
			string wsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(wsDir);
			File.WriteAllText(Path.Combine(wsDir, "app.code-workspace"), "{}");

			try
			{
				var vs           = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
				var codeStable   = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
				var codeInsiders = CreateMockInstance(IdeType.VsCode, "vscode-insiders-user", "1.96.0");
				var folder       = new VsFolder("app", wsDir);
				var defaults     = new Dictionary<IdeType, string> { [IdeType.VsCode] = "vscode-insiders-user" };

				var bothManager = new IdeManager(new[]
				{
					CreateMockProvider(IdeType.VisualStudio, vs, vs).Object,
					CreateMockProvider(IdeType.VsCode, codeStable, codeStable, codeInsiders).Object
				});

				bothManager.ResolveInstance(folder, defaults)!.Identifier.ShouldBe("vscode-insiders-user");
				bothManager.ResolveInstance(folder, null)!.Identifier.ShouldBe("vscode-stable-user");

				var vsOnlyManager = new IdeManager(new[]
				{
					CreateMockProvider(IdeType.VisualStudio, vs, vs).Object,
					CreateMockProvider(IdeType.VsCode, defaultInstance: null).Object
				});

				vsOnlyManager.ResolveInstance(folder, defaults)!.Identifier.ShouldBe("vs22");
			}
			finally
			{
				Directory.Delete(wsDir, true);
			}
		}

		/// <summary>
		/// Verifies that a <see cref="VsCodeProject"/> does not fall back to Visual Studio when VS Code is uninstalled.
		/// </summary>
		[Test]
		public void ResolveInstance_WhenVsCodeProjectAndOnlyVisualStudioInstalled_ReturnsNull()
		{
			var vs      = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
			var manager = new IdeManager(new[]
			{
				CreateMockProvider(IdeType.VisualStudio, vs, vs).Object,
				CreateMockProvider(IdeType.VsCode, defaultInstance: null).Object
			});
			var codeProject = new VsCodeProject("demo", @"C:\src\demo", VsCodeProjectKind.Folder);

			manager.ResolveInstance(codeProject).ShouldBeNull();
		}

		/// <summary>
		/// Verifies that a plain disk folder explicitly configured for VS Code resolves to VS Code even without a <c>.code-workspace</c> file.
		/// </summary>
		[Test]
		public void ResolveInstance_WhenPlainEmptyFolderConfiguredForVsCode_ResolvesVsCodeEditionOrDefault()
		{
			string emptyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(emptyDir);

			try
			{
				var vs           = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
				var codeStable   = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
				var codeInsiders = CreateMockInstance(IdeType.VsCode, "vscode-insiders-user", "1.96.0");
				var manager      = new IdeManager(new[]
				{
					CreateMockProvider(IdeType.VisualStudio, vs, vs).Object,
					CreateMockProvider(IdeType.VsCode, codeStable, codeStable, codeInsiders).Object
				});
				var pinnedFolder  = new VsFolder("notes", emptyDir) { IdeType = IdeType.VsCode, VsCodeOptions = new VsCodeLaunchOptions { Edition = "vscode-insiders-user" } };
				var defaultFolder = new VsFolder("plain", emptyDir) { IdeType = IdeType.VsCode };
				var legacyFolder  = new VsFolder("legacy", emptyDir) { VsCodeOptions = new VsCodeLaunchOptions() };

				manager.ResolveInstance(pinnedFolder)!.Identifier.ShouldBe("vscode-insiders-user");
				manager.ResolveInstance(defaultFolder)!.Identifier.ShouldBe("vscode-stable-user");
				manager.ResolveInstance(legacyFolder)!.Identifier.ShouldBe("vscode-stable-user");
			}
			finally
			{
				Directory.Delete(emptyDir, true);
			}
		}

		/// <summary>
		/// Verifies that <see cref="IdeManager.ResolvePreferredIdentifier"/> returns the preferred installed edition, falls back to the provider default, and returns <c>null</c> when the IDE is not installed.
		/// </summary>
		[Test]
		public void ResolvePreferredIdentifier_PrefersInstalledSelectionThenDefaultThenNullWhenMissing()
		{
			var codeStable   = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
			var codeInsiders = CreateMockInstance(IdeType.VsCode, "vscode-insiders-user", "1.96.0");
			var manager      = new IdeManager(new[]
			{
				CreateMockProvider(IdeType.VisualStudio, defaultInstance: null).Object,
				CreateMockProvider(IdeType.VsCode, codeStable, codeStable, codeInsiders).Object
			});

			manager.ResolvePreferredIdentifier(IdeType.VsCode, "vscode-insiders-user").ShouldBe("vscode-insiders-user");
			manager.ResolvePreferredIdentifier(IdeType.VsCode, "missing-id").ShouldBe("vscode-stable-user");
			manager.ResolvePreferredIdentifier(IdeType.VsCode, null).ShouldBe("vscode-stable-user");
			manager.ResolvePreferredIdentifier(IdeType.VisualStudio, "vs22").ShouldBeNull();
		}

		/// <summary>
		/// Verifies provider querying, installation flags, and MRU aggregation across registered providers.
		/// </summary>
		[Test]
		public void ProviderQueriesAndAggregation_ReturnInstalledStateAndCombinedRecentProjects()
		{
			var vs           = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
			var vsProvider   = CreateMockProvider(IdeType.VisualStudio, vs, vs);
			var codeProvider = CreateMockProvider(IdeType.VsCode, defaultInstance: null);

			vsProvider.Setup(p => p.GetRecentProjects(false)).Returns(new VsItemList(null) { new VsFolder("VS 2022", string.Empty) });
			codeProvider.Setup(p => p.GetRecentProjects(false)).Returns(new VsItemList(null) { new VsFolder("VS Code", string.Empty) });

			var manager = new IdeManager(new[] { vsProvider.Object, codeProvider.Object });

			manager.GetProvider(IdeType.VisualStudio).ShouldBeSameAs(vsProvider.Object);
			manager.GetInstances(IdeType.VisualStudio).Count.ShouldBe(1);
			manager.GetInstances(IdeType.VsCode).Count.ShouldBe(0);
			manager.HasInstalled(IdeType.VisualStudio).ShouldBeTrue();
			manager.HasInstalled(IdeType.VsCode).ShouldBeFalse();
			manager.AnyInstalled.ShouldBeTrue();
			manager.GetAllRecentProjects(false).Select(i => i.Name).ShouldBe(new[] { "VS 2022", "VS Code" });

			var emptyManager = new IdeManager(new[] { CreateMockProvider(IdeType.VisualStudio, defaultInstance: null).Object });

			emptyManager.AnyInstalled.ShouldBeFalse();
		}

		private static IIdeInstance CreateMockInstance(IdeType ideType, string identifier, string version)
		{
			var mock = new Mock<IIdeInstance>();

			mock.SetupGet(i => i.IdeType).Returns(ideType);
			mock.SetupGet(i => i.Identifier).Returns(identifier);
			mock.SetupGet(i => i.Version).Returns(version);
			mock.SetupGet(i => i.Name).Returns($"{ideType} {version}");
			mock.SetupGet(i => i.ShortName).Returns(identifier);
			mock.SetupGet(i => i.Location).Returns($@"C:\IDEs\{identifier}\ide.exe");
			mock.Setup(i => i.GetProfiles()).Returns(new List<string> { "<default>" });

			return mock.Object;
		}

		private static Mock<IIdeProvider> CreateMockProvider(IdeType ideType, IIdeInstance? defaultInstance, params IIdeInstance[] instances)
		{
			var mock = new Mock<IIdeProvider>();

			mock.SetupGet(p => p.IdeType).Returns(ideType);
			mock.SetupGet(p => p.DisplayName).Returns(ideType.ToString());
			mock.SetupGet(p => p.DownloadUrl).Returns("https://example.com/");
			mock.SetupGet(p => p.Instances).Returns(instances);
			mock.SetupGet(p => p.IsInstalled).Returns(instances.Length > 0);
			mock.SetupGet(p => p.DefaultInstance).Returns(defaultInstance);
			mock.Setup(p => p.FindByIdentifier(It.IsAny<string?>())).Returns<string?>(id =>
				string.IsNullOrWhiteSpace(id)
					? null
					: instances.FirstOrDefault(i => i.Identifier.Equals(id, StringComparison.OrdinalIgnoreCase)));
			mock.Setup(p => p.FindByVersion(It.IsAny<string?>())).Returns<string?>(ver =>
				string.IsNullOrWhiteSpace(ver)
					? null
					: instances.FirstOrDefault(i => i.Version.StartsWith(ver, StringComparison.OrdinalIgnoreCase)));
			mock.Setup(p => p.GetRecentProjects(It.IsAny<bool>())).Returns(new VsItemList(null));

			return mock;
		}
	}
}
