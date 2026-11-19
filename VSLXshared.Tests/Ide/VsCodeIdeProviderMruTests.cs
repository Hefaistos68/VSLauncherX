using Microsoft.Data.Sqlite;

using Moq;

using Newtonsoft.Json;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for VS Code profile discovery (<c>storage.json</c>) and MRU reading (<c>state.vscdb</c>).
	/// </summary>
	[TestFixture]
	public class VsCodeIdeProviderMruTests
	{
		private string tempDir = string.Empty;

		/// <summary>
		/// Creates an isolated temporary directory before each test.
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			tempDir = Path.Combine(Path.GetTempPath(), "VSLXsharedVsCodeMru_" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(tempDir);
		}

		/// <summary>
		/// Clears SQLite pools and deletes the temporary directory after each test.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			SqliteConnection.ClearAllPools();

			if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
			{
				Directory.Delete(tempDir, recursive: true);
			}
		}

		/// <summary>
		/// Verifies that valid <c>storage.json</c> <c>userDataProfiles</c> entries return <c>&lt;default&gt;</c> followed by distinct profile names.
		/// </summary>
		[Test]
		public void GetProfiles_WhenStorageJsonValid_ReturnsDefaultAndDistinctProfileNames()
		{
			string userDataDir = Path.Combine(tempDir, "Code");
			string storagePath = Path.Combine(userDataDir, "User", "globalStorage", "storage.json");

			Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
			File.WriteAllText(
				storagePath,
				"""
				{
					"userDataProfiles": [
						{ "name": "Work", "location": "111" },
						{ "name": "OSS", "location": "222" },
						{ "name": "work", "location": "333" },
						{ "name": "", "location": "444" }
					]
				}
				""");

			var instance = new VsCodeInstance(
				"Visual Studio Code",
				"1.95.0",
				@"C:\VSCode\Code.exe",
				"vscode-stable-user",
				userDataDir,
				isInsiders: false);

			List<string> profiles = instance.GetProfiles();

			profiles.ShouldBe(new[] { "<default>", "Work", "OSS" });
		}

		/// <summary>
		/// Verifies that missing or malformed <c>storage.json</c> returns only <c>&lt;default&gt;</c> without throwing.
		/// </summary>
		[Test]
		public void GetProfiles_WhenStorageJsonMissingOrCorrupt_ReturnsDefaultOnly()
		{
			string missingUserData = Path.Combine(tempDir, "MissingCode");
			var missingInstance    = new VsCodeInstance("VS Code", "1.95.0", @"C:\VSCode\Code.exe", "vscode-stable-user", missingUserData, false);

			missingInstance.GetProfiles().ShouldBe(new[] { "<default>" });

			string corruptUserData = Path.Combine(tempDir, "CorruptCode");
			string storagePath     = Path.Combine(corruptUserData, "User", "globalStorage", "storage.json");

			Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
			File.WriteAllText(storagePath, "{not valid json");

			var corruptInstance = new VsCodeInstance("VS Code", "1.95.0", @"C:\VSCode\Code.exe", "vscode-stable-user", corruptUserData, false);

			corruptInstance.GetProfiles().ShouldBe(new[] { "<default>" });
		}

		/// <summary>
		/// Verifies decoding of Windows <c>file://</c> workspace/folder URIs, manifest refinement, existence warnings, and skipping of remote/file/malformed entries.
		/// </summary>
		[Test]
		public void GetRecentProjects_ReadsStateVscdb_DecodesLocalEntriesAndSkipsUnsupportedOnes()
		{
			string appData     = Path.Combine(tempDir, "AppData");
			string exePath     = Path.Combine(tempDir, "Programs", "Code.exe");
			string userDataDir = Path.Combine(appData, "Code");
			string dbPath      = Path.Combine(userDataDir, "User", "globalStorage", "state.vscdb");

			string existingWsFile  = Path.Combine(tempDir, "Projects", "live.code-workspace");
			string existingFolder  = Path.Combine(tempDir, "Projects", "web-app");
			string plainFolder     = Path.Combine(tempDir, "Projects", "plain-notes");

			Directory.CreateDirectory(Path.GetDirectoryName(existingWsFile)!);
			File.WriteAllText(existingWsFile, "{}");
			Directory.CreateDirectory(existingFolder);
			File.WriteAllText(Path.Combine(existingFolder, "package.json"), "{}");
			Directory.CreateDirectory(plainFolder);

			string existingWsUri     = new Uri(existingWsFile).AbsoluteUri;
			string existingFolderUri = new Uri(existingFolder).AbsoluteUri;
			string plainFolderUri    = new Uri(plainFolder).AbsoluteUri;

			var mruPayload = new
			{
				entries = new object[]
				{
					new { workspace = new { configPath = "file:///c%3A/Users/Me/My%20Apps/demo.code-workspace" } },
					new { workspace = new { configPath = existingWsUri } },
					new { folderUri = existingFolderUri },
					new { folderUri = plainFolderUri },
					new { folderUri = existingFolderUri, remoteAuthority = "wsl+ubuntu" },
					new { folderUri = "vscode-remote://wsl%2Bubuntu/home/me/app" },
					new { folderUri = ":::not-a-valid-uri" },
					new { workspace = "not-an-object" },
					new { fileUri = existingWsUri }
				}
			};

			CreateStateDb(dbPath, JsonConvert.SerializeObject(mruPayload));

			Mock<IVsCodeEnvironment> env = CreateEnvironmentForSingleStable(appData, exePath, Path.Combine(tempDir, "TempCopyRoot"));
			var provider                 = new VsCodeIdeProvider(env.Object);

			VsItemList groups = provider.GetRecentProjects(onlyDefaultProfiles: false);

			VsFolder group = groups.ShouldHaveSingleItem().ShouldBeOfType<VsFolder>();

			group.ItemType.ShouldBe(ItemTypeEnum.VisualStudio);
			group.Path.ShouldBe(string.Empty);
			group.Name.ShouldBe("Visual Studio Code");
			group.Icon.ShouldNotBeNull();
			group.Items.Count.ShouldBe(4);

			var missingWs = group.Items[0].ShouldBeOfType<VsCodeProject>();

			missingWs.ProjectKind.ShouldBe(VsCodeProjectKind.Workspace);
			missingWs.Name.ShouldBe("demo");
			missingWs.Path.ShouldBe(@"C:\Users\Me\My Apps\demo.code-workspace");
			missingWs.VsVersion.ShouldBe("vscode-stable-user");
			missingWs.Warning.ShouldBeTrue();

			var existingWs = group.Items[1].ShouldBeOfType<VsCodeProject>();

			existingWs.ProjectKind.ShouldBe(VsCodeProjectKind.Workspace);
			existingWs.Name.ShouldBe("live");
			existingWs.Warning.ShouldBeFalse();

			var nodeProj = group.Items[2].ShouldBeOfType<VsCodeProject>();

			nodeProj.ProjectKind.ShouldBe(VsCodeProjectKind.NodeJs);
			nodeProj.ManifestFile.ShouldBe("package.json");
			nodeProj.Name.ShouldBe("web-app");
			nodeProj.Warning.ShouldBeFalse();

			var plainProj = group.Items[3].ShouldBeOfType<VsCodeProject>();

			plainProj.ProjectKind.ShouldBe(VsCodeProjectKind.Folder);
			plainProj.Name.ShouldBe("plain-notes");
			plainProj.Warning.ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that missing <c>state.vscdb</c> or databases yielding zero valid local entries omit the edition group.
		/// </summary>
		[Test]
		public void GetRecentProjects_WhenStateDbMissingOrHasNoValidLocalEntries_ReturnsEmptyList()
		{
			string appData     = Path.Combine(tempDir, "AppData");
			string exePath     = Path.Combine(tempDir, "Programs", "Code.exe");
			string userDataDir = Path.Combine(appData, "Code");

			Mock<IVsCodeEnvironment> env = CreateEnvironmentForSingleStable(appData, exePath, Path.Combine(tempDir, "TempCopyRoot"));
			var provider                 = new VsCodeIdeProvider(env.Object);

			provider.GetRecentProjects(false).ShouldBeEmpty();

			string dbPath = Path.Combine(userDataDir, "User", "globalStorage", "state.vscdb");

			CreateStateDb(dbPath, "{\"entries\":[{\"folderUri\":\"vscode-remote://ssh-remote+host/src\"}]}");

			provider.GetRecentProjects(false).ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that when <c>state.vscdb</c> is exclusively locked by SQLite, reading falls back to a temporary copy and removes the temp directory afterward.
		/// </summary>
		[Test]
		public void GetRecentProjects_WhenStateDbLocked_FallsBackToTempCopyAndDeletesTempDirectory()
		{
			string appData      = Path.Combine(tempDir, "AppData");
			string exePath      = Path.Combine(tempDir, "Programs", "Code.exe");
			string userDataDir  = Path.Combine(appData, "Code");
			string dbPath       = Path.Combine(userDataDir, "User", "globalStorage", "state.vscdb");
			string tempCopyRoot = Path.Combine(tempDir, "TempCopyRoot");
			string projDir      = Path.Combine(tempDir, "LockedProj");

			Directory.CreateDirectory(tempCopyRoot);
			Directory.CreateDirectory(projDir);
			File.WriteAllText(Path.Combine(projDir, "Cargo.toml"), "[package]");

			string payload = JsonConvert.SerializeObject(new
			{
				entries = new[] { new { folderUri = new Uri(projDir).AbsoluteUri } }
			});

			CreateStateDb(dbPath, payload);

			Mock<IVsCodeEnvironment> env = CreateEnvironmentForSingleStable(appData, exePath, tempCopyRoot);
			var provider                 = new VsCodeIdeProvider(env.Object);

			string lockConnStr = new SqliteConnectionStringBuilder
			{
				DataSource = dbPath,
				Pooling    = false
			}.ToString();

			using (var lockConnection = new SqliteConnection(lockConnStr))
			{
				lockConnection.Open();

				using var lockCmd = lockConnection.CreateCommand();
				lockCmd.CommandText = "BEGIN EXCLUSIVE;";
				lockCmd.ExecuteNonQuery();

				VsItemList groups = provider.GetRecentProjects(false);

				VsFolder group      = groups.ShouldHaveSingleItem().ShouldBeOfType<VsFolder>();
				VsCodeProject entry = group.Items.ShouldHaveSingleItem().ShouldBeOfType<VsCodeProject>();

				entry.ProjectKind.ShouldBe(VsCodeProjectKind.Rust);
				entry.ManifestFile.ShouldBe("Cargo.toml");
				entry.Warning.ShouldBeFalse();
			}

			env.Verify(e => e.GetTempPath(), Times.Once);
			Directory.GetFileSystemEntries(tempCopyRoot).ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that when <c>state.vscdb</c> uses WAL mode with uncheckpointed <c>-wal</c>/<c>-shm</c> sidecars and is exclusively locked, temp-copy fallback still reads the latest MRU entries and cleans up.
		/// </summary>
		[Test]
		public void GetRecentProjects_WhenWalModeStateDbLockedWithUncheckpointedChanges_ReadsViaTempCopyWithSidecarsAndCleansUp()
		{
			string appData      = Path.Combine(tempDir, "AppData");
			string exePath      = Path.Combine(tempDir, "Programs", "Code.exe");
			string userDataDir  = Path.Combine(appData, "Code");
			string dbPath       = Path.Combine(userDataDir, "User", "globalStorage", "state.vscdb");
			string tempCopyRoot = Path.Combine(tempDir, "TempCopyRoot");
			string projDir      = Path.Combine(tempDir, "WalProj");

			Directory.CreateDirectory(tempCopyRoot);
			Directory.CreateDirectory(projDir);
			File.WriteAllText(Path.Combine(projDir, "go.mod"), "module example.com/wal");

			CreateStateDb(dbPath, "{\"entries\":[]}");

			string walConnStr = new SqliteConnectionStringBuilder
			{
				DataSource = dbPath,
				Pooling    = false
			}.ToString();

			string updatedPayload = JsonConvert.SerializeObject(new
			{
				entries = new[] { new { folderUri = new Uri(projDir).AbsoluteUri } }
			});

			Mock<IVsCodeEnvironment> env = CreateEnvironmentForSingleStable(appData, exePath, tempCopyRoot);
			var provider                 = new VsCodeIdeProvider(env.Object);

			using (var walWriter = new SqliteConnection(walConnStr))
			{
				walWriter.Open();

				using (SqliteCommand modeCmd = walWriter.CreateCommand())
				{
					modeCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA locking_mode=EXCLUSIVE;";
					modeCmd.ExecuteNonQuery();
				}

				using (SqliteCommand updateCmd = walWriter.CreateCommand())
				{
					updateCmd.CommandText = "UPDATE ItemTable SET value = $val WHERE key = 'history.recentlyOpenedPathsList';";
					updateCmd.Parameters.AddWithValue("$val", updatedPayload);
					updateCmd.ExecuteNonQuery();
				}

				VsItemList groups   = provider.GetRecentProjects(false);
				VsFolder group      = groups.ShouldHaveSingleItem().ShouldBeOfType<VsFolder>();
				VsCodeProject entry = group.Items.ShouldHaveSingleItem().ShouldBeOfType<VsCodeProject>();

				entry.ProjectKind.ShouldBe(VsCodeProjectKind.Go);
				entry.ManifestFile.ShouldBe("go.mod");
				entry.Warning.ShouldBeFalse();
			}

			env.Verify(e => e.GetTempPath(), Times.Once);
			Directory.GetFileSystemEntries(tempCopyRoot).ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that when <c>state.vscdb-shm</c> holds an active byte-range lock during WAL fallback, reading still recovers MRU entries from the copied DB and <c>-wal</c> and cleans up.
		/// </summary>
		[Test]
		public void GetRecentProjects_WhenWalShmSidecarHasByteRangeLock_RecoversFromTempCopyAndCleansUp()
		{
			string appData      = Path.Combine(tempDir, "AppData");
			string exePath      = Path.Combine(tempDir, "Programs", "Code.exe");
			string userDataDir  = Path.Combine(appData, "Code");
			string dbPath       = Path.Combine(userDataDir, "User", "globalStorage", "state.vscdb");
			string shmPath      = dbPath + "-shm";
			string tempCopyRoot = Path.Combine(tempDir, "TempCopyRoot");
			string projDir      = Path.Combine(tempDir, "WalShmProj");

			Directory.CreateDirectory(tempCopyRoot);
			Directory.CreateDirectory(projDir);
			File.WriteAllText(Path.Combine(projDir, "Cargo.toml"), "[package]");

			CreateStateDb(dbPath, "{\"entries\":[]}");

			string walConnStr = new SqliteConnectionStringBuilder
			{
				DataSource = dbPath,
				Pooling    = false
			}.ToString();

			string updatedPayload = JsonConvert.SerializeObject(new
			{
				entries = new[] { new { folderUri = new Uri(projDir).AbsoluteUri } }
			});

			Mock<IVsCodeEnvironment> env = CreateEnvironmentForSingleStable(appData, exePath, tempCopyRoot);
			var provider                 = new VsCodeIdeProvider(env.Object);

			using (var walWriter = new SqliteConnection(walConnStr))
			{
				walWriter.Open();

				using (SqliteCommand modeCmd = walWriter.CreateCommand())
				{
					modeCmd.CommandText = "PRAGMA journal_mode=WAL;";
					modeCmd.ExecuteNonQuery();
				}

				using (SqliteCommand updateCmd = walWriter.CreateCommand())
				{
					updateCmd.CommandText = "UPDATE ItemTable SET value = $val WHERE key = 'history.recentlyOpenedPathsList';";
					updateCmd.Parameters.AddWithValue("$val", updatedPayload);
					updateCmd.ExecuteNonQuery();
				}

				using (var shmLock = new FileStream(shmPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
				{
					shmLock.Lock(120, 8);

					try
					{
						VsItemList groups   = provider.GetRecentProjects(false);
						VsFolder group      = groups.ShouldHaveSingleItem().ShouldBeOfType<VsFolder>();
						VsCodeProject entry = group.Items.ShouldHaveSingleItem().ShouldBeOfType<VsCodeProject>();

						entry.ProjectKind.ShouldBe(VsCodeProjectKind.Rust);
						entry.ManifestFile.ShouldBe("Cargo.toml");
						entry.Warning.ShouldBeFalse();
					}
					finally
					{
						shmLock.Unlock(120, 8);
					}
				}
			}

			env.Verify(e => e.GetTempPath(), Times.Once);
			Directory.GetFileSystemEntries(tempCopyRoot).ShouldBeEmpty();
		}

		/// <summary>
		/// Verifies that User and System installs of the same edition sharing one <c>UserDataDirectory</c> do not produce duplicate MRU groups.
		/// </summary>
		[Test]
		public void GetRecentProjects_WhenUserAndSystemShareUserDataDirectory_DoesNotDuplicateGroup()
		{
			string appData      = Path.Combine(tempDir, "AppData");
			string localAppData = Path.Combine(tempDir, "LocalAppData");
			string programFiles = Path.Combine(tempDir, "ProgramFiles");
			string userExe      = Path.Combine(localAppData, "Programs", "Microsoft VS Code", "Code.exe");
			string systemExe    = Path.Combine(programFiles, "Microsoft VS Code", "Code.exe");
			string dbPath       = Path.Combine(appData, "Code", "User", "globalStorage", "state.vscdb");
			string projDir      = Path.Combine(tempDir, "Repo");

			Directory.CreateDirectory(projDir);
			CreateStateDb(dbPath, JsonConvert.SerializeObject(new { entries = new[] { new { folderUri = new Uri(projDir).AbsoluteUri } } }));

			var env = new Mock<IVsCodeEnvironment>();

			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Returns(localAppData);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(appData);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(programFiles);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(Path.Combine(tempDir, "ProgramFiles86"));
			env.Setup(e => e.GetUninstallEntries()).Returns(Array.Empty<VsCodeUninstallEntry>());
			env.Setup(e => e.GetTempPath()).Returns(Path.Combine(tempDir, "TempCopyRoot"));
			env.Setup(e => e.FileExists(It.Is<string?>(p => p == userExe || p == systemExe || p == dbPath))).Returns(true);
			env.Setup(e => e.GetFileVersion(It.IsAny<string>())).Returns("1.95.0");

			var provider = new VsCodeIdeProvider(env.Object);

			provider.Instances.Count.ShouldBe(2);
			provider.GetRecentProjects(false).Count.ShouldBe(1);
		}

		/// <summary>
		/// Creates a mock <see cref="IVsCodeEnvironment"/> exposing a single User Stable VS Code instance backed by <paramref name="appData"/>.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateEnvironmentForSingleStable(string appData, string exePath, string tempCopyRoot)
		{
			string localAppData = Directory.GetParent(Path.GetDirectoryName(exePath)!)!.FullName;
			string standardExe  = Path.Combine(localAppData, "Programs", "Microsoft VS Code", "Code.exe");

			var env = new Mock<IVsCodeEnvironment>();

			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).Returns(localAppData);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(appData);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(Path.Combine(localAppData, "PF"));
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(Path.Combine(localAppData, "PF86"));
			env.Setup(e => e.GetUninstallEntries()).Returns(Array.Empty<VsCodeUninstallEntry>());
			env.Setup(e => e.GetTempPath()).Returns(tempCopyRoot);
			env.Setup(e => e.FileExists(standardExe)).Returns(true);
			env.Setup(e => e.FileExists(It.Is<string?>(p => p != standardExe && p != null && File.Exists(p)))).Returns<string?>(p => File.Exists(p));
			env.Setup(e => e.GetFileVersion(standardExe)).Returns("1.95.0");

			return env;
		}

		/// <summary>
		/// Creates a real SQLite <c>state.vscdb</c> file containing the specified JSON MRU payload.
		/// </summary>
		private static void CreateStateDb(string dbPath, string jsonValue)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

			string connStr = new SqliteConnectionStringBuilder
			{
				DataSource = dbPath,
				Pooling    = false
			}.ToString();

			using var conn = new SqliteConnection(connStr);
			conn.Open();

			using var createCmd = conn.CreateCommand();
			createCmd.CommandText = "CREATE TABLE ItemTable (key TEXT UNIQUE ON CONFLICT REPLACE, value BLOB);";
			createCmd.ExecuteNonQuery();

			using var insertCmd = conn.CreateCommand();
			insertCmd.CommandText = "INSERT INTO ItemTable (key, value) VALUES ('history.recentlyOpenedPathsList', $val);";
			insertCmd.Parameters.AddWithValue("$val", jsonValue);
			insertCmd.ExecuteNonQuery();

			SqliteConnection.ClearPool(conn);
		}
	}
}
