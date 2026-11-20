# VS Code-Based IDEs (Forks & Custom Installs) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend VSLauncherX's VS Code provider, updater, context menu, and settings UI so any VS Code-based IDE (e.g., Antigravity IDE, Cursor, Windsurf, VSCodium, plus manually added installations) is discovered, selectable as the sidebar default, launchable via an `"Open in"` submenu, and checked/updated together from the sidebar updater button.

**Architecture:** Keep all VS Code-based editors inside `IdeType.VsCode` (`VsCodeIdeProvider` + generalized `VsCodeInstance`) via data-driven `VsCodeProductDescriptor` definitions, generic `product.json` + `out\cli.js` directory discovery, and a shared `%AppData%\VSLauncher\vscode-custom-ides.json` store readable by both `VSLauncherX` and `BackgroundLaunch`. Generalize `VsCodeUpdateService` to check every installed edition with strategy-based endpoints (VS Code update API, GitHub Releases, or `Unsupported` for forks with placeholder/missing update URLs) and present a modal multi-select dialog (`dlgSelectIdeUpdates`) whenever 2+ IDEs have updates available.

**Tech Stack:** C# / .NET 10 (`net10.0-windows` / `net10.0-windows7.0`), WinForms, Newtonsoft.Json, Microsoft.Data.Sqlite, NUnit, Moq, Shouldly.

**Design Basis:** Approved user requirements (2026-11-20):
1. **Discovery**: Auto-discover installed VS Code-based IDEs (known forks + generic `product.json`/`cli.js` probing) AND allow adding/removing custom IDE paths manually in Settings.
2. **Default edition**: Always determined by the sidebar `selectVsCodeEdition` selection (persisted in `SelectedVsCodeEdition` and propagated to launches, JumpList, workspace auto-pinning, and `dlgExecuteVsCode`).
3. **Context menu**: When 2+ VS Code-based IDEs are installed, display an `"Open in"` submenu listing all discovered IDEs (1 IDE keeps a direct `"Open in <ShortName>"` item).
4. **Updater button**: Check all installed VS Code-based IDEs for updates; when clicked and 2+ IDEs have available updates, display a checklist dialog letting the user choose which IDE(s) to update.

## Global Constraints

- **Target Frameworks**: `VSLXshared` (`net10.0-windows`), `VSLauncherX` (`net10.0-windows7.0`), `BackgroundLaunch` (`net10.0-windows7.0`, `EnforceCodeStyleInBuild=True`, `AnalysisLevel=latest-all`).
- **Backward Compatibility**: Existing `VSLauncher.json` files and existing edition identifiers (`vscode-stable-user`, `vscode-stable-system`, `vscode-insiders-user`, `vscode-insiders-system`) MUST remain unchanged and resolvable without migration. `VsSerializationBinder` type allowlisting in `VSLXshared\Helpers\FileHelper.cs` MUST NOT be loosened.
- **Process Boundary**: Manually registered IDEs MUST be stored in `%AppData%\VSLauncher\vscode-custom-ides.json` (not `VSLauncherX` `user.config`) so `BackgroundLaunch.exe` resolves manually added IDE identifiers identically to `VSLauncherX.exe`.
- **C# Coding Standards**: Max cognitive complexity `15` per method; max `7` parameters per method (except DI constructors); block-scoped namespaces; empty line before `return`, `if`, `for`, `foreach`, `try` (unless preceded by a comment or `{`); empty line separating variable declarations from statements; align subsequent member initializations on `=`; XML documentation comments (`///` or `<inheritdoc/>`) on all new classes, interfaces, and methods; cache any `Regex` as `static readonly Regex`.
- **Testing Standards**: NUnit + Moq + Shouldly in `VSLXshared.Tests` / `BackgroundLaunch.Tests`, creating a fresh SUT per test (never a shared test-class SUT member). Verify builds with `-c Release` (in case `Debug` binaries are locked by a running Visual Studio session).
- **Theming**: Every new or modified form, control, and dynamic context submenu MUST support both Light and Dark palettes via `VSLauncherX\Helpers\ThemeHelper.cs`.
- **Git Workflow**: Implement on a dedicated feature branch (e.g., `feature/vscode-fork-ides`) branched off `origin/master` (or the user's chosen base branch); never commit directly to `master`.

## Review Focus

1. **False-positive non-VS-Code Electron apps (e.g., `%LocalAppData%\Programs\antigravity\Antigravity.exe`)**: Generic auto-discovery MUST require BOTH `resources\app\product.json` AND `resources\app\out\cli.js` inside the install directory and skip uninstaller executables (`unins*.exe`, `Uninstall*.exe`), preventing unrelated Electron apps from appearing in the IDE list. *(Pinned by tests in Task 3.)*
2. **Placeholder `updateUrl` in fork `product.json` (e.g., Antigravity IDE's `"updateUrl": "https://example.com"`)**: `VsCodeUpdateService` MUST classify placeholder hosts (`example.com`, `example.org`, `example.net`, `localhost`, IP literals) and non-HTTPS URLs as `VsCodeUpdateCheckStatus.Unsupported` without issuing an HTTP request or failing the check. *(Pinned by tests in Task 4.)*
3. **Missing/moved manually registered IDE executable & cross-process resolution**: If a saved custom `.exe` is later moved or uninstalled, `VsCodeIdeProvider` MUST skip it gracefully without throwing or deleting the saved entry from `vscode-custom-ides.json`, and `IdeManager` instantiated by `BackgroundLaunch` MUST discover valid custom entries automatically. *(Pinned by tests in Tasks 2 and 3.)*
4. **GDI handle leaks & Dark Mode rendering on dynamic `"Open in"` context submenus**: Rebuilding `"Open in"` dropdown items on every right-click MUST reuse cached/owned 16x16 bitmaps (disposing old bitmaps before clearing) and re-apply `ThemeHelper.ApplyTheme(this.ctxMenu, this.currentTheme)` so nested items render in Dark Mode. *(Pinned by Task 6.)*
5. **Partial failure or UAC cancellation during multi-IDE batch updates**: If one installer in a multi-IDE update batch fails or the user cancels UAC (`exitCode == -1`), the runner MUST still clean up its temporary installer file, process remaining selected IDEs, clear `vsCodeUpdateInstallInProgress`, refresh versions/tooltips for succeeded IDEs, and show one summary warning. *(Pinned by tests and verification in Task 7.)*

## File Structure

- **New files in `VSLXshared\Ide`**:
  - `VsCodeUpdateStrategy.cs` — Enums `VsCodeUpdateStrategyKind` (`VsCodeApi`, `GitHubReleases`, `Unsupported`), `VsCodeInstallerKind` (`InnoSetupVsCode`, `InnoSetupGeneric`, `NsisSilent`, `ExternalDownload`), and `VsCodeUpdateCheckStatus` (`UpToDate`, `UpdateAvailable`, `Unsupported`, `Failed`).
  - `VsCodeProductDescriptor.cs` — Immutable product metadata record + built-in catalog (`KnownProducts`: VS Code Stable, VS Code Insiders, Antigravity IDE, Cursor, Windsurf, VSCodium, Positron, Kiro, Trae) and `product.json` parsing helper.
  - `CustomVsCodeIdeEntry.cs` — Serializable model for a user-added IDE (`ExecutablePath`, optional `DisplayName`, optional `UserDataDirectory`).
  - `CustomVsCodeIdeStore.cs` — Atomic JSON persistence (`%AppData%\VSLauncher\vscode-custom-ides.json`) and executable inspection helper (`TryInspectExecutable`).
  - `VsCodeUpdateSummary.cs` — Pure, UI-independent aggregator computing updatable `(VsCodeInstance, VsCodeUpdateInfo)` pairs, sidebar button text/enabled state, and multi-line tooltips across all installed IDEs.
- **Modified files in `VSLXshared\Ide`**:
  - `IVsCodeEnvironment.cs` & `WindowsVsCodeEnvironment.cs` — Add `GetDirectories(string path)` and `GetFiles(string path, string searchPattern)` for testable directory probing.
  - `VsCodeInstance.cs` — Generalize beyond `IsInsiders` with `ShortName`, `FlavorId`, `DownloadUrl`, `UpdateStrategy`, `InstallerKind`, `GitHubRepo`, `TrustedDownloadHosts`, `IsCustom`, and `IsSystemInstall` while keeping existing constructor overload compatible.
  - `VsCodeIdeProvider.cs` — Descriptor-driven + generic `product.json`/`cli.js` + registry + `PATH` + `CustomVsCodeIdeStore` discovery, plus `Refresh()`.
  - `IIdeProvider.cs` & `IdeManager.cs` — Add `IdeManager.Refresh(IdeType)` (invoking provider refresh when supported) without breaking existing `IIdeProvider` mocks.
  - `VsCodeUpdateInfo.cs` & `VsCodeUpdateService.cs` — Multi-strategy update check (`VsCodeApi`, `GitHubReleases`, `Unsupported`), placeholder URL filtering, per-instance trusted download host validation, `CheckAllForUpdatesAsync`, and `InstallerKind`-aware silent arguments.
- **New & modified files in `VSLauncherX`**:
  - `VSLauncherX\Helpers\ThemeHelper.cs` — Ensure `ListView` (`BackColor`/`ForeColor` in Dark Mode) and `GroupBox` (`ForeColor`) render cleanly in Dark Mode.
  - `VSLauncherX\Forms\dlgSettings.Designer.cs` & `dlgSettings.cs` — Add `"VS Code-based IDEs"` section (`ListView` + `Add...` + `Remove` + `Rescan IDEs`) backed by `CustomVsCodeIdeStore` and `IdeManager`.
  - `VSLauncherX\Forms\dlgExecuteVsCode.cs` — Accept optional `defaultEditionIdentifier` from sidebar selection so `SelectDefaultVsCodeEditionIfUnset()` uses the sidebar default.
  - `VSLauncherX\Forms\dlgSelectIdeUpdates.Designer.cs` & `dlgSelectIdeUpdates.cs` — Modal checklist dialog for selecting which updatable IDEs to update.
  - `VSLauncherX\MainDialog.cs` — Wire settings live refresh, JumpList update on sidebar edition change, dynamic `"Open in"` submenu with icon caching & theme re-application, multi-IDE `btnVsCodeInstaller` state via `VsCodeUpdateSummary`, and sequential batch updater.
- **Test files in `VSLXshared.Tests\Ide`**:
  - `VsCodeProductDescriptorTests.cs` (new)
  - `CustomVsCodeIdeStoreTests.cs` (new)
  - `VsCodeUpdateSummaryTests.cs` (new)
  - `VsCodeInstanceTests.cs` (modified)
  - `VsCodeIdeProviderDiscoveryTests.cs` (modified)
  - `VsCodeUpdateServiceTests.cs` (modified)
  - `IdeManagerTests.cs` (modified)

---

### Task 1: Extend `IVsCodeEnvironment`, Add `VsCodeProductDescriptor` Catalog, and Generalize `VsCodeInstance`

**Files:**
- Create: `VSLXshared\Ide\VsCodeUpdateStrategy.cs`
- Create: `VSLXshared\Ide\VsCodeProductDescriptor.cs`
- Modify: `VSLXshared\Ide\IVsCodeEnvironment.cs:13-68`
- Modify: `VSLXshared\Ide\WindowsVsCodeEnvironment.cs:38-87`
- Modify: `VSLXshared\Ide\VsCodeInstance.cs:17-128`
- Test: `VSLXshared.Tests\Ide\VsCodeProductDescriptorTests.cs` (create)
- Test: `VSLXshared.Tests\Ide\VsCodeInstanceTests.cs` (modify)

**Interfaces:**
- Consumes: `IVsCodeEnvironment`, `IIdeInstance`, `IdeType`
- Produces:
  - `VsCodeUpdateStrategyKind` (`VsCodeApi`, `GitHubReleases`, `Unsupported`)
  - `VsCodeInstallerKind` (`InnoSetupVsCode`, `InnoSetupGeneric`, `NsisSilent`, `ExternalDownload`)
  - `VsCodeUpdateCheckStatus` (`UpToDate`, `UpdateAvailable`, `Unsupported`, `Failed`)
  - `VsCodeProductDescriptor` with properties `FlavorId`, `DisplayName`, `ShortName`, `ExecutableNames` (`IReadOnlyList<string>`), `UserRelativeDirs`, `SystemRelativeDirs`, `UninstallNamePrefixes`, `PathCommandNames`, `UserDataFolderName`, `DownloadUrl`, `UpdateStrategy`, `InstallerKind`, `GitHubRepo`, `TrustedDownloadHosts`, `IsInsiders`, static singletons `VsCodeStable` and `VsCodeInsiders`, `static IReadOnlyList<VsCodeProductDescriptor> KnownProducts`, and `static VsCodeProductDescriptor? TryCreateFromProductJson(IVsCodeEnvironment environment, string installDir, string executablePath)`
  - `IVsCodeEnvironment.GetDirectories(string? path)` and `IVsCodeEnvironment.GetFiles(string? path, string searchPattern)` (returning `IReadOnlyList<string>`)
  - Generalized `VsCodeInstance` constructor `VsCodeInstance(string name, string version, string location, string identifier, string userDataDirectory, VsCodeProductDescriptor descriptor, IVsCodeEnvironment? environment = null)` (7 parameters) with `init` properties `bool IsSystemInstall` and `bool IsCustom`, plus descriptor-backed properties `ShortName`, `FlavorId`, `DownloadUrl`, `UpdateStrategy`, `InstallerKind`, `GitHubRepo`, `TrustedDownloadHosts`, `IsInsiders`; legacy 7-parameter `bool isInsiders` constructor forwards to it via `VsCodeProductDescriptor.VsCodeStable` / `VsCodeInsiders`.

- [ ] **Step 1: Write failing unit tests for `VsCodeProductDescriptor` and generalized `VsCodeInstance`**

Create `VSLXshared.Tests\Ide\VsCodeProductDescriptorTests.cs` with tests asserting:
- `KnownProducts_ContainsExpectedBuiltInFlavors`: `KnownProducts` contains descriptors for `vscode-stable`, `vscode-insiders`, `antigravity-ide`, `cursor`, `windsurf`, `vscodium`, `positron`, `kiro`, and `trae` with unique `FlavorId` values and non-empty `ExecutableNames` / `UserDataFolderName`.
- `TryCreateFromProductJson_WithValidForkMetadata_ExtractsNamesAndDataDir`: Given `resources\app\product.json` containing `nameShort: "Antigravity IDE"`, `nameLong: "Antigravity IDE"`, `win32DirName: "Antigravity IDE"`, `updateUrl: "https://example.com"` and `resources\app\out\cli.js` existing, returns a descriptor with `ShortName == "Antigravity IDE"`, `DisplayName == "Antigravity IDE"`, `UserDataFolderName == "Antigravity IDE"`, and `FlavorId == "antigravity-ide"`.
- `TryCreateFromProductJson_WhenCliJsMissing_ReturnsNull`: Given `product.json` present without `resources\app\out\cli.js`, returns `null`.
- `TryCreateFromProductJson_WhenProductJsonMalformed_ReturnsNull`: Malformed JSON returns `null` without throwing.

Update `VSLXshared.Tests\Ide\VsCodeInstanceTests.cs` with test asserting:
- `Constructor_WithForkMetadata_ExposesCustomShortNameAndCapabilities`: Constructs a `VsCodeInstance` for `"Antigravity IDE"` with `shortName: "Antigravity IDE"`, `flavorId: "antigravity-ide"`, `updateStrategy: VsCodeUpdateStrategyKind.Unsupported`, `isCustom: true`, and asserts `ShortName == "Antigravity IDE"`, `FlavorId == "antigravity-ide"`, `UpdateStrategy == VsCodeUpdateStrategyKind.Unsupported`, `IsCustom == true`, `IsInsiders == false`, while legacy 7-parameter constructor still yields `ShortName == "VS Code"` / `"VS Code Insiders"`.

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~VsCodeProductDescriptorTests|FullyQualifiedName~VsCodeInstanceTests"`
Expected: FAIL with compile errors (`VsCodeProductDescriptor`, `VsCodeUpdateStrategyKind`, and generalized constructor not defined).

- [ ] **Step 3: Implement `VsCodeUpdateStrategy.cs`, `VsCodeProductDescriptor.cs`, `IVsCodeEnvironment` extensions, and generalized `VsCodeInstance`**

- Create `VSLXshared\Ide\VsCodeUpdateStrategy.cs` defining `VsCodeUpdateStrategyKind`, `VsCodeInstallerKind`, and `VsCodeUpdateCheckStatus`.
- Extend `IVsCodeEnvironment` with `IReadOnlyList<string> GetDirectories(string? path)` and `IReadOnlyList<string> GetFiles(string? path, string searchPattern)`; implement both in `WindowsVsCodeEnvironment` returning `Array.Empty<string>()` on missing/inaccessible paths (`IOException`, `UnauthorizedAccessException`, `ArgumentException`).
- Create `VSLXshared\Ide\VsCodeProductDescriptor.cs`:
  - Define `KnownProducts` with VS Code Stable (`vscode-stable`), VS Code Insiders (`vscode-insiders`), Antigravity IDE (`antigravity-ide`, `UpdateStrategy = Unsupported`, `UserRelativeDirs = [@"Programs\Antigravity IDE"]`, `ExecutableNames = ["Antigravity IDE.exe"]`, `UserDataFolderName = "Antigravity IDE"`), Cursor (`cursor`, `UpdateStrategy = VsCodeApi`, `TrustedDownloadHosts = ["downloader.cursor.sh", "downloads.cursor.com", "api2.cursor.sh"]`), Windsurf (`windsurf`, `UpdateStrategy = VsCodeApi`, `TrustedDownloadHosts = ["windsurf-stable.codeiumdata.com", "windsurf-stable.codeium.com"]`), VSCodium (`vscodium`, `UpdateStrategy = GitHubReleases`, `GitHubRepo = "VSCodium/vscodium"`, `TrustedDownloadHosts = ["github.com", "objects.githubusercontent.com", "release-assets.githubusercontent.com"]`), Positron (`positron`, `UpdateStrategy = GitHubReleases`, `GitHubRepo = "posit-dev/positron"`), Kiro (`kiro`, `UpdateStrategy = Unsupported`), and Trae (`trae`, `UpdateStrategy = Unsupported`).
  - Implement `TryCreateFromProductJson(IVsCodeEnvironment environment, string installDir, string executablePath)`: require both `resources\app\product.json` and `resources\app\out\cli.js`; derive `ShortName` (`nameShort` -> `nameLong` -> exe stem), `DisplayName` (`nameLong` -> `nameShort` -> exe stem), `UserDataFolderName` (`win32DirName` -> `nameShort` -> exe stem), slugified `FlavorId` (ASCII lowercase alphanumerics + `-`), and `UpdateStrategy` (`VsCodeApi` when `updateUrl` is a valid non-placeholder HTTPS URL, else `Unsupported`).
- Generalize `VsCodeInstance`:
  - Add constructor `public VsCodeInstance(string name, string version, string location, string identifier, string userDataDirectory, VsCodeProductDescriptor descriptor, IVsCodeEnvironment? environment = null)` (respecting the `<= 7` parameter limit) and `init` properties `public bool IsSystemInstall { get; init; }` and `public bool IsCustom { get; init; }`.
  - Expose `Descriptor`, `ShortName`, `FlavorId`, `DownloadUrl`, `UpdateStrategy`, `InstallerKind`, `GitHubRepo`, `TrustedDownloadHosts`, and `IsInsiders` from the supplied descriptor.
  - Keep the existing 7-parameter `(..., bool isInsiders, IVsCodeEnvironment? environment = null)` constructor forwarding to the descriptor constructor using `isInsiders ? VsCodeProductDescriptor.VsCodeInsiders : VsCodeProductDescriptor.VsCodeStable`.

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~VsCodeProductDescriptorTests|FullyQualifiedName~VsCodeInstanceTests"`
Expected: PASS (all existing + new tests green).

- [ ] **Step 5: Commit**

```powershell
git add VSLXshared/Ide/VsCodeUpdateStrategy.cs VSLXshared/Ide/VsCodeProductDescriptor.cs VSLXshared/Ide/IVsCodeEnvironment.cs VSLXshared/Ide/WindowsVsCodeEnvironment.cs VSLXshared/Ide/VsCodeInstance.cs VSLXshared.Tests/Ide/VsCodeProductDescriptorTests.cs VSLXshared.Tests/Ide/VsCodeInstanceTests.cs
git commit -m "feat(vscode): add product descriptor catalog and generalize VsCodeInstance"
```

---

### Task 2: Shared Custom VS Code IDE Store (`CustomVsCodeIdeStore`)

**Files:**
- Create: `VSLXshared\Ide\CustomVsCodeIdeEntry.cs`
- Create: `VSLXshared\Ide\CustomVsCodeIdeStore.cs`
- Test: `VSLXshared.Tests\Ide\CustomVsCodeIdeStoreTests.cs` (create)

**Interfaces:**
- Consumes: `IVsCodeEnvironment`, `VsCodeProductDescriptor`, `VsCodeIdeProvider.ResolveVersion`
- Produces:
  - `CustomVsCodeIdeEntry` (`ExecutablePath`, `DisplayName`, `UserDataDirectory`)
  - `CustomVsCodeIdeInspection` (`ExecutablePath`, `SuggestedDisplayName`, `SuggestedShortName`, `SuggestedUserDataDirectory`, `Version`, `IsVerifiedVsCodeFork`)
  - `CustomVsCodeIdeStore(IVsCodeEnvironment? environment = null, string? storeFilePath = null)` with:
    - `IReadOnlyList<CustomVsCodeIdeEntry> Load()`
    - `void Save(IEnumerable<CustomVsCodeIdeEntry> entries)`
    - `bool TryAdd(CustomVsCodeIdeEntry entry)`
    - `bool RemoveByPath(string executablePath)`
    - `CustomVsCodeIdeInspection? TryInspectExecutable(string executablePath)`
  - Default store file path: `%AppData%\VSLauncher\vscode-custom-ides.json` (accessible to both `VSLauncherX` and `BackgroundLaunch`).

- [ ] **Step 1: Write failing unit tests for `CustomVsCodeIdeStore`**

Create `VSLXshared.Tests\Ide\CustomVsCodeIdeStoreTests.cs` asserting:
- `Load_WhenFileMissingOrCorrupt_ReturnsEmptyList`: Missing file or invalid JSON returns empty list without throwing.
- `SaveAndLoad_RoundTripsAndDeduplicatesByCanonicalPath`: Adding two entries pointing to the same canonical executable (different casing or `..` segments) keeps one entry and round-trips `ExecutablePath`, `DisplayName`, and `UserDataDirectory` atomically across fresh store instances.
- `RemoveByPath_RemovesMatchingEntryCaseInsensitively`: Removes matching entry and persists updated list.
- `TryInspectExecutable_WithProductJsonAndCliJs_ReturnsVerifiedInspection`: Populates `SuggestedDisplayName`, `SuggestedShortName`, `SuggestedUserDataDirectory` (`%AppData%\<win32DirName>`), `Version`, and `IsVerifiedVsCodeFork == true`.
- `TryInspectExecutable_WithoutProductJson_ReturnsUnverifiedInspection`: Returns `IsVerifiedVsCodeFork == false` and falls back to exe filename stem as `SuggestedDisplayName` so the Settings UI can warn the user before allowing manual addition.

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~CustomVsCodeIdeStoreTests"`
Expected: FAIL with compile errors (`CustomVsCodeIdeStore` / `CustomVsCodeIdeEntry` not defined).

- [ ] **Step 3: Implement `CustomVsCodeIdeEntry.cs` and `CustomVsCodeIdeStore.cs`**

- Store entries in `%AppData%\VSLauncher\vscode-custom-ides.json` using atomic write-to-temp + `File.Move(..., overwrite: true)` guarded by a lock object (matching `VsCodeUpdateService.SaveCacheEntry`).
- Normalize and deduplicate paths via `Path.GetFullPath(...)` case-insensitively on Windows.
- In `TryInspectExecutable(string executablePath)`: validate non-empty existing `.exe`, check `VsCodeProductDescriptor.TryCreateFromProductJson(...)`, read version via `VsCodeIdeProvider.ResolveVersion`, and resolve default `%AppData%\<UserDataFolderName>`.

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~CustomVsCodeIdeStoreTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add VSLXshared/Ide/CustomVsCodeIdeEntry.cs VSLXshared/Ide/CustomVsCodeIdeStore.cs VSLXshared.Tests/Ide/CustomVsCodeIdeStoreTests.cs
git commit -m "feat(vscode): add shared CustomVsCodeIdeStore for manually registered IDEs"
```

---

### Task 3: Generalize `VsCodeIdeProvider` Discovery (Known Forks, Generic `product.json`+`cli.js`, Custom Store) & Live Refresh

**Files:**
- Modify: `VSLXshared\Ide\VsCodeIdeProvider.cs:18-530`
- Modify: `VSLXshared\Ide\IdeManager.cs:49-78`
- Test: `VSLXshared.Tests\Ide\VsCodeIdeProviderDiscoveryTests.cs` (modify)
- Test: `VSLXshared.Tests\Ide\IdeManagerTests.cs` (modify)

**Interfaces:**
- Consumes: `VsCodeProductDescriptor`, `CustomVsCodeIdeStore`, `IVsCodeEnvironment.GetDirectories` / `GetFiles`
- Produces:
  - `VsCodeIdeProvider(IVsCodeEnvironment environment, CustomVsCodeIdeStore? customStore = null)` (default parameter keeps all existing single-arg calls unchanged)
  - `VsCodeIdeProvider.Refresh()` — re-runs discovery and replaces `this.instances` atomically
  - `IdeManager.Refresh(IdeType ideType)` — calls `Refresh()` on `VsCodeIdeProvider` when registered

- [ ] **Step 1: Write failing unit tests covering multi-fork discovery, Review Focus #1, Review Focus #3, and live refresh**

Add tests to `VSLXshared.Tests\Ide\VsCodeIdeProviderDiscoveryTests.cs`:
- `Discovery_DetectsKnownForksIncludingAntigravityIdeAndCursor`: Sets up VS Code Stable, Antigravity IDE (`%LocalAppData%\Programs\Antigravity IDE\Antigravity IDE.exe` + `product.json` + `cli.js`), and Cursor; asserts all 3 are discovered with stable identifiers (`vscode-stable-user`, `antigravity-ide-user`, `cursor-user`), accurate `ShortName`, `UserDataDirectory`, and `DefaultInstance` prefers VS Code Stable.
- `Discovery_GenericAutoDiscovery_DetectsUnknownForkWithProductJsonAndCliJs_AndIgnoresNonVsCodeElectronApps`: Places an unknown fork (`%LocalAppData%\Programs\MyCustomFork\MyCustomFork.exe` + `resources\app\product.json` + `resources\app\out\cli.js`) alongside a non-VS-Code Electron app (`%LocalAppData%\Programs\antigravity\Antigravity.exe` + `Uninstall Antigravity.exe`, no `product.json`/`cli.js`) and an uninstaller (`unins000.exe`); asserts ONLY `MyCustomFork` is discovered and neither `Antigravity.exe` nor `unins000.exe` is included. *(Pins Review Focus #1.)*
- `Discovery_LoadsCustomStoreEntries_DeduplicatesAgainstAutoDiscovered_AndSkipsMissingCustomExe`: Configures a `CustomVsCodeIdeStore` with (a) a valid portable fork at `D:\Tools\PortableCode\Code.exe`, (b) a duplicate path matching auto-discovered VS Code Stable, and (c) a missing executable `Z:\Gone\Fork.exe`; asserts portable fork is discovered with `IsCustom == true` and deterministic `vscode-custom-<hash>` ID, duplicate is not added twice, missing executable is skipped without throwing, and the store file still retains the missing entry. *(Pins Review Focus #3.)*
- `Refresh_ReloadsInstancesAfterCustomStoreMutation`: Adds a custom entry to the store after provider creation, calls `provider.Refresh()`, and asserts `provider.Instances` now includes the new instance.

Add test to `VSLXshared.Tests\Ide\IdeManagerTests.cs`:
- `Refresh_ForVsCodeProvider_RebuildsDiscoveredInstances`: Calls `manager.Refresh(IdeType.VsCode)` and verifies updated instances are returned by `GetInstances(IdeType.VsCode)`.

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~VsCodeIdeProviderDiscoveryTests|FullyQualifiedName~IdeManagerTests"`
Expected: FAIL (new constructor parameter / `Refresh()` / fork discovery not implemented).

- [ ] **Step 3: Refactor `VsCodeIdeProvider` discovery pipeline and add `Refresh()`**

- Structure `DiscoverInstances()` cleanly into small helper methods (each cognitive complexity <= 15):
  1. `DiscoverKnownStandardCandidates(state)` — loops `VsCodeProductDescriptor.KnownProducts` across User (`%LocalAppData%`) and System (`%ProgramFiles%`, `%ProgramFiles(x86)%`) relative dirs.
  2. `DiscoverGenericProgramDirectories(state)` — inspects direct subdirectories of `%LocalAppData%\Programs`, `%ProgramFiles%`, and `%ProgramFiles(x86)%`; for any directory containing both `resources\app\product.json` and `resources\app\out\cli.js`, locates the main `.exe` (excluding `unins*.exe` and `Uninstall*.exe`, preferring exact match to `nameShort` / `win32DirName` / directory name), matches a known descriptor if applicable or builds a generic descriptor via `VsCodeProductDescriptor.TryCreateFromProductJson`.
  3. `DiscoverFromRegistry(state)` — inspects `GetUninstallEntries()`: matches known descriptor `UninstallNamePrefixes`, and also validates any `InstallLocation` containing `resources\app\product.json` + `resources\app\out\cli.js`.
  4. `DiscoverFromPath(state)` — checks `PathCommandNames` from `KnownProducts` (e.g., `code.cmd`, `code-insiders.cmd`, `cursor.cmd`, `windsurf.cmd`, `codium.cmd`, `antigravity-ide.cmd`).
  5. `DiscoverFromCustomStore(state)` — loads entries from `CustomVsCodeIdeStore` (using `this.environment`), skips non-existent executables without mutating the store, inspects `product.json` when available, applies user's custom `DisplayName` / `UserDataDirectory` overrides, and marks `IsCustom = true`.
- Assign deterministic identifiers: `<flavorId>-user` / `<flavorId>-system` for standard User/System installs (preserving exact `vscode-stable-user`, `vscode-stable-system`, `vscode-insiders-user`, `vscode-insiders-system`), and `vscode-custom-<hash>` for custom/duplicate installs.
- Scope display-name disambiguation per `FlavorId` (if both User and System exist for a given `FlavorId`, format as `"{ShortName} (User)"` / `"{ShortName} (System)"`; otherwise `DisplayName`).
- Order `DefaultInstance`: prefer `vscode-stable-*`, then `vscode-insiders-*`, then first discovered instance.
- Add `Refresh()` on `VsCodeIdeProvider` (updating a `volatile IReadOnlyList<VsCodeInstance>`) and `IdeManager.Refresh(IdeType)`.

- [ ] **Step 4: Run full `VSLXshared.Tests` and `BackgroundLaunch.Tests` to verify no regressions**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo; if ($LASTEXITCODE -eq 0) { dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj -c Release --nologo }`
Expected: PASS (all existing + new discovery tests green; `BackgroundLaunch` tests green).

- [ ] **Step 5: Commit**

```powershell
git add VSLXshared/Ide/VsCodeIdeProvider.cs VSLXshared/Ide/IdeManager.cs VSLXshared.Tests/Ide/VsCodeIdeProviderDiscoveryTests.cs VSLXshared.Tests/Ide/IdeManagerTests.cs
git commit -m "feat(vscode): auto-discover VS Code forks via product.json, custom store, and live refresh"
```

---

### Task 4: Multi-IDE Update Checking (`VsCodeUpdateService`, `VsCodeUpdateInfo`, `VsCodeUpdateSummary`)

**Files:**
- Create: `VSLXshared\Ide\VsCodeUpdateSummary.cs`
- Modify: `VSLXshared\Ide\VsCodeUpdateInfo.cs:6-63`
- Modify: `VSLXshared\Ide\VsCodeUpdateService.cs:16-811`
- Test: `VSLXshared.Tests\Ide\VsCodeUpdateServiceTests.cs` (modify)
- Test: `VSLXshared.Tests\Ide\VsCodeUpdateSummaryTests.cs` (create)

**Interfaces:**
- Consumes: `VsCodeInstance`, `VsCodeProductDescriptor`, `VsCodeUpdateStrategyKind`, `VsCodeInstallerKind`, `VsCodeUpdateCheckStatus`
- Produces:
  - `VsCodeUpdateInfo` additions: `UpdateCheckStatus` (`VsCodeUpdateCheckStatus`, default derived consistently with `IsUpdateAvailable`/`CheckSucceeded`), `InstallerKind` (`VsCodeInstallerKind`, default `InnoSetupVsCode`)
  - `VsCodeUpdateService.CheckAllForUpdatesAsync(IEnumerable<VsCodeInstance> instances, bool forceRefresh = false, CancellationToken cancellationToken = default)` returning `Task<IReadOnlyDictionary<string, VsCodeUpdateInfo>>`
  - `VsCodeUpdateService.IsValidUpdateEndpointUrl(string? url)` (rejects non-HTTPS, `example.com`/`example.org`/`example.net`, `localhost`, IP addresses)
  - `VsCodeUpdateService.IsTrustedDownloadUrl(string? url, VsCodeInstance? instance = null)` (preserves 1-arg static overload for existing tests; allows descriptor's `TrustedDownloadHosts` or exact/subdomain match to the validated `updateUrl` host)
  - `VsCodeUpdateService.BuildInstallerStartInfo(string installerPath, bool isSystemInstall, VsCodeInstallerKind installerKind = VsCodeInstallerKind.InnoSetupVsCode)`
  - `VsCodeUpdateSummary.Build(IReadOnlyList<VsCodeInstance> instances, IReadOnlyDictionary<string, VsCodeUpdateInfo> updateStates, VsCodeInstance? selectedInstance, bool installInProgress, string? activeInstallLabel = null)` returning `VsCodeUpdateSummary` with `AvailableUpdates` (`IReadOnlyList<VsCodeUpdateCandidate>`), `ButtonText`, `ButtonEnabled`, `ShowDownloadIcon`, `TooltipText`

- [ ] **Step 1: Write failing unit tests for multi-IDE update checking, Review Focus #2, and `VsCodeUpdateSummary`**

Add tests to `VSLXshared.Tests\Ide\VsCodeUpdateServiceTests.cs`:
- `CheckForUpdateAsync_WhenProductJsonHasPlaceholderUpdateUrl_ReturnsUnsupportedWithoutHttpCall`: Uses Antigravity IDE (`product.json` with `"updateUrl": "https://example.com"`) and a mock `HttpMessageHandler` that fails the test if invoked; asserts `UpdateCheckStatus == VsCodeUpdateCheckStatus.Unsupported`, `IsUpdateAvailable == false`, `CheckSucceeded == true`, and 0 HTTP requests sent. *(Pins Review Focus #2.)*
- `CheckForUpdateAsync_ForVsCodeApiFork_QueriesProductJsonUpdateUrlAndTrustsForkHost`: Sets up a Cursor instance with `product.json` containing `"updateUrl": "https://api2.cursor.sh/updates"`, `"commit": "abcdef1234567"`, `"quality": "stable"`; mock HTTP returns `200 OK` with `url: "https://downloader.cursor.sh/CursorUserSetup-x64.exe"`, `productVersion: "0.49.0"`, `sha256hash: "<sha>"`; asserts `IsUpdateAvailable == true`, `UpdateCheckStatus == VsCodeUpdateCheckStatus.UpdateAvailable`, and `DownloadInstallerAsync` accepts the fork's trusted download host.
- `CheckForUpdateAsync_ForGitHubReleasesFork_ParsesLatestReleaseAndMatchingWinAsset`: Sets up VSCodium (`GitHubRepo = "VSCodium/vscodium"`, version `1.95.0`); mock HTTP returns GitHub release JSON with `tag_name: "1.96.2"` and asset `VSCodiumUserSetup-x64-1.96.2.exe` hosted on `https://github.com/VSCodium/vscodium/releases/download/...`; asserts `IsUpdateAvailable == true`, `LatestVersion == "1.96.2"`, and `DownloadUrl` matches the User x64 `.exe` asset.
- `CheckAllForUpdatesAsync_ChecksAllInstancesAndReturnsPerIdentifierMap`: Passes VS Code Stable (update available), VS Code Insiders (up to date), and Antigravity IDE (unsupported); asserts all 3 identifiers appear with expected statuses.
- `BuildInstallerStartInfo_SelectsArgumentsByInstallerKind`: Verifies `InnoSetupVsCode` uses `/SILENT /NORESTART /CLOSEAPPLICATIONS /MERGETASKS=!runcode`, `InnoSetupGeneric` uses `/SILENT /NORESTART /CLOSEAPPLICATIONS`, and `NsisSilent` uses `/S`.

Create `VSLXshared.Tests\Ide\VsCodeUpdateSummaryTests.cs`:
- `Build_WhenNoIdesInstalled_ReturnsDownloadState`
- `Build_WhenOneOfMultipleIdesHasUpdate_EnablesButtonForThatIdeEvenIfDifferentIdeSelected`: Selected IDE is Antigravity IDE (unsupported), VS Code Stable has an update -> asserts `ButtonEnabled == true`, `ButtonText == "Update VS Code"`, `AvailableUpdates.Count == 1`.
- `Build_WhenMultipleIdesHaveUpdates_ReturnsMultiUpdateCountAndCombinedTooltip`: 2 IDEs have updates -> asserts `ButtonEnabled == true`, `ButtonText == "Update IDEs (2)"`, `AvailableUpdates.Count == 2`, tooltip includes both IDE names and versions.
- `Build_WhenAllIdesUpToDateOrUnsupported_DisablesButtonWithPerIdeTooltip`: Asserts `ButtonEnabled == false` and tooltip summarizes both up-to-date and manual-update-only IDEs.
- `Build_WhenInstallInProgress_DisablesButtonWithActiveProgressLabel`.

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~VsCodeUpdateServiceTests|FullyQualifiedName~VsCodeUpdateSummaryTests"`
Expected: FAIL with compile errors.

- [ ] **Step 3: Implement multi-strategy `VsCodeUpdateService`, `VsCodeUpdateInfo`, and `VsCodeUpdateSummary`**

- Update `VsCodeUpdateInfo` with `UpdateCheckStatus` and `InstallerKind` (keep existing defaults compatible with cached JSON in `vscode-update-cache.json`).
- In `VsCodeUpdateService`:
  - Add `IsValidUpdateEndpointUrl(string? url)`: require `Uri.TryCreate`, `https` scheme, non-empty host, not an IP literal (`UriHostNameType.Dns`), not `localhost`, not `*.local`, and not reserved RFC 2606 example domains (`example.com`, `example.org`, `example.net`, `*.example`, `*.invalid`, `*.test`).
  - Resolve effective update strategy from `instance.UpdateStrategy` + `product.json` (`updateUrl`):
    - `VsCodeApi`: resolve base URL (Microsoft `UpdateApiBaseUrl` for `vscode-stable`/`vscode-insiders`, else validated `product.json` `updateUrl` trimmed of trailing `/`). If base URL fails `IsValidUpdateEndpointUrl`, return `Unsupported`.
    - `GitHubReleases`: query `https://api.github.com/repos/{instance.GitHubRepo}/releases/latest` (with `Accept: application/vnd.github+json`), compare `tag_name` vs `localInfo.Version` via `CompareVersions`, select matching Windows arch + User/System `.exe` asset.
    - `Unsupported`: return immediately with `UpdateCheckStatus = Unsupported`, `CheckSucceeded = true`, `IsUpdateAvailable = false` (never hitting the network).
  - Add `CheckAllForUpdatesAsync` iterating instances cleanly (respecting cancellation and per-instance 24h cache).
  - Keep `DownloadInstallerAsync(VsCodeUpdateInfo, VsCodeInstance?, CancellationToken)` backward-compatible (existing 2-arg signature delegates with `instance: null`), checking `IsTrustedDownloadUrl(updateInfo.DownloadUrl, instance)` (also preserving the existing 1-arg `IsTrustedDownloadUrl(string?)` for Microsoft hosts).
- Implement `VsCodeUpdateSummary` as a pure static builder in `VSLXshared\Ide\VsCodeUpdateSummary.cs`.

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --filter "FullyQualifiedName~VsCodeUpdate"`
Expected: PASS (all existing + new update tests green).

- [ ] **Step 5: Commit**

```powershell
git add VSLXshared/Ide/VsCodeUpdateInfo.cs VSLXshared/Ide/VsCodeUpdateService.cs VSLXshared/Ide/VsCodeUpdateSummary.cs VSLXshared.Tests/Ide/VsCodeUpdateServiceTests.cs VSLXshared.Tests/Ide/VsCodeUpdateSummaryTests.cs
git commit -m "feat(vscode): support multi-IDE update checking and status aggregation"
```

---

### Task 5: Manual IDE Registration in `dlgSettings`, Live Reload, and Sidebar-Driven Default Edition

**Files:**
- Modify: `VSLauncherX\Helpers\ThemeHelper.cs:404-505`
- Modify: `VSLauncherX\Forms\dlgSettings.Designer.cs:1-156`
- Modify: `VSLauncherX\Forms\dlgSettings.cs:1-45`
- Modify: `VSLauncherX\Forms\dlgExecuteVsCode.cs:20-55,183-200`
- Modify: `VSLauncherX\MainDialog.cs:310-375,770-855,1735-1755`

**Interfaces:**
- Consumes: `CustomVsCodeIdeStore`, `CustomVsCodeIdeEntry`, `CustomVsCodeIdeInspection`, `IdeManager.Refresh(IdeType)`, `ThemeHelper.ApplyCurrentTheme`
- Produces:
  - `dlgSettings(IdeManager? ideManager = null, CustomVsCodeIdeStore? customIdeStore = null)` (optional parameters preserve parameterless designer support) with `bool CustomIdesChanged { get; }` and `bool IdesRescanned { get; }`
  - `dlgExecuteVsCode(VsItem item, IdeManager ideManager, string? defaultEditionIdentifier = null)` — uses `defaultEditionIdentifier` in `SelectDefaultVsCodeEditionIfUnset()`
  - `MainDialog.RefreshVsCodeIdesAfterSettingsChange(bool reRunDiscovery = true)` — optionally refreshes `ideManager`, reloads `selectVsCodeEdition` preserving current selection, updates sidebar buttons/state, rebuilds taskbar JumpList, and triggers `CheckVsCodeUpdatesAsync(forceRefresh: false)`

- [ ] **Step 1: Verify `ThemeHelper` dark styling for `ListView` and `GroupBox`**

In `VSLauncherX\Helpers\ThemeHelper.cs` inside `ApplySpecificControlTheme`:
- Update the `case ListView listView:` block to explicitly set `listView.BackColor = palette.SurfaceAlt;` and `listView.ForeColor = palette.Text;` (in addition to `BorderStyle` and `ApplyNativeScrollbarTheme`) so `ListView` controls in `dlgSettings` and `dlgSelectIdeUpdates` do not inherit wrong backgrounds in Dark Mode.
- Add `case GroupBox groupBox:` setting `groupBox.BackColor = groupBox.Parent is Form ? palette.Window : palette.Surface;` and `groupBox.ForeColor = palette.Text;`.

- [ ] **Step 2: Add `"VS Code-based IDEs"` management UI to `dlgSettings`**

- Resize `dlgSettings` cleanly (e.g., `ClientSize = new Size(560, 440)`) and add a `GroupBox` (`grpVsCodeIdes`, text `"VS Code-based IDEs"`) below `chkDarkMode` containing:
  - `ListView` (`lvVsCodeIdes`, `View = View.Details`, `FullRowSelect = true`, `HideSelection = false`, columns: `Name`, `Source` (`Auto` / `Manual`), `Executable Path`).
  - `Button` (`btnAddVsCodeIde`, text `"Add..."`) — opens `OpenFileDialog` (`Executable files (*.exe)|*.exe`), inspects via `CustomVsCodeIdeStore.TryInspectExecutable(path)`:
    - Rejects duplicates already in the list with an informational `MessageBox`.
    - If `!inspection.IsVerifiedVsCodeFork`, prompts a Yes/No confirmation (`"The selected executable does not appear to contain VS Code metadata (resources\\app\\product.json and cli.js). Add it anyway?"`).
    - Adds pending `CustomVsCodeIdeEntry` to the dialog's working list and refreshes `lvVsCodeIdes`.
  - `Button` (`btnRemoveVsCodeIde`, text `"Remove"`, enabled only when a `Manual` entry is selected) — removes the selected custom entry from the pending list.
  - `Button` (`btnRescanVsCodeIdes`, text `"Rescan IDEs"`) — calls `this.ideManager?.Refresh(IdeType.VsCode)` under `Cursors.WaitCursor`, rebuilds `lvVsCodeIdes` by merging the newly discovered auto instances with the dialog's pending manual entries (deduplicating by canonical path), and sets `IdesRescanned = true`.
- Persist pending custom entries via `CustomVsCodeIdeStore.Save(...)` only when `btnOk` is clicked (cancelling `dlgSettings` discards unsaved add/remove edits), setting `CustomIdesChanged = true` when changes were saved.

- [ ] **Step 3: Wire live reload and sidebar-default propagation in `MainDialog` and `dlgExecuteVsCode`**

- In `VSLauncherX\Forms\dlgExecuteVsCode.cs`:
  - Add optional `string? defaultEditionIdentifier = null` parameter to `dlgExecuteVsCode` constructor and store in field `this.defaultEditionIdentifier`.
  - Update `SelectDefaultVsCodeEditionIfUnset()` to call `this.ideManager.ResolvePreferredIdentifier(IdeType.VsCode, this.defaultEditionIdentifier)`.
- In `VSLauncherX\MainDialog.cs`:
  - Pass `this.selectVsCodeEdition.SelectedIdeInstance?.Identifier` when constructing `dlgExecuteVsCode` in `ShowItemSettingsDialog`.
  - Pass `this.ideManager` into `new dlgSettings(this.ideManager)` in `mainSettings_Click`:
    - When `DialogResult.OK`: if `dlg.CustomIdesChanged || dlg.IdesRescanned`, call `RefreshVsCodeIdesAfterSettingsChange(reRunDiscovery: dlg.CustomIdesChanged)`.
    - When `DialogResult.Cancel` but `dlg.IdesRescanned` is true: call `RefreshVsCodeIdesAfterSettingsChange(reRunDiscovery: false)` so the sidebar and update checker stay synchronized with the already-refreshed `ideManager`.
  - Implement `RefreshVsCodeIdesAfterSettingsChange(bool reRunDiscovery = true)`:
    - Capture previous selected identifier (`GetSelectedVsCodeInstance()?.Identifier ?? Properties.Settings.Default.SelectedVsCodeEdition`).
    - When `reRunDiscovery` is true, call `this.ideManager.Refresh(IdeType.VsCode)`.
    - Re-run `InitializeVsCodeSelector()`, `SetVsCodeSidebarEnabled(this.ideManager.HasInstalled(IdeType.VsCode))`, update quick-action buttons if an instance is selected, call `RebuildTaskbarItems()`, and call `CheckVsCodeUpdatesAsync(forceRefresh: false)`.
  - In `selectVsCodeEdition_SelectedIndexChanged` (when `!this.bInUpdate`): also call `RebuildTaskbarItems()` so taskbar favorites reflect the updated sidebar default edition immediately.

- [ ] **Step 4: Build and verify existing tests pass**

Run: `dotnet build VSLauncher.sln -c Release --no-incremental --nologo -clp:Summary; if ($LASTEXITCODE -eq 0) { dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --no-build }; if ($LASTEXITCODE -eq 0) { dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj -c Release --nologo --no-build }`
Expected: `0 Error(s)`, all unit tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add VSLauncherX/Helpers/ThemeHelper.cs VSLauncherX/Forms/dlgSettings.Designer.cs VSLauncherX/Forms/dlgSettings.cs VSLauncherX/Forms/dlgExecuteVsCode.cs VSLauncherX/MainDialog.cs
git commit -m "feat(ui): add manual VS Code IDE management in settings and propagate sidebar default"
```

---

### Task 6: Context Menu `"Open in"` Submenu for Multiple VS Code-Based IDEs

**Files:**
- Modify: `VSLauncherX\MainDialog.cs:35-45,520-545,2548-2620`

**Interfaces:**
- Consumes: `IdeManager.GetInstances(IdeType.VsCode)`, `VsCodeInstance.OpenPath(string)`, `ThemeHelper.ApplyTheme(ToolStripDropDown?, ThemePalette)`
- Produces:
  - `MainDialog.ConfigureOpenInMenu(bool hasDiskPath)` — configures `openInVsCodeToolStripMenuItem` for 0, 1, or 2+ installed VS Code-based IDEs
  - `MainDialog.OpenSelectedItemInVsCodeInstance(VsCodeInstance instance)` — shared target-path resolution & launch helper
  - Proper disposal of cached 16x16 menu bitmaps (`openInMenuIcons`) on refresh and form close (pins Review Focus #4)

- [ ] **Step 1: Implement `ConfigureOpenInMenu` with bitmap cache/disposal and Dark Mode theming**

In `VSLauncherX\MainDialog.cs`:
- Add a small dictionary/list `private readonly Dictionary<string, Bitmap> openInMenuIcons = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);` (disposed on `MainDialog_FormClosing` and cleared/disposed inside `RefreshVsCodeIdesAfterSettingsChange()`).
- Implement helper `GetMenuIconBitmap(VsCodeInstance instance)` returning a cached 16x16 `Bitmap` converted once per instance identifier (never allocating a fresh `Bitmap` per right-click).
- Implement `ConfigureOpenInMenu(bool hasDiskPath)` called from `ctxMenu_Opening`:
  - Read `List<VsCodeInstance> instances = this.ideManager.GetInstances(IdeType.VsCode).OfType<VsCodeInstance>().ToList();`
  - Clear `this.openInVsCodeToolStripMenuItem.DropDownItems`.
  - **Case 0 instances**: `Text = "Open in VS Code"`, `Image = null`, `Enabled = false`.
  - **Case 1 instance**: `Text = $"Open in {instances[0].ShortName}"`, `Image = GetMenuIconBitmap(instances[0])`, `Enabled = hasDiskPath`, `Tag = instances[0]`.
  - **Case 2+ instances**: `Text = "Open in"`, `Image = null`, `Enabled = hasDiskPath`, `Tag = null`; populate `DropDownItems` with one `ToolStripMenuItem` per instance:
    - `Text = instance.Name`, `Image = GetMenuIconBitmap(instance)`, `Tag = instance`.
    - Bold the font on the item matching `GetSelectedVsCodeInstance()?.Identifier` so the sidebar default is immediately recognizable.
    - Wire `Click += OpenInSubmenuItem_Click`.
  - Call `ThemeHelper.ApplyTheme(this.ctxMenu, this.currentTheme);` when dropdown items were populated so Dark Mode colors and renderer apply to every submenu item. *(Pins Review Focus #4.)*
- Update `openInVsCodeToolStripMenuItem_Click`:
  - Return immediately if `this.openInVsCodeToolStripMenuItem.HasDropDownItems`.
  - Otherwise open in `(this.openInVsCodeToolStripMenuItem.Tag as VsCodeInstance) ?? GetSelectedVsCodeInstance()`.
- Implement `OpenInSubmenuItem_Click` delegating to `OpenSelectedItemInVsCodeInstance((sender as ToolStripMenuItem)?.Tag as VsCodeInstance)`.

- [ ] **Step 2: Build and verify**

Run: `dotnet build VSLauncher.sln -c Release --nologo -clp:Summary; if ($LASTEXITCODE -eq 0) { dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --no-build }`
Expected: `0 Error(s)`, all unit tests PASS.

- [ ] **Step 3: Commit**

```powershell
git add VSLauncherX/MainDialog.cs
git commit -m "feat(ui): use Open in submenu when multiple VS Code-based IDEs are installed"
```

---

### Task 7: Multi-IDE Update Picker Dialog (`dlgSelectIdeUpdates`) and Multi-Update Orchestration

**Files:**
- Create: `VSLauncherX\Forms\dlgSelectIdeUpdates.Designer.cs`
- Create: `VSLauncherX\Forms\dlgSelectIdeUpdates.cs`
- Modify: `VSLauncherX\MainDialog.cs:2180-2518`

**Interfaces:**
- Consumes: `VsCodeUpdateService.CheckAllForUpdatesAsync`, `VsCodeUpdateService.DownloadInstallerAsync`, `VsCodeUpdateService.BuildInstallerStartInfo`, `VsCodeUpdateSummary.Build`, `VsCodeUpdateCandidate`, `ThemeHelper.ApplyCurrentTheme`
- Produces:
  - `dlgSelectIdeUpdates(IReadOnlyList<VsCodeUpdateCandidate> availableUpdates)` with `IReadOnlyList<VsCodeUpdateCandidate> SelectedUpdates { get; }`
  - `MainDialog.RunVsCodeUpdatesAsync(IReadOnlyList<VsCodeUpdateCandidate> targets)` — sequential batch updater with per-IDE error/UAC isolation and summary reporting

- [ ] **Step 1: Create `dlgSelectIdeUpdates` modal dialog**

- Design `VSLauncherX\Forms\dlgSelectIdeUpdates.Designer.cs` & `dlgSelectIdeUpdates.cs`:
  - `FormBorderStyle = FormBorderStyle.FixedDialog`, `StartPosition = FormStartPosition.CenterParent`, `Text = "Select IDE Updates"`, `MinimizeBox = false`, `MaximizeBox = false`.
  - Top label explaining: `"Select the IDEs to download and update:"`.
  - `ListView` (`lvUpdates`, `View = View.Details`, `CheckBoxes = true`, `FullRowSelect = true`, `SmallImageList` populated with 16x16 icons for each candidate, columns: `IDE` (`210px`), `Installed` (`100px`), `Available` (`100px`)). All items pre-checked on load; store each `VsCodeUpdateCandidate` in `item.Tag`.
  - Bottom buttons: `Select All`, `Select None`, `Update Selected` (`DialogResult.OK`, enabled iff `lvUpdates.CheckedItems.Count > 0`), and `Cancel` (`DialogResult.Cancel`).
  - Call `ThemeHelper.ApplyCurrentTheme(this)` in constructor and dispose the `ImageList` cleanly in `Dispose`.
  - On `btnUpdate_Click`, populate `SelectedUpdates` from checked items' `Tag` values.

- [ ] **Step 2: Wire `VsCodeUpdateSummary`, `dlgSelectIdeUpdates`, and sequential batch updating in `MainDialog.cs`**

- Update `CheckVsCodeUpdatesAsync(bool forceRefresh)` to call `await this.vsCodeUpdateService.CheckAllForUpdatesAsync(editions, forceRefresh, ct)` and store all returned entries in `this.vsCodeUpdateStates` (pruning stale identifiers no longer present in `editions`).
- Replace single-edition button logic in `UpdateVsCodeInstallerButtonState()` with `VsCodeUpdateSummary.Build(editions, this.vsCodeUpdateStates, GetSelectedVsCodeInstance(), installInProgress, this.activeVsCodeUpdateStatus)`:
  - Sets `btnVsCodeInstaller.Text`, `Image` (`Resources.Download` vs `Resources.Installer`), `Enabled`, and tooltip from the summary.
- Update `btnVsCodeInstaller_Click`:
  - If `!this.ideManager.HasInstalled(IdeType.VsCode)` -> `OpenVsCodeDownloadPage()`.
  - Compute `IReadOnlyList<VsCodeUpdateCandidate> available` via `VsCodeUpdateSummary.Build(...)`.
  - If `available.Count == 0` -> return.
  - If `available.Count == 1` -> `await RunVsCodeUpdatesAsync(available)`.
  - If `available.Count > 1` -> show `using var dlg = new dlgSelectIdeUpdates(available);`; if `dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedUpdates.Count > 0`, `await RunVsCodeUpdatesAsync(dlg.SelectedUpdates)`.
- Refactor `RunVsCodeUpdateAsync` into `RunVsCodeUpdatesAsync(IReadOnlyList<VsCodeUpdateCandidate> targets)` (keeping cognitive complexity <= 15 via helper `RunSingleVsCodeUpdateAsync`):
  - Guard with `Interlocked.CompareExchange(ref this.vsCodeUpdateInstallInProgress, 1, 0)`.
  - Loop `i = 0..targets.Count - 1`:
    - Update `this.activeVsCodeUpdateStatus` and `this.mainStatusLabel.Text` (e.g., `$"Downloading {vsCode.ShortName} ({i + 1}/{targets.Count})..."` and `$"Updating {vsCode.ShortName} ({i + 1}/{targets.Count})..."`).
    - Call `DownloadInstallerAsync(updateInfo, vsCode, ct)` and `LaunchVsCodeInstallerAsync(installerPath, updateInfo.IsSystemInstall, updateInfo.InstallerKind, ct)`.
    - Always delete `installerPath` in inner `finally` before moving to the next IDE.
    - On `exitCode == 0`: re-check `CheckForUpdateAsync(vsCode, forceRefresh: true, ct)`, call `vsCode.RefreshVersion(fallbackVersion)`, refresh `this.vsCodeUpdateStates[vsCode.Identifier]`, invalidate `this.selectVsCodeEdition` (so displayed version updates), and call `UpdateVsCodeQuickActionButtons(vsCode)` if currently selected.
    - On non-zero failure (`exitCode != 0 && exitCode != -1`) or exception: record failure line for consolidated summary and continue to the next selected IDE. *(Pins Review Focus #5.)*
  - In outer `finally`: clear `activeVsCodeUpdateStatus`, reset `vsCodeUpdateInstallInProgress` to `0`, restore cursor/status bar, and call `UpdateVsCodeInstallerButtonState()`.
  - If any failures were recorded and `!this.IsDisposed`, display one consolidated warning `MessageBox`.

- [ ] **Step 3: Full solution Release build and full unit test verification**

Run: `dotnet build VSLauncher.sln -c Release --no-incremental --nologo -clp:Summary; if ($LASTEXITCODE -eq 0) { dotnet test VSLXshared.Tests\VSLXshared.Tests.csproj -c Release --nologo --no-build }; if ($LASTEXITCODE -eq 0) { dotnet test BackgroundLaunch.Tests\BackgroundLaunch.Tests.csproj -c Release --nologo --no-build }`
Expected: `0 Error(s)`, 0 new warnings, all tests in `VSLXshared.Tests` and `BackgroundLaunch.Tests` PASS.

- [ ] **Step 4: Commit**

```powershell
git add VSLauncherX/Forms/dlgSelectIdeUpdates.Designer.cs VSLauncherX/Forms/dlgSelectIdeUpdates.cs VSLauncherX/MainDialog.cs
git commit -m "feat(vscode): add multi-IDE update picker dialog and batch updater"
```
