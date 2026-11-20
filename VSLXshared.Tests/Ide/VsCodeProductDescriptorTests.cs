using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeProductDescriptor"/> catalog entries, <c>product.json</c> parsing, and update URL validation.
	/// </summary>
	[TestFixture]
	public class VsCodeProductDescriptorTests
	{
		private const string AntigravityInstallDir = @"C:\Users\Dev\AppData\Local\Programs\Antigravity IDE";
		private const string AntigravityExePath    = @"C:\Users\Dev\AppData\Local\Programs\Antigravity IDE\Antigravity IDE.exe";

		/// <summary>
		/// Verifies that <see cref="VsCodeProductDescriptor.KnownProducts"/> contains all built-in flavors with unique identifiers and populated metadata.
		/// </summary>
		[Test]
		public void KnownProducts_ContainsExpectedBuiltInFlavors()
		{
			IReadOnlyList<VsCodeProductDescriptor> products = VsCodeProductDescriptor.KnownProducts;
			string[] expectedFlavorIds =
			[
				"vscode-stable",
				"vscode-insiders",
				"antigravity-ide",
				"cursor",
				"windsurf",
				"vscodium",
				"positron",
				"kiro",
				"trae"
			];

			products.Count.ShouldBeGreaterThanOrEqualTo(expectedFlavorIds.Length);

			var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (VsCodeProductDescriptor product in products)
			{
				product.FlavorId.ShouldNotBeNullOrWhiteSpace();
				uniqueIds.Add(product.FlavorId).ShouldBeTrue($"Duplicate FlavorId: {product.FlavorId}");
				product.DisplayName.ShouldNotBeNullOrWhiteSpace();
				product.ShortName.ShouldNotBeNullOrWhiteSpace();
				product.ExecutableNames.ShouldNotBeEmpty();
				product.UserDataFolderName.ShouldNotBeNullOrWhiteSpace();
			}

			foreach (string expectedId in expectedFlavorIds)
			{
				products.ShouldContain(p => p.FlavorId.Equals(expectedId, StringComparison.OrdinalIgnoreCase));
			}

			VsCodeProductDescriptor.VsCodeStable.FlavorId.ShouldBe("vscode-stable");
			VsCodeProductDescriptor.VsCodeStable.IsInsiders.ShouldBeFalse();
			VsCodeProductDescriptor.VsCodeInsiders.FlavorId.ShouldBe("vscode-insiders");
			VsCodeProductDescriptor.VsCodeInsiders.IsInsiders.ShouldBeTrue();
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeProductDescriptor.TryCreateFromProductJson"/> extracts fork display names, user data folder, slugified flavor ID, and unsupported placeholder update strategy.
		/// </summary>
		[Test]
		public void TryCreateFromProductJson_WithValidForkMetadata_ExtractsNamesAndDataDir()
		{
			string productJsonPath = Path.Combine(AntigravityInstallDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(AntigravityInstallDir, "resources", "app", "out", "cli.js");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(cliJsPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns("""
				{
					"nameShort": "Antigravity IDE",
					"nameLong": "Antigravity IDE",
					"win32DirName": "Antigravity IDE",
					"updateUrl": "https://example.com"
				}
				""");

			VsCodeProductDescriptor? descriptor = VsCodeProductDescriptor.TryCreateFromProductJson(
				env.Object,
				AntigravityInstallDir,
				AntigravityExePath);

			descriptor.ShouldNotBeNull();
			descriptor.ShortName.ShouldBe("Antigravity IDE");
			descriptor.DisplayName.ShouldBe("Antigravity IDE");
			descriptor.UserDataFolderName.ShouldBe("Antigravity IDE");
			descriptor.FlavorId.ShouldBe("antigravity-ide");
			descriptor.UpdateStrategy.ShouldBe(VsCodeUpdateStrategyKind.Unsupported);
			descriptor.ExecutableNames.ShouldContain("Antigravity IDE.exe");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeProductDescriptor.TryCreateFromProductJson"/> enables <see cref="VsCodeUpdateStrategyKind.VsCodeApi"/> when <c>updateUrl</c> is a valid HTTPS endpoint.
		/// </summary>
		[Test]
		public void TryCreateFromProductJson_WithValidHttpsUpdateUrl_SetsVsCodeApiStrategy()
		{
			string installDir      = @"C:\Users\Dev\AppData\Local\Programs\cursor";
			string exePath         = Path.Combine(installDir, "Cursor.exe");
			string productJsonPath = Path.Combine(installDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(installDir, "resources", "app", "out", "cli.js");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(cliJsPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns("""
				{
					"nameShort": "Cursor",
					"nameLong": "Cursor",
					"win32DirName": "Cursor",
					"updateUrl": "https://API2.Cursor.SH./updates"
				}
				""");

			VsCodeProductDescriptor? descriptor = VsCodeProductDescriptor.TryCreateFromProductJson(
				env.Object,
				installDir,
				exePath);

			descriptor.ShouldNotBeNull();
			descriptor.FlavorId.ShouldBe("cursor");
			descriptor.UpdateStrategy.ShouldBe(VsCodeUpdateStrategyKind.VsCodeApi);
			descriptor.TrustedDownloadHosts.ShouldContain("api2.cursor.sh");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeProductDescriptor.TryCreateFromProductJson"/> returns <c>null</c> when <c>resources\app\out\cli.js</c> is absent.
		/// </summary>
		[Test]
		public void TryCreateFromProductJson_WhenCliJsMissing_ReturnsNull()
		{
			string productJsonPath = Path.Combine(AntigravityInstallDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(AntigravityInstallDir, "resources", "app", "out", "cli.js");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(cliJsPath)).Returns(false);

			VsCodeProductDescriptor? descriptor = VsCodeProductDescriptor.TryCreateFromProductJson(
				env.Object,
				AntigravityInstallDir,
				AntigravityExePath);

			descriptor.ShouldBeNull();
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeProductDescriptor.TryCreateFromProductJson"/> returns <c>null</c> without throwing when <c>product.json</c> contains malformed JSON.
		/// </summary>
		[Test]
		public void TryCreateFromProductJson_WhenProductJsonMalformed_ReturnsNull()
		{
			string productJsonPath = Path.Combine(AntigravityInstallDir, "resources", "app", "product.json");
			string cliJsPath       = Path.Combine(AntigravityInstallDir, "resources", "app", "out", "cli.js");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.FileExists(productJsonPath)).Returns(true);
			env.Setup(e => e.FileExists(cliJsPath)).Returns(true);
			env.Setup(e => e.ReadAllText(productJsonPath)).Returns("{ not valid json");

			VsCodeProductDescriptor? descriptor = VsCodeProductDescriptor.TryCreateFromProductJson(
				env.Object,
				AntigravityInstallDir,
				AntigravityExePath);

			descriptor.ShouldBeNull();
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeProductDescriptor.IsValidUpdateEndpointUrl"/> rejects placeholder, local, IP, and non-HTTPS URLs while accepting valid public HTTPS DNS endpoints.
		/// </summary>
		/// <param name="url">The candidate endpoint URL.</param>
		/// <param name="expectedValid">Whether the URL is expected to be accepted.</param>
		[TestCase(null, false)]
		[TestCase("", false)]
		[TestCase("   ", false)]
		[TestCase("http://api2.cursor.sh/updates", false)]
		[TestCase("https://127.0.0.1/updates", false)]
		[TestCase("https://[::1]/updates", false)]
		[TestCase("https://localhost/updates", false)]
		[TestCase("https://sub.localhost/updates", false)]
		[TestCase("https://mybox.local/updates", false)]
		[TestCase("https://example.com", false)]
		[TestCase("https://sub.example.com/updates", false)]
		[TestCase("https://example.org/updates", false)]
		[TestCase("https://example.net/updates", false)]
		[TestCase("https://internal.example/updates", false)]
		[TestCase("https://service.invalid/updates", false)]
		[TestCase("https://staging.test/updates", false)]
		[TestCase("https://api2.cursor.sh/updates", true)]
		[TestCase("https://update.code.visualstudio.com/api/update", true)]
		public void IsValidUpdateEndpointUrl_ValidatesEndpointSecurityAndPlaceholderRules(string? url, bool expectedValid)
		{
			VsCodeProductDescriptor.IsValidUpdateEndpointUrl(url).ShouldBe(expectedValid);
		}

		/// <summary>
		/// Verifies that <see cref="WindowsVsCodeEnvironment.GetDirectories"/> and <see cref="WindowsVsCodeEnvironment.GetFiles"/> enumerate entries safely and return empty collections on missing or invalid paths.
		/// </summary>
		[Test]
		public void WindowsVsCodeEnvironment_GetDirectoriesAndGetFiles_HandleExistingAndMissingPaths()
		{
			string tempRoot = Path.Combine(Path.GetTempPath(), $"vslx_env_test_{Guid.NewGuid():N}");
			string subDir   = Path.Combine(tempRoot, "MyFork");
			string exeFile  = Path.Combine(subDir, "MyFork.exe");
			var env         = new WindowsVsCodeEnvironment();

			try
			{
				Directory.CreateDirectory(subDir);
				File.WriteAllText(exeFile, string.Empty);

				IReadOnlyList<string> dirs  = env.GetDirectories(tempRoot);
				IReadOnlyList<string> files = env.GetFiles(subDir, "*.exe");

				dirs.ShouldContain(subDir);
				files.ShouldContain(exeFile);

				env.GetDirectories(null).ShouldBeEmpty();
				env.GetDirectories(Path.Combine(tempRoot, "Missing")).ShouldBeEmpty();
				env.GetFiles(null, "*.exe").ShouldBeEmpty();
				env.GetFiles(Path.Combine(tempRoot, "Missing"), "*.exe").ShouldBeEmpty();
			}
			finally
			{
				if (Directory.Exists(tempRoot))
				{
					Directory.Delete(tempRoot, recursive: true);
				}
			}
		}
	}
}
