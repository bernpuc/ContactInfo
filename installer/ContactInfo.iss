; Inno Setup script for ContactInfo
; Build with: installer\build.ps1  (requires Inno Setup 6)

#define MyAppName      "ContactInfo"
#define MyAppVersion   "1.0"
#define MyAppPublisher "bernpuc"
#define MyAppExeName   "ContactInfo.exe"
#define MyAppURL       "http://localhost:5100"

[Setup]
AppId={{A3F7E1B2-4C9D-4E2A-8F1B-2D3C4E5F6A7B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=ContactInfoSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Run as the installing user (not system) so %APPDATA% resolves correctly for settings
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
Name: "startup";     Description: "Start ContactInfo automatically when Windows starts"; GroupDescription: "Startup:"

[Files]
; All published output — self-contained, no .NET install required on target machine
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu
Name: "{group}\{#MyAppName}";           Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
; Desktop (optional task)
Name: "{commondesktop}\{#MyAppName}";   Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon
; Windows startup (optional task)
Name: "{userstartup}\{#MyAppName}";     Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: startup

[Run]
; Launch the app after install finishes (the app auto-opens the browser)
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Launch {#MyAppName} now"; \
    Flags: postinstall nowait skipifsilent

[UninstallRun]
; Stop any running instance before uninstalling
Filename: "taskkill.exe"; Parameters: "/IM {#MyAppExeName} /F"; Flags: runhidden; RunOnceId: "KillApp"

[Code]
// Show a warning if another instance is already running when the installer starts
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/IM {#MyAppExeName} /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;
