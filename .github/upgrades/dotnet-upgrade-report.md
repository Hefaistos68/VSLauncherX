# .NET8 Upgrade Report

## Project target framework modifications

| Project name | Old Target Framework | New Target Framework | Commits |
|:-----------------------------------------------|:---------------------------------:|:---------------------------:|:-------------------------------------------|
| VSLXshared\VSLXshared.csproj | net6.0-windows | net8.0-windows7.0 |264d3893,581cb580, f9495082 |
| ObjectListView\ObjectListView2022.csproj | net6.0-windows7.0 | net8.0-windows7.0 |31915a20,0b182a00,9da51f61 |
| BackgroundLaunch\BackgroundLaunch.csproj | net6.0-windows10.0.17763.0 | net8.0-windows7.0 |9da51f61,7a4baea0, c04cf2c8 |
| VSLauncherX\VSLauncherX.csproj | net6.0-windows10.0.17763.0 | net8.0-windows7.0 |70bb74e9,4b04ad4b,44fd947e, d54b0464 |

## NuGet Packages

| Package Name | Old Version | New Version | Commit Id |
|:------------------------------------|:-----------:|:-----------:|:-----------|
| Newtonsoft.Json |13.0.3 |13.0.4 |581cb580 |
| System.Drawing.Common |7.0.0 |8.0.21 |581cb580 |
| System.Management (VSLXshared) |7.0.2 |8.0.0 |581cb580 |
| System.Management (VSLauncherX) |9.0.10 |8.0.0 |4b04ad4b |

## All commits

| Commit ID | Description |
|:-----------|:----------------------------------------------------------------------------------------------------------------|
|8f2d2f67 | Commit upgrade plan |
|264d3893 | Update VSLXshared.csproj to target .NET8.0 |
|581cb580 | Update package versions in VSLXshared.csproj |
| daaf4376 | Added using directive for System.Windows.Forms in VisualStudioInstance.cs |
| f9495082 | Store final changes for step 'Upgrade VSLXshared\VSLXshared.csproj' |
|31915a20 | Disambiguated MethodInvoker usages and removed redundant using in VirtualObjectListView.cs |
|0b182a00 | Additional MethodInvoker disambiguation in VirtualObjectListView.cs |
|9da51f61 | Update projects to target .NET8.0 |
|70d68425 | Switch state serialization to System.Text.Json, framework adjustments |
|7a4baea0 | Update BackgroundLaunch.csproj target framework |
| c04cf2c8 | Update BackgroundLaunch.csproj to target net8.0-windows7.0 |
|70bb74e9 | Add credential-aware Git operations and update dependencies |
|4b04ad4b | Downgrade System.Management in VSLauncherX.csproj |
|44fd947e | Update VSLauncherX.csproj to target net8.0-windows7.0 |
| d54b0464 | Store final changes for step 'Upgrade VSLauncherX\VSLauncherX.csproj' |

## Project feature upgrades

### VSLXshared\VSLXshared.csproj
- Target framework upgraded and packages updated (Newtonsoft.Json, System.Drawing.Common, System.Management).
- Added Windows Forms using to fix MessageBox reference.

### ObjectListView\ObjectListView2022.csproj
- Target framework upgraded. Resolved MethodInvoker ambiguity and removed unneeded reflection using.
- Migrated state serialization from BinaryFormatter to System.Text.Json.

### BackgroundLaunch\BackgroundLaunch.csproj
- Target framework consolidated to net8.0-windows7.0 for compatibility.

### VSLauncherX\VSLauncherX.csproj
- Target framework upgraded; credential-aware Git operations added (branch checkout, fetch, pull with PAT / default credentials).
- Adjusted System.Management package version for consistency.
- Removed Google Drive experimental sync.

## Next steps
- Consider adding WPF migration plan (full UI rewrite with dark mode) after verifying stability on .NET8.
- Review any remaining System.Drawing usage for future cross-platform considerations.
- Implement dark mode theming (WinForms or during WPF migration).

## Upgrade session resource usage
- Model tokens/cost tracking not available.
