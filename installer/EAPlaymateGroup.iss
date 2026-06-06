#define MyAppName "EA Playmate Group"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "EA Playmate Group"
#define MyAppExeName "EAPlaymateGroup.exe"
#define PublishDir "..\bin\Release\net9.0\publish"

[Setup]
AppId={{9A6B07C0-897D-4E4C-A5ED-37652F03FA20}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\EA Playmate Group
DefaultGroupName=EA Playmate Group
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=EAPlaymateGroup_Setup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
PrivilegesRequired=lowest
SetupLogging=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "建立桌面捷徑"; GroupDescription: "捷徑："; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.json,appsettings.Development.json,appsettings.Production.json"
Source: "{#PublishDir}\appsettings.json"; DestDir: "{app}"; DestName: "appsettings.json"; Flags: ignoreversion onlyifdoesntexist
Source: "{#PublishDir}\appsettings.example.json"; DestDir: "{app}"; DestName: "appsettings.example.json"; Flags: ignoreversion

[Icons]
Name: "{group}\EA Playmate Group"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\EA Playmate Group"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "啟動 EA Playmate Group"; Flags: nowait postinstall skipifsilent
