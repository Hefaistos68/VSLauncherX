# .NET8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET8.0 upgrade.
3. Upgrade VSLXshared\VSLXshared.csproj
4. Upgrade ObjectListView\ObjectListView2022.csproj
5. Upgrade BackgroundLaunch\BackgroundLaunch.csproj
6. Upgrade VSLauncherX\VSLauncherX.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

(No excluded projects)

### Aggregate NuGet packages modifications across all projects

| Package Name | Current Version | New Version | Description |
|:--------------------|:---------------:|:-----------:|:----------------------------------------------|
| Newtonsoft.Json |13.0.3 |13.0.4 | Recommended update for .NET8 compatibility |
| System.Drawing.Common |7.0.0 |8.0.21 | Recommended update for .NET8 compatibility |
| System.Management |7.0.2 |8.0.0 | Recommended update for .NET8 compatibility |

### Project upgrade details

#### VSLXshared\VSLXshared.csproj modifications

Project properties changes:
 - Target framework should be changed from `net6.0-windows` to `net8.0--windows`

NuGet packages changes:
 - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4`
 - System.Drawing.Common should be updated from `7.0.0` to `8.0.21`
 - System.Management should be updated from `7.0.2` to `8.0.0`

Other changes:
 - Review Windows-specific APIs for .NET8 compatibility.

#### ObjectListView\ObjectListView2022.csproj modifications

Project properties changes:
 - Target framework should be changed from `net6.0-windows7.0` to `net8.0--windows7.0`

Other changes:
 - Validate custom renderers and GDI+ usage for any System.Drawing limitations under .NET8.

#### BackgroundLaunch\BackgroundLaunch.csproj modifications

Project properties changes:
 - Target framework should be changed from `net6.0-windows10.0.17763.0` to `net8.0-windows`

Other changes:
 - Verify any Windows API calls and adjust if deprecated.

#### VSLauncherX\VSLauncherX.csproj modifications

Project properties changes:
 - Target framework should be changed from `net6.0-windows10.0.17763.0` to `net8.0-windows`

NuGet packages changes:
 - Newtonsoft.Json should be updated from `13.0.3` to `13.0.4`
 - System.Management should be updated from `7.0.2` to `8.0.0`

Other changes:
 - Ensure WinForms designer compatibility after upgrade.
