using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeInstance"/> version refreshing and display formatting.
	/// </summary>
	[TestFixture]
	public class VsCodeInstanceTests
	{
		private const string StableExePath = @"C:\Users\Dev\AppData\Local\Programs\Microsoft VS Code\Code.exe";

		/// <summary>
		/// Verifies that <see cref="VsCodeInstance.RefreshVersion"/> updates <see cref="VsCodeInstance.Version"/> and <see cref="VsCodeInstance.ToString"/> when the executable product version changes on disk.
		/// </summary>
		[Test]
		public void RefreshVersion_WhenExecutableVersionChangedOnDisk_UpdatesVersionAndToString()
		{
			var env = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetFileVersion(StableExePath)).Returns("1.147.0");

			var instance = new VsCodeInstance(
				"Visual Studio Code",
				"1.146.0",
				StableExePath,
				"vscode-stable-user",
				@"C:\Users\Dev\AppData\Roaming\Code",
				isInsiders: false,
				env.Object);

			string updated = instance.RefreshVersion();

			updated.ShouldBe("1.147.0");
			instance.Version.ShouldBe("1.147.0");
			instance.ToString().ShouldBe("Visual Studio Code (1.147.0)");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeInstance.RefreshVersion"/> falls back to <c>resources\app\package.json</c> when file version metadata is absent.
		/// </summary>
		[Test]
		public void RefreshVersion_WhenFileVersionUnavailable_FallsBackToPackageJson()
		{
			string packageJsonPath = Path.Combine(Path.GetDirectoryName(StableExePath)!, "resources", "app", "package.json");
			var env                = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetFileVersion(StableExePath)).Returns((string?)null);
			env.Setup(e => e.FileExists(packageJsonPath)).Returns(true);
			env.Setup(e => e.ReadAllText(packageJsonPath)).Returns("{\"version\":\"1.147.1\"}");

			var instance = new VsCodeInstance(
				"Visual Studio Code",
				"1.146.0",
				StableExePath,
				"vscode-stable-user",
				@"C:\Users\Dev\AppData\Roaming\Code",
				isInsiders: false,
				env.Object);

			string updated = instance.RefreshVersion();

			updated.ShouldBe("1.147.1");
			instance.Version.ShouldBe("1.147.1");
			instance.ToString().ShouldBe("Visual Studio Code (1.147.1)");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeInstance.RefreshVersion"/> uses the supplied fallback version when disk metadata cannot be read.
		/// </summary>
		[Test]
		public void RefreshVersion_WhenDiskMetadataUnavailable_UsesFallbackVersion()
		{
			var env = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetFileVersion(StableExePath)).Returns((string?)null);
			env.Setup(e => e.FileExists(It.IsAny<string>())).Returns(false);

			var instance = new VsCodeInstance(
				"Visual Studio Code",
				"1.146.0",
				StableExePath,
				"vscode-stable-user",
				@"C:\Users\Dev\AppData\Roaming\Code",
				isInsiders: false,
				env.Object);

			string updated = instance.RefreshVersion(" 1.148.0 ");

			updated.ShouldBe("1.148.0");
			instance.Version.ShouldBe("1.148.0");
			instance.ToString().ShouldBe("Visual Studio Code (1.148.0)");
		}

		/// <summary>
		/// Verifies that <see cref="VsCodeInstance.RefreshVersion"/> preserves the existing version when neither disk metadata nor a fallback is available.
		/// </summary>
		[Test]
		public void RefreshVersion_WhenDiskAndFallbackUnavailable_PreservesExistingVersion()
		{
			var env = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);

			env.Setup(e => e.GetFileVersion(StableExePath)).Returns((string?)null);
			env.Setup(e => e.FileExists(It.IsAny<string>())).Returns(false);

			var instance = new VsCodeInstance(
				"Visual Studio Code",
				"1.146.0",
				StableExePath,
				"vscode-stable-user",
				@"C:\Users\Dev\AppData\Roaming\Code",
				isInsiders: false,
				env.Object);

			string updated = instance.RefreshVersion(null);

			updated.ShouldBe("1.146.0");
			instance.Version.ShouldBe("1.146.0");
			instance.ToString().ShouldBe("Visual Studio Code (1.146.0)");
		}
	}
}
