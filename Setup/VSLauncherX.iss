; Inno Setup 6 installer script for VSLauncherX (self-contained win-x64)
; Can be compiled locally or from CI via ISCC.exe with /DAppVersion, /DAppFileVersion, /DPublishDir, /DOutputDir, /DOutputBaseFilename.

#ifndef AppVersion
  #define AppVersion "0.2.2"
#endif

#ifndef AppFileVersion
  #define AppFileVersion "0.2.2.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\release"
#endif

#ifndef OutputBaseFilename
  #define OutputBaseFilename "VSLauncherX-" + AppVersion + "-win-x64-setup"
#endif

#define AppName "VSLauncherX"
#define AppPublisher "Hefaistos68"
#define AppURL "https://github.com/Hefaistos68/VSLauncherX"
#define AppExeName "VSLauncherX.exe"

[Setup]
AppId={{93953DFF-12DC-4A3A-86F5-F6D73ECD8345}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
VersionInfoVersion={#AppFileVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppPublisher}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE.md
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile=..\VSLauncherX\Resources\AppLogo2.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
