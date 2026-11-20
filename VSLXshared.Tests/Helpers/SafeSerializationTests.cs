using Newtonsoft.Json;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

using VSLXshared.Helpers;

namespace VSLXshared.Tests.Helpers
{
	/// <summary>
	/// Unit tests for safe JSON serialization, backward compatibility, and file extension import mapping.
	/// </summary>
	[TestFixture]
	public class SafeSerializationTests
	{
		/// <summary>
		/// Verifies that legacy JSON without <c>IdeType</c> or <c>VsCodeOptions</c> deserializes cleanly and untrusted types are rejected.
		/// </summary>
		[Test]
		public void Deserialize_LegacyJsonWithoutIdeType_DefaultsToVisualStudioAndRejectsUntrustedTypes()
		{
			string legacyJson = """
                {
                  "$type": "VSLauncher.DataModel.VsFolder, VSLXshared",
                  "Name": "Root",
                  "FolderItems": [
                    { "$type": "VSLauncher.DataModel.VsSolution, VSLXshared", "Name": "App", "Path": "C:\\app\\App.sln" }
                  ]
                }
                """;

			var folder = JsonConvert.DeserializeObject<VsFolder>(legacyJson, FileHelper.CreateSafeJsonSettings());

			folder.ShouldNotBeNull();
			folder.VsCodeOptions.ShouldBeNull();
			folder.Items[0].IdeType.ShouldBe(IdeType.VisualStudio);

			string untrustedJson = """{ "$type": "System.Diagnostics.Process, System.Diagnostics.Process" }""";

			Should.Throw<JsonSerializationException>(() =>
				JsonConvert.DeserializeObject<VsItem>(untrustedJson, FileHelper.CreateSafeJsonSettings()));
		}

		/// <summary>
		/// Verifies that a disallowed type name within an allowed assembly is still rejected by the serialization binder.
		/// </summary>
		[Test]
		public void Deserialize_DisallowedTypeInAllowedAssembly_ThrowsJsonSerializationException()
		{
			string disallowedJson = """{ "$type": "VSLauncher.Helpers.ImportHelper, VSLXshared" }""";

			Should.Throw<JsonSerializationException>(() =>
				JsonConvert.DeserializeObject<VsItem>(disallowedJson, FileHelper.CreateSafeJsonSettings()));
		}

		/// <summary>
		/// Verifies round-trip serialization of <see cref="VsCodeProject"/>, <see cref="VsFolder.VsCodeOptions"/>, <see cref="LaunchInfo.DefaultTargets"/>, and <see cref="ImportHelper.GetItemFromExtension"/>.
		/// </summary>
		[Test]
		public void SerializeRoundTrip_VsCodeProjectAndFolderOptionsAndLaunchInfo_PreservesAllFields()
		{
			var project = new VsCodeProject("demo", "C:\\repo\\demo.code-workspace", VsCodeProjectKind.Workspace)
			{
				VsVersion         = "vscode-stable-user",
				Instance          = "Work",
				WindowMode        = VsCodeWindowMode.NewWindow,
				DisableExtensions = true
			};

			var folder = new VsFolder("repo", "C:\\repo")
			{
				VsCodeOptions = new VsCodeLaunchOptions { Edition = "vscode-insiders-user", Profile = "Dev", DisableExtensions = true }
			};

			folder.Items.Add(project);

			var launch = new LaunchInfo
			{
				Solution       = folder,
				DefaultTargets = new[] { new IdeDefaultTarget(IdeType.VsCode, "vscode-stable-user") }
			};

			var settings     = FileHelper.CreateSafeJsonSettings();
			var roundTripped = JsonConvert.DeserializeObject<LaunchInfo>(JsonConvert.SerializeObject(launch, settings), settings);

			roundTripped.ShouldNotBeNull();
			roundTripped.DefaultTargets!.Single().TargetIdentifier.ShouldBe("vscode-stable-user");
			roundTripped.DefaultTargets!.Single().IdeType.ShouldBe(IdeType.VsCode);
			roundTripped.Solution!.VsCodeOptions!.Profile.ShouldBe("Dev");

			var rtProject = roundTripped.Solution.Items.Single().ShouldBeOfType<VsCodeProject>();

			rtProject.WindowMode.ShouldBe(VsCodeWindowMode.NewWindow);
			rtProject.DisableExtensions.ShouldBeTrue();
			ImportHelper.GetItemFromExtension("demo", "C:\\repo\\demo.code-workspace").ShouldBeOfType<VsCodeProject>();
		}

		/// <summary>
		/// Verifies that <see cref="ImportHelper.GetItemFromExtension"/> maps <c>.slnx</c> to <see cref="VsSolution"/> and <c>.code-workspace</c> to <see cref="VsCodeProject"/>.
		/// </summary>
		[Test]
		public void GetItemFromExtension_MapsSlnxAndCodeWorkspaceProperly()
		{
			VsItem slnxItem      = ImportHelper.GetItemFromExtension("ModernApp", "C:\\repo\\ModernApp.slnx");
			VsItem workspaceItem = ImportHelper.GetItemFromExtension("Workspace", "C:\\repo\\Workspace.CODE-WORKSPACE");

			slnxItem.ShouldBeOfType<VsSolution>();
			slnxItem.IdeType.ShouldBe(IdeType.VisualStudio);

			var codeProject = workspaceItem.ShouldBeOfType<VsCodeProject>();

			codeProject.ProjectKind.ShouldBe(VsCodeProjectKind.Workspace);
			codeProject.IdeType.ShouldBe(IdeType.VsCode);
		}

		/// <summary>
		/// Verifies that <see cref="FileHelper.SolutionFilterString"/> includes VS Code workspaces.
		/// </summary>
		[Test]
		public void SolutionFilterString_IncludesVsCodeWorkspacesFilter()
		{
			FileHelper.SolutionFilterString.ShouldContain("VS Code Workspaces (*.code-workspace)|*.code-workspace");
		}
	}
}
