# Multi-IDE Support (VS Code Iteration) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend VSLauncherX with a shared multi-IDE provider architecture and full Visual Studio Code support (edition/profile discovery, MRU import, multi-IDE folder scanning, `.code-workspace` & folder rules, tabbed right sidebar, tool-specific settings dialog, single-IDE disabling, and background launch).

**Architecture:** Introduce `IdeType`, `IIdeInstance`, `IIdeProvider`, `IdeManager`, `VsCodeProject`, `VsCodeLaunchOptions`, and `ProjectScanner` in `VSLXshared`, preserving all existing `Vs*` data-model class names and strict `VsSerializationBinder` allowlisting so existing `%AppData%\VSLauncher\VSLauncher.json` files deserialize without migration. Both `VSLauncherX` (tabbed sidebar, `IdeCombobox`, `dlgExecuteVsCode`, combined MRU import, Git/Explorer/Taskbar integration) and `BackgroundLaunch` (`Runner` delegating CLI construction and workspace-backed folder execution to `IdeManager`) consume this shared layer.

**Tech Stack:** C# / .NET 10 Windows (`net10.0-windows` / `net10.0-windows7.0`), Windows Forms, ObjectListView2022, Newtonsoft.Json 13.0.4, Microsoft.Data.Sqlite, LibGit2Sharp 0.31.0, WindowsAPICodePack 8.0.12, NUnit 4 + Moq + Shouldly.

**Spec:** `docs/superpowers/specs/2026-11-19-multi-ide-vscode-design.md`

## Global Constraints

- Target frameworks: `VSLXshared` (`net10.0-windows`), `VSLauncherX` (`net10.0-windows7.0`), `BackgroundLaunch` (`net10.0-windows7.0`, `EnforceCodeStyleInBuild=True`, `AnalysisLevel=latest-all`).
- JSON safety: `Newtonsoft.Json` with `TypeNameHandling.Auto` and `VsSerializationBinder` restricted strictly to `VSLXshared`/`VSLauncherX` allowlisted types; every new serialized model/enum type must be registered in `VsSerializationBinder.AllowedTypes` in `VSLXshared\Helpers\FileHelper.cs`.
- Backward compatibility: Existing `VSLauncher.json` files without `IdeType`, `VsCodeOptions`, or `DefaultTargets` must deserialize cleanly with `IdeType.VisualStudio` defaults.
- SQLite reading: Use `Microsoft.Data.Sqlite` in `VSLXshared`, opened strictly `Mode=ReadOnly`, with copy-to-temp fallback when `state.vscdb` is locked by a running VS Code process.
- Theming: All new forms, tabs, and controls must support Light and Dark palettes via `VSLauncherX\Helpers\ThemeHelper.cs`.
- C# rules: Cognitive complexity <= 15 per method; <= 7 parameters per method (except DI constructors); block-scoped namespaces; XML documentation comments on all new classes, interfaces, and methods (`<inheritdoc/>` when implementing documented interface members); cache `Regex` instances as `static readonly Regex` fields; empty line before `return`/`if`/`for`/`foreach`/`try` unless preceded by a comment or `{`; separate variable declarations from statements with an empty line; align subsequent member initializations on `=`.
- Testing rules: NUnit, Moq, and Shouldly in `.Tests` projects (`VSLXshared.Tests`, `BackgroundLaunch.Tests`), creating a fresh SUT instance inside each test method (no shared SUT test-class field).

## Review Focus

- **Percent-encoded Windows `file://` URIs and remote entries in `state.vscdb`**: Entries like `file:///c%3A/Users/Me/My%20Apps/demo.code-workspace`, malformed URIs, `fileUri` entries, and `vscode-remote://` / `remoteAuthority` entries must decode valid local Windows paths accurately, skip remote/single-file/malformed entries without throwing, and mark missing local paths with `Warning = true`. *(Pinned in Task 3)*
- **Legacy `VSLauncher.json` compatibility and binder security**: Legacy JSON lacking `IdeType`/`VsCodeOptions` must deserialize to `IdeType.VisualStudio` with `VsCodeOptions == null`, while untrusted `$type` payloads outside `VSLXshared`/`VSLauncherX` must still throw `JsonSerializationException`. *(Pinned in Task 1)*
- **Locked/missing `state.vscdb` and corrupt `storage.json` while VS Code is running**: Corrupt `storage.json` must return `["<default>"]` without throwing; missing or locked `state.vscdb` must fall back to copy-to-temp read or empty MRU list and clean up temp files. *(Pinned in Task 3)*
- **Scanner pruning, `.vscode` non-recursion, and noise directories**: Directories containing `package.json` with nested `sub/package.json`, `node_modules/pkg/package.json`, and `.vscode/settings.json` must emit only the root `VsCodeProject`, stop recursing deeper, never traverse `.vscode` or noise folders, and ignore inaccessible paths. *(Pinned in Task 4)*
- **Cross-IDE option stripping and mixed-IDE partial failure in `BackgroundLaunch`**: Launching a Visual Studio `VsSolution` with `Instance = "Exp"` and `Commands = "/SafeMode"` under single-IDE VS Code fallback must open its containing folder without `--profile` or `devenv.exe` switches; launching a mixed group when one IDE is uninstalled must skip unlaunchable children and still launch the remaining children. *(Pinned in Task 5)*

---

## File Structure

### New Files
- `VSLXshared.Tests\VSLXshared.Tests.csproj` — NUnit + Moq + Shouldly test project targeting `net10.0-windows`.
- `BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj` — NUnit + Moq + Shouldly test project targeting `net10.0-windows7.0`.
- `VSLXshared\DataModel\IdeType.cs` — `IdeType` enum (`VisualStudio`, `VsCode`).
- `VSLXshared\DataModel\VsCodeProjectKind.cs` — `VsCodeProjectKind` enum.
- `VSLXshared\DataModel\VsCodeWindowMode.cs` — `VsCodeWindowMode` enum.
- `VSLXshared\DataModel\VsCodeLaunchOptions.cs` — Optional VS Code launch settings stored on `VsFolder`.
- `VSLXshared\DataModel\VsCodeProject.cs` — `VsItem` subclass for `.code-workspace` files and folder/manifest VS Code projects.
- `VSLXshared\DataModel\IdeDefaultTarget.cs` — Serializable `{ IdeType, TargetIdentifier }` pair for `LaunchInfo.DefaultTargets`.
- `VSLXshared\Ide\IIdeInstance.cs` — Common installed IDE instance contract.
- `VSLXshared\Ide\IIdeProvider.cs` — Common IDE discovery, MRU, and CLI-building provider contract.
- `VSLXshared\Ide\VisualStudioIdeProvider.cs` — `IIdeProvider` wrapping `VisualStudioInstanceManager` and `devenv.exe` CLI building.
- `VSLXshared\Ide\IdeManager.cs` — Multi-IDE registry, resolution precedence, and single-IDE fallback.
- `VSLXshared\Ide\IVsCodeEnvironment.cs` — Environment/filesystem/registry/PATH/version abstraction for testable VS Code discovery and storage access.
- `VSLXshared\Ide\WindowsVsCodeEnvironment.cs` — Production Windows implementation of `IVsCodeEnvironment`.
- `VSLXshared\Ide\VsCodeInstance.cs` — `IIdeInstance` for VS Code Stable / Insiders with profile discovery and direct quick actions.
- `VSLXshared\Ide\VsCodeIdeProvider.cs` — VS Code edition discovery, `state.vscdb` MRU reading, and CLI/`cli.js --wait` building.
- `VSLXshared\Helpers\ProjectScanner.cs` — Multi-IDE recursive folder scanner with manifest priority, pruning, noise exclusion, and tree/flat shaping.
- `VSLauncherX\Controls\IdeCombobox.cs` — Owner-drawn IDE selector combobox backed by `IIdeInstance` items.
- `VSLauncherX\Forms\dlgExecuteVsCode.cs` / `.Designer.cs` / `.resx` — VS Code item and folder settings dialog.

### Modified Files
- `VSLauncher.sln` — Register `VSLXshared.Tests` and `BackgroundLaunch.Tests`.
- `VSLXshared\VSLXshared.csproj` — Add `Microsoft.Data.Sqlite` package reference.
- `VSLXshared\DataModel\VsItem.cs` — Add `IdeType` defaulting to `IdeType.VisualStudio`.
- `VSLXshared\DataModel\VsFolder.cs` — Add `VsCodeOptions`, deep-clone it, count `VsCodeProject`, add `IsWorkspaceBacked()`.
- `VSLXshared\DataModel\LaunchInfo.cs` — Add `IdeDefaultTarget[]? DefaultTargets`.
- `VSLXshared\DataModel\VisualStudioInstance.cs` — Implement `IIdeInstance`.
- `VSLXshared\DataModel\FileIcons.cs` — Register `.code-workspace` icon lookup and VS Code fallback icons.
- `VSLXshared\Helpers\FileHelper.cs` — Add `.code-workspace` filter and register new serialized types in `VsSerializationBinder`.
- `VSLXshared\Helpers\ImportHelper.cs` — Map `.code-workspace` extension to `VsCodeProject`.
- `BackgroundLaunch\BackgroundLaunch.csproj` — Add `InternalsVisibleTo` for `BackgroundLaunch.Tests`.
- `BackgroundLaunch\Runner.cs` — Resolve via `IdeManager`, handle workspace-backed `VsFolder` without recursion, directory working paths, `--wait`, and mixed-IDE skipping.
- `VSLauncherX\Helpers\ItemLauncher.cs` — Accept per-IDE `IdeDefaultTarget[]` defaults and optional legacy `Target`.
- `VSLauncherX\Controls\VisualStudioCombobox.cs` — Delegate/specialize cleanly via `IdeCombobox` to avoid duplicate owner-draw logic.
- `VSLauncherX\Helpers\ColumnHelper.cs` — Icons, aspect labels, and descriptions for `VsCodeProject` and workspace-backed `VsFolder`.
- `VSLauncherX\Helpers\ThemeHelper.cs` — Style `IdeCombobox` and owner-drawn `TabControl`/`TabPage` for Light and Dark modes.
- `VSLauncherX\Properties\Settings.settings` / `Settings.Designer.cs` — Persist `SelectedVsCodeEdition` and `SelectedIdeTab`.
- `VSLauncherX\Forms\dlgImportFolder.cs` / `.Designer.cs` — Delegate scanning to `ProjectScanner`; update checkbox text to cover Solutions & Workspaces.
- `VSLauncherX\Forms\dlgImportVisualStudio.cs` — Use `IdeManager.GetAllRecentProjects` and disable VS-only checkbox when Visual Studio is not installed.
- `VSLauncherX\MainDialog.Designer.cs` / `MainDialog.cs` / `MainDialog.Utilities.cs` — Tabbed right sidebar (`tabControlIdes`), single-IDE disabling, VS Code quick actions, tab-aware folder Settings dispatch, `"Open in VS Code"` direct launch, directory-aware Git/Explorer, `.code-workspace` drag-and-drop, and `IdeManager`-based Taskbar JumpList.

---

### Task 1: Test Project Scaffolding, Multi-IDE Data Model, and Safe Serialization

**Files:**
- Create: `VSLXshared.Tests\VSLXshared.Tests.csproj`
- Create: `VSLXshared\DataModel\IdeType.cs`
- Create: `VSLXshared\DataModel\VsCodeProjectKind.cs`
- Create: `VSLXshared\DataModel\VsCodeWindowMode.cs`
- Create: `VSLXshared\DataModel\VsCodeLaunchOptions.cs`
- Create: `VSLXshared\DataModel\VsCodeProject.cs`
- Create: `VSLXshared\DataModel\IdeDefaultTarget.cs`
- Modify: `VSLauncher.sln`
- Modify: `VSLXshared\DataModel\VsItem.cs:20-134`
- Modify: `VSLXshared\DataModel\VsFolder.cs:8-243`
- Modify: `VSLXshared\DataModel\LaunchInfo.cs:1-18`
- Modify: `VSLXshared\Helpers\FileHelper.cs:27-75`
- Modify: `VSLXshared\Helpers\ImportHelper.cs:18-56`
- Test: `VSLXshared.Tests\DataModel\VsCodeProjectTests.cs`
- Test: `VSLXshared.Tests\DataModel\VsFolderWorkspaceTests.cs`
- Test: `VSLXshared.Tests\Helpers\SafeSerializationTests.cs`

**Interfaces:**
- Consumes: Existing `VsItem`, `VsFolder`, `VsSolution`, `VsProject`, `LaunchInfo`, `FileHelper.CreateSafeJsonSettings`.
- Produces:
  - `enum IdeType { VisualStudio = 0, VsCode = 1 }` (`VSLauncher.DataModel`)
  - `enum VsCodeProjectKind { Workspace, Folder, NodeJs, Python, Rust, Go, Java, CMake, Flutter, Php, Generic }`
  - `enum VsCodeWindowMode { Default, NewWindow, ReuseWindow }`
  - `class VsCodeLaunchOptions` (`Edition`, `Profile`, `WindowMode`, `DisableExtensions`, `Commands`, `Clone()`)
  - `class VsCodeProject : VsItem` (`ProjectKind`, `ManifestFile`, `WindowMode`, `DisableExtensions`, `IsDirectoryTarget`, `Refresh()`, `TypeAsName()`)
  - `class IdeDefaultTarget` (`IdeType IdeType`, `string TargetIdentifier`)
  - `VsItem.IdeType` (default `IdeType.VisualStudio`)
  - `VsFolder.VsCodeOptions`, `VsFolder.IsWorkspaceBacked()`, updated `VsFolder.ContainedProjectsCount()`
  - `LaunchInfo.DefaultTargets` (`IdeDefaultTarget[]?`)
  - `ImportHelper.GetItemFromExtension` handling `.code-workspace`

- [ ] **Step 1: Scaffold `VSLXshared.Tests` and add it to `VSLauncher.sln`**

Create `VSLXshared.Tests\VSLXshared.Tests.csproj` targeting `net10.0-windows` (`<UseWindowsForms>true</UseWindowsForms>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<IsPackable>false</IsPackable>`) referencing `..\VSLXshared\VSLXshared.csproj` and packages `Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`, `Moq`, and `Shouldly`. Register the project in `VSLauncher.sln` via `dotnet sln VSLauncher.sln add VSLXshared.Tests\VSLXshared.Tests.csproj`.

- [ ] **Step 2: Write the failing unit tests for `VsCodeProject`, `VsFolder.IsWorkspaceBacked`, `ImportHelper`, and `VsSerializationBinder`**

Create `VSLXshared.Tests\DataModel\VsCodeProjectTests.cs`, `VSLXshared.Tests\DataModel\VsFolderWorkspaceTests.cs`, and `VSLXshared.Tests\Helpers\SafeSerializationTests.cs` (block-scoped namespaces, fresh SUT per test):

```csharp
[Test]
public void Refresh_WhenWorkspaceFileExists_ClearsWarningAndMarksNonDirectoryTarget()
{
    string file = Path.Combine(tempDir, "demo.code-workspace");
    File.WriteAllText(file, "{}");

    var project = new VsCodeProject("demo", file, VsCodeProjectKind.Workspace);

    project.IdeType.ShouldBe(IdeType.VsCode);
    project.ItemType.ShouldBe(ItemTypeEnum.Project);
    project.IsDirectoryTarget.ShouldBeFalse();
    project.Warning.ShouldBeFalse();
    project.TypeAsName().ShouldBe("VS Code Workspace");
}

[Test]
public void Refresh_WhenManifestDirectoryExists_ClearsWarningAndMarksDirectoryTarget()
{
    string manifest = Path.Combine(tempDir, "package.json");
    File.WriteAllText(manifest, "{}");

    var project = new VsCodeProject("web", tempDir, VsCodeProjectKind.NodeJs, "package.json");

    project.IsDirectoryTarget.ShouldBeTrue();
    project.Warning.ShouldBeFalse();
    project.TypeAsName().ShouldBe("Node.js");
}

[Test]
public void IsWorkspaceBacked_TrueOnlyForDiskBackedFolderWithCodeWorkspaceAndFolderItemType()
{
    string wsFile = Path.Combine(tempDir, "app.code-workspace");
    File.WriteAllText(wsFile, "{}");

    var diskFolder = new VsFolder("app", tempDir);
    var virtualFolder = new VsFolder("group", string.Empty);
    var mruOpenFolder = new VsFolder("app", tempDir) { ItemType = ItemTypeEnum.Project };

    diskFolder.IsWorkspaceBacked().ShouldBeTrue();
    virtualFolder.IsWorkspaceBacked().ShouldBeFalse();
    mruOpenFolder.IsWorkspaceBacked().ShouldBeFalse();
}

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

    var settings = FileHelper.CreateSafeJsonSettings();
    var roundTripped = JsonConvert.DeserializeObject<LaunchInfo>(JsonConvert.SerializeObject(launch, settings), settings);

    roundTripped.ShouldNotBeNull();
    roundTripped.DefaultTargets!.Single().TargetIdentifier.ShouldBe("vscode-stable-user");
    roundTripped.Solution!.VsCodeOptions!.Profile.ShouldBe("Dev");
    var rtProject = roundTripped.Solution.Items.Single().ShouldBeOfType<VsCodeProject>();
    rtProject.WindowMode.ShouldBe(VsCodeWindowMode.NewWindow);
    rtProject.DisableExtensions.ShouldBeTrue();
    ImportHelper.GetItemFromExtension("demo", "C:\\repo\\demo.code-workspace").ShouldBeOfType<VsCodeProject>();
}
```

Also test `VsFolder.Clone(true)` deep-copies `VsCodeOptions` and `VsFolder.ContainedProjectsCount()` counts `VsCodeProject` items.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj`
Expected: FAIL with compile errors (`IdeType`, `VsCodeProject`, `VsCodeLaunchOptions`, `IdeDefaultTarget`, `IsWorkspaceBacked` not defined).

- [ ] **Step 4: Implement model types, `VsFolder` extensions, `LaunchInfo.DefaultTargets`, `ImportHelper`, and `VsSerializationBinder`**

- Create `IdeType.cs`, `VsCodeProjectKind.cs`, `VsCodeWindowMode.cs`, `IdeDefaultTarget.cs`, `VsCodeLaunchOptions.cs` (with `Clone()`), and `VsCodeProject.cs` (`Refresh()`, `IsDirectoryTarget`, `TypeAsName()` returning `"VS Code Workspace"`, `"Node.js"`, `"Python"`, `"Rust"`, `"Go"`, `"Java"`, `"CMake"`, `"Flutter"`, `"PHP"`, `"VS Code Folder"`).
- Add `public IdeType IdeType { get; set; } = IdeType.VisualStudio;` to `VsItem.cs`.
- Update `VsFolder.cs`: add `VsCodeLaunchOptions? VsCodeOptions`, deep-copy `VsCodeOptions` in `Clone(bool)`, include `i is VsProject or VsCodeProject` in `ContainedProjectsCount()`, and implement `public bool IsWorkspaceBacked()` checking `ItemType == ItemTypeEnum.Folder`, non-empty existing directory `Path`, and either a direct `VsCodeProject` child with `ProjectKind == VsCodeProjectKind.Workspace` or `Directory.EnumerateFiles(Path, "*.code-workspace", SearchOption.TopDirectoryOnly).Any()` (catching IO/UnauthorizedAccess exceptions cleanly).
- Update `LaunchInfo.cs` to add `public IdeDefaultTarget[]? DefaultTargets { get; set; }`.
- Update `FileHelper.cs`: add `"VS Code Workspaces (*.code-workspace)|*.code-workspace"` to `SolutionFilterString` and register `IdeType`, `VsCodeProject`, `VsCodeProjectKind`, `VsCodeWindowMode`, `VsCodeLaunchOptions`, `IdeDefaultTarget`, and `IdeDefaultTarget[]` in `VsSerializationBinder.AllowedTypes`.
- Update `ImportHelper.GetItemFromExtension` to return `new VsCodeProject(name, path, VsCodeProjectKind.Workspace)` when extension is `.code-workspace` (and ensure `.slnx` also returns `VsSolution` if not already mapped).

- [ ] **Step 5: Run tests and build solution projects to verify pass**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj` and `dotnet build VSLauncherX\VSLauncherX.csproj`
Expected: All tests PASS; 0 build errors.

- [ ] **Step 6: Commit**

```bash
git add VSLauncher.sln VSLXshared VSLXshared.Tests
git commit -m "feat: add multi-IDE data models, VsCodeProject, and safe serialization tests"
```

---

### Task 2: IDE Provider Interfaces, `VisualStudioIdeProvider`, and `IdeManager`

**Files:**
- Create: `VSLXshared\Ide\IIdeInstance.cs`
- Create: `VSLXshared\Ide\IIdeProvider.cs`
- Create: `VSLXshared\Ide\VisualStudioIdeProvider.cs`
- Create: `VSLXshared\Ide\IdeManager.cs`
- Modify: `VSLXshared\DataModel\VisualStudioInstance.cs:20-287`
- Test: `VSLXshared.Tests\Ide\VisualStudioIdeProviderTests.cs`
- Test: `VSLXshared.Tests\Ide\IdeManagerTests.cs`

**Interfaces:**
- Consumes: Task 1 (`IdeType`, `VsItem`, `VsFolder`, `VsSolution`, `VsCodeProject`, `VsCodeLaunchOptions`), existing `VisualStudioInstance`, `VisualStudioInstanceManager`.
- Produces:
  - `interface IIdeInstance` (`IdeType`, `Name`, `ShortName`, `Version`, `Location`, `Identifier`, `AppIcon`, `List<string> GetProfiles()`)
  - `interface IIdeProvider` (`IdeType`, `DisplayName`, `DownloadUrl`, `IReadOnlyList<IIdeInstance> Instances`, `bool IsInstalled`, `IIdeInstance? DefaultInstance`, `IIdeInstance? FindByIdentifier(string?)`, `IIdeInstance? FindByVersion(string?)`, `VsItemList GetRecentProjects(bool onlyDefaultProfiles)`, `ProcessStartInfo? BuildStartInfo(VsItem item, IIdeInstance instance, bool forceAdmin = false, bool waitForExit = false)`)
  - `class VisualStudioIdeProvider : IIdeProvider`
  - `class IdeManager` (constructors `IdeManager()` and `IdeManager(IEnumerable<IIdeProvider> providers)`; `GetProvider(IdeType)`, `GetInstances(IdeType)`, `HasInstalled(IdeType)`, `bool AnyInstalled`, `VsItemList GetAllRecentProjects(bool onlyDefaultProfiles)`, `IIdeInstance? ResolveInstance(VsItem item, IReadOnlyDictionary<IdeType, string>? defaultTargetIdentifiers = null)`)

- [ ] **Step 1: Write failing unit tests for `VisualStudioIdeProvider` and `IdeManager`**

Create `VSLXshared.Tests\Ide\VisualStudioIdeProviderTests.cs` and `VSLXshared.Tests\Ide\IdeManagerTests.cs`:

```csharp
[Test]
public void BuildStartInfo_BuildsDevenvArgumentsWithSplashAndRootSuffix()
{
    var vs = new VisualStudioInstance("Visual Studio 2022", "17.10.0", @"C:\VS\devenv.exe", "abc123", "2022");
    var provider = new VisualStudioIdeProvider(new[] { vs });
    var sln = new VsSolution { Name = "App", Path = @"C:\src\App.sln", Instance = "Exp", Commands = "/SafeMode", ShowSplash = false, RunAsAdmin = true };

    ProcessStartInfo? psi = provider.BuildStartInfo(sln, vs);

    psi.ShouldNotBeNull();
    psi.FileName.ShouldBe(@"C:\VS\devenv.exe");
    psi.Verb.ShouldBe("runas");
    psi.UseShellExecute.ShouldBeTrue();
    psi.Arguments.ShouldBe("\"C:\\src\\App.sln\" /SafeMode /NoSplash /RootSuffix Exp");
}

[Test]
public void ResolveInstance_FollowsExplicitIdThenSolutionRequiredVersionThenDefaultTargetThenProviderDefault()
{
    var vs2019 = CreateMockInstance(IdeType.VisualStudio, "vs19", "16.11");
    var vs2022 = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
    var vsProvider = CreateMockProvider(IdeType.VisualStudio, vs2022, vs2019, vs2022);
    vsProvider.Setup(p => p.FindByVersion("16")).Returns(vs2019);

    var manager = new IdeManager(new[] { vsProvider.Object });
    var pinned = new VsSolution { VsVersion = "vs19", RequiredVersion = "17" };
    var byReq = new VsSolution { RequiredVersion = "16" };
    var bySidebar = new VsProject();
    var defaults = new Dictionary<IdeType, string> { [IdeType.VisualStudio] = "vs19" };

    manager.ResolveInstance(pinned, defaults)!.Identifier.ShouldBe("vs19");
    manager.ResolveInstance(byReq, defaults)!.Identifier.ShouldBe("vs19");
    manager.ResolveInstance(bySidebar, defaults)!.Identifier.ShouldBe("vs19");
    manager.ResolveInstance(bySidebar, null)!.Identifier.ShouldBe("vs22");
}

[Test]
public void ResolveInstance_WhenOnlyVsCodeInstalled_FallsBackVisualStudioSolutionToVsCode()
{
    var vsProvider = CreateMockProvider(IdeType.VisualStudio, defaultInstance: null);
    var code = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
    var codeProvider = CreateMockProvider(IdeType.VsCode, code, code);

    var manager = new IdeManager(new[] { vsProvider.Object, codeProvider.Object });
    var sln = new VsSolution { Path = @"C:\src\App.sln" };

    IIdeInstance? resolved = manager.ResolveInstance(sln);

    resolved.ShouldNotBeNull();
    resolved.IdeType.ShouldBe(IdeType.VsCode);
    resolved.Identifier.ShouldBe("vscode-stable-user");
}

[Test]
public void ResolveInstance_WhenWorkspaceBackedFolderAndVsCodeInstalled_ResolvesVsCodeEdition()
{
    string wsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(wsDir);
    File.WriteAllText(Path.Combine(wsDir, "app.code-workspace"), "{}");

    try
    {
        var vs = CreateMockInstance(IdeType.VisualStudio, "vs22", "17.10");
        var codeStable = CreateMockInstance(IdeType.VsCode, "vscode-stable-user", "1.95.0");
        var codeInsiders = CreateMockInstance(IdeType.VsCode, "vscode-insiders-user", "1.96.0");
        var manager = new IdeManager(new[]
        {
            CreateMockProvider(IdeType.VisualStudio, vs, vs).Object,
            CreateMockProvider(IdeType.VsCode, codeStable, codeStable, codeInsiders).Object
        });
        var folder = new VsFolder("app", wsDir) { VsCodeOptions = new VsCodeLaunchOptions { Edition = "vscode-insiders-user" } };

        manager.ResolveInstance(folder)!.Identifier.ShouldBe("vscode-insiders-user");
    }
    finally
    {
        Directory.Delete(wsDir, true);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj`
Expected: FAIL with compile errors (`IIdeInstance`, `IIdeProvider`, `VisualStudioIdeProvider`, `IdeManager` not defined).

- [ ] **Step 3: Implement `IIdeInstance`, `IIdeProvider`, `VisualStudioIdeProvider`, `IdeManager`, and update `VisualStudioInstance`**

- Make `VisualStudioInstance` implement `IIdeInstance` (`IdeType => IdeType.VisualStudio`, `GetProfiles() => GetInstances()`).
- Implement `VisualStudioIdeProvider` with a default constructor wrapping `VisualStudioInstanceManager` and an internal/public test constructor accepting `IEnumerable<VisualStudioInstance>` + optional `Func<bool, VsItemList>` MRU loader. `BuildStartInfo` builds exact `devenv.exe` args (`"<path>"`, optional `Commands`, `/NoSplash` when `!ShowSplash`, `/RootSuffix <instance>` quoted when containing spaces).
- Implement `IdeManager` following the exact 5-step resolution precedence in spec Section 5.1 (including single-IDE fallback when Visual Studio is not installed and VS Code is installed, and workspace-backed `VsFolder` resolving against `IdeType.VsCode` using `folder.VsCodeOptions?.Edition`). Keep parameterless `IdeManager()` registering `VisualStudioIdeProvider` now (Task 3 adds `VsCodeIdeProvider` to it).

- [ ] **Step 4: Run tests and verify pass**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 5: Commit**

```bash
git add VSLXshared VSLXshared.Tests
git commit -m "feat: add IIdeProvider, IIdeInstance, VisualStudioIdeProvider, and IdeManager"
```

---

### Task 3: `VsCodeInstance` and `VsCodeIdeProvider` — Discovery, Profiles, MRU (`state.vscdb`), and CLI Building

**Files:**
- Modify: `VSLXshared\VSLXshared.csproj` (add `Microsoft.Data.Sqlite`)
- Create: `VSLXshared\Ide\IVsCodeEnvironment.cs`
- Create: `VSLXshared\Ide\WindowsVsCodeEnvironment.cs`
- Create: `VSLXshared\Ide\VsCodeInstance.cs`
- Create: `VSLXshared\Ide\VsCodeIdeProvider.cs`
- Modify: `VSLXshared\Ide\IdeManager.cs` (register `VsCodeIdeProvider` in default constructor)
- Test: `VSLXshared.Tests\Ide\VsCodeIdeProviderDiscoveryTests.cs`
- Test: `VSLXshared.Tests\Ide\VsCodeIdeProviderMruTests.cs`
- Test: `VSLXshared.Tests\Ide\VsCodeIdeProviderLaunchTests.cs`

**Interfaces:**
- Consumes: Task 1 (`VsCodeProject`, `VsCodeProjectKind`, `VsCodeWindowMode`, `VsCodeLaunchOptions`), Task 2 (`IIdeInstance`, `IIdeProvider`, `IdeManager`).
- Produces:
  - `interface IVsCodeEnvironment` (environment folders, file/directory existence, file text reading, file version lookup, registry uninstall locations, `PATH` lookup)
  - `class VsCodeInstance : IIdeInstance` (adds `UserDataDirectory`, `IsInsiders`, `Execute()`, `ExecuteAsAdmin()`, `ExecuteNewWindow()`, `ExecuteWithoutExtensions()`, `OpenPath(string targetPath)`)
  - `class VsCodeIdeProvider : IIdeProvider` (constructors `VsCodeIdeProvider()` and `VsCodeIdeProvider(IVsCodeEnvironment environment)`)

- [ ] **Step 1: Add `Microsoft.Data.Sqlite` to `VSLXshared\VSLXshared.csproj`**

Run: `dotnet add VSLXshared\VSLXshared.csproj package Microsoft.Data.Sqlite`

- [ ] **Step 2: Write failing unit tests for discovery, profile parsing, `state.vscdb` MRU, and CLI / `--wait` building**

Create the three test files in `VSLXshared.Tests\Ide\` covering:
- **Discovery (`VsCodeIdeProviderDiscoveryTests`)**:
  - Detects User Stable, System Stable, User Insiders, System Insiders; deduplicates identical canonical paths; labels `"VS Code (User)"` / `"VS Code (System)"` when both scopes exist for Stable, vs `"Visual Studio Code"` when only one exists; discovers via Registry Uninstall fallback and `PATH` `code.cmd` -> sibling `..\Code.exe` fallback; reads version from `ProductVersion` or fallback `resources\app\package.json`.
- **Profiles & MRU (`VsCodeIdeProviderMruTests` — pins Review Focus #1 & #3)**:
  - Parses valid `storage.json` `userDataProfiles` into `["<default>", "Work", "OSS"]`.
  - Corrupt or missing `storage.json` returns `["<default>"]` without throwing.
  - Real temporary SQLite `state.vscdb` containing `ItemTable` (`key = 'history.recentlyOpenedPathsList'`) with:
    - `workspace.configPath`: `"file:///c%3A/Users/Me/My%20Apps/demo.code-workspace"` -> decoded `VsCodeProject` (`Workspace`, path normalized to `C:\Users\Me\My Apps\demo.code-workspace`, `Warning` reflects existence).
    - `folderUri`: existing local directory URI -> `VsCodeProject` (`Folder` or manifest-refined kind, `Warning == false`).
    - `folderUri` with `remoteAuthority: "wsl+ubuntu"`, `vscode-remote://...`, malformed URI, and standalone `fileUri` -> all skipped cleanly without throwing.
  - Missing `state.vscdb` returns empty list; locked `state.vscdb` (held open exclusively or simulated lock) falls back to copy-to-temp read and deletes temp directory afterwards.
- **Launch & CLI (`VsCodeIdeProviderLaunchTests` — pins Review Focus #5 cross-IDE stripping)**:
  - `VsCodeProject` with `Instance = "Work"`, `WindowMode = NewWindow`, `DisableExtensions = true`, `Commands = "--goto a.ts:1"` -> `Code.exe` args `"<path>" --profile "Work" --new-window --disable-extensions --goto a.ts:1`.
  - `VsFolder` with `VsCodeOptions` -> uses `folder.VsCodeOptions` (NOT `folder.Instance` / `folder.Commands`).
  - `VsSolution` (`IdeType == VisualStudio`) with `Instance = "Exp"` and `Commands = "/SafeMode"` -> targets `Path.GetDirectoryName(sln.Path)` and strips `--profile` and `/SafeMode`.
  - Waiting triggered when `item.WaitForCompletion || item.RunAfter?.WaitForCompletion == true` and not elevated: sets `UseShellExecute = false`, `Environment["ELECTRON_RUN_AS_NODE"] = "1"`, and prepends `"<installDir>\resources\app\out\cli.js"` with `--wait` when `cli.js` exists; falls back to direct `Code.exe --wait` when elevated or when `cli.js` is absent.
  - Direct quick actions on `VsCodeInstance` build expected args (`""`, `runas`, `--new-window`, `--disable-extensions`, `"<path>"`).

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj --filter "FullyQualifiedName~VsCode"`
Expected: FAIL with compile errors (`IVsCodeEnvironment`, `VsCodeInstance`, `VsCodeIdeProvider` not defined).

- [ ] **Step 4: Implement `IVsCodeEnvironment`, `WindowsVsCodeEnvironment`, `VsCodeInstance`, and `VsCodeIdeProvider`, and register in `IdeManager`**

- Implement `VsCodeIdeProvider` discovery in the exact 6-step order from spec Section 5.3 with canonical-path deduplication (`Path.GetFullPath`).
- Implement `VsCodeInstance.GetProfiles()` reading `<UserDataDirectory>\User\globalStorage\storage.json` (`userDataProfiles[].name`).
- Implement `VsCodeIdeProvider.GetRecentProjects(bool onlyDefaultProfiles)` reading `state.vscdb` (`Data Source=...;Mode=ReadOnly`) with copy-to-temp fallback (`state.vscdb`, `-wal`, `-shm`) on `SqliteException`/`IOException`, decoding Windows `file://` URIs via `Uri.TryCreate` + `Uri.UnescapeDataString` + leading-slash cleanup (`file:///c%3A/...` -> `C:\...`), skipping `remoteAuthority`/non-`file://`/`fileUri`, and omitting empty edition groups.
- Implement `VsCodeIdeProvider.BuildStartInfo` with cross-IDE option stripping and `ELECTRON_RUN_AS_NODE=1` + `resources\app\out\cli.js --wait` handling.
- Register `new VsCodeIdeProvider()` alongside `new VisualStudioIdeProvider()` in `IdeManager`'s parameterless constructor.

- [ ] **Step 5: Run all tests to verify pass**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add VSLXshared VSLXshared.Tests
git commit -m "feat: implement VsCodeInstance and VsCodeIdeProvider with discovery, profiles, MRU, and CLI launch"
```

---

### Task 4: Multi-IDE `ProjectScanner` and `dlgImportFolder` Integration

**Files:**
- Create: `VSLXshared\Helpers\ProjectScanner.cs`
- Modify: `VSLauncherX\Forms\dlgImportFolder.cs:22-255`
- Modify: `VSLauncherX\Forms\dlgImportFolder.Designer.cs` (update `chkSolutionOnly` label text to `"Solutions & Workspaces only"`)
- Test: `VSLXshared.Tests\Helpers\ProjectScannerTests.cs`

**Interfaces:**
- Consumes: Task 1 (`VsFolder`, `VsSolution`, `VsProject`, `VsCodeProject`, `VsCodeProjectKind`, `ImportHelper`).
- Produces:
  - `class ProjectScanner` (`public VsFolder Scan(string folderPath, bool onlySolutions, bool flat)`, `public static VsCodeProjectKind? DetectDirectoryManifest(string directoryPath, out string? manifestFile)`)

- [ ] **Step 1: Write failing unit tests for `ProjectScanner` (pins Review Focus #4)**

Create `VSLXshared.Tests\Helpers\ProjectScannerTests.cs`:

```csharp
[Test]
public void Scan_WhenDirectoryHasPackageJsonAndNestedPackageJson_EmitsRootOnlyAndStopsRecursing()
{
    string appDir = CreateDir(tempDir, "my-app");
    File.WriteAllText(Path.Combine(appDir, "package.json"), "{}");
    string nested = CreateDir(appDir, "sub-pkg");
    File.WriteAllText(Path.Combine(nested, "package.json"), "{}");
    string nodeModules = CreateDir(appDir, "node_modules", "dep");
    File.WriteAllText(Path.Combine(nodeModules, "package.json"), "{}");

    var scanner = new ProjectScanner();
    VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

    // Tree shape: leaf VsCodeProject directly under parent (no my-app > my-app nesting)
    var app = root.Items.Single().ShouldBeOfType<VsCodeProject>();
    app.Name.ShouldBe("my-app");
    app.Path.ShouldBe(appDir);
    app.ProjectKind.ShouldBe(VsCodeProjectKind.NodeJs);
    app.ManifestFile.ShouldBe("package.json");
}

[Test]
public void Scan_WhenDirectoryHasCodeWorkspaceAndManifestAndSubfolderSln_OmitsDuplicateManifestAndFindsNestedSln()
{
    string repo = CreateDir(tempDir, "repo");
    File.WriteAllText(Path.Combine(repo, "repo.code-workspace"), "{}");
    File.WriteAllText(Path.Combine(repo, "package.json"), "{}");
    string src = CreateDir(repo, "src");
    File.WriteAllText(Path.Combine(src, "App.sln"), "Microsoft Visual Studio Solution File");

    var scanner = new ProjectScanner();
    VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

    var repoFolder = root.Items.Single().ShouldBeOfType<VsFolder>();
    repoFolder.Items.OfType<VsCodeProject>().Count().ShouldBe(1);
    repoFolder.Items.OfType<VsCodeProject>().Single().ProjectKind.ShouldBe(VsCodeProjectKind.Workspace);
    repoFolder.ContainedSolutionsCount().ShouldBe(1);
}

[Test]
public void Scan_WhenOnlySolutionsTrue_IncludesSlnSlnxAndCodeWorkspaceOnly()
{
    File.WriteAllText(Path.Combine(tempDir, "A.sln"), "");
    File.WriteAllText(Path.Combine(tempDir, "B.slnx"), "<Solution />");
    File.WriteAllText(Path.Combine(tempDir, "C.code-workspace"), "{}");
    File.WriteAllText(Path.Combine(tempDir, "D.csproj"), "<Project />");
    string nodeDir = CreateDir(tempDir, "web");
    File.WriteAllText(Path.Combine(nodeDir, "package.json"), "{}");

    var scanner = new ProjectScanner();
    VsFolder root = scanner.Scan(tempDir, onlySolutions: true, flat: true);

    root.Items.Select(i => i.Name).ShouldBe(new[] { "A", "B", "C" }, ignoreOrder: true);
}

[Test]
public void Scan_DetectsVsCodeFolderWithoutRecursingIntoDotVsCodeAndSkipsNoiseDirectories()
{
    string dotVsCodeProj = CreateDir(tempDir, "tool");
    CreateDir(dotVsCodeProj, ".vscode");
    File.WriteAllText(Path.Combine(dotVsCodeProj, ".vscode", "package.json"), "{}");
    string binDir = CreateDir(tempDir, "bin", "nested");
    File.WriteAllText(Path.Combine(binDir, "Cargo.toml"), "");
    string vsOnlyDir = CreateDir(tempDir, "legacy");
    File.WriteAllText(Path.Combine(vsOnlyDir, "Legacy.sln"), "");

    var scanner = new ProjectScanner();
    VsFolder root = scanner.Scan(tempDir, onlySolutions: false, flat: false);

    root.Items.OfType<VsCodeProject>().Select(p => (p.Name, p.ProjectKind)).ShouldBe(new[] { ("tool", VsCodeProjectKind.Folder) });
    root.Items.OfType<VsFolder>().Single().Name.ShouldBe("legacy");
}
```

Also add a test verifying manifest priority order (`package.json` > `pyproject.toml` > `Cargo.toml` > `go.mod` > `pom.xml`/`build.gradle`/`build.gradle.kts` > `CMakeLists.txt` > `pubspec.yaml` > `composer.json` > `.vscode`).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj --filter "FullyQualifiedName~ProjectScanner"`
Expected: FAIL (`ProjectScanner` not defined).

- [ ] **Step 3: Implement `ProjectScanner` and wire `dlgImportFolder` to it**

- Implement `VSLXshared\Helpers\ProjectScanner.cs` following the exact 3-step algorithm in spec Section 5.5 (cycle guard via `HashSet<string>`, noise directory exclusion set `{ "node_modules", "bin", "obj", "dist", "build", "target", "venv", "__pycache__", "packages", "TestResults" }`, skip dot-directories and hidden/reparse points, check `.vscode` existence without recursing into it, stop recursing deeper once a leaf manifest/`.vscode/` is found, emit sole manifest project directly as leaf in tree mode).
- Update `VSLauncherX\Forms\dlgImportFolder.cs` to replace `IterateFolder`/`IterateFolderInternal`/`IsOfInterest` with `new ProjectScanner().Scan(folderPath, bOnlySolutions, bFlat)` and update `chkSolutionOnly.Text` to `"Solutions & Workspaces only"`.

- [ ] **Step 4: Run tests and build `VSLauncherX`**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj` and `dotnet build VSLauncherX\VSLauncherX.csproj`
Expected: All tests PASS; 0 build errors.

- [ ] **Step 5: Commit**

```bash
git add VSLXshared VSLXshared.Tests VSLauncherX\Forms\dlgImportFolder.cs VSLauncherX\Forms\dlgImportFolder.Designer.cs
git commit -m "feat: add multi-IDE ProjectScanner and integrate into dlgImportFolder"
```

---

### Task 5: Multi-IDE Background Launch Pipeline (`BackgroundLaunch` + `ItemLauncher`)

**Files:**
- Create: `BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj`
- Modify: `VSLauncher.sln`
- Modify: `BackgroundLaunch\BackgroundLaunch.csproj` (add `InternalsVisibleTo` for `BackgroundLaunch.Tests`)
- Modify: `BackgroundLaunch\Runner.cs:16-355`
- Modify: `VSLauncherX\Helpers\ItemLauncher.cs:20-137`
- Test: `BackgroundLaunch.Tests\RunnerTests.cs`

**Interfaces:**
- Consumes: Task 1 (`LaunchInfo.DefaultTargets`, `VsFolder.IsWorkspaceBacked`, `VsCodeProject`), Task 2 (`IdeManager`, `IIdeProvider`), Task 3 (`VsCodeIdeProvider`).
- Produces:
  - `Runner(LaunchInfo item, IdeManager? ideManager = null, Func<ProcessStartInfo, Process?>? processStarter = null)`
  - `ItemLauncher(VsItem item, IdeDefaultTarget[]? defaultTargets, string? legacyTarget = null)` and `ItemLauncher(VsFolder folder, IdeDefaultTarget[]? defaultTargets, string? legacyTarget = null)`

- [ ] **Step 1: Scaffold `BackgroundLaunch.Tests`, add `InternalsVisibleTo`, and write failing tests for `Runner` (pins Review Focus #5)**

Add `BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj` (`net10.0-windows7.0`, WinForms, NUnit + Moq + Shouldly, referencing `..\BackgroundLaunch\BackgroundLaunch.csproj` and `..\VSLXshared\VSLXshared.csproj`), add to `VSLauncher.sln`, and add `<InternalsVisibleTo Include="BackgroundLaunch.Tests" />` in `BackgroundLaunch\BackgroundLaunch.csproj`.

Create `BackgroundLaunch.Tests\RunnerTests.cs`:

```csharp
[Test]
public void RunAll_NestedWorkspaceBackedFolder_OpensInVsCodeAndDoesNotRecurseIntoChildren()
{
    string wsDir = CreateWorkspaceDir(tempDir, "ws-app");
    var launched = new List<ProcessStartInfo>();
    var codeProvider = CreateCapturingVsCodeProvider(out var codeInstance);
    var vsProvider = CreateCapturingVsProvider(out _);

    var outerGroup = new VsFolder("Outer", string.Empty);
    var wsFolder = new VsFolder("ws-app", wsDir);
    wsFolder.Items.Add(new VsSolution { Name = "ChildSln", Path = Path.Combine(wsDir, "Child.sln") });
    outerGroup.Items.Add(wsFolder);

    var runner = new Runner(
        new LaunchInfo { Solution = outerGroup },
        new IdeManager(new[] { vsProvider, codeProvider }),
        psi => { launched.Add(psi); return null; });

    runner.Run();

    launched.Count.ShouldBe(1);
    launched[0].FileName.ShouldBe(codeInstance.Location);
    launched[0].Arguments.ShouldContain($"\"{wsDir}\"");
}

[Test]
public void RunAll_MixedIdeGroupWhenVsCodeMissing_SkipsVsCodeChildAndLaunchesVisualStudioChild()
{
    string slnPath = Path.Combine(tempDir, "App.sln");
    File.WriteAllText(slnPath, "");
    var launched = new List<ProcessStartInfo>();
    var vsProvider = CreateCapturingVsProvider(out var vsInstance);
    var emptyCodeProvider = new Mock<IIdeProvider>();
    emptyCodeProvider.SetupGet(p => p.IdeType).Returns(IdeType.VsCode);
    emptyCodeProvider.SetupGet(p => p.IsInstalled).Returns(false);

    var group = new VsFolder("Mixed", string.Empty);
    group.Items.Add(new VsCodeProject("web", tempDir, VsCodeProjectKind.Folder));
    group.Items.Add(new VsSolution { Name = "App", Path = slnPath });

    var runner = new Runner(
        new LaunchInfo { Solution = group },
        new IdeManager(new[] { vsProvider, emptyCodeProvider.Object }),
        psi => { launched.Add(psi); return null; });

    runner.Run();

    launched.Count.ShouldBe(1);
    launched[0].FileName.ShouldBe(vsInstance.Location);
}

[Test]
public void RunAll_MixedIdeGroupUsesPerIdeDefaultTargetsAndSetsDirectoryWorkingPath()
{
    string slnPath = Path.Combine(tempDir, "App.sln");
    File.WriteAllText(slnPath, "");
    string webDir = Path.Combine(tempDir, "web");
    Directory.CreateDirectory(webDir);
    var launched = new List<ProcessStartInfo>();
    var vs1 = new VisualStudioInstance("VS 2019", "16.11.0", @"C:\VS19\devenv.exe", "vs19", "2019");
    var vs2 = new VisualStudioInstance("VS 2022", "17.10.0", @"C:\VS22\devenv.exe", "vs22", "2022");
    var codeStable = CreateTestVsCodeInstance("vscode-stable-user", @"C:\Code\Code.exe");
    var codeInsiders = CreateTestVsCodeInstance("vscode-insiders-user", @"C:\CodeInsiders\Code - Insiders.exe");

    var group = new VsFolder("Mixed", string.Empty);
    group.Items.Add(new VsSolution { Name = "App", Path = slnPath });
    group.Items.Add(new VsCodeProject("web", webDir, VsCodeProjectKind.NodeJs));

    var launchInfo = new LaunchInfo
    {
        Solution       = group,
        DefaultTargets = new[]
        {
            new IdeDefaultTarget(IdeType.VisualStudio, "vs19"),
            new IdeDefaultTarget(IdeType.VsCode, "vscode-insiders-user")
        }
    };
    var runner = new Runner(
        launchInfo,
        new IdeManager(new IIdeProvider[]
        {
            new VisualStudioIdeProvider(new[] { vs1, vs2 }),
            CreateStubVsCodeProvider(codeStable, codeInsiders)
        }),
        psi => { launched.Add(psi); return null; });

    runner.Run();

    launched.Count.ShouldBe(2);
    launched[0].FileName.ShouldBe(@"C:\VS19\devenv.exe");
    launched[1].FileName.ShouldBe(@"C:\CodeInsiders\Code - Insiders.exe");
    launched[1].WorkingDirectory.ShouldBe(webDir);
}
```

Also add a test confirming that when `item.RunAfter?.WaitForCompletion == true`, `Runner` passes `waitForExit: true` to `IIdeProvider.BuildStartInfo`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj`
Expected: FAIL (`Runner` constructor overload with `IdeManager` / `processStarter` does not exist).

- [ ] **Step 3: Refactor `BackgroundLaunch\Runner.cs` and update `VSLauncherX\Helpers\ItemLauncher.cs`**

- In `BackgroundLaunch\Runner.cs`:
  - Replace `VisualStudioInstanceManager` with `IdeManager` (defaulting to `new IdeManager()` when null) and add injectable `Func<ProcessStartInfo, Process?>` starter (defaulting to `Process.Start`).
  - Extract helper `ResolveWorkingDirectory(string? path)` returning `path` when `Directory.Exists(path)` else `Path.GetDirectoryName(path)`.
  - Convert `launchInfo.DefaultTargets` into a dictionary `Dictionary<IdeType, string>`.
  - In `RunAll(VsItem item)`: run `item.RunBefore`, then if `item is VsFolder folder`:
    - If `folder.IsWorkspaceBacked() && ideManager.HasInstalled(IdeType.VsCode)`: launch `folder` via `RunItem(folder, waitForExit: item.RunAfter?.WaitForCompletion == true)` and do **not** recurse into `folder.Items`.
    - Otherwise: recurse `RunAll(child)` across `folder.Items`.
  - For workspace-backed `VsFolder` (when `folder.IsWorkspaceBacked() && ideManager.HasInstalled(IdeType.VsCode)`) and leaf items (`Solution` / `Project` including `VsCodeProject`), resolve instance via `ideManager.ResolveInstance(item, defaultTargets)`. If `null`, log with `Debug.WriteLine` and return `null` (skipping unlaunchable children in mixed groups cleanly). Otherwise delegate to `ideManager.GetProvider(instance.IdeType)!.BuildStartInfo(item, instance, waitForExit: ...)`, setting `startInfo.WorkingDirectory` via `ResolveWorkingDirectory(item.Path)`. Non-workspace `VsFolder` returns `null` from `BuildStartInfo` as before.
- Update `VSLauncherX\Helpers\ItemLauncher.cs` to accept `IdeDefaultTarget[]? defaultTargets` + optional `string? legacyTarget` (keeping backward-compatible overloads temporarily if `MainDialog` still calls old overload until Task 9, or updating callers cleanly in Task 9; keeping a thin compat overload accepting `IIdeInstance` keeps Task 5 independently green!).

- [ ] **Step 4: Run all tests and build `VSLauncherX`**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj`, `dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj`, and `dotnet build VSLauncherX\VSLauncherX.csproj`
Expected: All tests PASS; 0 build errors.

- [ ] **Step 5: Commit**

```bash
git add VSLauncher.sln BackgroundLaunch BackgroundLaunch.Tests VSLauncherX\Helpers\ItemLauncher.cs
git commit -m "feat: route BackgroundLaunch and ItemLauncher through IdeManager with workspace folder rules"
```

---

### Task 6: Generalized `IdeCombobox`, Icons (`FileIcons` / `ColumnHelper`), and Theme Support (`ThemeHelper`)

**Files:**
- Modify: `VSLXshared\DataModel\FileIcons.cs:21-85`
- Create: `VSLauncherX\Controls\IdeCombobox.cs`
- Modify: `VSLauncherX\Controls\VisualStudioCombobox.cs:1-271`
- Modify: `VSLauncherX\Helpers\ColumnHelper.cs:37-432`
- Modify: `VSLauncherX\Helpers\ThemeHelper.cs:369-453`
- Test: `VSLXshared.Tests\DataModel\FileIconsTests.cs`

**Interfaces:**
- Consumes: Task 1 (`VsCodeProject`, `VsFolder.IsWorkspaceBacked`), Task 2 (`IIdeInstance`, `IdeManager`), Task 3 (`VsCodeInstance`).
- Produces:
  - `FileIcons.GetIcon("CodeWorkspace")` and `FileIcons.GetIcon("VsCodeProject")` (never throws or returns null)
  - `class IdeCombobox : ComboBox` (`LoadInstances(IReadOnlyList<IIdeInstance> instances)`, `ShowDefault`, `IIdeInstance? SelectedIdeInstance`, `bool IsDefaultSelected`, `SelectDefault()`, `SelectByIdentifier(string? id)`, `ApplyTheme(ThemePalette palette)`)
  - Updated `ColumnHelper` rendering icons/descriptions for `VsCodeProject` and workspace-backed `VsFolder`
  - `ThemeHelper` supporting `IdeCombobox` and owner-drawn Dark/Light `TabControl` / `TabPage`

- [ ] **Step 1: Write failing unit test for `FileIcons` VS Code icon lookup**

Create `VSLXshared.Tests\DataModel\FileIconsTests.cs`:

```csharp
[Test]
public void GetIcon_ForCodeWorkspaceAndVsCodeProject_ReturnsNonNullIcon()
{
    var icons = new FileIcons(bLarge: true);

    icons.GetIcon("CodeWorkspace").ShouldNotBeNull();
    icons.GetIcon("VsCodeProject").ShouldNotBeNull();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj --filter "FullyQualifiedName~FileIconsTests"`
Expected: FAIL (`KeyNotFoundException` for `"CodeWorkspace"`).

- [ ] **Step 3: Implement `FileIcons`, `IdeCombobox`, `VisualStudioCombobox`, `ColumnHelper`, and `ThemeHelper` updates**

- In `VSLXshared\DataModel\FileIcons.cs`: add `".code-workspace"` to `RegisteredFileType.listExtensionsOfInterest`, extract `.code-workspace` icon (falling back to installed VS Code `AppIcon` or `Resources.DefaultIcon`), and register `"CodeWorkspace"` and `"VsCodeProject"` keys (making `GetIcon(string name)` fall back safely to `Resources.DefaultIcon` if missing).
- Create `VSLauncherX\Controls\IdeCombobox.cs` supporting `IReadOnlyList<IIdeInstance>` (loaded via `LoadInstances(...)`, owner-drawn icon + display name, `ShowDefault`, `SelectedIdeInstance`, `IsDefaultSelected`, `SelectDefault()`, `SelectByIdentifier(string?)`, `ApplyTheme(ThemePalette)`). Keep `VisualStudioCombobox` inheriting or wrapping `IdeCombobox` so existing forms (`dlgExecuteVisualStudio`) continue working cleanly.
- Update `VSLauncherX\Helpers\ColumnHelper.cs`:
  - `GetAspectForFile`: handle `VsCodeProject` (`$"{p.Name} - {p.TypeAsName()}"`).
  - `GetImageNameForFile`, `GetImageNameForFileImport`, `GetImageNameForMru`: handle `VsCodeProject` (`Warning` -> warning icon, `Workspace` -> `"CodeWorkspace"`, others -> `"VsCodeProject"`).
  - `GetDescription`: handle `VsCodeProject` (`$"VS Code: {p.TypeAsName()}"` + optional newline path when `ShowPathForSolutions` is true, or `"Could not find " + p.Path` when `Warning`).
- Update `VSLauncherX\Helpers\ThemeHelper.cs`:
  - Theme `IdeCombobox` in `ApplySpecificControlTheme`.
  - Add `TabControl` and `TabPage` theming: in dark mode set `DrawMode = TabDrawMode.OwnerDrawFixed` and attach an idempotent `DrawItem` handler painting tab headers with `palette.SurfaceAlt`/`palette.Selection` and `palette.Text`/`palette.SelectionText` plus dark tab-page surfaces (`palette.SurfaceContrast` / `palette.Surface`).

- [ ] **Step 4: Run tests and build `VSLauncherX`**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj` and `dotnet build VSLauncherX\VSLauncherX.csproj`
Expected: All tests PASS; 0 build errors.

- [ ] **Step 5: Commit**

```bash
git add VSLXshared VSLXshared.Tests VSLauncherX\Controls VSLauncherX\Helpers\ColumnHelper.cs VSLauncherX\Helpers\ThemeHelper.cs
git commit -m "feat: add IdeCombobox, VS Code icon support, and TabControl dark theming"
```

---

### Task 7: VS Code Item Settings Dialog (`dlgExecuteVsCode`)

**Files:**
- Create: `VSLauncherX\Forms\dlgExecuteVsCode.cs`
- Create: `VSLauncherX\Forms\dlgExecuteVsCode.Designer.cs`
- Create: `VSLauncherX\Forms\dlgExecuteVsCode.resx`

**Interfaces:**
- Consumes: Task 1 (`VsCodeProject`, `VsFolder`, `VsCodeLaunchOptions`, `VsCodeWindowMode`), Task 2 (`IdeManager`, `IIdeInstance`), Task 6 (`IdeCombobox`, `ThemeHelper`), existing `dlgBeforeAfter`, `frmPing`, `FileHelper.SolutionFilterString`.
- Produces:
  - `class dlgExecuteVsCode : Form` (constructors `dlgExecuteVsCode(VsItem item, IdeManager ideManager)`, property `VsItem Item`)

- [ ] **Step 1: Create `dlgExecuteVsCode` form and designer**

Implement `VSLauncherX\Forms\dlgExecuteVsCode.cs`, `.Designer.cs`, and `.resx` mirroring the clean structure of `dlgExecuteVisualStudio` with VS Code-specific controls:
- `txtName` (`item.Name`)
- `cbxVsCodeEdition` (`IdeCombobox`, `ShowDefault = true`, loaded from `ideManager.GetInstances(IdeType.VsCode)`)
- `cbxProfile` (`ComboBoxStyle.DropDown`, replacing VS's Instance dropdown; repopulated on edition change from `selectedEdition?.GetProfiles()` or `ideManager.GetProvider(IdeType.VsCode)?.DefaultInstance?.GetProfiles()`; defaults to `"<default>"`; accepts typing a new profile name)
- `cbxWindowMode` (`ComboBoxStyle.DropDownList`: `"Default"`, `"New Window"`, `"Reuse Window"`)
- `chkDisableExtensions` (`"Disable Extensions (--disable-extensions)"`)
- `txtCommand` (additional CLI arguments) + `lnkCommandsHelp` opening `https://code.visualstudio.com/docs/editor/command-line`
- `txtPath` + `btnBrowseFile` (filter: `VS Code Workspaces (*.code-workspace)|*.code-workspace|All files (*.*)|*.*`) + `btnBrowseFolder` (`FolderBrowserDialog`)
- `cbxMonitors` + `btnPingMonitor` (reusing `frmPing`)
- `chkAdmin` (`"Run as Admin"`)
- `btnBeforeAfter` (opening `dlgBeforeAfter(item, item.Name ?? string.Empty)`)
- `btnOk` (`"&Save"`) and `btnCancel`
- No `ShowSplash` checkbox.

Data binding on Load and `btnOk_Click`:
- When `item is VsCodeProject project`: read/write `project.VsVersion` (Edition), `project.Instance` (Profile; `null` if `<default>` or whitespace), `project.WindowMode`, `project.DisableExtensions`, `project.Commands`, plus shared `Name`, `Path`, `RunAsAdmin`, `PreferredMonitor`, and call `project.Refresh()`.
- When `item is VsFolder folder`: read/write `folder.VsCodeOptions` (allocating `new VsCodeLaunchOptions()` on save), setting `Edition`, `Profile`, `WindowMode`, `DisableExtensions`, `Commands` without touching `folder.VsVersion`/`folder.Instance`/`folder.Commands`, plus shared `Name`, `Path`, `RunAsAdmin`, `PreferredMonitor`.
- Apply `ThemeHelper.ApplyCurrentTheme(this)` in constructor.

- [ ] **Step 2: Build `VSLauncherX` to verify form compilation and designer cleanliness**

Run: `dotnet build VSLauncherX\VSLauncherX.csproj`
Expected: 0 build errors.

- [ ] **Step 3: Commit**

```bash
git add VSLauncherX\Forms\dlgExecuteVsCode.*
git commit -m "feat: add tool-specific dlgExecuteVsCode settings dialog"
```

---

### Task 8: Tabbed Right Sidebar, Single-IDE Disabling, Settings Persistence, and Combined MRU Import

**Files:**
- Modify: `VSLauncherX\Properties\Settings.settings`
- Modify: `VSLauncherX\Properties\Settings.Designer.cs`
- Modify: `VSLauncherX\MainDialog.Designer.cs:60-665`
- Modify: `VSLauncherX\MainDialog.cs:20-260,1206-1555`
- Modify: `VSLauncherX\Forms\dlgImportVisualStudio.cs:18-222`

**Interfaces:**
- Consumes: Task 2 (`IdeManager`, `IIdeInstance`), Task 3 (`VsCodeInstance`), Task 6 (`IdeCombobox`, `ThemeHelper`).
- Produces:
  - `Properties.Settings.Default.SelectedVsCodeEdition` (`string`) and `Properties.Settings.Default.SelectedIdeTab` (`string`)
  - `MainDialog.tabControlIdes` (`tabVisualStudio`, `tabVsCode`), `selectVsCodeEdition`, the 4 VS Code quick-action buttons (`btnVsCodeStart`, `btnVsCodeStartAdmin`, `btnVsCodeStartNewWindow`, `btnVsCodeStartNoExt`), and `btnVsCodeDownload`
  - `MainDialog.ideManager` (`IdeManager` field) and `MainDialog.GetCurrentDefaultTargets()` returning `IdeDefaultTarget[]`
  - `dlgImportVisualStudio` powered by `IdeManager.GetAllRecentProjects`

- [ ] **Step 1: Add `SelectedVsCodeEdition` and `SelectedIdeTab` to `Settings.settings` and `Settings.Designer.cs`**

Add two `User`-scoped `System.String` settings (`SelectedVsCodeEdition`, `SelectedIdeTab`, default empty string) to `VSLauncherX\Properties\Settings.settings` and matching strongly-typed properties in `VSLauncherX\Properties\Settings.Designer.cs`.

- [ ] **Step 2: Refactor right sidebar in `MainDialog.Designer.cs` into `tabControlIdes`**

Replace `flowLayoutPanel1` + `btnVsInstaller` in column 1 of `mainPanel` with `tabControlIdes` (`Dock = DockStyle.Fill`, `RowSpan = 2` on `mainPanel`):
- **`tabVisualStudio`** (`Text = "Visual Studio"`): contains `flowLayoutPanel1` (existing `label2`, `selectVisualStudioVersion`, `btnMainStartVisualStudio1..5`, `btnMainOpenActivityLog`) and `btnVsInstaller` docked at bottom.
- **`tabVsCode`** (`Text = "VS Code"`): contains `flowLayoutPanelVsCode` with `lblVsCodeEdition` (`"VS Code edition:"`), `selectVsCodeEdition` (`IdeCombobox`), and the 4 quick-action buttons (matching 232×64 bold `ImageBeforeText` layout):
  1. `btnVsCodeStart` (`Tag = "Start {0}"`)
  2. `btnVsCodeStartAdmin` (`Tag = "Start {0} as Admin"`)
  3. `btnVsCodeStartNewWindow` (`Tag = "Start {0}\r\nwith New Window"`)
  4. `btnVsCodeStartNoExt` (`Tag = "Start {0}\r\nwithout Extensions"`)
  plus `btnVsCodeDownload` docked at bottom (`Text = "Download VS Code"`, shown/enabled when VS Code is not installed).
- Update `btnImportVS` tooltip to `"Import Recent Projects"`.

- [ ] **Step 3: Wire `IdeManager`, VS Code quick actions, and single-IDE disabling in `MainDialog.cs`**

- Instantiate `private readonly IdeManager ideManager = new IdeManager();` in `MainDialog`.
- Populate `selectVsCodeEdition` from `ideManager.GetInstances(IdeType.VsCode)` and restore `Properties.Settings.Default.SelectedVsCodeEdition`.
- On `selectVsCodeEdition.SelectedIndexChanged`: save `SelectedVsCodeEdition`, update icons, text, and tooltips of the 4 VS Code buttons (disposing previous button bitmap cleanly, mirroring `currentVsButtonBitmap`).
- Wire the 4 button click handlers to `VsCodeInstance.Execute()`, `ExecuteAsAdmin()`, `ExecuteNewWindow()`, `ExecuteWithoutExtensions()`, and `btnVsCodeDownload_Click` to open `https://code.visualstudio.com/download`.
- Implement `ApplyInstalledIdeState()` called during initialization:
  - Check `bool hasVs = ideManager.HasInstalled(IdeType.VisualStudio)` and `bool hasCode = ideManager.HasInstalled(IdeType.VsCode)`.
  - Disable `selectVisualStudioVersion`, `btnMainStartVisualStudio1..5`, and `btnMainOpenActivityLog` when `!hasVs` (keeping `btnVsInstaller` / Download enabled).
  - Disable `selectVsCodeEdition` and the 4 VS Code buttons when `!hasCode`; show/hide `btnVsCodeDownload` (`Visible = !hasCode`).
  - Default active tab: if `!hasVs && hasCode` select `tabVsCode`; if `hasVs && !hasCode` select `tabVisualStudio`; if both installed restore `Properties.Settings.Default.SelectedIdeTab` (defaulting to `tabVisualStudio`). Persist `SelectedIdeTab` on `tabControlIdes.SelectedIndexChanged` when both IDEs are installed.
- Add `GetCurrentDefaultTargets()` returning `IdeDefaultTarget[]` from `selectVisualStudioVersion.SelectedItem` and `selectVsCodeEdition.SelectedIdeInstance`.

- [ ] **Step 4: Update `dlgImportVisualStudio.cs` to import combined Visual Studio + VS Code MRUs**

- Replace `VisualStudioInstanceManager` with `IdeManager` (constructor accepting optional `IdeManager`).
- Call `ideManager.GetAllRecentProjects(this.OnlyDefaultInstances)`.
- Disable/hide `chkDefaultInstance` when `!ideManager.HasInstalled(IdeType.VisualStudio)`.
- Update form title to `"Import Recent Projects"`.

- [ ] **Step 5: Build `VSLauncherX` and run all tests**

Run: `dotnet build VSLauncherX\VSLauncherX.csproj`, `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj`, `dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj`
Expected: 0 build errors; all tests PASS.

- [ ] **Step 6: Commit**

```bash
git add VSLauncherX\Properties\Settings.* VSLauncherX\MainDialog.* VSLauncherX\Forms\dlgImportVisualStudio.cs
git commit -m "feat: add tabbed IDE sidebar, single-IDE disabling, and combined MRU import"
```

---

### Task 9: Main Tree Integration — Settings Dispatch, `"Open in VS Code"`, Git, Explorer, Drag-and-Drop, Taskbar, and Full Verification

**Files:**
- Modify: `VSLauncherX\MainDialog.Designer.cs` (add `openInVsCodeToolStripMenuItem` to `ctxMenu`)
- Modify: `VSLauncherX\MainDialog.cs:116-140,340-520,656-785,947-1205,1584-1645`
- Modify: `VSLauncherX\MainDialog.Utilities.cs:172-209,512-578`

**Interfaces:**
- Consumes: Task 1–8 (`IdeManager`, `VsCodeProject`, `VsFolder.IsWorkspaceBacked`, `VsCodeInstance.OpenPath`, `dlgExecuteVsCode`, `ItemLauncher`, `GetCurrentDefaultTargets`).
- Produces: Complete end-to-end multi-IDE tree UX and verified Debug + Release builds + test suite.

- [ ] **Step 1: Update `olvColumnVersion`, status bar, and drag-and-drop in `MainDialog.cs`**

- In `olvColumnVersion.AspectGetter`:
  - `VsCodeProject p`: if `string.IsNullOrEmpty(p.VsVersion)` return `"<default>"`; else return `ideManager.GetProvider(IdeType.VsCode)?.FindByIdentifier(p.VsVersion)?.ShortName ?? "<default>"`.
  - `VsFolder f` where `f.IsWorkspaceBacked() && ideManager.HasInstalled(IdeType.VsCode)`: return pinned `f.VsCodeOptions?.Edition` short name or `"VS Code"`.
  - Visual Studio items: preserve existing `"{vs.Year} ({vs.ShortVersion})"` / `"<default>"` formatting.
- In `olvFiles_SelectedIndexChanged`:
  - Handle `VsCodeProject p`: set `mainStatusLabel.Text = $"VS Code: {p.TypeAsName()}"` and `enableOpenInExplorer = !string.IsNullOrWhiteSpace(p.Path)`.
  - Handle `VsFolder sg`: display combined solution + project count, and set `enableOpenInExplorer = !string.IsNullOrWhiteSpace(sg.Path) && Directory.Exists(sg.Path)`.
- In `olvFiles_Dropped`: `.code-workspace` is now returned as `VsCodeProject` (`ItemType == Project`) by `ImportHelper.GetItemFromExtension`, so it merges directly into the tree; verify directory drops still invoke `dlgImportFolder(file)`.

- [ ] **Step 2: Wire tab-aware Settings dispatch, launch pipeline, Taskbar JumpList, and `"Open in VS Code"`**

- In `settingsToolStripMenuItem_Click` and `mainImportSoP_Click`:
  - Create helper `ShowItemSettingsDialog(VsItem item)`:
    - `VsCodeProject` -> `new dlgExecuteVsCode(item, this.ideManager)`
    - `VsFolder` when `this.tabControlIdes.SelectedTab == this.tabVsCode` -> `new dlgExecuteVsCode(item, this.ideManager)`
    - Otherwise -> `new dlgExecuteVisualStudio(item)`
- In `runToolStripMenuItem_Click` and `runAsAdminToolStripMenuItem_Click`:
  - Require `ideManager.AnyInstalled` (instead of checking only `ResolveTargetVisualStudio != null`); show clear `"No supported IDE installation was found."` message if neither VS nor VS Code is installed.
  - For a `VsFolder`: if `!f.IsWorkspaceBacked() || !ideManager.HasInstalled(IdeType.VsCode)`, run the existing `>3` confirmation count (counting workspace-backed subfolders as 1 launch each).
  - Launch `VsFolder`, `VsSolution`, `VsProject`, and `VsCodeProject` via `ItemLauncher` passing `GetCurrentDefaultTargets()` and legacy target.
- In `AddItemToTaskbar(VsItem item)`:
  - Bail only if `!ideManager.AnyInstalled`; create `ItemLauncher` using `GetCurrentDefaultTargets()` so favorited VS Code items launch cleanly from the taskbar even when Visual Studio is not installed.
- Add `openInVsCodeToolStripMenuItem` (`Text = "Open in VS Code"`) to `ctxMenu`:
  - In `ctxMenu_Opening`: enabled when `ideManager.HasInstalled(IdeType.VsCode)` and selected `VsItem` has a valid existing file or directory `Path`.
  - In `openInVsCodeToolStripMenuItem_Click`: resolve selected `VsCodeInstance` from `selectVsCodeEdition` (or `ideManager.GetProvider(IdeType.VsCode)?.DefaultInstance`), compute `targetPath = (Directory.Exists(item.Path) || Path.GetExtension(item.Path).Equals(".code-workspace", StringComparison.OrdinalIgnoreCase)) ? item.Path : Path.GetDirectoryName(item.Path)`, and call `vsCodeInstance.OpenPath(targetPath)` directly (no `BackgroundLaunch`, no item hooks).

- [ ] **Step 3: Make Git discovery and Open-in-Explorer directory-aware**

- In `MainDialog.Utilities.cs` (`ReadItemGitStatus`, `SetupBranchMenu`) and `MainDialog.cs` (`DiscoverRepositoryPath`):
  - Compute `string? dir = Directory.Exists(itemPath) ? itemPath : Path.GetDirectoryName(itemPath);` so directory-backed `VsCodeProject` items inspect their own repo rather than their parent directory.
- In `mainOpenInExplorer_Click` and `explorerToolStripMenuItem_Click`:
  - Remove the `item is not VsFolder` restriction in `mainOpenInExplorer_Click` so disk-backed folders are supported whenever `!string.IsNullOrWhiteSpace(item.Path)`.
  - If `Directory.Exists(item.Path)`: launch `Process.Start("explorer.exe", $"\"{item.Path}\"")`; otherwise keep `Process.Start("explorer.exe", $"/select, \"{item.Path}\"")`.

- [ ] **Step 4: Run full verification (Debug & Release builds + all unit tests)**

Run:
```powershell
dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -v normal
dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj -v normal
dotnet build VSLauncherX\VSLauncherX.csproj -c Debug
dotnet build VSLauncherX\VSLauncherX.csproj -c Release
```
Expected: All unit tests PASS (0 failed, 0 skipped); both Debug and Release builds succeed with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add VSLauncherX\MainDialog.Designer.cs VSLauncherX\MainDialog.cs VSLauncherX\MainDialog.Utilities.cs
git commit -m "feat: wire multi-IDE launch, tab-aware settings, Open in VS Code, Git, Explorer, and JumpList"
```
