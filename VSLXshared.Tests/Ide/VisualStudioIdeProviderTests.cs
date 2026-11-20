using System.Diagnostics;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Ide;

namespace VSLXshared.Tests.Ide
{
	/// <summary>
	/// Unit tests for <see cref="VisualStudioIdeProvider"/> and <see cref="VisualStudioInstance"/> <see cref="IIdeInstance"/> compliance.
	/// </summary>
	[TestFixture]
	public class VisualStudioIdeProviderTests
	{
		/// <summary>
		/// Verifies that <see cref="VisualStudioIdeProvider.BuildStartInfo"/> constructs exact devenv.exe arguments and elevation flags.
		/// </summary>
		[Test]
		public void BuildStartInfo_BuildsDevenvArgumentsWithSplashAndRootSuffix()
		{
			var vs       = new VisualStudioInstance("Visual Studio 2022", "17.10.0", @"C:\VS\devenv.exe", "abc123", "2022");
			var provider = new VisualStudioIdeProvider(new[] { vs });
			var sln      = new VsSolution { Name = "App", Path = @"C:\src\App.sln", Instance = "Exp", Commands = "/SafeMode", ShowSplash = false, RunAsAdmin = true };

			ProcessStartInfo? psi = provider.BuildStartInfo(sln, vs);

			psi.ShouldNotBeNull();
			psi.FileName.ShouldBe(@"C:\VS\devenv.exe");
			psi.Verb.ShouldBe("runas");
			psi.UseShellExecute.ShouldBeTrue();
			psi.ErrorDialog.ShouldBeTrue();
			psi.Arguments.ShouldBe("\"C:\\src\\App.sln\" /SafeMode /NoSplash /RootSuffix Exp");
		}

		/// <summary>
		/// Verifies quoting for RootSuffix containing spaces, suppression of /NoSplash when enabled, and forceAdmin override.
		/// </summary>
		[Test]
		public void BuildStartInfo_QuotesSpacedRootSuffixAndHonorsShowSplashAndForceAdmin()
		{
			var vs       = new VisualStudioInstance("Visual Studio 2022", "17.10.0", @"C:\VS\devenv.exe", "abc123", "2022");
			var provider = new VisualStudioIdeProvider(new[] { vs });
			var project  = new VsProject { Name = "Lib", Path = @"C:\src\Lib.csproj", Instance = "My Exp", ShowSplash = true, RunAsAdmin = false };

			ProcessStartInfo? nonAdminPsi = provider.BuildStartInfo(project, vs, forceAdmin: false);
			ProcessStartInfo? forcedPsi   = provider.BuildStartInfo(project, vs, forceAdmin: true);

			nonAdminPsi.ShouldNotBeNull();
			nonAdminPsi.Verb.ShouldBe(string.Empty);
			nonAdminPsi.UseShellExecute.ShouldBeFalse();
			nonAdminPsi.ErrorDialog.ShouldBeFalse();
			nonAdminPsi.Arguments.ShouldBe("\"C:\\src\\Lib.csproj\" /RootSuffix \"My Exp\"");

			forcedPsi.ShouldNotBeNull();
			forcedPsi.Verb.ShouldBe("runas");
			forcedPsi.UseShellExecute.ShouldBeTrue();
			forcedPsi.ErrorDialog.ShouldBeTrue();
		}

		/// <summary>
		/// Verifies that <see cref="VisualStudioIdeProvider.BuildStartInfo"/> handles MRU open-folder items with Project item type.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenFolderHasProjectItemType_BuildsDevenvStartInfo()
		{
			var vs         = new VisualStudioInstance("Visual Studio 2022", "17.10.0", @"C:\VS\devenv.exe", "abc123", "2022");
			var provider   = new VisualStudioIdeProvider(new[] { vs });
			var openFolder = new VsFolder("Repo", @"C:\src\Repo") { ItemType = ItemTypeEnum.Project };

			ProcessStartInfo? psi = provider.BuildStartInfo(openFolder, vs);

			psi.ShouldNotBeNull();
			psi.FileName.ShouldBe(@"C:\VS\devenv.exe");
			psi.Arguments.ShouldBe("\"C:\\src\\Repo\" /NoSplash");
		}

		/// <summary>
		/// Verifies that non-launchable items or items without paths return null from <see cref="VisualStudioIdeProvider.BuildStartInfo"/>.
		/// </summary>
		[Test]
		public void BuildStartInfo_WhenGroupFolderOtherOrMissingPath_ReturnsNull()
		{
			var vs          = new VisualStudioInstance("Visual Studio 2022", "17.10.0", @"C:\VS\devenv.exe", "abc123", "2022");
			var provider    = new VisualStudioIdeProvider(new[] { vs });
			var groupFolder = new VsFolder("Group", @"C:\src\Group");
			var otherItem   = new VsItem { Path = @"C:\tools\prep.cmd", ItemType = ItemTypeEnum.Other };
			var emptySln    = new VsSolution { Path = string.Empty };

			provider.BuildStartInfo(groupFolder, vs).ShouldBeNull();
			provider.BuildStartInfo(otherItem, vs).ShouldBeNull();
			provider.BuildStartInfo(emptySln, vs).ShouldBeNull();
		}

		/// <summary>
		/// Verifies provider metadata, identifier/version lookup, MRU delegation, and <see cref="VisualStudioInstance"/> interface implementation.
		/// </summary>
		[Test]
		public void ProviderLookupsAndMetadata_BehaveAccordingToSpecification()
		{
			var vs2019 = new VisualStudioInstance("Visual Studio 2019", "16.11.5", @"C:\VS19\devenv.exe", "id-19", "2019");
			var vs2022 = new VisualStudioInstance("Visual Studio 2022", "17.10.2", @"C:\VS22\devenv.exe", "id-22", "2022");
			var mru    = new VsItemList(null) { new VsFolder("Visual Studio 2022", string.Empty) { ItemType = ItemTypeEnum.VisualStudio } };

			var provider = new VisualStudioIdeProvider(
				new[] { vs2022, vs2019 },
				onlyDefault => onlyDefault ? new VsItemList(null) : mru);

			provider.IdeType.ShouldBe(IdeType.VisualStudio);
			provider.DisplayName.ShouldBe("Visual Studio");
			provider.DownloadUrl.ShouldBe("https://visualstudio.microsoft.com/downloads/");
			provider.IsInstalled.ShouldBeTrue();
			provider.Instances.Count.ShouldBe(2);
			provider.DefaultInstance.ShouldBeSameAs(vs2022);
			provider.FindByIdentifier("ID-19").ShouldBeSameAs(vs2019);
			provider.FindByIdentifier(null).ShouldBeNull();
			provider.FindByVersion("16").ShouldBeSameAs(vs2019);
			provider.FindByVersion(string.Empty).ShouldBeNull();
			provider.GetRecentProjects(false).Count.ShouldBe(1);
			provider.GetRecentProjects(true).Count.ShouldBe(0);

			IIdeInstance ideInstance = vs2022;

			ideInstance.IdeType.ShouldBe(IdeType.VisualStudio);
			ideInstance.ShortName.ShouldBe("Visual Studio 2022");
			ideInstance.AppIcon.ShouldNotBeNull();
			ideInstance.GetProfiles().ShouldContain("<default>");

			var emptyProvider = new VisualStudioIdeProvider(Array.Empty<VisualStudioInstance>());

			emptyProvider.IsInstalled.ShouldBeFalse();
			emptyProvider.DefaultInstance.ShouldBeNull();
		}
	}
}
