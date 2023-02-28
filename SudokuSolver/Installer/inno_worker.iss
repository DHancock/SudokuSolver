; This script assumes that the release configuration has been published
; and that the publishing profile defines a self contained app.
; Inno 6.2.2

#ifndef platform
  #error platform is not defined
#endif
  
#if !((platform == "x64") || (platform == "x86") || (platform == "arm64"))
  #error invalid platform definition
#endif

#define appDisplayName "Sudoku Solver"
#define appVer "1.5"
#define appName "sudokusolver"
#define appExeName appName + ".exe"
#define appId "sudukosolver.8628521D92E74106"

[Setup]
AppId={#appId}
appName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
DefaultDirName={autopf}\{#appDisplayName}
DefaultGroupName={#appDisplayName}
SourceDir=..\bin\{#platform}\Release\publish
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2
SolidCompression=yes
OutputBaseFilename={#appDisplayName}_v{#appVer}_{#platform}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
ChangesAssociations=yes
PrivilegesRequired=lowest
WizardStyle=classic
DisableWelcomePage=no
DirExistsWarning=yes
DisableProgramGroupPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/SudokuSolver/releases

#if ((platform == "x64") || (platform == "arm64"))
  ArchitecturesAllowed={#platform}
  ArchitecturesInstallIn64BitMode={#platform}
#else
  ArchitecturesAllowed=x86 x64
#endif

[Files]
Source: "*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Parameters: "/register"; 
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appDisplayName}}"; Flags: nowait postinstall skipifsilent

[Registry]
#define fileExt ".sdku"

Root: HKA; Subkey: "Software\Classes\{#fileExt}\OpenWithProgids"; ValueType: string; ValueName: "{#appId}"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#appId}"; ValueType: string; ValueName: ""; ValueData: "sudoku files"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#appId}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#appExeName},0"
Root: HKA; Subkey: "Software\Classes\{#appId}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#appExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#appExeName}\SupportedTypes"; ValueType: string; ValueName: "{#fileExt}"; ValueData: ""

[UninstallRun]
Filename: "{app}\{#appExeName}"; Parameters: "/unregister"; 
Filename: powershell.exe ; Parameters: "Get-Process {#appName} | where Path -eq '{app}\{#appExeName}' | kill -Force "; Flags: runhidden

[code]
function InitializeUninstall(): Boolean;
var
  message: string;
begin
  Result := true;

  if CheckForMutexes('{#appId}') then begin
    message := 'Uninstall has detected that {#appDisplayName} is running. ' #13#13 +
                'Please close all {#appDisplayName} windows before uninstalling. ' #13#13 +
                'If you continue anyway, {#appDisplayName} will be terminated ' +
                'and any unsaved changes will be lost.' 
                 
    Result := MsgBox(message, mbError, MB_OKCANCEL) = IDOK ;
     
  end;
end;