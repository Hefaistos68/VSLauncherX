using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeIdeProvider"/> edition discovery, version detection, and <see cref="IdeManager"/> registration.
	/// </summary>
	[TestFixture]
	public class VsCodeIdeProviderDiscoveryTests
	{
		private const string LocalAppData   = @"C:\Users\Dev\AppData\Local";
		private const string AppData        = @"C:\Users\Dev\AppData\Roaming";
		private const string ProgramFiles   = @"C:\Program Files";
		private const string ProgramFiles86 = @"C:\Program Files (x86)";

		private const string UserStableExe     = @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\Code.exe";
		private const string SystemStableExe   = @"C:\Program Files\Microsoft VS Code\Code.exe";
		private const string SystemStable86Exe = @"C:\Program Files (x86)\Microsoft VS Code\Code.exe";
		private const string UserInsidersExe   = @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code Insiders\Code - Insiders.exe";
		private const string SystemInsidersExe = @"C:\Program Files\Microsoft VS Code Insiders\Code - Insiders.exe";

		/// <summary>
		/// Verifies that all four standard User/System Stable/Insiders installs are discovered in order with scoped names when both scopes exist.
		/// </summary>
		[Test]
		public void Discovery_WhenBothUserAndSystemExistForBothEditions_UsesScopedNamesAndStableIdentifiers()
		{
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, UserStableExe, "1.95.1");
			SetupExecutable(env, SystemStableExe, "1.95.0");
			SetupExecutable(env, UserInsidersExe, "1.96.0-insider");
			SetupExecutable(env, SystemInsidersExe, "1.96.0-insider");

			var provider = new VsCodeIdeProvider(env.Object);

			provider.IsInstalled.ShouldBeTrue();
			provider.Instances.Count.ShouldBe(4);
			provider.Instances.Select(i => i.Identifier).ShouldBe(new[]
			{
				"vscode-stable-user",
				"vscode-stable-system",
				"vscode-insiders-user",
				"vscode-insiders-system"
			});
			provider.Instances.Select(i => i.Name).ShouldBe(new[]
			{
				"VS Code (User)",
				"VS Code (System)",
				"VS Code Insiders (User)",
				"VS Code Insiders (System)"
			});
			provider.Instances.Select(i => i.ShortName).ShouldBe(new[]
			{
				"VS Code",
				"VS Code",
				"VS Code Insiders",
				"VS Code Insiders"
			});

			provider.Instances[0].ShouldBeOfType<VsCodeInstance>().UserDataDirectory.ShouldBe(Path.Combine(AppData, "Code"));
			provider.Instances[0].ShouldBeOfType<VsCodeInstance>().IsInsiders.ShouldBeFalse();
			provider.Instances[2].ShouldBeOfType<VsCodeInstance>().UserDataDirectory.ShouldBe(Path.Combine(AppData, "Code - Insiders"));
			provider.Instances[2].ShouldBeOfType<VsCodeInstance>().IsInsiders.ShouldBeTrue();
			provider.DefaultInstance.ShouldBeSameAs(provider.Instances[0]);
		}

		/// <summary>
		/// Verifies that single-scope installations per edition receive unscoped display names.
		/// </summary>
		[Test]
		public void Discovery_WhenOnlyOneScopeExistsPerEdition_UsesUnscopedDisplayNames()
		{
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, UserStableExe, "1.95.1");
			SetupExecutable(env, SystemInsidersExe, "1.96.0-insider");

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances.Select(i => i.Name).ShouldBe(new[]
			{
				"Visual Studio Code",
				"Visual Studio Code - Insiders"
			});
			provider.Instances.Select(i => i.Identifier).ShouldBe(new[]
			{
				"vscode-stable-user",
				"vscode-insiders-system"
			});
		}

		/// <summary>
		/// Verifies that Program Files (x86) System Stable install is discovered when 64-bit Program Files install is absent.
		/// </summary>
		[Test]
		public void Discovery_WhenSystemStableIsInProgramFilesX86_DetectsSystemStable()
		{
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, SystemStable86Exe, "1.94.2");

			var provider = new VsCodeIdeProvider(env.Object);

			IIdeInstance instance = provider.Instances.ShouldHaveSingleItem();

			instance.Identifier.ShouldBe("vscode-stable-system");
			instance.Name.ShouldBe("Visual Studio Code");
			instance.Location.ShouldBe(SystemStable86Exe);
		}

		/// <summary>
		/// Verifies that duplicate canonical executable paths from Registry Uninstall and PATH fallbacks are deduplicated.
		/// </summary>
		[Test]
		public void Discovery_DeduplicatesIdenticalCanonicalPathsAcrossStandardRegistryAndPath()
		{
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, UserStableExe, "1.95.1");

			env.Setup(e => e.GetUninstallEntries()).Returns(new[]
			{
				new VsCodeUninstallEntry("Microsoft Visual Studio Code (User)", @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\")
			});
			env.Setup(e => e.GetEnvironmentVariable("PATH")).Returns(@"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\bin");
			env.Setup(e => e.FileExists(@"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\bin\code.cmd")).Returns(true);

			var provider = new VsCodeIdeProvider(env.Object);

			IIdeInstance instance = provider.Instances.ShouldHaveSingleItem();

			instance.Identifier.ShouldBe("vscode-stable-user");
		}

		/// <summary>
		/// Verifies Registry Uninstall fallback discovers custom VS Code and Insiders paths while ignoring unrelated entries.
		/// </summary>
		[Test]
		public void Discovery_ViaRegistryUninstallFallback_FindsCustomLocationsAndIgnoresNonVsCodeEntries()
		{
			const string customStableExe   = @"D:\Apps\VSCode\Code.exe";
			const string customInsidersExe = @"D:\Apps\VSCodeInsiders\Code - Insiders.exe";

			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, customStableExe, "1.95.0");
			SetupExecutable(env, customInsidersExe, "1.96.0-insider");

			env.Setup(e => e.GetUninstallEntries()).Returns(new[]
			{
				new VsCodeUninstallEntry("Unrelated Editor", @"D:\Apps\VSCode"),
				new VsCodeUninstallEntry("Microsoft Visual Studio Code", @"D:\Apps\VSCode"),
				new VsCodeUninstallEntry("Microsoft Visual Studio Code Insiders", @"D:\Apps\VSCodeInsiders")
			});

			var providerFirst  = new VsCodeIdeProvider(env.Object);
			var providerSecond = new VsCodeIdeProvider(env.Object);

			providerFirst.Instances.Count.ShouldBe(2);
			providerFirst.Instances[0].Location.ShouldBe(customStableExe);
			providerFirst.Instances[0].Identifier.ShouldStartWith("vscode-custom-");
			providerFirst.Instances[0].Identifier.ShouldBe(providerSecond.Instances[0].Identifier);
			providerFirst.Instances[0].Name.ShouldBe("Visual Studio Code");

			providerFirst.Instances[1].Location.ShouldBe(customInsidersExe);
			providerFirst.Instances[1].Identifier.ShouldStartWith("vscode-custom-");
			providerFirst.Instances[1].Identifier.ShouldNotBe(providerFirst.Instances[0].Identifier);
			providerFirst.Instances[1].ShouldBeOfType<VsCodeInstance>().IsInsiders.ShouldBeTrue();
		}

		/// <summary>
		/// Verifies PATH fallback resolves <c>code.cmd</c> and <c>code-insiders.cmd</c> to sibling executables.
		/// </summary>
		[Test]
		public void Discovery_ViaPathFallback_ResolvesSiblingExecutablesFromCmdWrappers()
		{
			const string portableStableExe   = @"E:\Portable\Code\Code.exe";
			const string portableInsidersExe = @"E:\Portable\CodeInsiders\Code - Insiders.exe";

			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, portableStableExe, "1.95.3");
			SetupExecutable(env, portableInsidersExe, "1.96.0-insider");

			env.Setup(e => e.GetEnvironmentVariable("PATH")).Returns("\";E:\\Portable\\Code\\bin\";E:\\Portable\\CodeInsiders\\bin;");
			env.Setup(e => e.FileExists(@"E:\Portable\Code\bin\code.cmd")).Returns(true);
			env.Setup(e => e.FileExists(@"E:\Portable\CodeInsiders\bin\code-insiders.cmd")).Returns(true);

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances.Count.ShouldBe(2);
			provider.Instances[0].Location.ShouldBe(portableStableExe);
			provider.Instances[0].Identifier.ShouldStartWith("vscode-custom-");
			provider.Instances[1].Location.ShouldBe(portableInsidersExe);
			provider.Instances[1].ShouldBeOfType<VsCodeInstance>().IsInsiders.ShouldBeTrue();
		}

		/// <summary>
		/// Verifies version resolution prefers file <c>ProductVersion</c> and falls back to <c>resources\app\package.json</c>.
		/// </summary>
		[Test]
		public void Discovery_WhenProductVersionMissing_FallsBackToPackageJsonVersion()
		{
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, UserStableExe, null);

			string packageJsonPath = Path.Combine(Path.GetDirectoryName(UserStableExe)!, "resources", "app", "package.json");

			env.Setup(e => e.FileExists(packageJsonPath)).Returns(true);
			env.Setup(e => e.ReadAllText(packageJsonPath)).Returns("{\"name\":\"code\",\"version\":\"1.95.4\"}");

			SetupExecutable(env, UserInsidersExe, string.Empty);

			string corruptPkgPath = Path.Combine(Path.GetDirectoryName(UserInsidersExe)!, "resources", "app", "package.json");

			env.Setup(e => e.FileExists(corruptPkgPath)).Returns(true);
			env.Setup(e => e.ReadAllText(corruptPkgPath)).Returns("{corrupt");

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances[0].Version.ShouldBe("1.95.4");
			provider.Instances[1].Version.ShouldBe(string.Empty);
		}

		/// <summary>
		/// Verifies provider metadata, lookup methods, and empty-install defaults.
		/// </summary>
		[Test]
		public void ProviderMetadataAndLookups_ReturnExpectedValues()
		{
			Mock<IVsCodeEnvironment> emptyEnv = CreateBaseEnvironmentMock();
			var emptyProvider                 = new VsCodeIdeProvider(emptyEnv.Object);

			emptyProvider.IdeType.ShouldBe(IdeType.VsCode);
			emptyProvider.DisplayName.ShouldBe("Visual Studio Code");
			emptyProvider.DownloadUrl.ShouldBe("https://code.visualstudio.com/download");
			emptyProvider.IsInstalled.ShouldBeFalse();
			emptyProvider.DefaultInstance.ShouldBeNull();
			emptyProvider.Instances.ShouldBeEmpty();
			emptyProvider.FindByIdentifier("vscode-stable-user").ShouldBeNull();
			emptyProvider.FindByVersion("1.95").ShouldBeNull();

			Mock<IVsCodeEnvironment> populatedEnv = CreateBaseEnvironmentMock();

			SetupExecutable(populatedEnv, UserStableExe, "1.95.1");
			SetupExecutable(populatedEnv, UserInsidersExe, "1.96.0-insider");

			var provider = new VsCodeIdeProvider(populatedEnv.Object);

			provider.FindByIdentifier("VSCODE-INSIDERS-USER").ShouldBeSameAs(provider.Instances[1]);
			provider.FindByIdentifier(" ").ShouldBeNull();
			provider.FindByVersion("1.96").ShouldBeSameAs(provider.Instances[1]);
			provider.FindByVersion(null).ShouldBeNull();
		}

		/// <summary>
		/// Verifies that <see cref="IdeManager"/> parameterless constructor registers <see cref="VsCodeIdeProvider"/> alongside <see cref="VisualStudioIdeProvider"/>.
		/// </summary>
		[Test]
		public void IdeManager_DefaultConstructor_RegistersVsCodeIdeProvider()
		{
			var manager = new IdeManager();

			manager.GetProvider(IdeType.VisualStudio).ShouldBeOfType<VisualStudioIdeProvider>();
			manager.GetProvider(IdeType.VsCode).ShouldBeOfType<VsCodeIdeProvider>();
		}

		/// <summary>
		/// Verifies discovery of known VS Code forks including Antigravity IDE and Cursor with stable identifiers, short names, data directories, and VS Code Stable default preference.
		/// </summary>
		[Test]
		public void Discovery_DetectsKnownForksIncludingAntigravityIdeAndCursor()
		{
			string antigravityDir        = Path.Combine(LocalAppData, "Programs", "Antigravity IDE");
			string antigravityExe        = Path.Combine(antigravityDir, "Antigravity IDE.exe");
			string cursorDir             = Path.Combine(LocalAppData, "Programs", "cursor");
			string cursorExe             = Path.Combine(cursorDir, "Cursor.exe");
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, UserStableExe, "1.95.1");
			SetupExecutable(env, antigravityExe, "2.7.0");
			SetupVsCodeAppMetadata(env, antigravityDir, "Antigravity IDE", "Antigravity IDE", "Antigravity IDE", "https://example.com");
			SetupExecutable(env, cursorExe, "0.49.0");
			SetupVsCodeAppMetadata(env, cursorDir, "Cursor", "Cursor", "Cursor", "https://api2.cursor.sh/updates");

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances.Count.ShouldBe(3);
			provider.Instances.Select(i => i.Identifier).ShouldBe(new[]
			{
				"vscode-stable-user",
				"antigravity-ide-user",
				"cursor-user"
			});
			provider.Instances.Select(i => i.ShortName).ShouldBe(new[]
			{
				"VS Code",
				"Antigravity IDE",
				"Cursor"
			});

			provider.Instances[1].ShouldBeOfType<VsCodeInstance>().UserDataDirectory.ShouldBe(Path.Combine(AppData, "Antigravity IDE"));
			provider.Instances[2].ShouldBeOfType<VsCodeInstance>().UserDataDirectory.ShouldBe(Path.Combine(AppData, "Cursor"));
			provider.DefaultInstance.ShouldBeSameAs(provider.Instances[0]);
		}

		/// <summary>
		/// Verifies that generic directory probing discovers unknown forks having both <c>product.json</c> and <c>cli.js</c> while ignoring non-VS-Code Electron apps and uninstallers.
		/// </summary>
		[Test]
		public void Discovery_GenericAutoDiscovery_DetectsUnknownForkWithProductJsonAndCliJs_AndIgnoresNonVsCodeElectronApps()
		{
			string programsDir           = Path.Combine(LocalAppData, "Programs");
			string customForkDir         = Path.Combine(programsDir, "MyCustomFork");
			string customForkExe         = Path.Combine(customForkDir, "MyCustomFork.exe");
			string customForkUninst      = Path.Combine(customForkDir, "unins000.exe");
			string customForkUninstall   = Path.Combine(customForkDir, "Uninstall MyCustomFork.exe");
			string nonVsCodeDir          = Path.Combine(programsDir, "antigravity");
			string nonVsCodeExe          = Path.Combine(nonVsCodeDir, "Antigravity.exe");
			string nonVsCodeUninst       = Path.Combine(nonVsCodeDir, "Uninstall Antigravity.exe");
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			env.Setup(e => e.GetDirectories(programsDir)).Returns(new[] { nonVsCodeDir, customForkDir });
			env.Setup(e => e.GetFiles(customForkDir, "*.exe")).Returns(new[] { customForkUninst, customForkUninstall, customForkExe });
			env.Setup(e => e.GetFiles(nonVsCodeDir, "*.exe")).Returns(new[] { nonVsCodeExe, nonVsCodeUninst });

			SetupExecutable(env, customForkExe, "1.2.3");
			SetupExecutable(env, customForkUninst, "1.0.0");
			SetupExecutable(env, customForkUninstall, "1.0.0");
			SetupVsCodeAppMetadata(env, customForkDir, "MyCustomFork", "MyCustomFork Studio", "MyCustomFork", null);

			SetupExecutable(env, nonVsCodeExe, "3.0.0");
			SetupExecutable(env, nonVsCodeUninst, "3.0.0");

			var provider = new VsCodeIdeProvider(env.Object);

			IIdeInstance discovered    = provider.Instances.ShouldHaveSingleItem();
			VsCodeInstance vsCodeInst  = discovered.ShouldBeOfType<VsCodeInstance>();

			discovered.Location.ShouldBe(customForkExe);
			discovered.Identifier.ShouldBe("mycustomfork-user");
			discovered.ShortName.ShouldBe("MyCustomFork");
			discovered.Name.ShouldBe("MyCustomFork Studio");
			vsCodeInst.UserDataDirectory.ShouldBe(Path.Combine(AppData, "MyCustomFork"));
			vsCodeInst.IsCustom.ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that custom store entries are loaded with <c>IsCustom = true</c>, deduplicated against auto-discovered installs, and missing executables are skipped without mutating the store.
		/// </summary>
		[Test]
		public void Discovery_LoadsCustomStoreEntries_DeduplicatesAgainstAutoDiscovered_AndSkipsMissingCustomExe()
		{
			string tempDir       = CreateIsolatedTempDirectory();
			string storeFilePath = Path.Combine(tempDir, "vscode-custom-ides.json");
			string portableExe   = @"D:\Tools\PortableCode\Code.exe";
			string portableData  = @"D:\Tools\PortableCode\Data";
			string duplicatePath = Path.Combine(LocalAppData, "Programs", "Microsoft VS Code", "sub", "..", "code.exe");
			string missingExe    = @"Z:\Gone\Fork.exe";

			try
			{
				var store = new CustomVsCodeIdeStore(storeFilePath: storeFilePath);

				store.Save(new[]
				{
					new CustomVsCodeIdeEntry
					{
						ExecutablePath    = portableExe,
						DisplayName       = "Portable Code",
						UserDataDirectory = portableData
					},
					new CustomVsCodeIdeEntry
					{
						ExecutablePath = duplicatePath,
						DisplayName    = "Duplicate VS Code"
					},
					new CustomVsCodeIdeEntry
					{
						ExecutablePath = missingExe,
						DisplayName    = "Gone Fork"
					}
				});

				Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

				SetupExecutable(env, UserStableExe, "1.95.1");
				SetupExecutable(env, portableExe, "1.95.2");

				var providerFirst  = new VsCodeIdeProvider(env.Object, store);
				var providerSecond = new VsCodeIdeProvider(env.Object, store);

				providerFirst.Instances.Count.ShouldBe(2);
				providerFirst.Instances[0].Identifier.ShouldBe("vscode-stable-user");
				providerFirst.Instances[0].ShouldBeOfType<VsCodeInstance>().IsCustom.ShouldBeFalse();

				VsCodeInstance portableInstance = providerFirst.Instances[1].ShouldBeOfType<VsCodeInstance>();

				portableInstance.Location.ShouldBe(portableExe);
				portableInstance.Name.ShouldBe("Portable Code");
				portableInstance.ShortName.ShouldBe("Portable Code");
				portableInstance.UserDataDirectory.ShouldBe(portableData);
				portableInstance.IsCustom.ShouldBeTrue();
				portableInstance.Identifier.ShouldStartWith("vscode-custom-");
				portableInstance.Identifier.ShouldBe(providerSecond.Instances[1].Identifier);

				IReadOnlyList<CustomVsCodeIdeEntry> persistedEntries = new CustomVsCodeIdeStore(storeFilePath: storeFilePath).Load();

				persistedEntries.Count.ShouldBe(3);
				persistedEntries.ShouldContain(e => e.ExecutablePath.Equals(missingExe, StringComparison.OrdinalIgnoreCase));
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeIdeProvider.Refresh"/> reloads discovered instances after custom store changes.
		/// </summary>
		[Test]
		public void Refresh_ReloadsInstancesAfterCustomStoreMutation()
		{
			string tempDir       = CreateIsolatedTempDirectory();
			string storeFilePath = Path.Combine(tempDir, "vscode-custom-ides.json");
			string addedForkExe  = @"D:\Tools\AddedFork\AddedFork.exe";

			try
			{
				var store                    = new CustomVsCodeIdeStore(storeFilePath: storeFilePath);
				Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

				SetupExecutable(env, UserStableExe, "1.95.1");

				var provider = new VsCodeIdeProvider(env.Object, store);

				provider.Instances.Count.ShouldBe(1);

				SetupExecutable(env, addedForkExe, "1.0.0");
				store.TryAdd(new CustomVsCodeIdeEntry
				{
					ExecutablePath = addedForkExe,
					DisplayName    = "Added Fork"
				}).ShouldBeTrue();

				provider.Refresh();

				provider.Instances.Count.ShouldBe(2);
				provider.Instances.ShouldContain(i => i.Location == addedForkExe && i.Name == "Added Fork");
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that generic directory probing requires both <c>product.json</c> and <c>cli.js</c> and ignores directories missing either file.
		/// </summary>
		[Test]
		public void Discovery_GenericAutoDiscovery_WhenOnlyProductJsonOrOnlyCliJsExists_SkipsDirectory()
		{
			string programsDir           = Path.Combine(LocalAppData, "Programs");
			string onlyProductDir        = Path.Combine(programsDir, "OnlyProductJson");
			string onlyProductExe        = Path.Combine(onlyProductDir, "OnlyProductJson.exe");
			string onlyProductJsonPath   = Path.Combine(onlyProductDir, "resources", "app", "product.json");
			string onlyCliDir            = Path.Combine(programsDir, "OnlyCliJs");
			string onlyCliExe            = Path.Combine(onlyCliDir, "OnlyCliJs.exe");
			string onlyCliJsPath         = Path.Combine(onlyCliDir, "resources", "app", "out", "cli.js");
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			env.Setup(e => e.GetDirectories(programsDir)).Returns(new[] { onlyProductDir, onlyCliDir });
			env.Setup(e => e.GetFiles(onlyProductDir, "*.exe")).Returns(new[] { onlyProductExe });
			env.Setup(e => e.GetFiles(onlyCliDir, "*.exe")).Returns(new[] { onlyCliExe });

			SetupExecutable(env, onlyProductExe, "1.0.0");
			env.Setup(e => e.FileExists(onlyProductJsonPath)).Returns(true);
			env.Setup(e => e.ReadAllText(onlyProductJsonPath)).Returns("{\"nameShort\":\"OnlyProductJson\"}");

			SetupExecutable(env, onlyCliExe, "1.0.0");
			env.Setup(e => e.FileExists(onlyCliJsPath)).Returns(true);

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances.ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies per-flavor User/System display-name disambiguation, generic registry fork discovery, and <see cref="VsCodeIdeProvider.DefaultInstance"/> fallback order.
		/// </summary>
		[Test]
		public void Discovery_SupportsForkScopeDisambiguationGenericRegistryForksAndDefaultPrecedence()
		{
			string cursorUserExe         = Path.Combine(LocalAppData, "Programs", "cursor", "Cursor.exe");
			string cursorSystemExe       = Path.Combine(ProgramFiles, "Cursor", "Cursor.exe");
			string regForkDir            = @"D:\Apps\RegFork";
			string regForkExe            = Path.Combine(regForkDir, "RegFork.exe");
			Mock<IVsCodeEnvironment> env = CreateBaseEnvironmentMock();

			SetupExecutable(env, UserInsidersExe, "1.96.0-insider");
			SetupExecutable(env, cursorUserExe, "0.49.0");
			SetupExecutable(env, cursorSystemExe, "0.49.0");
			SetupExecutable(env, regForkExe, "1.1.0");
			SetupVsCodeAppMetadata(env, regForkDir, "RegFork", "RegFork Editor", "RegFork", null);

			env.Setup(e => e.GetFiles(regForkDir, "*.exe")).Returns(new[] { regForkExe });
			env.Setup(e => e.GetUninstallEntries()).Returns(new[]
			{
				new VsCodeUninstallEntry("RegFork Editor", regForkDir)
			});

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances.Count.ShouldBe(4);
			provider.Instances.Select(i => i.Identifier).ShouldBe(new[]
			{
				"vscode-insiders-user",
				"cursor-user",
				"cursor-system"
			}.Concat(new[] { provider.Instances[3].Identifier }));
			provider.Instances[1].Name.ShouldBe("Cursor (User)");
			provider.Instances[2].Name.ShouldBe("Cursor (System)");
			provider.Instances[2].ShouldBeOfType<VsCodeInstance>().IsSystemInstall.ShouldBeTrue();
			provider.Instances[3].Location.ShouldBe(regForkExe);
			provider.Instances[3].Identifier.ShouldStartWith("vscode-custom-");
			provider.Instances[3].ShouldBeOfType<VsCodeInstance>().IsCustom.ShouldBeFalse();
			provider.DefaultInstance!.Identifier.ShouldBe("vscode-insiders-user");
		}

		/// <summary>
		/// Creates a mock <see cref="IVsCodeEnvironment"/> configured with standard Windows folder paths and no installed executables.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateBaseEnvironmentMock()
		{
			var mock = new Mock<IVsCodeEnvironment>();

			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Returns(LocalAppData);
			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(AppData);
			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(ProgramFiles);
			mock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(ProgramFiles86);
			mock.Setup(e => e.GetUninstallEntries()).Returns(Array.Empty<VsCodeUninstallEntry>());
			mock.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns((string?)null);
			mock.Setup(e => e.GetTempPath()).Returns(Path.GetTempPath());

			return mock;
		}

		/// <summary>
		/// Registers an executable file and optional product version on a mock <see cref="IVsCodeEnvironment"/>.
		/// </summary>
		private static void SetupExecutable(Mock<IVsCodeEnvironment> env, string exePath, string? version)
		{
			env.Setup(e => e.FileExists(exePath)).Returns(true);
			env.Setup(e => e.GetFileVersion(exePath)).Returns(version);
		}

		/// <summary>
		/// Registers <c>resources\app\product.json</c> and <c>resources\app\out\cli.js</c> on a mock <see cref="IVsCodeEnvironment"/>.
		/// </summary>
		private static void SetupVsCodeAppMetadata(
			Mock<IVsCodeEnvironment> env,
			string installDir,
			string nameShort,
			string nameLong,
			string win32DirName,
			string? updateUrl)
		{
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(installDir, "resources", "app", "out", "cli.js");
			string updateUrlProp   = updateUrl is null ? string.Empty : $",\"updateUrl\":\"{updateUrl}\"";

			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(cliJsPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns(
				$"{{\"nameShort\":\"{nameShort}\",\"nameLong\":\"{nameLong}\",\"win32DirName\":\"{win32DirName}\"{updateUrlProp}}}");
		}

		/// <summary>
		/// Creates an isolated temporary directory for test file operations.
		/// </summary>
		private static string CreateIsolatedTempDirectory()
		{
			string dir = Path.Combine(Path.GetTempPath(), "VSLauncherX_Tests", Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(dir);

			return dir;
		}

		/// <summary>
		/// Deletes a temporary test directory on a best-effort basis.
		/// </summary>
		private static void TryDeleteDirectory(string path)
		{
			try
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive: true);
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
