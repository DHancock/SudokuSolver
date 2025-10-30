; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.5.4

; Caution: There be dragons here. The only way I could get upgrades to work reliably with trimming
; which rewrites dlls is to delete the install dir contents before copying the new stuff in.
; To that end I specify a compulsory unique dir for the install in the users hidden AppData dir.
; I wouldn't recomend it. This makes the install experience similar to installing a store app. 

#define appDisplayName "Sudoku Solver"
#define appName "SudokuSolver"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId "C0A2E954-F594-42C4-B0C4-48BA0723C14A"
#define appMutexName "51ECE64E-1954-41C4-81FB-E3A60CE4C224"
#define setupMutexName "35D5D1E9-1FF3-48B7-B80C-E6BD3EA20751"

[Setup]
AppId={#appId}
AppName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
DefaultDirName={autopf}\{#appId}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
AppMutex={#appMutexName},Global\{#appMutexName}
SetupMutex={#setupMutexName},Global\{#setupMutexName}
Compression=lzma2/ultra64
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
PrivilegesRequired=lowest
WizardStyle=modern
WizardSizePercent=100,100
DisableProgramGroupPage=yes
DisableDirPage=yes
MinVersion=10.0.17763
AppPublisher=David
ShowLanguageDialog=auto
ArchitecturesAllowed=x64compatible or arm64
ArchitecturesInstallIn64BitMode=x64compatible or arm64

[Files]
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs;
Source: "..\bin\Release\win-x64\publish\*";   DestDir: "{app}"; Check: PreferX64Files;   Flags: ignoreversion recursesubdirs solidbreak;

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
Name: "{group}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"; 

[Run]
Filename: "{app}\{#appExeName}"; Parameters: "/register";
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appDisplayName}}"; Flags: nowait postinstall skipifsilent

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


procedure CurPageChanged(CurPageID: Integer);
begin  
  if CurPageID = wpInstalling then 
  begin   
    // hide the extracted file name etc.          
    WizardForm.FilenameLabel.Visible := false;
    WizardForm.StatusLabel.Visible := false;
  end;
end;


procedure TransferSettingsIfRequired;
var
  old, new, root, file : String; 
begin    
  old := '\sudokusolver.davidhancock.net';
  new := '\sudokusolver.davidhancock.net.v2';
  root := ExpandConstant('{localappdata}')
  file := '\settings.json';
  
  if (not FileExists(root + new + file)) and FileExists(root + old + file) then
  begin 
    CreateDir(root + new);
    CopyFile(root + old + file,  root + new + file, false);
    
    file := '\session.xml';
    CopyFile(root + old + file,  root + new + file, false);
    
    DelTree(root + old, True, True, True);
  end;
end;


procedure UninstallOnUpgrade;
var
  Key, UninstallerPath, AppPath: String;
  ResultCode: Integer;
begin
  // Uninstalling an old version shouldn't have any side effects as it's now a different app
  Key := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\sudukosolver.8628521D92E74106_is1';
  
  if RegQueryStringValue(HKCU, Key, 'UninstallString', UninstallerPath) and
     RegQueryStringValue(HKCU, Key, 'InstallLocation', AppPath) then
  begin 
    AppPath := RemoveBackslashUnlessRoot(RemoveQuotes(AppPath)) + '\{#AppExeName}' ;
    
    if not Exec('powershell.exe', 'gps | where path -eq ''' + AppPath + ''' | kill -force', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Begin
      Exec('taskkill.exe', '/t /f /im {#appExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end;
    
    Exec(RemoveQuotes(UninstallerPath), '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;


procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) then
  begin
    // unfortunately I once thought that deleting the settings on uninstall was a good idea
    TransferSettingsIfRequired;
    UninstallOnUpgrade;
  end;
end;

