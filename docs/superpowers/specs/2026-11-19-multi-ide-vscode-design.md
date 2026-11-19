# Multi-IDE Support (VS Code Iteration) Design Specification

**Date**: 2026-11-19  
**Author**: Hefaistos68  
**Selected Approach**: Approach 1 — Shared IDE Provider Abstraction + Backward-Compatible Model  
**Scope Size**: Project (spans `VSLXshared`, `VSLauncherX`, `BackgroundLaunch`, and a new `VSLXshared.Tests` test project)

---

## 1. Intent and Why

VSLauncherX currently manages and launches only Microsoft Visual Studio solutions, projects, and solution groups. Modern developer workflows routinely span both Visual Studio and Visual Studio Code, and many machines have only VS Code installed.

This design introduces a shared IDE provider architecture so VSLauncherX can support multiple IDEs cleanly, delivering complete **Visual Studio Code** support in this iteration without breaking existing Visual Studio workflows or existing `%AppData%\VSLauncher\VSLauncher.json` user data files.

---

## 2. Goals, Non-Goals, and Anti-Goals

### Goals
- Introduce a clean, extensible IDE provider layer (`IdeType`, `IIdeInstance`, `IIdeProvider`, `IdeManager`) in `VSLXshared`, shared by `VSLauncherX` and `BackgroundLaunch`.
- Add full Visual Studio Code support:
  - Auto-detection of installed VS Code editions (Stable and Insiders, User and System installs).
  - Profile discovery from VS Code's `storage.json` (`userDataProfiles`), mapping Visual Studio `/RootSuffix` instances to VS Code `--profile` profiles.
  - Recent projects/workspaces import from VS Code's `state.vscdb` via the existing toolbar import flow.
  - Folder scanning that detects both Visual Studio and VS Code project types.
  - First-class `.code-workspace` entries alongside explicit folder-open semantics.
  - Tabbed right sidebar with a Visual Studio tab and a VS Code tab.
  - Tool-specific item settings dialog (`dlgExecuteVsCode` for VS Code items; existing `dlgExecuteVisualStudio` for Visual Studio items).
- Disable Visual Studio-specific features automatically when only VS Code is installed (and symmetrically disable VS Code-specific features when only Visual Studio is installed).
- Maintain full backward compatibility with existing `VSLauncher.json` files and existing light/dark theme support.

### Non-Goals (This Iteration)
- Implementing providers for IDEs beyond Visual Studio and VS Code (e.g., Rider, Cursor, IntelliJ).
- User-defined custom IDE definitions or runtime third-party plugin loading.
- Importing remote VS Code MRU entries (`vscode-remote://` SSH, WSL, Dev Containers); local filesystem entries (`file://`) only.
- Cross-platform (macOS/Linux) support.
- Full domain renaming of legacy `Vs*` data model classes (`VsItem`, `VsFolder`, `VsSolution`, `VsProject`) or a schema v2 JSON migration.

### Anti-Goals
- Breaking deserialization of existing `VSLauncher.json` files or loosening `VsSerializationBinder` type allowlisting in `VSLXshared\Helpers\FileHelper.cs`.
- Blocking launches or crashing when Visual Studio is not installed.
- Polluting folder scans with dependency or build-output directories (`node_modules`, `.venv`, `bin`, `obj`, `dist`, `target`, `.git`) or recursing inside already-detected VS Code project directories.
- Replacing group-launch behavior on plain folders that do not contain a `.code-workspace` file.

---

## 3. Constraints

- **Platform & Frameworks**:
  - `VSLXshared`: `net10.0-windows` (WinForms enabled).
  - `VSLauncherX`: `net10.0-windows7.0` (WinForms + `ObjectListView2022`).
  - `BackgroundLaunch`: `net10.0-windows7.0` (`EnforceCodeStyleInBuild=True`, `AnalysisLevel=latest-all`, outputs to `..\VSLauncherX\bin`).
- **Serialization Security**: `VSLauncher.json` and `BackgroundLaunch` `b64:` command-line payloads use `Newtonsoft.Json` with `TypeNameHandling.Auto` and `VsSerializationBinder`. Every newly serialized class or enum must be explicitly added to `VsSerializationBinder.AllowedTypes` in `VSLXshared\Helpers\FileHelper.cs`.
- **SQLite Reading**: Reading VS Code's `state.vscdb` uses the standard `Microsoft.Data.Sqlite` NuGet package in `VSLXshared`, opened strictly read-only (`Mode=ReadOnly`) with copy-to-temp fallback if the database is locked by a running VS Code process.
- **Theming**: All new controls, dialogs, and the sidebar `TabControl` must support both Light and Dark palettes via `VSLauncherX\Helpers\ThemeHelper.cs`.
- **C# Coding Standards**:
  - Cognitive complexity <= 15 per method; <= 7 parameters per method (except DI constructors).
  - Block-scoped namespaces; XML documentation comments on all new classes, interfaces, and methods (`<inheritdoc/>` when implementing documented interface members).
  - Cache `Regex` instances as `static readonly Regex` fields.
  - Unit tests written with NUnit, Moq, and Shouldly in a `.Tests` project (`VSLXshared.Tests`), creating a fresh SUT per test.

---

## 4. Approaches Considered

| Approach | Summary | Tradeoffs |
|---|---|---|
| **Approach 1: Shared IDE Provider Abstraction + Backward-Compatible Model** *(Selected)* | Introduce `IdeType`, `IIdeProvider`, `IIdeInstance`, and `IdeManager` in `VSLXshared`, wrapping `VisualStudioInstanceManager` and adding `VsCodeIdeProvider`. Keep existing `VsItem`/`VsFolder`/`VsSolution`/`VsProject` names intact; add `IdeType` on `VsItem`, optional `VsCodeLaunchOptions` on `VsFolder`, and a new `VsCodeProject` subclass registered in `VsSerializationBinder`. Delegate CLI building in `BackgroundLaunch.Runner` to `IdeManager`. | Moderate upfront refactor in `VSLXshared` and `BackgroundLaunch`, but zero JSON breaking changes, single source of truth for discovery/MRU/CLI/scanning, and clean extensibility for future IDEs. |
| **Approach 2: Minimal Parallel Side-by-Side Implementation** | Keep Visual Studio classes untouched; add standalone VS Code classes and route with `if`/`else` checks across `MainDialog`, `ColumnHelper`, import dialogs, and `BackgroundLaunch.Runner`. | Smallest initial diff and lowest short-term risk, but duplicates IDE branching across UI and background launch and creates refactor debt when adding a third IDE. |
| **Approach 3: Full Domain Rename + Schema v2 Migration** | Rename all `Vs*` types and properties to IDE-neutral names (`LaunchItem`, `ItemGroup`, `IdeInstanceId`, `IdeProfile`) and write a `VSLauncher.json` v1-to-v2 migrator. | Cleanest long-term naming, but touches nearly every file in the solution and introduces unnecessary schema migration risk for this iteration. |

**Why Approach 1 was chosen**: It isolates IDE-specific mechanics inside `VSLXshared` for both `VSLauncherX` and `BackgroundLaunch`, preserves 100% compatibility with existing user data, and keeps future IDE additions localized to a new provider, detector, sidebar tab, and settings dialog.

---

## 5. Architecture & Detailed Design

### 5.1 Shared IDE Provider Layer (`VSLXshared`)

Add the following abstractions in `VSLXshared`:

- **`IdeType` enum**:
  - `VisualStudio = 0`
  - `VsCode = 1`
- **`IIdeInstance` interface**:
  - `IdeType IdeType { get; }`
  - `string Name { get; }` — full display name (e.g., `"Visual Studio Enterprise 2022"`, `"Visual Studio Code"`, `"Visual Studio Code - Insiders"`).
  - `string ShortName { get; }` — compact label for quick-launch buttons (e.g., `"Visual Studio 2022"`, `"VS Code"`, `"VS Code Insiders"`).
  - `string Version { get; }` — product version string.
  - `string Location { get; }` — full path to IDE executable (`devenv.exe`, `Code.exe`, `Code - Insiders.exe`).
  - `string Identifier { get; }` — stable unique ID (VS setup instance hash; deterministic VS Code edition+scope key such as `"vscode-stable-user"`, `"vscode-stable-system"`, `"vscode-insiders-user"`, `"vscode-insiders-system"`, or `"vscode-custom-<hash>"`).
  - `Icon AppIcon { get; }` — lazily extracted via `Icon.ExtractAssociatedIcon(Location)` with safe fallback icon.
  - `List<string> GetProfiles()` — returns `"<default>"` followed by discovered profile/instance names.
- **`IIdeProvider` interface**:
  - `IdeType IdeType { get; }`
  - `string DisplayName { get; }`
  - `string DownloadUrl { get; }`
  - `IReadOnlyList<IIdeInstance> Instances { get; }`
  - `bool IsInstalled { get; }`
  - `IIdeInstance? DefaultInstance { get; }`
  - `IIdeInstance? FindByIdentifier(string? identifier)`
  - `VsItemList GetRecentProjects(bool onlyDefaultProfiles)`
  - `ProcessStartInfo? BuildStartInfo(VsItem item, IIdeInstance instance, bool forceAdmin = false, bool waitForExit = false)`
- **`IdeManager` class**:
  - Aggregates registered `IIdeProvider` implementations (`VisualStudioIdeProvider`, `VsCodeIdeProvider`), with an injectable constructor for unit tests and a parameterless default constructor used at runtime.
  - Methods: `GetProvider(IdeType)`, `GetInstances(IdeType)`, `HasInstalled(IdeType)`, `AnyInstalled`, `GetAllRecentProjects(bool onlyDefaultProfiles)`, and `ResolveInstance(VsItem item, IReadOnlyDictionary<IdeType, string>? defaultTargetIdentifiers)`.
  - **Instance resolution precedence** for an item:
    1. Determine target IDE: `item.IdeType` (or `IdeType.VsCode` for a workspace-backed `VsFolder` when VS Code is installed).
    2. Look up explicit identifier (`item.VsVersion`, or `folder.VsCodeOptions?.Edition` for a workspace-backed `VsFolder`) in that IDE's provider.
    3. For `VsSolution` with `IdeType.VisualStudio`, fall back to `FindByVersion(s.RequiredVersion)`.
    4. Fall back to the per-IDE default identifier supplied in `defaultTargetIdentifiers` (from the matching sidebar dropdown), then `provider.DefaultInstance`.
    5. **Single-IDE fallback**: If `item.IdeType == IdeType.VisualStudio`, no Visual Studio instance is installed, and VS Code IS installed, resolve to the VS Code provider's preferred/default instance so existing `.sln`/`.slnx`/`.csproj` items open in VS Code.

### 5.2 Visual Studio Provider (`VisualStudioIdeProvider`)

- Wraps `VisualStudioInstanceManager` and makes `VisualStudioInstance` implement `IIdeInstance`:
  - `IdeType => IdeType.VisualStudio`
  - `GetProfiles()` delegates to existing `GetInstances()`.
- `BuildStartInfo` encapsulates the exact `devenv.exe` argument construction currently in `BackgroundLaunch\Runner.cs` (`"<path>" <commands> /NoSplash /RootSuffix <instance>`).
- `GetRecentProjects(onlyDefaultProfiles)` delegates to `VisualStudioInstanceManager.GetRecentProjects(onlyDefaultProfiles)`.
- `DownloadUrl` returns `"https://visualstudio.microsoft.com/downloads/"`.

### 5.3 VS Code Provider (`VsCodeIdeProvider` & `VsCodeInstance`)

#### Edition Discovery (Windows)
Probes for both **VS Code (Stable)** and **VS Code Insiders**, deduplicating by canonical full path to `Code.exe` / `Code - Insiders.exe`:
1. User Stable: `%LocalAppData%\Programs\Microsoft VS Code\Code.exe`
2. System Stable: `%ProgramFiles%\Microsoft VS Code\Code.exe` and `%ProgramFiles(x86)%\Microsoft VS Code\Code.exe`
3. User Insiders: `%LocalAppData%\Programs\Microsoft VS Code Insiders\Code - Insiders.exe`
4. System Insiders: `%ProgramFiles%\Microsoft VS Code Insiders\Code - Insiders.exe`
5. Registry fallback: `HKCU` and `HKLM` `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*` (matching `InstallLocation` containing `Code.exe` / `Code - Insiders.exe` where `DisplayName` starts with `"Microsoft Visual Studio Code"`).
6. `PATH` fallback: locate `code.cmd` / `code-insiders.cmd` on `PATH` and resolve sibling `..\Code.exe` / `..\Code - Insiders.exe`.

For each discovered executable:
- Read version via `FileVersionInfo.GetVersionInfo(exePath).ProductVersion` (falling back to `resources\app\package.json` `version`).
- Map user data directory: `%AppData%\Code` for Stable, `%AppData%\Code - Insiders` for Insiders.
- Assign `ShortName` (`"VS Code"` or `"VS Code Insiders"`) and `Name` (distinguishing User vs. System if both are installed for the same edition, e.g., `"VS Code (User)"` / `"VS Code (System)"`; otherwise `"Visual Studio Code"` / `"Visual Studio Code - Insiders"`).
- Launch `Code.exe` / `Code - Insiders.exe` directly rather than `.cmd` wrappers to prevent flashing console windows and to ensure reliable icon extraction.

#### Profile Discovery
- Reads `<UserDataRoot>\User\globalStorage\storage.json` and parses the `userDataProfiles` JSON array (`name` string property).
- Returns `["<default>", ...distinctNames]`. Missing or malformed `storage.json` returns `["<default>"]` without throwing.

#### Recent Projects Import (MRU)
- Reads `<UserDataRoot>\User\globalStorage\state.vscdb` via `Microsoft.Data.Sqlite` (`Data Source=<path>;Mode=ReadOnly`).
- If SQLite reports lock/busy/IO errors (e.g., VS Code currently holds a lock), copies `state.vscdb` (plus `-wal` and `-shm` sidecar files if present) to a unique temp directory, reads from the copy, and deletes the temp directory in `finally`.
- Executes `SELECT value FROM ItemTable WHERE key = 'history.recentlyOpenedPathsList'` and parses JSON `entries`:
  - `workspace.configPath` with `file://` scheme -> decode URI (handling Windows drive percent-encoding such as `file:///c%3A/...`) -> create `VsCodeProject` (`ProjectKind = Workspace`, `Name = Path.GetFileNameWithoutExtension(path)`, `VsVersion = instance.Identifier`), `Warning = !File.Exists(path)`.
  - `folderUri` with `file://` scheme -> decode URI -> create `VsCodeProject` (`ProjectKind = Folder` or refined by checking directory manifests, `Name = directory name`, `VsVersion = instance.Identifier`), `Warning = !Directory.Exists(path)`.
  - Entries with `remoteAuthority`, non-`file://` schemes, or standalone `fileUri` entries are skipped in this iteration.
- Wraps each edition's entries in a group `VsFolder` (`ItemType = ItemTypeEnum.VisualStudio` for existing MRU tree compatibility, `Path = string.Empty`, `Name = instance.Name`, `Icon = instance.AppIcon`). Omit editions that yield zero valid local entries.

#### CLI Argument Building, Wait Handling, and Direct Quick Actions
`VsCodeIdeProvider.BuildStartInfo(VsItem item, IIdeInstance instance, bool forceAdmin, bool waitForExit)`:
- Effective `runAsAdmin = forceAdmin || item.RunAsAdmin`.
- Effective `shouldWait = waitForExit || item.WaitForCompletion || (item.RunAfter?.WaitForCompletion == true)`.
- **Target path**:
  - For `VsCodeProject` or workspace-backed `VsFolder`: `item.Path`.
  - For cross-IDE single-IDE fallback on a Visual Studio file (`.sln`/`.slnx`/`.*proj`): `Path.GetDirectoryName(item.Path)` so VS Code opens the containing workspace folder.
- **Cross-IDE option guard**:
  - When `item is VsCodeProject vcp`: read profile (`vcp.Instance`), window mode (`vcp.WindowMode`), disable extensions (`vcp.DisableExtensions`), and extra arguments (`vcp.Commands`).
  - When `item is VsFolder folder`: read VS Code options from `folder.VsCodeOptions` (profile, window mode, disable extensions, extra arguments) rather than `folder.Instance` / `folder.Commands` (which belong to Visual Studio).
  - When `item.IdeType == IdeType.VisualStudio` (single-IDE fallback on a VS solution/project): strip `item.Instance` (`/RootSuffix`) and `item.Commands` (`devenv.exe` switches) so VS flags never create bogus VS Code profiles or invalid switches.
- **Arguments** (in order):
  1. `"<targetPath>"`
  2. If profile is non-empty: `--profile "<profile.Trim()>"`
  3. Window mode: `--new-window` for `NewWindow`, `--reuse-window` for `ReuseWindow`
  4. If disable extensions: `--disable-extensions`
  5. If `shouldWait`: `--wait`
  6. If extra arguments are non-empty: append extra arguments
- **Blocking wait reliability**:
  - When `shouldWait == false` (or when `runAsAdmin == true`, since Windows UAC elevation requires `UseShellExecute = true` and forbids setting custom environment variables): launch `instance.Location` (`Code.exe` / `Code - Insiders.exe`) directly with the built arguments.
  - When `shouldWait == true && !runAsAdmin`: locate `<installDir>\resources\app\out\cli.js`. If present, launch `instance.Location` with `UseShellExecute = false`, `Environment["ELECTRON_RUN_AS_NODE"] = "1"`, and `"<cli.js>"` prepended before the arguments (including `--wait`). This avoids console windows and blocks reliably until the opened VS Code window closes, even when another VS Code instance is already running. If `cli.js` is absent, fall back to launching `instance.Location` directly with `--wait`.

Direct quick-launch methods on `VsCodeInstance` (used by sidebar quick actions and explicit `"Open in VS Code"`):
1. **Start**: launches `Location` with no extra arguments.
2. **Start as Admin**: launches `Location` with `Verb = "runas"`, `UseShellExecute = true`.
3. **Start with New Window**: launches `Location` with `--new-window`.
4. **Start without Extensions**: launches `Location` with `--disable-extensions`.
5. **OpenPath(string path)**: launches `Location` directly with `"<path>"` (used by context-menu `"Open in VS Code"` without invoking `BackgroundLaunch` or item hooks).

### 5.4 Data Model, Backward Compatibility, and Folder Semantics (`VSLXshared`)

#### `VsItem` Extensions
- Add `public IdeType IdeType { get; set; } = IdeType.VisualStudio;` on `VsItem`.
  - Existing `VSLauncher.json` files omit `IdeType`; Newtonsoft.Json leaves it at `IdeType.VisualStudio`.
- Reuse `VsItem.VsVersion` for the item's IDE instance identifier (VS instance ID or VS Code edition ID; empty = `<default>`).
- Reuse `VsItem.Instance` for the item's profile name (VS `/RootSuffix` or VS Code `--profile`; null/empty = `<default>`).

#### New `VsCodeLaunchOptions` Class (`VSLauncher.DataModel`)
Attached as an optional property `public VsCodeLaunchOptions? VsCodeOptions { get; set; }` on `VsFolder` so editing a workspace-backed folder under the VS Code tab never overwrites its Visual Studio `VsVersion` / `Commands` / `ShowSplash`:
- `string? Edition { get; set; }` (VS Code `IIdeInstance.Identifier`; null/empty = `<default>`)
- `string? Profile { get; set; }` (VS Code `--profile`; null/empty = `<default>`)
- `VsCodeWindowMode WindowMode { get; set; } = VsCodeWindowMode.Default`
- `bool DisableExtensions { get; set; }`
- `string? Commands { get; set; }`
- `VsCodeLaunchOptions Clone()` (deep-copied in `VsFolder.Clone`).

#### New `VsCodeProject` Model Class (`VSLauncher.DataModel`)
Derives from `VsItem`, with `ItemType = ItemTypeEnum.Project` and `IdeType = IdeType.VsCode`:
- `VsCodeProjectKind ProjectKind { get; set; }` enum:
  - `Workspace` (`.code-workspace` file)
  - `Folder` (plain folder or `.vscode`-configured folder)
  - `NodeJs` (`package.json`)
  - `Python` (`pyproject.toml`)
  - `Rust` (`Cargo.toml`)
  - `Go` (`go.mod`)
  - `Java` (`pom.xml`, `build.gradle`, `build.gradle.kts`)
  - `CMake` (`CMakeLists.txt`)
  - `Flutter` (`pubspec.yaml`)
  - `Php` (`composer.json`)
  - `Generic`
- `string? ManifestFile { get; set; }` — optional filename of the detected manifest inside `Path` (e.g., `"package.json"`).
- `VsCodeWindowMode WindowMode { get; set; } = VsCodeWindowMode.Default` (`Default`, `NewWindow`, `ReuseWindow`).
- `bool DisableExtensions { get; set; }`
- `bool IsDirectoryTarget` (`[JsonIgnore]`): `true` when `ProjectKind != VsCodeProjectKind.Workspace` and `!string.IsNullOrWhiteSpace(Path)` and (directory exists or path does not end in `.code-workspace`).
- `Refresh()` override:
  - If `ProjectKind == VsCodeProjectKind.Workspace`: validates `File.Exists(Path)` and reads `FileInfo.LastWriteTime`.
  - Otherwise: validates `Directory.Exists(Path)` and reads `ManifestFile` or `DirectoryInfo` `LastWriteTime`.
  - Sets `Warning = !accessible`.
- `TypeAsName()` returning readable labels (`"VS Code Workspace"`, `"Node.js"`, `"Python"`, `"Rust"`, `"Go"`, `"Java"`, `"CMake"`, `"Flutter"`, `"PHP"`, `"VS Code Folder"`).

#### Serialization Binder Update
Add the following types to `VsSerializationBinder.AllowedTypes` in `VSLXshared\Helpers\FileHelper.cs`:
- `IdeType`
- `VsCodeProject`
- `VsCodeProjectKind`
- `VsCodeWindowMode`
- `VsCodeLaunchOptions`
- `IdeDefaultTarget` (and `List<IdeDefaultTarget>` if `TypeNameHandling.Auto` emits it, or expose as an array `IdeDefaultTarget[]?` on `LaunchInfo` so no generic `System.Collections.Generic.List` type name needs binder whitelisting!) — **design call**: use `IdeDefaultTarget[]? DefaultTargets` on `LaunchInfo` to keep `VsSerializationBinder` strictly limited to `VSLXshared`/`VSLauncherX` types.

#### Folder and `.code-workspace` Rules
- **`.code-workspace` entries**: Appear as standalone `VsCodeProject` (`ProjectKind = Workspace`) items inside their containing `VsFolder`. Double-clicking the `.code-workspace` item opens that workspace file in VS Code.
- **Workspace-backed `VsFolder` predicate** (`VsFolder.IsWorkspaceBacked()` in `VSLXshared`):
  - Returns `true` ONLY when:
    1. `ItemType == ItemTypeEnum.Folder` (excludes Visual Studio MRU "Open Folder" items which use `ItemTypeEnum.Project`), AND
    2. `!string.IsNullOrWhiteSpace(Path) && Directory.Exists(Path)`, AND
    3. Either `Items` contains a direct `VsCodeProject` child with `ProjectKind == VsCodeProjectKind.Workspace`, OR `Directory.EnumerateFiles(Path, "*.code-workspace", SearchOption.TopDirectoryOnly).Any()` is `true`.
- **Launching a workspace-backed `VsFolder`** (both top-level double-click AND nested inside an outer group launch):
  - When VS Code is installed: opens `folder.Path` directly in VS Code (honoring `folder.RunBefore`, `folder.RunAfter`, `folder.RunAsAdmin`, `folder.PreferredMonitor`, and `folder.VsCodeOptions`) and does **NOT** recurse into `folder.Items`.
  - When VS Code is NOT installed: falls back to normal group-launch behavior (recursing into `folder.Items` for Visual Studio).
- **Launching folders WITHOUT a `.code-workspace` file** (virtual groups from `dlgAddFolder` or plain disk-backed folders):
  - Double-click / Run preserves existing group-launch behavior (launches contained items recursively, applying the `>3` warning check).
  - Opening the folder in VS Code happens only via the explicit `"Open in VS Code"` context-menu command.
- **`VsFolder` child counts**: Update `VsFolder.ContainedProjectsCount()` to include `VsCodeProject` items alongside `VsProject`. For the multi-launch confirmation count in `MainDialog`, a workspace-backed folder counts as 1 launch (since it does not recurse).

### 5.5 Folder Scanning & Project Detection (`VSLXshared` + `dlgImportFolder`)

Extract folder scanning into a testable `ProjectScanner` class in `VSLXshared`:

1. **Per-directory inspection**:
   - Guard against cycles via canonical full-path `HashSet<string>` (preserving existing symlink/reparse-point safety).
   - Inspect files directly inside the current directory:
     - Visual Studio solutions: `.sln`, `.slnx` (always included).
     - VS Code workspaces: `.code-workspace` -> `VsCodeProject(name, filePath, VsCodeProjectKind.Workspace)` (always included).
     - Visual Studio project files (when `onlySolutions == false`): `.csproj`, `.vbproj`, `.vcxproj`, `.vcproj`, `.fsproj`, `.esproj`, `.tsproj`.
   - When `onlySolutions == false` and the current directory does **NOT** contain a `.code-workspace` file, check for VS Code project markers in priority order:
     1. `package.json` (`NodeJs`)
     2. `pyproject.toml` (`Python`)
     3. `Cargo.toml` (`Rust`)
     4. `go.mod` (`Go`)
     5. `pom.xml`, `build.gradle`, `build.gradle.kts` (`Java`)
     6. `CMakeLists.txt` (`CMake`)
     7. `pubspec.yaml` (`Flutter`)
     8. `composer.json` (`Php`)
     9. `.vscode` subdirectory present (`Folder`)
   - Deduplication:
     - If a directory has ONLY Visual Studio files (no `.code-workspace`, no manifest, no `.vscode/`), emit only the Visual Studio items.
     - If a directory has a `.code-workspace` file AND a manifest (e.g., `package.json`), emit only the `.code-workspace` item (avoiding duplicate entries for the same folder).
2. **Recursion pruning (decided)**:
   - If a VS Code project marker (manifest or `.vscode/`) is detected in the current directory, emit its `VsCodeProject` and **stop recursing deeper** into subdirectories of that directory.
   - If no leaf VS Code project marker is detected (including directories containing a `.code-workspace` file or plain parent directories), continue recursing into eligible subdirectories.
   - Always skip dot-directories (`.git`, `.vs`, `.vscode`, `.venv`, etc. — `.vscode` is checked for existence only, never traversed), hidden/reparse directories, and noise directories (`node_modules`, `bin`, `obj`, `dist`, `build`, `target`, `venv`, `__pycache__`, `packages`, `TestResults`).
3. **Tree shape (`bFlat == false` vs `bFlat == true`)**:
   - When `bFlat == false` (hierarchical): if a scanned subdirectory yielded a single `VsCodeProject` representing that directory itself and no other files/children, emit that `VsCodeProject` directly as a leaf child of its parent `VsFolder` (preventing `my-app > my-app` double nesting). If the directory contains multiple items or child subdirectories (e.g., `.code-workspace` plus `.sln` or subfolders), keep the `VsFolder` node wrapping its items.
   - When `bFlat == true`: collect all leaf items (`VsSolution`, `VsProject`, `VsCodeProject`) directly into the root result list.
4. **`chkSolutionOnly` ("Solutions / Workspaces only") in `dlgImportFolder`**:
   - Checked: includes `.sln`, `.slnx`, and `.code-workspace`.
   - Unchecked: includes all Visual Studio and VS Code project types.
5. **Single-file Add & Drag-and-Drop**:
   - Update `FileHelper.SolutionFilterString` to include `"VS Code Workspaces (*.code-workspace)|*.code-workspace"`.
   - Update `ImportHelper.GetItemFromExtension` to return `VsCodeProject(name, path, VsCodeProjectKind.Workspace)` for `.code-workspace`.
   - Dropping `.code-workspace` onto `MainDialog` adds it directly; dropping a directory invokes `dlgImportFolder(dir)`.

### 5.6 UI Design (`VSLauncherX`)

#### Tabbed Right Sidebar (`MainDialog`)
Replace the single right-side `flowLayoutPanel1` Visual Studio stack with a `TabControl` (`tabControlIdes`) hosting two tabs:
- **Tab 1 — `Visual Studio`**:
  - Preserves existing controls and behavior: `selectVisualStudioVersion` (`IdeCombobox` bound to `IdeType.VisualStudio`), the 5 VS quick-launch buttons (`Start`, `Start as Admin`, `New Instance...`, `New Project...`, `Start...`), `Open ActivityLog`, and `Visual Studio Installer` (which falls back to `Download Visual Studio`).
- **Tab 2 — `VS Code`**:
  - Top dropdown: `selectVsCodeEdition` (`IdeCombobox` bound to `IdeType.VsCode`), custom-drawn with each edition's icon and display name; selection saved in `Properties.Settings.Default.SelectedVsCodeEdition`.
  - Four quick-action buttons (matching the VS button styling, icon rendering, and `<ShortName>` text formatting):
    1. **Start** (`Start {0}`)
    2. **Start as Admin** (`Start {0} as Admin`)
    3. **Start with New Window** (`Start {0} with New Window`)
    4. **Start without Extensions** (`Start {0} without Extensions`)
  - Bottom fallback button (`btnVsCodeDownload`): visible/enabled when no VS Code installation is detected (opens `https://code.visualstudio.com/download`).

#### Single-IDE Disabling Behavior
On startup (`MainDialog` initialization):
- **Only VS Code installed**:
  - Disable `selectVisualStudioVersion`, all 5 VS quick-launch buttons, and `btnMainOpenActivityLog` on the Visual Studio tab; keep `Download Visual Studio` enabled.
  - Select the **VS Code** tab as active by default.
  - Launching `.sln`/`.slnx`/`.*proj` items resolves via `IdeManager`'s single-IDE fallback to VS Code.
- **Only Visual Studio installed**:
  - Disable `selectVsCodeEdition` and the 4 VS Code quick-action buttons on the VS Code tab; keep `Download VS Code` enabled.
  - Disable context-menu `"Open in VS Code"`.
  - Select the **Visual Studio** tab as active by default.
- **Both installed**: Both tabs enabled; restore last active tab (`Properties.Settings.Default.SelectedIdeTab`) or default to Visual Studio.
- **Neither installed**: Disable IDE launch controls on both tabs, enable both Download buttons, and display an informative error if launch is attempted.

#### Generalized `IdeCombobox` Control
Evolve `VisualStudioCombobox` into `IdeCombobox` (keeping or aliasing `VisualStudioCombobox` only if needed for designer compatibility) bound to an `IdeType` and `IdeManager`:
- Supports `ShowDefault` (`"<default>"`), custom owner-drawn icon + name, Light/Dark theming via `ThemeHelper`, and `SelectByIdentifier(string?)` / `SelectFromVersion(string?)`.

#### Tool-Specific Item Settings Dialogs
Dispatch from `MainDialog.settingsToolStripMenuItem_Click` (and `mainImportSoP_Click`):
- `VsCodeProject` -> always **`dlgExecuteVsCode`**.
- `VsSolution` / `VsProject` -> always **`dlgExecuteVisualStudio`**.
- `VsFolder` (workspace-backed or any folder):
  - **Decided**: Depends on which sidebar tab is currently active (`tabControlIdes.SelectedTab`):
    - Active tab is **VS Code** -> opens **`dlgExecuteVsCode`** (binding edition, profile, window mode, disable extensions, and CLI args to `folder.VsCodeOptions`, plus shared Name, Path, RunAsAdmin, PreferredMonitor, and RunBefore/RunAfter).
    - Active tab is **Visual Studio** -> opens **`dlgExecuteVisualStudio`** (existing VS folder settings behavior).

**`dlgExecuteVsCode` controls**:
- **Name**: TextBox (`item.Name`).
- **Path**: TextBox (`item.Path`) + file/folder picker (supports `.code-workspace` files and directories).
- **Edition**: `IdeCombobox` (`IdeType = VsCode`, `ShowDefault = true`) -> binds to `VsCodeProject.VsVersion` or `VsFolder.VsCodeOptions.Edition`.
- **Profile**: Editable ComboBox (`ComboBoxStyle.DropDown`) replacing Visual Studio's Instance dropdown -> populated from `selectedEdition.GetProfiles()` (`<default>` + profiles from `storage.json`); allows entering a new profile name (`--profile` auto-creates it) -> binds to `VsCodeProject.Instance` or `VsFolder.VsCodeOptions.Profile` (`null` when `<default>` or blank).
- **Window Mode**: DropDownList (`Default`, `New Window`, `Reuse Window`) -> binds to `WindowMode`.
- **Disable Extensions**: CheckBox (`--disable-extensions`) -> binds to `DisableExtensions`.
- **Additional Arguments**: TextBox (`Commands`) + LinkLabel to `https://code.visualstudio.com/docs/editor/command-line`.
- **Run as Admin**: CheckBox (`item.RunAsAdmin`).
- **Preferred Monitor**: DropDownList + Ping button (reusing `frmPing`) -> binds to `item.PreferredMonitor`.
- **Before / After**: Button opening existing `dlgBeforeAfter`.
- Visual Studio-only `Show Splash` checkbox is omitted.

#### Combined MRU Import Dialog (`dlgImportVisualStudio`)
- Reuses the existing toolbar button (`Import from VS`, tooltip updated to `"Import recent projects from installed IDEs"`).
- Calls `IdeManager.GetAllRecentProjects(OnlyDefaultInstances)`.
- Shows Visual Studio version/instance groups and VS Code edition groups in the same checkbox tree with their IDE icons.
- Disables/hides `"Only default instances"` when Visual Studio is not installed.

#### Main Tree, Context Menu, Git, Explorer, Taskbar, and Theme
- **Version / IDE Column (`olvColumnVersion`)**:
  - Visual Studio items: unchanged (`<default>` or `2022 (17.x)`).
  - VS Code items: `"<default>"` when unpinned, or `instance.ShortName` (`"VS Code"` / `"VS Code Insiders"`) when an edition is pinned.
  - Workspace-backed `VsFolder`: shows `"VS Code"` / pinned edition short name when VS Code is installed (or `"<default>"`).
- **Icons (`FileIcons` & `ColumnHelper`)**:
  - Register `.code-workspace` icon lookup via `RegisteredFileType`, falling back to the installed VS Code app icon or an embedded fallback icon resource.
  - Render icons and descriptions for `VsCodeProject` in `MainDialog` (32px) and import dialogs (16px/32px).
- **Context Menu (`ctxMenu`)**:
  - Add `"Open in VS Code"`: enabled when VS Code is installed and the selected item has a valid disk path.
  - **Decided**: Invokes the currently selected sidebar `VsCodeInstance.OpenPath(targetPath)` **directly** (bypassing `BackgroundLaunch` and not running `RunBefore`/`RunAfter`/`RunAsAdmin`/`PreferredMonitor`). Target path is `item.Path` for directories and `.code-workspace` files, or `Path.GetDirectoryName(item.Path)` for other files (`.sln`, `.csproj`, etc.).
- **Git Status & Branch Switching (`MainDialog.Utilities.cs` & `MainDialog.cs`)**:
  - Update `ReadItemGitStatus`, `DiscoverRepositoryPath`, and `SetupBranchMenu`: when `Directory.Exists(item.Path)`, call `Repository.Discover(item.Path)` directly instead of `Path.GetDirectoryName(item.Path)`.
- **Open Location in Explorer**:
  - For directory targets (`Directory.Exists(item.Path)`), launch `explorer.exe "<path>"`; for files, retain `explorer.exe /select, "<path>"`.
- **Taskbar JumpList (`AddItemToTaskbar`)**:
  - Resolve launch configuration via `IdeManager` (passing current per-IDE default targets) so favorited VS Code workspaces/projects launch cleanly from the taskbar jump list even when Visual Studio is not installed.
- **Theme Support (`ThemeHelper.cs`)**:
  - Add Light/Dark theming for `TabControl` / `TabPage` (owner-drawn tab headers in dark mode so tabs match the dark palette) and `IdeCombobox`.

### 5.7 Background Launch Pipeline (`ItemLauncher` & `BackgroundLaunch`)

- **`LaunchInfo` & `IdeDefaultTarget`**:
  - Keep `Target` (`string?`) and `Solution` (`VsFolder?`) for backward compatibility.
  - Add `public IdeDefaultTarget[]? DefaultTargets { get; set; }` where `IdeDefaultTarget` has `{ IdeType IdeType; string TargetIdentifier; }` (both registered in `VsSerializationBinder`).
- **`ItemLauncher`**:
  - Accepts `VsItem` or `VsFolder` plus the active sidebar defaults (`IdeDefaultTarget[]` built from `selectVisualStudioVersion` and `selectVsCodeEdition`, plus legacy `Target` populated for backward compatibility).
  - No artificial `VsFolder` -> `VsCodeProject` wrapper is needed because `BackgroundLaunch.Runner` evaluates `VsFolder.IsWorkspaceBacked()` uniformly for both top-level and nested folders.
- **`BackgroundLaunch\Runner.cs`**:
  - Replaces `VisualStudioInstanceManager` with `IdeManager`.
  - Resolves working directory safely: `Directory.Exists(item.Path) ? item.Path : Path.GetDirectoryName(item.Path)`.
  - **`RunAll(VsItem item)`**:
    - Runs `item.RunBefore`.
    - If `item is VsFolder folder`:
      - If `folder.IsWorkspaceBacked() && ideManager.HasInstalled(IdeType.VsCode)`: runs `folder` directly in VS Code via `VsCodeIdeProvider.BuildStartInfo(...)` (honoring `folder.VsCodeOptions`, `folder.RunAsAdmin`, `folder.PreferredMonitor`, and `folder.RunAfter?.WaitForCompletion`) and does **NOT** recurse into `folder.Items`.
      - Otherwise: recurses `RunAll(child)` across `folder.Items` (existing group behavior).
    - If `item` is a launchable item (`VsSolution`, `VsProject`, `VsCodeProject`, `ItemTypeEnum.Other`): resolves target instance via `ideManager.ResolveInstance(item, defaultTargetsMap)` and delegates `ProcessStartInfo` creation to the resolved `IIdeProvider.BuildStartInfo(...)`.
    - **Unavailable IDE inside a mixed group**: If a child item's IDE is unavailable and has no fallback (e.g., a `VsCodeProject` when VS Code is uninstalled), log via `Debug.WriteLine` and skip that child so the rest of the group still launches.
    - Waits on `main` when `item.RunAfter?.WaitForCompletion == true`, then runs `item.RunAfter`.
  - **Known Electron limitation**: When VS Code delegates to an already-running Electron instance, the spawned process may exit before exposing a `MainWindowHandle`; `MoveProcessToMonitor` already handles early process exit safely and remains best-effort for VS Code.

---

## 6. Design Calls Made (For Review)

The following minor technical decisions were settled to keep the design unambiguous:
1. **VS Code MRU filtering**: Only local `file://` folders and `.code-workspace` entries are imported from `state.vscdb`; standalone `fileUri` entries and remote URIs (`vscode-remote://`) are skipped in this iteration.
2. **Manifest project path**: Manifest-detected VS Code projects (`package.json`, `Cargo.toml`, etc.) store the **project directory** in `VsCodeProject.Path` (with `ManifestFile` stored alongside) because VS Code opens folders rather than manifest files.
3. **Single-IDE fallback for `.sln`/`.csproj`**: When only VS Code is installed, launching a Visual Studio solution/project opens its containing folder in VS Code and strips Visual Studio-only `Instance` (`/RootSuffix`) and `Commands` (`devenv.exe` switches) so they do not pollute VS Code arguments or create bogus profiles.
4. **Workspace-backed `VsFolder` scoping**: Auto-opening a folder in VS Code applies only when VS Code is installed and `folder.ItemType == ItemTypeEnum.Folder` (not Visual Studio MRU "Open Folder" items which use `ItemTypeEnum.Project`). Separate `VsCodeLaunchOptions` on `VsFolder` prevent VS Code tab edits from overwriting Visual Studio folder settings.
5. **Scanner tree shape and deduplication**: In tree mode (`bFlat == false`), a standalone manifest/`.vscode/` directory emits a direct `VsCodeProject` leaf under its parent folder (avoiding redundant `app > app` nesting); a directory with a `.code-workspace` file omits a duplicate manifest item for the same directory while still scanning subdirectories for nested `.sln` files unless a leaf manifest/`.vscode/` stops recursion.
6. **Mixed-IDE group launches**: `LaunchInfo` adds `IdeDefaultTarget[]? DefaultTargets` (array, avoiding generic collection types in `VsSerializationBinder`) so both sidebar dropdown selections are honored in mixed groups. Unlaunchable children in a mixed group are skipped with debug logging rather than aborting the group.
7. **VS Code `--wait` reliability**: Triggered when `item.WaitForCompletion || item.RunAfter?.WaitForCompletion == true`; uses `ELECTRON_RUN_AS_NODE=1` with `resources\app\out\cli.js --wait` (when not elevated) so `RunAfter` reliably waits until the window closes without spawning a console window.
8. **Download fallback & settings persistence**: Show a `Download VS Code` button on the VS Code tab when VS Code is not installed; persist `SelectedVsCodeEdition` and `SelectedIdeTab` in `Properties.Settings.Default`.

---

## 7. Left to the Builder

- Exact WinForms control coordinates, margins, and embedded fallback icon bitmaps for VS Code and project kinds.
- Exact internal folder/namespace organization for new IDE provider classes in `VSLXshared` (all serialized model types must be registered in `VsSerializationBinder`).
- Exact file-vs-folder browse UX inside `dlgExecuteVsCode` (e.g., separate File/Folder browse buttons or picker menu).
- Structure of the `VSLXshared.Tests` project and test doubles (using NUnit, Moq, and Shouldly per repository standards).
