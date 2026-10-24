; Generals Vanguard: one-click Windows installer.
; Copyright (C) 2026 KuiseRapper
; SPDX-License-Identifier: GPL-3.0-or-later
;
; Build from the repository root after the launcher has been published:
;   ISCC.exe /DMyAppVersion=0.6.0 Tools\VanguardInstaller\VanguardSetup.iss
; The release workflow does this for every release.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#define MyAppName "Generals Vanguard"
#define MyAppPublisher "KuiseRapper"
#define MyAppUrl "https://github.com/KuiseRapper/generals-vanguard"
#define MyAppExeName "VanguardLauncher.exe"

[Setup]
; Keep this AppId stable: it is how Windows recognises an update to the same install.
AppId={{81BE45BB-2045-40D6-BA6C-19F6D2CBC467}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}/issues
AppUpdatesURL={#MyAppUrl}/releases/latest
DefaultDirName={localappdata}\Programs\Generals Vanguard
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
UsePreviousAppDir=yes
DirExistsWarning=no
LicenseFile=..\..\LICENSE.md
InfoBeforeFile=Notice.txt
OutputDir=..\..\dist
OutputBaseFilename=VanguardSetup
SetupIconFile=..\..\Tools\VanguardLauncher\Assets\Vanguard.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "..\..\dist\launcher\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--install"; Description: "Download and install the Generals Vanguard game now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\game"
Type: filesandordirs; Name: "{app}\game.new"
Type: filesandordirs; Name: "{app}\game.old"
Type: filesandordirs; Name: "{app}\VanguardGame.download.zip"
