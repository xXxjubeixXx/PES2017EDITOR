#ifndef AppVersion
  #error AppVersion must be supplied by scripts/build-installer.ps1
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\installer-payload"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

#define AppName "PES 2017 Database Editor"
#define AppPublisher "PES 2017 Database Editor Contributors"
#define AppURL "https://github.com/xXxjubeixXx/PES2017EDITOR"
#define AppExeName "PES2017DatabaseEditor.exe"
#define AppId "{{2D7CBF18-2C78-4E35-8F1A-4F2E4B35A201}}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\PES 2017 Database Editor
DefaultGroupName={#AppName}
DisableProgramGroupPage=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=PES2017DatabaseEditor-Setup-v{#AppVersion}-win-x64
SetupIconFile=..\assets\PES2017DatabaseEditor.ico
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoVersion={#AppVersion}.0
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
AllowNoIcons=yes
CloseApplications=yes
RestartApplications=no
Uninstallable=yes
ChangesAssociations=no
OutputManifestFile=installer-manifest.txt

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Iniciar {#AppName}"; Flags: postinstall nowait skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
