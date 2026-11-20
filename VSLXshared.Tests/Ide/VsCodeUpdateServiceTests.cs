using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeUpdateService"/> covering update detection, daily caching, installer verification, and launch arguments.
	/// </summary>
	[TestFixture]
	public class VsCodeUpdateServiceTests
	{
		private const string OldCommit       = "1111111111111111111111111111111111111111";
		private const string NewCommit       = "8a14f0cc1b08d531eb83255ea3764ae22aa932b5";
		private const string UserInstallExe  = @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\Code.exe";
		private const string SystemInsiders  = @"C:\Program Files\Microsoft VS Code Insiders\Code - Insiders.exe";
		private const string TrustedExeUrl   = "https://vscode.download.prss.microsoft.com/dbazure/download/stable/8a14f0cc1b08d531eb83255ea3764ae22aa932b5/VSCodeUserSetup-x64-1.147.0.exe";
		private const string SampleSha256    = "903bee51e0114b8e7383703c038176e21cbd0f4a666a76c07705453fe824aaa9";

		/// <summary>
		/// Verifies that an HTTP 204 response reports no update available and marks the check as succeeded.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenApiReturns204NoContent_ReturnsUpToDate()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.147.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.147.0", false, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				var handler                  = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				handler.RequestUris[0].ToString().ShouldBe($"https://update.code.visualstudio.com/api/update/win32-x64-user/stable/{OldCommit}");
				result.CheckSucceeded.ShouldBeTrue();
				result.IsUpdateAvailable.ShouldBeFalse();
				result.CurrentVersion.ShouldBe("1.147.0");
				result.CurrentCommit.ShouldBe(OldCommit);
				result.IsSystemInstall.ShouldBeFalse();
				result.CheckedAtUtc.ShouldBe(now);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that an HTTP 200 update payload enables the update flag and extracts download URL, version, commit, and SHA-256 hash.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenApiReturns200WithUpdate_ParsesUpdateMetadata()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string payload               = BuildUpdateJson(TrustedExeUrl, "1.147.0", NewCommit, SampleSha256);
				var handler                  = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, payload));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				result.CheckSucceeded.ShouldBeTrue();
				result.IsUpdateAvailable.ShouldBeTrue();
				result.CurrentVersion.ShouldBe("1.146.0");
				result.LatestVersion.ShouldBe("1.147.0");
				result.LatestCommit.ShouldBe(NewCommit);
				result.DownloadUrl.ShouldBe(TrustedExeUrl);
				result.Sha256Hash.ShouldBe(SampleSha256);
				result.IsSystemInstall.ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that a second check within 24 hours reuses the persisted daily cache without making another HTTP call, even across new service instances.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_Within24Hours_UsesPersistedDailyCacheWithoutHttpRequest()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset currentTime   = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				string payload               = BuildUpdateJson(TrustedExeUrl, "1.147.0", NewCommit, SampleSha256);
				var handler                  = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, payload));

				using (var firstService = new VsCodeUpdateService(handler, env.Object, () => currentTime, cachePath, Architecture.X64))
				{
					VsCodeUpdateInfo firstResult = await firstService.CheckForUpdateAsync(instance);

					firstResult.IsUpdateAvailable.ShouldBeTrue();
				}

				currentTime = currentTime.AddHours(23);

				using var secondService = new VsCodeUpdateService(handler, env.Object, () => currentTime, cachePath, Architecture.X64);

				VsCodeUpdateInfo secondResult = await secondService.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				secondResult.CheckSucceeded.ShouldBeTrue();
				secondResult.IsUpdateAvailable.ShouldBeTrue();
				secondResult.LatestVersion.ShouldBe("1.147.0");
				secondResult.DownloadUrl.ShouldBe(TrustedExeUrl);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that checks occurring 24 hours or later after the last check query the API again and refresh the cache.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_After24Hours_RequeriesApi()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset currentTime   = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				int callCount                = 0;
				var handler                  = new StubHttpMessageHandler(_ =>
				{
					callCount++;

					return callCount == 1
						? new HttpResponseMessage(HttpStatusCode.NoContent)
						: CreateJsonResponse(HttpStatusCode.OK, BuildUpdateJson(TrustedExeUrl, "1.147.0", NewCommit, SampleSha256));
				});

				using var service = new VsCodeUpdateService(handler, env.Object, () => currentTime, cachePath, Architecture.X64);

				VsCodeUpdateInfo firstResult = await service.CheckForUpdateAsync(instance);

				firstResult.IsUpdateAvailable.ShouldBeFalse();

				currentTime = currentTime.AddHours(24).AddMinutes(1);

				VsCodeUpdateInfo secondResult = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(2);
				secondResult.IsUpdateAvailable.ShouldBeTrue();
				secondResult.LatestVersion.ShouldBe("1.147.0");
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that setting <c>forceRefresh</c> bypasses the 24-hour cache and issues a fresh API request.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenForceRefreshIsTrue_BypassesDailyCache()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				var handler                  = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				_ = await service.CheckForUpdateAsync(instance);
				_ = await service.CheckForUpdateAsync(instance, forceRefresh: true);

				handler.RequestUris.Count.ShouldBe(2);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that when the local installation commit now matches a cached available update commit, the cached update clears immediately without an HTTP request.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenLocalCommitMatchesCachedLatestCommit_ClearsUpdateWithoutHttpRequest()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				string currentCommit         = OldCommit;
				string currentVersion        = "1.146.0";
				Mock<IVsCodeEnvironment> env = CreateDynamicEnvironmentMock(
					tempDir,
					UserInstallExe,
					() => currentCommit,
					() => "stable",
					() => "user",
					() => currentVersion);
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				string payload               = BuildUpdateJson(TrustedExeUrl, "1.147.0", NewCommit, SampleSha256);
				var handler                  = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, payload));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo firstResult = await service.CheckForUpdateAsync(instance);

				firstResult.IsUpdateAvailable.ShouldBeTrue();

				// Simulate VS Code having been updated locally to the new commit
				currentCommit  = NewCommit;
				currentVersion = "1.147.0";

				VsCodeUpdateInfo secondResult = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				secondResult.CheckSucceeded.ShouldBeTrue();
				secondResult.IsUpdateAvailable.ShouldBeFalse();
				secondResult.CurrentVersion.ShouldBe("1.147.0");
				secondResult.CurrentCommit.ShouldBe(NewCommit);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that ARM64 system Insiders installations target the <c>win32-arm64</c> platform and <c>insider</c> channel.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenArm64SystemInsiders_UsesExpectedPlatformAndQualityUrl()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, SystemInsiders, OldCommit, "insider", "system", "1.148.0-insider");
				VsCodeInstance instance      = CreateInstance(SystemInsiders, "vscode-insiders-system", "1.148.0-insider", true, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				var handler                  = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.Arm64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				handler.RequestUris[0].ToString().ShouldBe($"https://update.code.visualstudio.com/api/update/win32-arm64/insider/{OldCommit}");
				result.IsSystemInstall.ShouldBeTrue();
				result.CheckSucceeded.ShouldBeTrue();
				result.IsUpdateAvailable.ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that missing <c>product.json</c> falls back to the <c>/latest</c> endpoint and compares semantic versions.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenProductJsonMissing_FallsBackToLatestAndComparesVersions()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				var env = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

				env.Setup(e => e.GetTempPath()).Returns(tempDir);
				env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(@"C:\Program Files");
				env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(@"C:\Program Files (x86)");
				env.Setup(e => e.FileExists(It.IsAny<string>())).Returns(false);

				VsCodeInstance instance = CreateInstance(@"C:\Program Files\Microsoft VS Code\Code.exe", "vscode-stable-system", "1.146.0", false, env.Object);
				DateTimeOffset now      = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				string payload          = BuildUpdateJson(TrustedExeUrl, "1.147.0", NewCommit, SampleSha256);
				var handler             = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, payload));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris[0].ToString().ShouldBe("https://update.code.visualstudio.com/api/update/win32-x64/stable/latest");
				result.CheckSucceeded.ShouldBeTrue();
				result.IsUpdateAvailable.ShouldBeTrue();
				result.IsSystemInstall.ShouldBeTrue();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that update payloads pointing to untrusted hosts or non-HTTPS schemes are rejected safely.
		/// </summary>
		[TestCase("http://vscode.download.prss.microsoft.com/VSCodeSetup.exe")]
		[TestCase("https://untrusted.example.com/VSCodeSetup.exe")]
		public async Task CheckForUpdateAsync_WhenDownloadUrlIsUntrusted_RejectsUpdateSafely(string untrustedUrl)
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				string payload               = BuildUpdateJson(untrustedUrl, "1.147.0", NewCommit, SampleSha256);
				var handler                  = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, payload));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				result.CheckSucceeded.ShouldBeFalse();
				result.IsUpdateAvailable.ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that network exceptions during update checks return a non-throwing failed status without poisoning the cache.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenHttpRequestThrows_ReturnsFailedResultWithoutThrowing()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				VsCodeInstance instance      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				DateTimeOffset now           = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
				var handler                  = new StubHttpMessageHandler(_ => throw new HttpRequestException("Offline"));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				result.CheckSucceeded.ShouldBeFalse();
				result.IsUpdateAvailable.ShouldBeFalse();
				File.Exists(cachePath).ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.DownloadInstallerAsync"/> saves the downloaded file when its SHA-256 hash matches.
		/// </summary>
		[Test]
		public async Task DownloadInstallerAsync_WhenSha256Matches_SavesVerifiedInstaller()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				byte[] payloadBytes          = Encoding.UTF8.GetBytes("fake-vscode-installer-binary-content");
				string expectedSha256        = Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant();
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				var handler                  = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(payloadBytes)
				});

				var updateInfo = new VsCodeUpdateInfo
				{
					InstanceIdentifier = "vscode-stable-user",
					IsUpdateAvailable  = true,
					LatestVersion      = "1.147.0",
					LatestCommit       = NewCommit,
					DownloadUrl        = TrustedExeUrl,
					Sha256Hash         = expectedSha256,
					CheckSucceeded     = true
				};

				using var service = new VsCodeUpdateService(handler, env.Object, () => DateTimeOffset.UtcNow, cachePath, Architecture.X64);

				string downloadedPath = await service.DownloadInstallerAsync(updateInfo);

				File.Exists(downloadedPath).ShouldBeTrue();
				Path.GetDirectoryName(downloadedPath).ShouldBe(tempDir);
				Path.GetExtension(downloadedPath).ShouldBe(".exe");
				File.ReadAllBytes(downloadedPath).ShouldBe(payloadBytes);

				VsCodeUpdateService.TryDeleteInstallerFile(downloadedPath);

				File.Exists(downloadedPath).ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.DownloadInstallerAsync"/> throws and removes the partial file when SHA-256 verification fails.
		/// </summary>
		[Test]
		public async Task DownloadInstallerAsync_WhenSha256Mismatches_ThrowsAndDeletesTempFile()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				byte[] payloadBytes          = Encoding.UTF8.GetBytes("tampered-installer-bytes");
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				var handler                  = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(payloadBytes)
				});

				var updateInfo = new VsCodeUpdateInfo
				{
					InstanceIdentifier = "vscode-stable-user",
					IsUpdateAvailable  = true,
					LatestVersion      = "1.147.0",
					LatestCommit       = NewCommit,
					DownloadUrl        = TrustedExeUrl,
					Sha256Hash         = SampleSha256,
					CheckSucceeded     = true
				};

				using var service = new VsCodeUpdateService(handler, env.Object, () => DateTimeOffset.UtcNow, cachePath, Architecture.X64);

				await Should.ThrowAsync<InvalidDataException>(() => service.DownloadInstallerAsync(updateInfo));

				Directory.GetFiles(tempDir, "*.exe").ShouldBeEmpty();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.DownloadInstallerAsync"/> refuses untrusted download URLs before issuing any network request.
		/// </summary>
		[Test]
		public async Task DownloadInstallerAsync_WhenUrlIsUntrusted_ThrowsInvalidOperationException()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, UserInstallExe, OldCommit, "stable", "user", "1.146.0");
				var handler                  = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

				var updateInfo = new VsCodeUpdateInfo
				{
					InstanceIdentifier = "vscode-stable-user",
					IsUpdateAvailable  = true,
					DownloadUrl        = "https://evil.example.com/setup.exe",
					CheckSucceeded     = true
				};

				using var service = new VsCodeUpdateService(handler, env.Object, () => DateTimeOffset.UtcNow, cachePath, Architecture.X64);

				await Should.ThrowAsync<InvalidOperationException>(() => service.DownloadInstallerAsync(updateInfo));

				handler.RequestUris.ShouldBeEmpty();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.BuildInstallerStartInfo"/> configures silent Inno Setup flags and sets UAC elevation only for System installs.
		/// </summary>
		[TestCase(false, "")]
		[TestCase(true, "runas")]
		public void BuildInstallerStartInfo_ConfiguresSilentFlagsAndScopeElevation(bool isSystemInstall, string expectedVerb)
		{
			ProcessStartInfo startInfo = VsCodeUpdateService.BuildInstallerStartInfo(@"C:\Temp\VSCodeUpdate.exe", isSystemInstall);

			startInfo.FileName.ShouldBe(@"C:\Temp\VSCodeUpdate.exe");
			startInfo.UseShellExecute.ShouldBeTrue();
			startInfo.Verb.ShouldBe(expectedVerb);
			startInfo.Arguments.ShouldContain("/SILENT");
			startInfo.Arguments.ShouldContain("/NORESTART");
			startInfo.Arguments.ShouldContain("/CLOSEAPPLICATIONS");
			startInfo.Arguments.ShouldContain("/MERGETASKS=!runcode");
		}

		/// <summary>
		/// Verifies numeric version comparisons across multi-digit components and prerelease suffixes.
		/// </summary>
		[TestCase("1.147.0", "1.146.2", 1)]
		[TestCase("1.100.0", "1.99.5", 1)]
		[TestCase("1.147.0-insider", "1.147.0", 0)]
		[TestCase("1.146.0", "1.147.0", -1)]
		public void CompareVersions_ReturnsExpectedOrdering(string left, string right, int expectedSign)
		{
			int comparison = VsCodeUpdateService.CompareVersions(left, right);

			Math.Sign(comparison).ShouldBe(expectedSign);
		}

		/// <summary>
		/// Creates a mock <see cref="IVsCodeEnvironment"/> with static <c>product.json</c> and <c>package.json</c> content.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateEnvironmentMock(
			string tempDir,
			string executablePath,
			string commit,
			string quality,
			string target,
			string version)
		{
			return CreateDynamicEnvironmentMock(tempDir, executablePath, () => commit, () => quality, () => target, () => version);
		}

		/// <summary>
		/// Creates a mock <see cref="IVsCodeEnvironment"/> whose <c>product.json</c> and <c>package.json</c> values can change dynamically during a test.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateDynamicEnvironmentMock(
			string tempDir,
			string executablePath,
			Func<string> commitProvider,
			Func<string> qualityProvider,
			Func<string> targetProvider,
			Func<string> versionProvider)
		{
			string installDir      = Path.GetDirectoryName(executablePath)!;
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string packageJsonPath = Path.Combine(installDir, "resources", "app", "package.json");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetTempPath()).Returns(tempDir);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(tempDir);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(@"C:\Program Files");
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(@"C:\Program Files (x86)");
			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(packageJsonPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns(() =>
				$"{{\"commit\":\"{commitProvider()}\",\"quality\":\"{qualityProvider()}\",\"target\":\"{targetProvider()}\"}}");
			env.Setup(e => e.ReadAllText(packageJsonPath)).Returns(() =>
				$"{{\"version\":\"{versionProvider()}\"}}");

			return env;
		}

		/// <summary>
		/// Creates a <see cref="VsCodeInstance"/> configured for unit tests.
		/// </summary>
		private static VsCodeInstance CreateInstance(
			string executablePath,
			string identifier,
			string version,
			bool isInsiders,
			IVsCodeEnvironment environment)
		{
			string displayName = isInsiders ? "Visual Studio Code - Insiders" : "Visual Studio Code";

			return new VsCodeInstance(displayName, version, executablePath, identifier, @"C:\Users\Dev\AppData\Roaming\Code", isInsiders, environment);
		}

		/// <summary>
		/// Builds a JSON payload matching Microsoft's VS Code update API schema.
		/// </summary>
		private static string BuildUpdateJson(string url, string productVersion, string commit, string sha256)
		{
			return $"{{\"url\":\"{url}\",\"name\":\"{productVersion}\",\"version\":\"{commit}\",\"productVersion\":\"{productVersion}\",\"sha256hash\":\"{sha256}\"}}";
		}

		/// <summary>
		/// Creates an <see cref="HttpResponseMessage"/> carrying UTF-8 JSON content.
		/// </summary>
		private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
		{
			return new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
		}

		/// <summary>
		/// Creates a unique temporary test directory.
		/// </summary>
		private static string CreateIsolatedTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "VSLX_UpdateTests_" + Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(path);

			return path;
		}

		/// <summary>
		/// Removes a temporary test directory if it exists.
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
			catch
			{
				// Ignore cleanup races in test teardown.
			}
		}

		/// <summary>
		/// Test HTTP message handler that records outgoing request URIs and delegates responses to a supplied callback.
		/// </summary>
		private sealed class StubHttpMessageHandler : HttpMessageHandler
		{
			private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

			/// <summary>
			/// Initializes a new instance of the <see cref="StubHttpMessageHandler"/> class.
			/// </summary>
			/// <param name="responder">Callback producing responses for incoming requests.</param>
			internal StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
			{
				this.responder = responder;
			}

			/// <summary>
			/// Gets the ordered list of request URIs received by this handler.
			/// </summary>
			internal List<Uri> RequestUris { get; } = new List<Uri>();

			/// <inheritdoc/>
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				if (request.RequestUri is not null)
				{
					this.RequestUris.Add(request.RequestUri);
				}

				return Task.FromResult(this.responder(request));
			}
		}
	}
}
