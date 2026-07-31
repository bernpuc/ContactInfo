; Inno Setup script for ContactInfo
; Build with: installer\build.ps1  (requires Inno Setup 6)

#define MyAppName      "ContactInfo"
#define MyAppVersion   "1.3.2"
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
; Per-user install only — no admin, no option to escalate to all-users
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
Name: "startup";     Description: "Start ContactInfo automatically when Windows starts"; GroupDescription: "Startup:"

[Files]
; All published output — self-contained, no .NET install required on target machine
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Getting-started guide, for post-install reference (see build.ps1 for how it gets here)
Source: "..\GETTING-STARTED.pdf"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\{#MyAppName}";           Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Getting Started";        Filename: "{app}\GETTING-STARTED.pdf"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
; Desktop (optional task)
Name: "{userdesktop}\{#MyAppName}";     Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon
; Windows startup (optional task)
Name: "{userstartup}\{#MyAppName}";     Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: startup

[Run]
; Launch the app after install finishes (the app auto-opens the browser).
; Check: guards against offering/running this when the exe never actually
; made it to disk (e.g. the user chose "Ignore" on a locked-file copy error).
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Launch {#MyAppName} now"; \
    Flags: postinstall nowait skipifsilent; \
    Check: AppInstalledOk

[UninstallRun]
; Stop any running instance before uninstalling
Filename: "taskkill.exe"; Parameters: "/IM {#MyAppExeName} /F"; Flags: runhidden; RunOnceId: "KillApp"

[Code]
const
  KillTimeoutMs = 5000;
  KillPollIntervalMs = 250;

// True if a process with this image name is still in the task list.
// taskkill returning doesn't guarantee the OS has released the exe's file
// handle yet, so InitializeSetup polls this rather than trusting the
// Exec call alone — otherwise the file-copy step can hit a locked-file
// error while the handle is still winding down.
function IsProcessRunning(const ExeName: string): Boolean;
var
  ResultCode: Integer;
  TmpFile: string;
  Output: TStringList;
  I: Integer;
begin
  Result := False;
  TmpFile := ExpandConstant('{tmp}\tasklist_check.txt');
  Exec(ExpandConstant('{cmd}'), '/C tasklist /FI "IMAGENAME eq ' + ExeName + '" /FO CSV /NH > "' + TmpFile + '" 2>nul', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if FileExists(TmpFile) then
  begin
    Output := TStringList.Create;
    try
      Output.LoadFromFile(TmpFile);
      for I := 0 to Output.Count - 1 do
      begin
        if Pos(Lowercase(ExeName), Lowercase(Output[I])) > 0 then
        begin
          Result := True;
          Break;
        end;
      end;
    finally
      Output.Free;
    end;
    DeleteFile(TmpFile);
  end;
end;

// Show a warning if another instance is already running when the installer starts,
// and wait for it to actually exit before letting file copy begin.
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  WaitedMs: Integer;
begin
  Exec('taskkill.exe', '/IM {#MyAppExeName} /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  WaitedMs := 0;
  while IsProcessRunning('{#MyAppExeName}') and (WaitedMs < KillTimeoutMs) do
  begin
    Sleep(KillPollIntervalMs);
    WaitedMs := WaitedMs + KillPollIntervalMs;
  end;

  Result := True;
end;

// Gate for the [Run] launch entry: only offer/run it if the exe actually
// made it onto disk, so a partially-failed install can't try (and fail)
// to auto-launch a broken install.
function AppInstalledOk(): Boolean;
begin
  Result := FileExists(ExpandConstant('{app}\{#MyAppExeName}'));
end;
