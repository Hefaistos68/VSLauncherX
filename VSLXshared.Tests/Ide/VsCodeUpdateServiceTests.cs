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
		/// Verifies that a fork whose <c>product.json</c> specifies a placeholder <c>updateUrl</c> (such as <c>https://example.com</c>)
		/// is classified as <see cref="VsCodeUpdateCheckStatus.Unsupported"/> without issuing any HTTP request.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenProductJsonHasPlaceholderUpdateUrl_ReturnsUnsupportedWithoutHttpCall()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\Antigravity IDE\Antigravity IDE.exe";

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(
					tempDir,
					exePath,
					OldCommit,
					"stable",
					"user",
					"1.0.0",
					"https://example.com");
				VsCodeProductDescriptor baseDescriptor = GetKnownDescriptor("antigravity-ide");
				VsCodeProductDescriptor apiDescriptor  = baseDescriptor with
				{
					UpdateStrategy = VsCodeUpdateStrategyKind.VsCodeApi
				};
				var instance = new VsCodeInstance("Antigravity IDE", "1.0.0", exePath, "antigravity-ide-user", @"C:\Users\Dev\AppData\Roaming\Antigravity IDE", apiDescriptor, env.Object);
				var now      = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				var handler  = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be invoked for placeholder updateUrl."));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.ShouldBeEmpty();
				result.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.Unsupported);
				result.IsUpdateAvailable.ShouldBeFalse();
				result.CheckSucceeded.ShouldBeTrue();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that non-HTTPS, loopback, local, and RFC 2606 reserved URLs are rejected by <see cref="VsCodeUpdateService.IsValidUpdateEndpointUrl"/>.
		/// </summary>
		/// <param name="url">The candidate update endpoint URL.</param>
		/// <param name="expectedValid">Whether the URL is expected to be valid.</param>
		[TestCase("https://example.com", false)]
		[TestCase("https://example.org/updates", false)]
		[TestCase("https://example.net/updates", false)]
		[TestCase("https://localhost/updates", false)]
		[TestCase("https://127.0.0.1/updates", false)]
		[TestCase("http://api2.cursor.sh/updates", false)]
		[TestCase("https://api2.cursor.sh/updates", true)]
		public void IsValidUpdateEndpointUrl_ValidatesPublicHttpsEndpoints(string url, bool expectedValid)
		{
			VsCodeUpdateService.IsValidUpdateEndpointUrl(url).ShouldBe(expectedValid);
		}

		/// <summary>
		/// Verifies that a VS Code API fork queries its <c>product.json</c> <c>updateUrl</c> endpoint and trusts its descriptor download host for downloading.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_ForVsCodeApiFork_QueriesProductJsonUpdateUrlAndTrustsForkHost()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\cursor\Cursor.exe";

			try
			{
				const string forkCommit        = "abcdef1234567";
				const string forkDownloadUrl   = "https://downloader.cursor.sh/CursorUserSetup-x64.exe";
				byte[] installerBytes          = Encoding.UTF8.GetBytes("cursor-installer-bytes");
				string installerSha256         = Convert.ToHexString(SHA256.HashData(installerBytes)).ToLowerInvariant();
				Mock<IVsCodeEnvironment> env   = CreateEnvironmentMock(
					tempDir,
					exePath,
					forkCommit,
					"stable",
					"user",
					"0.48.0",
					"https://api2.cursor.sh/updates");
				VsCodeProductDescriptor cursor = GetKnownDescriptor("cursor");
				var instance                   = new VsCodeInstance("Cursor", "0.48.0", exePath, "cursor-user", @"C:\Users\Dev\AppData\Roaming\Cursor", cursor, env.Object);
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string payload                 = BuildUpdateJson(forkDownloadUrl, "0.49.0", NewCommit, installerSha256);
				var handler                    = new StubHttpMessageHandler(request =>
				{
					if (request.RequestUri!.Host.Equals("downloader.cursor.sh", StringComparison.OrdinalIgnoreCase))
					{
						return new HttpResponseMessage(HttpStatusCode.OK)
						{
							Content = new ByteArrayContent(installerBytes)
						};
					}

					return CreateJsonResponse(HttpStatusCode.OK, payload);
				});

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				handler.RequestUris[0].ToString().ShouldStartWith("https://api2.cursor.sh/updates/");
				handler.RequestUris[0].ToString().ShouldContain("/win32-x64-user/stable/abcdef1234567");
				result.IsUpdateAvailable.ShouldBeTrue();
				result.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpdateAvailable);
				result.CheckSucceeded.ShouldBeTrue();
				result.LatestVersion.ShouldBe("0.49.0");
				result.DownloadUrl.ShouldBe(forkDownloadUrl);
				result.InstallerKind.ShouldBe(VsCodeInstallerKind.InnoSetupGeneric);

				string downloadedPath = await service.DownloadInstallerAsync(result, instance);

				File.Exists(downloadedPath).ShouldBeTrue();
				File.ReadAllBytes(downloadedPath).ShouldBe(installerBytes);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that a GitHub Releases fork queries the GitHub latest release API, compares semantic versions, and selects the matching Windows architecture and scope installer asset.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_ForGitHubReleasesFork_ParsesLatestReleaseAndMatchingWinAsset()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\VSCodium\VSCodium.exe";

			try
			{
				const string expectedAssetUrl  = "https://github.com/VSCodium/vscodium/releases/download/1.96.2/VSCodiumUserSetup-x64-1.96.2.exe";
				Mock<IVsCodeEnvironment> env   = CreateEnvironmentMock(tempDir, exePath, string.Empty, "stable", "user", "1.95.0");
				VsCodeProductDescriptor codium = GetKnownDescriptor("vscodium");
				var instance                   = new VsCodeInstance("VSCodium", "1.95.0", exePath, "vscodium-user", @"C:\Users\Dev\AppData\Roaming\VSCodium", codium, env.Object);
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string releaseJson             = """
					{
						"tag_name": "1.96.2",
						"assets": [
							{
								"name": "VSCodiumSetup-x64-1.96.2.exe",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/1.96.2/VSCodiumSetup-x64-1.96.2.exe"
							},
							{
								"name": "VSCodiumUserSetup-arm64-1.96.2.exe",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/1.96.2/VSCodiumUserSetup-arm64-1.96.2.exe"
							},
							{
								"name": "VSCodiumUserSetup-x64-1.96.2.exe",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/1.96.2/VSCodiumUserSetup-x64-1.96.2.exe"
							},
							{
								"name": "VSCodiumUserSetup-x64-1.96.2.exe.sha256",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/1.96.2/VSCodiumUserSetup-x64-1.96.2.exe.sha256"
							}
						]
					}
					""";
				var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, releaseJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				handler.RequestUris[0].ToString().ShouldBe("https://api.github.com/repos/VSCodium/vscodium/releases/latest");
				handler.AcceptHeaders[0].ShouldContain("application/vnd.github+json");
				result.CheckSucceeded.ShouldBeTrue();
				result.IsUpdateAvailable.ShouldBeTrue();
				result.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpdateAvailable);
				result.LatestVersion.ShouldBe("1.96.2");
				result.DownloadUrl.ShouldBe(expectedAssetUrl);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.CheckAllForUpdatesAsync"/> checks every supplied instance and maps each identifier to its resulting update state.
		/// </summary>
		[Test]
		public async Task CheckAllForUpdatesAsync_ChecksAllInstancesAndReturnsPerIdentifierMap()
		{
			string tempDir        = CreateIsolatedTempDirectory();
			string cachePath      = Path.Combine(tempDir, "cache.json");
			string insidersExe    = @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code Insiders\Code - Insiders.exe";
			string antigravityExe = @"C:\Users\Dev\AppData\Local\Programs\Antigravity IDE\Antigravity IDE.exe";

			try
			{
				var env = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

				env.Setup(e => e.GetTempPath()).Returns(tempDir);
				env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(tempDir);
				env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(@"C:\Program Files");
				env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(@"C:\Program Files (x86)");

				SetupInstanceJsonFiles(env, UserInstallExe, OldCommit, "stable", "user", "1.146.0", null);
				SetupInstanceJsonFiles(env, insidersExe, OldCommit, "insider", "user", "1.148.0-insider", null);
				SetupInstanceJsonFiles(env, antigravityExe, OldCommit, "stable", "user", "1.0.0", "https://example.com");

				VsCodeInstance stable      = CreateInstance(UserInstallExe, "vscode-stable-user", "1.146.0", false, env.Object);
				VsCodeInstance insiders    = CreateInstance(insidersExe, "vscode-insiders-user", "1.148.0-insider", true, env.Object);
				var antigravity            = new VsCodeInstance("Antigravity IDE", "1.0.0", antigravityExe, "antigravity-ide-user", @"C:\Users\Dev\AppData\Roaming\Antigravity IDE", GetKnownDescriptor("antigravity-ide"), env.Object);
				var now                    = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string stableUpdateJson    = BuildUpdateJson(TrustedExeUrl, "1.147.0", NewCommit, SampleSha256);
				var handler                = new StubHttpMessageHandler(req =>
					req.RequestUri!.ToString().Contains("/insider/", StringComparison.OrdinalIgnoreCase)
						? new HttpResponseMessage(HttpStatusCode.NoContent)
						: CreateJsonResponse(HttpStatusCode.OK, stableUpdateJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				IReadOnlyDictionary<string, VsCodeUpdateInfo> results = await service.CheckAllForUpdatesAsync([stable, insiders, antigravity]);

				results.Count.ShouldBe(3);
				results["vscode-stable-user"].UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpdateAvailable);
				results["vscode-insiders-user"].UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpToDate);
				results["antigravity-ide-user"].UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.Unsupported);
				handler.RequestUris.Count.ShouldBe(2);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.BuildInstallerStartInfo"/> selects silent command-line arguments based on <see cref="VsCodeInstallerKind"/>.
		/// </summary>
		/// <param name="installerKind">The target installer family.</param>
		/// <param name="expectedArguments">The exact expected silent argument string.</param>
		[TestCase(VsCodeInstallerKind.InnoSetupVsCode, "/SILENT /NORESTART /CLOSEAPPLICATIONS /MERGETASKS=!runcode")]
		[TestCase(VsCodeInstallerKind.InnoSetupGeneric, "/SILENT /NORESTART /CLOSEAPPLICATIONS")]
		[TestCase(VsCodeInstallerKind.NsisSilent, "/S")]
		public void BuildInstallerStartInfo_SelectsArgumentsByInstallerKind(VsCodeInstallerKind installerKind, string expectedArguments)
		{
			ProcessStartInfo startInfo = VsCodeUpdateService.BuildInstallerStartInfo(@"C:\Temp\IdeUpdate.exe", isSystemInstall: false, installerKind);

			startInfo.FileName.ShouldBe(@"C:\Temp\IdeUpdate.exe");
			startInfo.UseShellExecute.ShouldBeTrue();
			startInfo.Verb.ShouldBe(string.Empty);
			startInfo.Arguments.ShouldBe(expectedArguments);
		}

		/// <summary>
		/// Verifies that a VS Code API fork with missing <c>updateUrl</c> returns <see cref="VsCodeUpdateCheckStatus.Unsupported"/> without making an HTTP call.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenNonMicrosoftVsCodeApiForkHasNoUpdateUrl_ReturnsUnsupportedWithoutHttpCall()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\CustomFork\CustomFork.exe";

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(tempDir, exePath, OldCommit, "stable", "user", "1.0.0", updateUrl: null);
				var descriptor               = new VsCodeProductDescriptor
				{
					FlavorId           = "custom-fork",
					DisplayName        = "Custom Fork",
					ShortName          = "Custom Fork",
					UpdateStrategy     = VsCodeUpdateStrategyKind.VsCodeApi,
					InstallerKind      = VsCodeInstallerKind.InnoSetupGeneric,
					UserDataFolderName = "CustomFork"
				};
				var instance = new VsCodeInstance("Custom Fork", "1.0.0", exePath, "custom-fork-user", @"C:\Users\Dev\AppData\Roaming\CustomFork", descriptor, env.Object);
				var now      = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				var handler  = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be invoked without a valid updateUrl."));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.ShouldBeEmpty();
				result.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.Unsupported);
				result.CheckSucceeded.ShouldBeTrue();
				result.IsUpdateAvailable.ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeUpdateService.IsTrustedDownloadUrl(string?, VsCodeInstance?)"/> preserves Microsoft-only 1-arg validation
		/// while accepting descriptor hosts and subdomains of the instance's validated <c>updateUrl</c> host.
		/// </summary>
		[Test]
		public void IsTrustedDownloadUrl_ValidatesInstanceTrustedHostsAndUpdateUrlSubdomains()
		{
			string tempDir = CreateIsolatedTempDirectory();
			string exePath = @"C:\Users\Dev\AppData\Local\Programs\MyFork\MyFork.exe";

			try
			{
				Mock<IVsCodeEnvironment> env = CreateEnvironmentMock(
					tempDir,
					exePath,
					OldCommit,
					"stable",
					"user",
					"1.0.0",
					"https://updates.myfork.dev/api");
				var descriptor = new VsCodeProductDescriptor
				{
					FlavorId             = "myfork",
					DisplayName          = "MyFork",
					ShortName            = "MyFork",
					UpdateStrategy       = VsCodeUpdateStrategyKind.VsCodeApi,
					TrustedDownloadHosts = ["updates.myfork.dev"]
				};
				var instance = new VsCodeInstance("MyFork", "1.0.0", exePath, "myfork-user", @"C:\Users\Dev\AppData\Roaming\MyFork", descriptor, env.Object);

				VsCodeUpdateService.IsTrustedDownloadUrl(TrustedExeUrl).ShouldBeTrue();
				VsCodeUpdateService.IsTrustedDownloadUrl("https://cdn.updates.myfork.dev/MyForkSetup.exe").ShouldBeFalse();
				VsCodeUpdateService.IsTrustedDownloadUrl("https://cdn.updates.myfork.dev/MyForkSetup.exe", instance).ShouldBeTrue();
				VsCodeUpdateService.IsTrustedDownloadUrl("https://updates.myfork.dev/MyForkSetup.exe", instance).ShouldBeTrue();
				VsCodeUpdateService.IsTrustedDownloadUrl("https://evilmyfork.dev/MyForkSetup.exe", instance).ShouldBeFalse();
				VsCodeUpdateService.IsTrustedDownloadUrl("http://cdn.updates.myfork.dev/MyForkSetup.exe", instance).ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that GitHub Releases update checking normalizes <c>v</c>-prefixed release tags, selects ARM64 System assets,
		/// and clears cached updates when the local version reaches the cached release version.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_ForGitHubReleasesFork_HandlesVPrefixArm64SystemAndCacheClear()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Program Files\VSCodium\VSCodium.exe";

			try
			{
				string currentVersion          = "1.95.0";
				const string expectedAssetUrl  = "https://github.com/VSCodium/vscodium/releases/download/v1.96.2/VSCodiumSetup-arm64-1.96.2.exe";
				Mock<IVsCodeEnvironment> env   = CreateDynamicEnvironmentMock(tempDir, exePath, () => string.Empty, () => "stable", () => "system", () => currentVersion);
				VsCodeProductDescriptor codium = GetKnownDescriptor("vscodium");
				var instance                   = new VsCodeInstance("VSCodium", currentVersion, exePath, "vscodium-system", @"C:\Users\Dev\AppData\Roaming\VSCodium", codium, env.Object)
				{
					IsSystemInstall = true
				};
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string releaseJson             = """
					{
						"tag_name": "v1.96.2",
						"assets": [
							{
								"name": "VSCodiumUserSetup-arm64-1.96.2.exe",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/v1.96.2/VSCodiumUserSetup-arm64-1.96.2.exe"
							},
							{
								"name": "VSCodiumSetup-arm64-1.96.2.exe",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/v1.96.2/VSCodiumSetup-arm64-1.96.2.exe"
							},
							{
								"name": "VSCodiumSetup-x64-1.96.2.exe",
								"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/v1.96.2/VSCodiumSetup-x64-1.96.2.exe"
							}
						]
					}
					""";
				var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, releaseJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.Arm64);

				VsCodeUpdateInfo firstResult = await service.CheckForUpdateAsync(instance);

				firstResult.IsUpdateAvailable.ShouldBeTrue();
				firstResult.LatestVersion.ShouldBe("1.96.2");
				firstResult.DownloadUrl.ShouldBe(expectedAssetUrl);
				firstResult.IsSystemInstall.ShouldBeTrue();

				// Simulate VSCodium updated locally to 1.96.2; cache should clear without another HTTP call.
				currentVersion = "1.96.2";

				VsCodeUpdateInfo secondResult = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(1);
				secondResult.IsUpdateAvailable.ShouldBeFalse();
				secondResult.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpToDate);
				secondResult.CheckSucceeded.ShouldBeTrue();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that deserializing a legacy <see cref="VsCodeUpdateInfo"/> JSON cache entry without <c>UpdateCheckStatus</c>
		/// or <c>InstallerKind</c> derives consistent defaults.
		/// </summary>
		[Test]
		public void VsCodeUpdateInfo_LegacyCacheDeserialization_DerivesConsistentStatusAndInstallerKind()
		{
			const string legacyUpdateJson = """
				{
					"InstanceIdentifier": "vscode-stable-user",
					"IsUpdateAvailable": true,
					"CurrentVersion": "1.146.0",
					"LatestVersion": "1.147.0",
					"CheckSucceeded": true
				}
				""";
			const string legacyUpToDateJson = """
				{
					"InstanceIdentifier": "vscode-insiders-user",
					"IsUpdateAvailable": false,
					"CurrentVersion": "1.148.0-insider",
					"CheckSucceeded": true
				}
				""";

			VsCodeUpdateInfo? updateInfo   = Newtonsoft.Json.JsonConvert.DeserializeObject<VsCodeUpdateInfo>(legacyUpdateJson);
			VsCodeUpdateInfo? upToDateInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<VsCodeUpdateInfo>(legacyUpToDateJson);

			updateInfo.ShouldNotBeNull();
			updateInfo.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpdateAvailable);
			updateInfo.InstallerKind.ShouldBe(VsCodeInstallerKind.InnoSetupVsCode);

			upToDateInfo.ShouldNotBeNull();
			upToDateInfo.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpToDate);
			upToDateInfo.InstallerKind.ShouldBe(VsCodeInstallerKind.InnoSetupVsCode);
		}

		/// <summary>
		/// Verifies that VSCodium installations whose 3-part <c>package.json</c> version and 4th-segment <c>release</c> metadata
		/// (or 4-part executable file version) match a 4-part GitHub release tag report <see cref="VsCodeUpdateCheckStatus.UpToDate"/>.
		/// </summary>
		/// <param name="packageRelease">Optional <c>release</c> property in <c>package.json</c>.</param>
		/// <param name="productRelease">Optional <c>release</c> property in <c>product.json</c>.</param>
		/// <param name="fileVersion">Optional 4-part executable file version.</param>
		[TestCase("25017", null, null)]
		[TestCase(null, "25017", null)]
		[TestCase(null, null, "1.96.2.25017")]
		public async Task CheckForUpdateAsync_ForVscodiumMatchingFourPartReleaseTag_ReturnsUpToDate(
			string? packageRelease,
			string? productRelease,
			string? fileVersion)
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\VSCodium\VSCodium.exe";

			try
			{
				Mock<IVsCodeEnvironment> env   = CreateVscodiumEnvironmentMock(tempDir, exePath, "1.96.2", packageRelease, productRelease, fileVersion);
				VsCodeProductDescriptor codium = GetKnownDescriptor("vscodium");
				var instance                   = new VsCodeInstance("VSCodium", "1.96.2", exePath, "vscodium-user", @"C:\Users\Dev\AppData\Roaming\VSCodium", codium, env.Object);
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string releaseJson             = BuildVscodiumFourPartReleaseJson("1.96.2.25017");
				var handler                    = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, releaseJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				result.CheckSucceeded.ShouldBeTrue();
				result.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpToDate);
				result.IsUpdateAvailable.ShouldBeFalse();
				result.CurrentVersion.ShouldBe("1.96.2.25017");
				result.LatestVersion.ShouldBe("1.96.2.25017");
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that a VSCodium installation with an older 4th-segment <c>release</c> value than the GitHub release tag
		/// reports <see cref="VsCodeUpdateCheckStatus.UpdateAvailable"/>.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_ForVscodiumOlderFourPartRelease_ReturnsUpdateAvailable()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\VSCodium\VSCodium.exe";

			try
			{
				Mock<IVsCodeEnvironment> env   = CreateVscodiumEnvironmentMock(tempDir, exePath, "1.96.2", "25010", productRelease: null, fileVersion: null);
				VsCodeProductDescriptor codium = GetKnownDescriptor("vscodium");
				var instance                   = new VsCodeInstance("VSCodium", "1.96.2", exePath, "vscodium-user", @"C:\Users\Dev\AppData\Roaming\VSCodium", codium, env.Object);
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string releaseJson             = BuildVscodiumFourPartReleaseJson("1.96.2.25017");
				var handler                    = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, releaseJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				result.CheckSucceeded.ShouldBeTrue();
				result.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpdateAvailable);
				result.IsUpdateAvailable.ShouldBeTrue();
				result.CurrentVersion.ShouldBe("1.96.2.25010");
				result.LatestVersion.ShouldBe("1.96.2.25017");
				result.DownloadUrl.ShouldBe("https://github.com/VSCodium/vscodium/releases/download/1.96.2.25017/VSCodiumUserSetup-x64-1.96.2.25017.exe");
				result.Sha256Hash.ShouldBe(SampleSha256);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that a 3-part local version without 4th-segment metadata matching the first 3 segments of a 4-part GitHub tag
		/// reports <see cref="VsCodeUpdateCheckStatus.UpToDate"/> and clears matching cached updates without looping.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_WhenThreePartLocalMatchesFirstThreeSegmentsOfFourPartTag_ReturnsUpToDate()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\VSCodium\VSCodium.exe";

			try
			{
				string currentVersion          = "1.96.2";
				Mock<IVsCodeEnvironment> env   = CreateDynamicEnvironmentMock(tempDir, exePath, () => string.Empty, () => "stable", () => "user", () => currentVersion);
				VsCodeProductDescriptor codium = GetKnownDescriptor("vscodium");
				var instance                   = new VsCodeInstance("VSCodium", currentVersion, exePath, "vscodium-user", @"C:\Users\Dev\AppData\Roaming\VSCodium", codium, env.Object);
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string releaseJson             = BuildVscodiumFourPartReleaseJson("1.96.2.25017");
				var handler                    = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, releaseJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo upToDateResult = await service.CheckForUpdateAsync(instance);

				upToDateResult.CheckSucceeded.ShouldBeTrue();
				upToDateResult.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpToDate);
				upToDateResult.IsUpdateAvailable.ShouldBeFalse();
				upToDateResult.CurrentVersion.ShouldBe("1.96.2");
				upToDateResult.LatestVersion.ShouldBe("1.96.2.25017");

				// Also verify that upgrading from 1.95.0 to 3-part 1.96.2 clears a cached 1.96.2.25017 update without HTTP.
				File.Delete(cachePath);
				currentVersion = "1.95.0";

				VsCodeUpdateInfo availableResult = await service.CheckForUpdateAsync(instance);

				availableResult.IsUpdateAvailable.ShouldBeTrue();

				currentVersion = "1.96.2";

				VsCodeUpdateInfo clearedResult = await service.CheckForUpdateAsync(instance);

				handler.RequestUris.Count.ShouldBe(2);
				clearedResult.UpdateCheckStatus.ShouldBe(VsCodeUpdateCheckStatus.UpToDate);
				clearedResult.IsUpdateAvailable.ShouldBeFalse();
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
		}

		/// <summary>
		/// Verifies that non-hexadecimal 64-character GitHub release asset <c>digest</c> values are ignored safely.
		/// </summary>
		[Test]
		public async Task CheckForUpdateAsync_ForGitHubReleasesWithNonHexDigest_IgnoresInvalidSha256Hash()
		{
			string tempDir   = CreateIsolatedTempDirectory();
			string cachePath = Path.Combine(tempDir, "cache.json");
			string exePath   = @"C:\Users\Dev\AppData\Local\Programs\VSCodium\VSCodium.exe";

			try
			{
				Mock<IVsCodeEnvironment> env   = CreateEnvironmentMock(tempDir, exePath, string.Empty, "stable", "user", "1.95.0");
				VsCodeProductDescriptor codium = GetKnownDescriptor("vscodium");
				var instance                   = new VsCodeInstance("VSCodium", "1.95.0", exePath, "vscodium-user", @"C:\Users\Dev\AppData\Roaming\VSCodium", codium, env.Object);
				var now                        = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
				string invalidHexDigest        = "sha256:" + new string('z', 64);
				string releaseJson             = BuildVscodiumFourPartReleaseJson("1.96.2.25017", invalidHexDigest);
				var handler                    = new StubHttpMessageHandler(_ => CreateJsonResponse(HttpStatusCode.OK, releaseJson));

				using var service = new VsCodeUpdateService(handler, env.Object, () => now, cachePath, Architecture.X64);

				VsCodeUpdateInfo result = await service.CheckForUpdateAsync(instance);

				result.IsUpdateAvailable.ShouldBeTrue();
				result.Sha256Hash.ShouldBe(string.Empty);
			}
			finally
			{
				TryDeleteDirectory(tempDir);
			}
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
		/// Creates a mock <see cref="IVsCodeEnvironment"/> configured with VSCodium <c>package.json</c>, <c>product.json</c>, and file version metadata.
		/// </summary>
		private static Mock<IVsCodeEnvironment> CreateVscodiumEnvironmentMock(
			string tempDir,
			string executablePath,
			string packageVersion,
			string? packageRelease,
			string? productRelease,
			string? fileVersion)
		{
			string installDir      = Path.GetDirectoryName(executablePath)!;
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string packageJsonPath = Path.Combine(installDir, "resources", "app", "package.json");
			string pkgReleaseProp  = string.IsNullOrWhiteSpace(packageRelease) ? string.Empty : $",\"release\":\"{packageRelease}\"";
			string prodReleaseProp = string.IsNullOrWhiteSpace(productRelease) ? string.Empty : $",\"release\":\"{productRelease}\"";
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetTempPath()).Returns(tempDir);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(tempDir);
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).Returns(@"C:\Program Files");
			env.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).Returns(@"C:\Program Files (x86)");
			env.Setup(e => e.GetFileVersion(executablePath)).Returns(fileVersion);
			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(packageJsonPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns($"{{\"quality\":\"stable\",\"target\":\"user\"{prodReleaseProp}}}");
			env.Setup(e => e.ReadAllText(packageJsonPath)).Returns($"{{\"version\":\"{packageVersion}\"{pkgReleaseProp}}}");

			return env;
		}

		/// <summary>
		/// Builds a GitHub <c>releases/latest</c> JSON payload for a 4-part VSCodium release tag.
		/// </summary>
		private static string BuildVscodiumFourPartReleaseJson(string tagName, string? digest = null)
		{
			string assetDigest = digest ?? $"sha256:{SampleSha256}";

			return $$"""
				{
					"tag_name": "{{tagName}}",
					"assets": [
						{
							"name": "VSCodiumUserSetup-x64-{{tagName}}.exe",
							"browser_download_url": "https://github.com/VSCodium/vscodium/releases/download/{{tagName}}/VSCodiumUserSetup-x64-{{tagName}}.exe",
							"digest": "{{assetDigest}}"
						}
					]
				}
				""";
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
			string version,
			string? updateUrl = null)
		{
			return CreateDynamicEnvironmentMock(tempDir, executablePath, () => commit, () => quality, () => target, () => version, () => updateUrl);
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
			Func<string> versionProvider,
			Func<string?>? updateUrlProvider = null)
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
			{
				string? url        = updateUrlProvider?.Invoke();
				string urlProperty = string.IsNullOrWhiteSpace(url) ? string.Empty : $",\"updateUrl\":\"{url}\"";

				return $"{{\"commit\":\"{commitProvider()}\",\"quality\":\"{qualityProvider()}\",\"target\":\"{targetProvider()}\"{urlProperty}}}";
			});
			env.Setup(e => e.ReadAllText(packageJsonPath)).Returns(() =>
				$"{{\"version\":\"{versionProvider()}\"}}");

			return env;
		}

		/// <summary>
		/// Configures <c>product.json</c> and <c>package.json</c> mock responses for an individual executable path.
		/// </summary>
		private static void SetupInstanceJsonFiles(
			Mock<IVsCodeEnvironment> env,
			string executablePath,
			string commit,
			string quality,
			string target,
			string version,
			string? updateUrl)
		{
			string installDir      = Path.GetDirectoryName(executablePath)!;
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string packageJsonPath = Path.Combine(installDir, "resources", "app", "package.json");
			string urlProperty     = string.IsNullOrWhiteSpace(updateUrl) ? string.Empty : $",\"updateUrl\":\"{updateUrl}\"";

			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(packageJsonPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns(
				$"{{\"commit\":\"{commit}\",\"quality\":\"{quality}\",\"target\":\"{target}\"{urlProperty}}}");
			env.Setup(e => e.ReadAllText(packageJsonPath)).Returns(
				$"{{\"version\":\"{version}\"}}");
		}

		/// <summary>
		/// Looks up a built-in product descriptor by its flavor identifier.
		/// </summary>
		private static VsCodeProductDescriptor GetKnownDescriptor(string flavorId)
		{
			return VsCodeProductDescriptor.KnownProducts.Single(p => p.FlavorId.Equals(flavorId, StringComparison.OrdinalIgnoreCase));
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

			/// <summary>
			/// Gets the ordered list of <c>Accept</c> header values observed on incoming requests.
			/// </summary>
			internal List<string> AcceptHeaders { get; } = new List<string>();

			/// <inheritdoc/>
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				if (request.RequestUri is not null)
				{
					this.RequestUris.Add(request.RequestUri);
				}

				this.AcceptHeaders.Add(string.Join(",", request.Headers.Accept.Select(h => h.MediaType)));

				return Task.FromResult(this.responder(request));
			}
		}
	}
}
