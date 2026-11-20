using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="CustomVsCodeIdeStore"/> covering JSON persistence, deduplication, removal, and executable inspection.
	/// </summary>
	[TestFixture]
	public class CustomVsCodeIdeStoreTests
	{
		private const string RoamingAppData = @"C:\Users\Dev\AppData\Roaming";

		/// <summary>
		/// Verifies that <see cref="CustomVsCodeIdeStore.Load"/> returns an empty list without throwing when the store file is missing or contains invalid JSON.
		/// </summary>
		[Test]
		public void Load_WhenFileMissingOrCorrupt_ReturnsEmptyList()
		{
			var missingEnv = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			missingEnv.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(RoamingAppData);
			missingEnv.Setup(e => e.FileExists(It.IsAny<string?>())).Returns(false);

			var missingStore = new CustomVsCodeIdeStore(missingEnv.Object);

			missingStore.Load().ShouldBeEmpty();
			missingEnv.Verify(e => e.ReadAllText(It.IsAny<string>()), Times.Never);

			string expectedDefaultPath = Path.Combine(RoamingAppData, "VSLauncher", "vscode-custom-ides.json");
			var corruptEnv             = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			corruptEnv.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(RoamingAppData);
			corruptEnv.Setup(e => e.FileExists(expectedDefaultPath)).Returns(true);
			corruptEnv.Setup(e => e.ReadAllText(expectedDefaultPath)).Returns("{ not valid json !!!");

			var corruptStore = new CustomVsCodeIdeStore(corruptEnv.Object);

			corruptStore.Load().ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that <see cref="CustomVsCodeIdeStore.Save"/>, <see cref="CustomVsCodeIdeStore.TryAdd"/>, and <see cref="CustomVsCodeIdeStore.Load"/>
		/// round-trip entries across fresh store instances and deduplicate by canonical path.
		/// </summary>
		[Test]
		public void SaveAndLoad_RoundTripsAndDeduplicatesByCanonicalPath()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string storePath = Path.Combine(tempDir, "nested", "vscode-custom-ides.json");

			try
			{
				var writerStore    = new CustomVsCodeIdeStore(storeFilePath: storePath);
				string exeDir      = Path.Combine(tempDir, "Apps", "MyFork");
				string canonical   = Path.Combine(exeDir, "MyFork.exe");
				string uncanonical = Path.Combine(exeDir, "sub", "..", "myfork.exe");

				var firstEntry = new CustomVsCodeIdeEntry
				{
					ExecutablePath    = canonical,
					DisplayName       = "My Fork IDE",
					UserDataDirectory = Path.Combine(RoamingAppData, "My Fork")
				};
				var duplicateEntry = new CustomVsCodeIdeEntry
				{
					ExecutablePath    = uncanonical,
					DisplayName       = "Duplicate Fork",
					UserDataDirectory = Path.Combine(RoamingAppData, "Duplicate")
				};

				writerStore.Save(new[] { firstEntry, duplicateEntry });

				var readerStore                           = new CustomVsCodeIdeStore(storeFilePath: storePath);
				IReadOnlyList<CustomVsCodeIdeEntry> saved = readerStore.Load();

				saved.Count.ShouldBe(1);
				saved[0].ExecutablePath.ShouldBe(canonical);
				saved[0].DisplayName.ShouldBe("My Fork IDE");
				saved[0].UserDataDirectory.ShouldBe(Path.Combine(RoamingAppData, "My Fork"));

				readerStore.TryAdd(duplicateEntry).ShouldBeFalse();

				string secondExe = Path.Combine(tempDir, "Apps", "OtherFork", "OtherFork.exe");

				readerStore.TryAdd(new CustomVsCodeIdeEntry
				{
					ExecutablePath = secondExe,
					DisplayName    = "Other Fork"
				}).ShouldBeTrue();

				var secondReaderStore = new CustomVsCodeIdeStore(storeFilePath: storePath);

				secondReaderStore.Load().Count.ShouldBe(2);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="CustomVsCodeIdeStore.RemoveByPath"/> removes a matching entry case-insensitively and persists the updated list.
		/// </summary>
		[Test]
		public void RemoveByPath_RemovesMatchingEntryCaseInsensitively()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string storePath = Path.Combine(tempDir, "vscode-custom-ides.json");

			try
			{
				string firstExe  = Path.Combine(tempDir, "ForkOne", "ForkOne.exe");
				string secondExe = Path.Combine(tempDir, "ForkTwo", "ForkTwo.exe");
				var store        = new CustomVsCodeIdeStore(storeFilePath: storePath);

				store.Save(new[]
				{
					new CustomVsCodeIdeEntry { ExecutablePath = firstExe, DisplayName = "Fork One" },
					new CustomVsCodeIdeEntry { ExecutablePath = secondExe, DisplayName = "Fork Two" }
				});

				string differentCasePath = Path.Combine(tempDir, "forkone", "sub", "..", "FORKONE.EXE");

				store.RemoveByPath(differentCasePath).ShouldBeTrue();
				store.RemoveByPath(differentCasePath).ShouldBeFalse();

				var reloadedStore                            = new CustomVsCodeIdeStore(storeFilePath: storePath);
				IReadOnlyList<CustomVsCodeIdeEntry> entries  = reloadedStore.Load();

				entries.Count.ShouldBe(1);
				entries[0].ExecutablePath.ShouldBe(secondExe);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="CustomVsCodeIdeStore.TryInspectExecutable"/> returns a verified inspection when both
		/// <c>resources\app\product.json</c> and <c>resources\app\out\cli.js</c> are present.
		/// </summary>
		[Test]
		public void TryInspectExecutable_WithProductJsonAndCliJs_ReturnsVerifiedInspection()
		{
			string installDir      = @"C:\Apps\CustomFork";
			string exePath         = Path.Combine(installDir, "CustomFork.exe");
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(installDir, "resources", "app", "out", "cli.js");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(RoamingAppData);
			env.Setup(e => e.FileExists(exePath)).Returns(true);
			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(cliJsPath)).Returns(true);
			env.Setup(e => e.GetFileVersion(exePath)).Returns("2.4.1");
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns("""
				{
					"nameShort": "Custom Fork",
					"nameLong": "Custom Fork Studio",
					"win32DirName": "CustomForkData"
				}
				""");

			var store                              = new CustomVsCodeIdeStore(env.Object);
			CustomVsCodeIdeInspection? inspection  = store.TryInspectExecutable(exePath);

			inspection.ShouldNotBeNull();
			inspection.ExecutablePath.ShouldBe(exePath);
			inspection.SuggestedDisplayName.ShouldBe("Custom Fork Studio");
			inspection.SuggestedShortName.ShouldBe("Custom Fork");
			inspection.SuggestedUserDataDirectory.ShouldBe(Path.Combine(RoamingAppData, "CustomForkData"));
			inspection.Version.ShouldBe("2.4.1");
			inspection.IsVerifiedVsCodeFork.ShouldBeTrue();
		}

		/// <summary>
		/// Verifies that <see cref="CustomVsCodeIdeStore.TryInspectExecutable"/> returns an unverified inspection falling back
		/// to the executable stem when <c>product.json</c> or <c>cli.js</c> is absent.
		/// </summary>
		[Test]
		public void TryInspectExecutable_WithoutProductJson_ReturnsUnverifiedInspection()
		{
			string installDir = @"C:\Apps\StandaloneEditor";
			string exePath    = Path.Combine(installDir, "StandaloneEditor.exe");
			var env           = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(RoamingAppData);
			env.Setup(e => e.FileExists(exePath)).Returns(true);
			env.Setup(e => e.GetFileVersion(exePath)).Returns("1.0.0");

			var store                             = new CustomVsCodeIdeStore(env.Object);
			CustomVsCodeIdeInspection? inspection = store.TryInspectExecutable(exePath);

			inspection.ShouldNotBeNull();
			inspection.ExecutablePath.ShouldBe(exePath);
			inspection.SuggestedDisplayName.ShouldBe("StandaloneEditor");
			inspection.SuggestedShortName.ShouldBe("StandaloneEditor");
			inspection.Version.ShouldBe("1.0.0");
			inspection.IsVerifiedVsCodeFork.ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that <see cref="CustomVsCodeIdeStore.TryInspectExecutable"/> rejects empty, non-existent, or non-executable paths.
		/// </summary>
		[Test]
		public void TryInspectExecutable_WhenMissingOrNotExe_ReturnsNull()
		{
			var env = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.FileExists(@"C:\Apps\readme.txt")).Returns(true);
			env.Setup(e => e.FileExists(@"C:\Apps\missing.exe")).Returns(false);

			var store = new CustomVsCodeIdeStore(env.Object);

			store.TryInspectExecutable(string.Empty).ShouldBeNull();
			store.TryInspectExecutable(@"C:\Apps\readme.txt").ShouldBeNull();
			store.TryInspectExecutable(@"C:\Apps\missing.exe").ShouldBeNull();
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
