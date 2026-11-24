using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;

namespace VSLXshared.Tests.DataModel
{
	/// <summary>
	/// Unit tests for <see cref="VisualStudioInstanceManager"/> executable validation, display-name disambiguation, and deterministic sorting.
	/// </summary>
	[TestFixture]
	public class VisualStudioInstanceManagerTests
	{
		/// <summary>
		/// Verifies that only existing <c>devenv.exe</c> executables qualify as launchable Visual Studio IDEs, excluding Build Tools batch scripts and missing paths.
		/// </summary>
		[Test]
		public void IsLaunchableVisualStudioExecutable_AllowsExistingDevenvOnly()
		{
			string existingDevenv      = @"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe";
			string missingDevenv       = @"C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\Common7\IDE\devenv.exe";
			string buildToolsBatch     = @"C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\Common7\Tools\LaunchDevCmd.bat";
			Func<string, bool> exists  = path => string.Equals(path, existingDevenv, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(path, buildToolsBatch, StringComparison.OrdinalIgnoreCase);

			VisualStudioInstanceManager.IsLaunchableVisualStudioExecutable(existingDevenv, exists).ShouldBeTrue();
			VisualStudioInstanceManager.IsLaunchableVisualStudioExecutable(missingDevenv, exists).ShouldBeFalse();
			VisualStudioInstanceManager.IsLaunchableVisualStudioExecutable(buildToolsBatch, exists).ShouldBeFalse();
			VisualStudioInstanceManager.IsLaunchableVisualStudioExecutable(null, exists).ShouldBeFalse();
			VisualStudioInstanceManager.IsLaunchableVisualStudioExecutable("   ", exists).ShouldBeFalse();
		}

		/// <summary>
		/// Verifies that Insiders and Preview installations receive distinct display names without duplicating existing channel suffixes.
		/// </summary>
		[Test]
		public void QualifyDisplayName_DisambiguatesInsidersAndPreviewWithoutDuplicating()
		{
			string stableLoc   = @"C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe";
			string insidersLoc = @"C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe";
			string previewLoc  = @"C:\Program Files\Microsoft Visual Studio\18\Preview\Common7\IDE\devenv.exe";

			VisualStudioInstanceManager.QualifyDisplayName("Visual Studio Community 2026", stableLoc)
				.ShouldBe("Visual Studio Community 2026");
			VisualStudioInstanceManager.QualifyDisplayName("Visual Studio Community 2026", insidersLoc)
				.ShouldBe("Visual Studio Community 2026 Insiders");
			VisualStudioInstanceManager.QualifyDisplayName("Visual Studio Community 2026", previewLoc)
				.ShouldBe("Visual Studio Community 2026 Preview");
			VisualStudioInstanceManager.QualifyDisplayName("Visual Studio Community 2026 Insiders", insidersLoc)
				.ShouldBe("Visual Studio Community 2026 Insiders");
			VisualStudioInstanceManager.QualifyDisplayName("Visual Studio Community 2026 Preview", previewLoc)
				.ShouldBe("Visual Studio Community 2026 Preview");
		}

		/// <summary>
		/// Verifies that <see cref="VisualStudioInstanceManager.CompareInstances"/> orders deterministically by version, then name, then identifier.
		/// </summary>
		[Test]
		public void CompareInstances_OrdersByVersionThenNameThenIdentifier()
		{
			var older  = new VisualStudioInstance("Visual Studio Community 2022", "17.10.0", @"C:\VS17\devenv.exe", "id-17", "2022");
			var tiedB  = new VisualStudioInstance("Visual Studio Professional 2026", "18.12.0", @"C:\VS18P\devenv.exe", "id-b", "2026");
			var tiedA2 = new VisualStudioInstance("Visual Studio Community 2026", "18.12.0", @"C:\VS18C2\devenv.exe", "id-2", "2026");
			var tiedA1 = new VisualStudioInstance("Visual Studio Community 2026", "18.12.0", @"C:\VS18C1\devenv.exe", "id-1", "2026");
			var newer  = new VisualStudioInstance("Visual Studio Community 2026 Insiders", "18.13.0", @"C:\VS18I\devenv.exe", "id-ins", "2026");
			var list   = new List<VisualStudioInstance> { newer, tiedB, tiedA2, older, tiedA1 };

			list.Sort(VisualStudioInstanceManager.CompareInstances);

			list.Select(x => x.Identifier).ShouldBe(new[] { "id-17", "id-1", "id-2", "id-b", "id-ins" });
		}
	}
}
