using NUnit.Framework;

using Shouldly;

using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeUpdateInfo"/> defaults and property initialization.
	/// </summary>
	[TestFixture]
	public class VsCodeUpdateInfoTests
	{
		/// <summary>
		/// Verifies that a default-constructed <see cref="VsCodeUpdateInfo"/> initializes all properties to safe non-null defaults.
		/// </summary>
		[Test]
		public void DefaultInstance_HasSafeEmptyDefaults()
		{
			var info = new VsCodeUpdateInfo();

			info.InstanceIdentifier.ShouldBe(string.Empty);
			info.IsUpdateAvailable.ShouldBeFalse();
			info.CurrentVersion.ShouldBe(string.Empty);
			info.CurrentCommit.ShouldBe(string.Empty);
			info.LatestVersion.ShouldBe(string.Empty);
			info.LatestCommit.ShouldBe(string.Empty);
			info.DownloadUrl.ShouldBe(string.Empty);
			info.Sha256Hash.ShouldBe(string.Empty);
			info.IsSystemInstall.ShouldBeFalse();
			info.CheckSucceeded.ShouldBeFalse();
			info.CheckedAtUtc.ShouldBe(default);
		}

		/// <summary>
		/// Verifies that init-only properties retain all supplied update metadata values.
		/// </summary>
		[Test]
		public void Initializer_PopulatesAllMetadataFields()
		{
			DateTimeOffset checkedAt = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

			var info = new VsCodeUpdateInfo
			{
				InstanceIdentifier = "vscode-stable-user",
				IsUpdateAvailable  = true,
				CurrentVersion     = "1.146.0",
				CurrentCommit      = "1111111111111111111111111111111111111111",
				LatestVersion      = "1.147.0",
				LatestCommit       = "8a14f0cc1b08d531eb83255ea3764ae22aa932b5",
				DownloadUrl        = "https://vscode.download.prss.microsoft.com/dbazure/download/stable/8a14f0cc1b08d531eb83255ea3764ae22aa932b5/VSCodeUserSetup-x64-1.147.0.exe",
				Sha256Hash         = "903bee51e0114b8e7383703c038176e21cbd0f4a666a76c07705453fe824aaa9",
				IsSystemInstall    = true,
				CheckedAtUtc       = checkedAt,
				CheckSucceeded     = true
			};

			info.InstanceIdentifier.ShouldBe("vscode-stable-user");
			info.IsUpdateAvailable.ShouldBeTrue();
			info.CurrentVersion.ShouldBe("1.146.0");
			info.CurrentCommit.ShouldBe("1111111111111111111111111111111111111111");
			info.LatestVersion.ShouldBe("1.147.0");
			info.LatestCommit.ShouldBe("8a14f0cc1b08d531eb83255ea3764ae22aa932b5");
			info.DownloadUrl.ShouldStartWith("https://vscode.download.prss.microsoft.com/");
			info.Sha256Hash.ShouldBe("903bee51e0114b8e7383703c038176e21cbd0f4a666a76c07705453fe824aaa9");
			info.IsSystemInstall.ShouldBeTrue();
			info.CheckedAtUtc.ShouldBe(checkedAt);
			info.CheckSucceeded.ShouldBeTrue();
		}
	}
}
