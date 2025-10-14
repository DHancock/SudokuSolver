; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.2.2

#define appDisplayName "Sudoku Solver"
#define appName "SudokuSolver"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId "sudukosolver.8628521D92E74106"

[Setup]
AppId={#appId}
AppName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
DefaultDirName={autopf}\{#appDisplayName}
DefaultGroupName={#appDisplayName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
InfoBeforeFile="{#SourcePath}\0BSD.txt"
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
DisableReadyPage=yes
DisableDirPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/SudokuSolver/releases
ShowLanguageDialog=no
ArchitecturesInstallIn64BitMode=x64 arm64
ArchitecturesAllowed=x86 x64 arm64

[Files]
Source: "..\bin\Release\win-x64\publish\*"; DestDir: "{app}"; Check: IsX64; Flags: recursesubdirs;
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: IsARM64; Flags: recursesubdirs solidbreak;
Source: "..\bin\Release\win-x86\publish\*"; DestDir: "{app}"; Check: IsX86; Flags: recursesubdirs solidbreak;

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
Name: "{group}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Parameters: "/register"; 
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appDisplayName}}"; Flags: postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#appExeName}"; Parameters: "/unregister";

[Code]
function IsDowngradeInstall: Boolean; forward;
procedure BackupAppData; forward;
procedure RestoreAppData; forward;

// because "DisableReadyPage" and "DisableProgramGroupPage" are set to yes adjust the next/install button text
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
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


function GetUninstallRegKey: String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#appId}_is1';
end;


procedure UninatallAnyPreviousVersion();
var
  ResultCode, Attempts: Integer;
  UninstallerPath: String; 
begin    
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
  begin
    BackupAppData();
    
    Exec(RemoveQuotes(UninstallerPath), '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    if ResultCode = 0 then // wait until the uninstall has completed
    begin
      Attempts := 2 * 30 ; // timeout after approximately 30 seconds
       
      while FileExists(UninstallerPath) and (Attempts > 0) do
      Begin
        Sleep(500);
        Attempts := Attempts - 1;
      end;
    end;
  
    if (ResultCode <> 0) or FileExists(UninstallerPath) then
    begin
      SuppressibleMsgBox('Setup failed to uninstall a previous version.', mbCriticalError, MB_OK, IDOK) ;
      Abort;
    end;
    
    RestoreAppData();
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) then
  begin
    // when upgrading the remnants of an old install may cause the new version to fail to start. 
    UninatallAnyPreviousVersion();
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


function IsDowngradeInstall: Boolean;
var
  RegKey, InstalledVersion: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) then
    Result := VersionComparer(InstalledVersion, '{#appVer}') > 0;
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
        FileCopy(SourceDir + FilePart, DestDir + FilePart, false);
        
      FilePart := '\session.xml';
        
      if FileExists(SourceDir + FilePart) then
        FileCopy(SourceDir + FilePart, DestDir + FilePart, false)
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


