; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.5.4

#define appDisplayName "Sudoku Solver"
#define appName "SudokuSolver"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId "sudukosolver.8628521D92E74106"
#define appMutexName "51ECE64E-1954-41C4-81FB-E3A60CE4C224"
#define setupMutexName "35D5D1E9-1FF3-48B7-B80C-E6BD3EA20751"

[Setup]
AppId={#appId}
AppName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
DefaultDirName={autopf}\{#appDisplayName}
DefaultGroupName={#appDisplayName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
AppMutex={#appMutexName},Global\{#appMutexName}
SetupMutex={#setupMutexName},Global\{#setupMutexName}
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
PrivilegesRequired=lowest
WizardStyle=modern
DisableProgramGroupPage=yes
DisableDirPage=yes
DisableFinishedPage=yes
MinVersion=10.0.17763
AppPublisher=David
ShowLanguageDialog=auto
ArchitecturesInstallIn64BitMode=x64compatible or arm64

[Files]
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs;
Source: "..\bin\Release\win-x64\publish\*";   DestDir: "{app}"; Check: PreferX64Files;   Flags: ignoreversion recursesubdirs solidbreak;
Source: "..\bin\Release\win-x86\publish\*";   DestDir: "{app}"; Check: PreferX86Files;   Flags: ignoreversion recursesubdirs solidbreak;

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"
Name: fr; MessagesFile: "compiler:Languages\French.isl"
Name: de; MessagesFile: "compiler:Languages\German.isl"
Name: it; MessagesFile: "compiler:Languages\Italian.isl"
Name: es; MessagesFile: "compiler:Languages\Spanish.isl"
Name: zh_Hans; MessagesFile: "Languages\ChineseSimplified.isl"
Name: zh_Hant; MessagesFile: "Languages\ChineseTraditional.isl"

[CustomMessages]
en.DownGradeNotSupported=Downgrading isn't supported.%nPlease uninstall the current version first.
en.ExceptionHeader=An error occurred when checking install prerequesites:%n%n%1
fr.DownGradeNotSupported=La rétrogradation n'est pas prise en charge.%nVeuillez d'abord désinstaller la version actuelle.
fr.ExceptionHeader=Une erreur s'est produite lors de la vérification des conditions préalables à l'installation:%n%n%1
de.DownGradeNotSupported=La rétrogradation n'est pas prise en charge.%nVeuillez d'abord désinstaller la version actuelle.
de.ExceptionHeader=Bei der Überprüfung der Installationsvoraussetzungen ist ein Fehler aufgetreten:%n%n%1
it.DownGradeNotSupported=Il downgrade non è supportato.%nPer prima cosa disinstallare la versione corrente.
it.ExceptionHeader=Si è verificato un errore durante la verifica dei prerequisiti di installazione:%n%n%1
es.DownGradeNotSupported=No se admite la reducción de categoría.%nPor favor, desinstale primero la versión actual.
es.ExceptionHeader=Se ha producido un error al comprobar los prerrequisitos de instalación:%n%n%1
zh_Hans.DownGradeNotSupported=不支持降级。%n请先卸载当前版本。
zh_Hans.ExceptionHeader=检查安装前提条件时发生错误:%n%n%1
zh_Hant.DownGradeNotSupported=不支援降級。%n請先卸載目前版本。
zh_Hant.ExceptionHeader=檢查安裝前提條件時發生錯誤:%n%n%1

[Icons]
Name: "{autodesktop}\{#appDisplayName}"; Filename: "{app}\{#appExeName}";

[Run]
Filename: "{app}\{#appExeName}"; Parameters: "/register"; 
Filename: "{app}\{#appExeName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#appExeName}"; Parameters: "/unregister";

[InstallDelete]
Type: filesandordirs; Name: "{app}\*"


[Code]
function PreferArm64Files: Boolean;
begin
  Result := IsArm64;
end;

function PreferX64Files: Boolean;
begin
  Result := not PreferArm64Files and IsX64Compatible;
end;

function PreferX86Files: Boolean;
begin
  Result := not PreferArm64Files and not PreferX64Files;
end;


procedure CurPageChanged(CurPageID: Integer);
begin
  // if an old version of the app is running ensure inno setup shuts it down
  if CurPageID = wpPreparing then
  begin
    WizardForm.PreparingNoRadio.Enabled := false;
  end;
end;


// A < B returns -ve
// A = B returns 0
// A > B returns +ve
function VersionComparer(const A, B: String): Integer;
var
  X, Y: Int64;
begin
  if not (StrToVersion(A, X) and StrToVersion(B, Y)) then
    RaiseException('StrToVersion(''' + A + ''', ''' + B + ''')');
  
  Result := ComparePackedVersion(X, Y);
end;


function GetUninstallRegKey: String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#appId}_is1';
end;


function IsDowngradeInstall: Boolean;
var
  InstalledVersion, UninstallerPath: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) and 
     RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
  begin   
    // check both the app version and that it (may be) possible to uninstall it 
    Result := (VersionComparer(InstalledVersion, '{#appVer}') > 0) and FileExists(RemoveQuotes(UninstallerPath));
  end;
end;


function InitializeSetup: Boolean;
var 
  Message: String;
begin
  Result := true;
  
  try 
    if IsDowngradeInstall then
      RaiseException(CustomMessage('DownGradeNotSupported'));
    
  except
    Message := FmtMessage(CustomMessage('ExceptionHeader'), [GetExceptionMessage]);
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
  end;
end;


procedure TransferFiles(const BackUp: Boolean);
var
  SourceDir, DestDir, DirPart, FilePart, TempA, TempB: string;
begin
  try
    DirPart := '\sudokusolver.davidhancock.net';
    TempA := ExpandConstant('{localappdata}') + DirPart;
    TempB := ExpandConstant('{%TEMP}') + DirPart; 
    
    if BackUp then
    begin
      SourceDir := TempA;
      DestDir := TempB;
    end
    else
    begin
      SourceDir := TempB
      DestDir := TempA;
    end;
      
    if ForceDirectories(DestDir) then
    begin
      FilePart := '\settings.json';
      
      if FileExists(SourceDir + FilePart) then
        CopyFile(SourceDir + FilePart, DestDir + FilePart, false);
        
      FilePart := '\session.xml';
        
      if FileExists(SourceDir + FilePart) then
        CopyFile(SourceDir + FilePart, DestDir + FilePart, false)
    end;
  except
  end;
end;   


procedure BackupAppData();
begin
  TransferFiles(true);
end;  


procedure RestoreAppData();
begin
  TransferFiles(false);
end;  


procedure UninstallAnyPreviousVersion;
var
  ResultCode, Attempts: Integer;
  UninstallerPath: String;
begin    
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
  begin
    BackupAppData;
        
    Exec(RemoveQuotes(UninstallerPath), '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    if ResultCode = 0 then // wait until the uninstall has completed
    begin 
      Attempts := 2 * 30 ; // timeout after approximately 30 seconds
       
      while FileExists(UninstallerPath) and (Attempts > 0) do
      Begin
        Sleep(500);
        Attempts := Attempts - 1;
      end;
      
      // If the file still exists then the uninstall failed. 
      // There isn't much that can be done, informing the user or aborting 
      // won't acheive much and could render it imposible to install this new version.
      // Installing the new version will over write the registry and add a new uninstaller exe etc.
      
      RestoreAppData;
    end;
  end;
end;


procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    // When upgrading uninstall first or the app may trap on start up.
    // While some dll versions aren't incremented that isn't the only problem
    UninstallAnyPreviousVersion;
  end;
end;
