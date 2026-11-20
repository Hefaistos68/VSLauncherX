using Moq;

using NUnit.Framework;

using Shouldly;

using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VsCodeUpdateSummary"/> aggregation across multiple VS Code-based IDEs.
	/// </summary>
	[TestFixture]
	public class VsCodeUpdateSummaryTests
	{
		/// <summary>
		/// Verifies that when no VS Code-based IDEs are installed, the summary reports the download state.
		/// </summary>
		[Test]
		public void Build_WhenNoIdesInstalled_ReturnsDownloadState()
		{
			var states = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);

			VsCodeUpdateSummary summary = VsCodeUpdateSummary.Build(
				Array.Empty<VsCodeInstance>(),
				states,
				selectedInstance: null,
				installInProgress: false);

			summary.AvailableUpdates.ShouldBeEmpty();
			summary.ButtonEnabled.ShouldBeTrue();
			summary.ShowDownloadIcon.ShouldBeTrue();
			summary.ButtonText.ShouldBe("Download VS Code");
			summary.TooltipText.ShouldNotBeNullOrWhiteSpace();
		}

		/// <summary>
		/// Verifies that when an unsupported IDE is selected but another installed IDE has an available update,
		/// the button is enabled and targets that updatable IDE.
		/// </summary>
		[Test]
		public void Build_WhenOneOfMultipleIdesHasUpdate_EnablesButtonForThatIdeEvenIfDifferentIdeSelected()
		{
			VsCodeInstance vsCode      = CreateTestInstance("vscode-stable", "Visual Studio Code", "VS Code", "vscode-stable-user", "1.146.0", VsCodeUpdateStrategyKind.VsCodeApi);
			VsCodeInstance antigravity = CreateTestInstance("antigravity-ide", "Antigravity IDE", "Antigravity IDE", "antigravity-ide-user", "1.0.0", VsCodeUpdateStrategyKind.Unsupported);
			var states = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase)
			{
				[vsCode.Identifier] = new VsCodeUpdateInfo
				{
					InstanceIdentifier = vsCode.Identifier,
					IsUpdateAvailable  = true,
					UpdateCheckStatus  = VsCodeUpdateCheckStatus.UpdateAvailable,
					CurrentVersion     = "1.146.0",
					LatestVersion      = "1.147.0",
					CheckSucceeded     = true
				},
				[antigravity.Identifier] = new VsCodeUpdateInfo
				{
					InstanceIdentifier = antigravity.Identifier,
					IsUpdateAvailable  = false,
					UpdateCheckStatus  = VsCodeUpdateCheckStatus.Unsupported,
					CurrentVersion     = "1.0.0",
					CheckSucceeded     = true
				}
			};

			VsCodeUpdateSummary summary = VsCodeUpdateSummary.Build(
				[vsCode, antigravity],
				states,
				selectedInstance: antigravity,
				installInProgress: false);

			summary.AvailableUpdates.Count.ShouldBe(1);
			summary.AvailableUpdates[0].Instance.ShouldBe(vsCode);
			summary.ButtonEnabled.ShouldBeTrue();
			summary.ShowDownloadIcon.ShouldBeFalse();
			summary.ButtonText.ShouldBe("Update VS Code");
			summary.TooltipText.ShouldContain("VS Code");
			summary.TooltipText.ShouldContain("1.147.0");
		}

		/// <summary>
		/// Verifies that when multiple IDEs have updates available, the button displays the aggregate count and a combined tooltip.
		/// </summary>
		[Test]
		public void Build_WhenMultipleIdesHaveUpdates_ReturnsMultiUpdateCountAndCombinedTooltip()
		{
			VsCodeInstance vsCode = CreateTestInstance("vscode-stable", "Visual Studio Code", "VS Code", "vscode-stable-user", "1.146.0", VsCodeUpdateStrategyKind.VsCodeApi);
			VsCodeInstance cursor = CreateTestInstance("cursor", "Cursor", "Cursor", "cursor-user", "0.48.0", VsCodeUpdateStrategyKind.VsCodeApi);
			var states = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase)
			{
				[vsCode.Identifier] = new VsCodeUpdateInfo
				{
					InstanceIdentifier = vsCode.Identifier,
					IsUpdateAvailable  = true,
					UpdateCheckStatus  = VsCodeUpdateCheckStatus.UpdateAvailable,
					CurrentVersion     = "1.146.0",
					LatestVersion      = "1.147.0",
					CheckSucceeded     = true
				},
				[cursor.Identifier] = new VsCodeUpdateInfo
				{
					InstanceIdentifier = cursor.Identifier,
					IsUpdateAvailable  = true,
					UpdateCheckStatus  = VsCodeUpdateCheckStatus.UpdateAvailable,
					CurrentVersion     = "0.48.0",
					LatestVersion      = "0.49.0",
					CheckSucceeded     = true
				}
			};

			VsCodeUpdateSummary summary = VsCodeUpdateSummary.Build(
				[vsCode, cursor],
				states,
				selectedInstance: vsCode,
				installInProgress: false);

			summary.AvailableUpdates.Count.ShouldBe(2);
			summary.ButtonEnabled.ShouldBeTrue();
			summary.ShowDownloadIcon.ShouldBeFalse();
			summary.ButtonText.ShouldBe("Update IDEs (2)");
			summary.TooltipText.ShouldContain("VS Code");
			summary.TooltipText.ShouldContain("1.147.0");
			summary.TooltipText.ShouldContain("Cursor");
			summary.TooltipText.ShouldContain("0.49.0");
		}

		/// <summary>
		/// Verifies that when all installed IDEs are either up to date or unsupported, the button is disabled and the tooltip summarizes each IDE.
		/// </summary>
		[Test]
		public void Build_WhenAllIdesUpToDateOrUnsupported_DisablesButtonWithPerIdeTooltip()
		{
			VsCodeInstance vsCode      = CreateTestInstance("vscode-stable", "Visual Studio Code", "VS Code", "vscode-stable-user", "1.147.0", VsCodeUpdateStrategyKind.VsCodeApi);
			VsCodeInstance antigravity = CreateTestInstance("antigravity-ide", "Antigravity IDE", "Antigravity IDE", "antigravity-ide-user", "1.0.0", VsCodeUpdateStrategyKind.Unsupported);
			var states = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase)
			{
				[vsCode.Identifier] = new VsCodeUpdateInfo
				{
					InstanceIdentifier = vsCode.Identifier,
					IsUpdateAvailable  = false,
					UpdateCheckStatus  = VsCodeUpdateCheckStatus.UpToDate,
					CurrentVersion     = "1.147.0",
					LatestVersion      = "1.147.0",
					CheckSucceeded     = true
				},
				[antigravity.Identifier] = new VsCodeUpdateInfo
				{
					InstanceIdentifier = antigravity.Identifier,
					IsUpdateAvailable  = false,
					UpdateCheckStatus  = VsCodeUpdateCheckStatus.Unsupported,
					CurrentVersion     = "1.0.0",
					CheckSucceeded     = true
				}
			};

			VsCodeUpdateSummary summary = VsCodeUpdateSummary.Build(
				[vsCode, antigravity],
				states,
				selectedInstance: vsCode,
				installInProgress: false);

			summary.AvailableUpdates.ShouldBeEmpty();
			summary.ButtonEnabled.ShouldBeFalse();
			summary.ShowDownloadIcon.ShouldBeFalse();
			summary.TooltipText.ShouldContain("VS Code");
			summary.TooltipText.ShouldContain("up to date", Case.Insensitive);
			summary.TooltipText.ShouldContain("Antigravity IDE");
			summary.TooltipText.ShouldContain("manual", Case.Insensitive);
		}

		/// <summary>
		/// Verifies that while an update installation is in progress, the button is disabled and reflects the active progress label.
		/// </summary>
		[Test]
		public void Build_WhenInstallInProgress_DisablesButtonWithActiveProgressLabel()
		{
			VsCodeInstance vsCode = CreateTestInstance("vscode-stable", "Visual Studio Code", "VS Code", "vscode-stable-user", "1.146.0", VsCodeUpdateStrategyKind.VsCodeApi);
			var states            = new Dictionary<string, VsCodeUpdateInfo>(StringComparer.OrdinalIgnoreCase);

			VsCodeUpdateSummary summary = VsCodeUpdateSummary.Build(
				[vsCode],
				states,
				selectedInstance: vsCode,
				installInProgress: true,
				activeInstallLabel: "Updating VS Code (1/2)...");

			summary.ButtonEnabled.ShouldBeFalse();
			summary.ShowDownloadIcon.ShouldBeFalse();
			summary.ButtonText.ShouldBe("Updating VS Code (1/2)...");
			summary.TooltipText.ShouldContain("Updating VS Code (1/2)...");
		}

		/// <summary>
		/// Creates a minimal <see cref="VsCodeInstance"/> for summary aggregation unit tests.
		/// </summary>
		private static VsCodeInstance CreateTestInstance(
			string flavorId,
			string displayName,
			string shortName,
			string identifier,
			string version,
			VsCodeUpdateStrategyKind strategy)
		{
			var env        = new Mock<IVsCodeEnvironment>(MockBehavior.Loose);
			var descriptor = new VsCodeProductDescriptor
			{
				FlavorId       = flavorId,
				DisplayName    = displayName,
				ShortName      = shortName,
				UpdateStrategy = strategy
			};

			return new VsCodeInstance(
				displayName,
				version,
				$@"C:\Apps\{flavorId}\{shortName}.exe",
				identifier,
				$@"C:\Users\Dev\AppData\Roaming\{shortName}",
				descriptor,
				env.Object);
		}
	}
}
