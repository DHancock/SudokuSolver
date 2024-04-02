; This script assumes that all release configurations have been published
; and they are WinAppSdk and .Net framework are self contained.
; Inno 6.2.2

#define appDisplayName "Sudoku Solver"
#define appName "SudokuSolver"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName))
#define appId "sudukosolver.8628521D92E74106"

[Setup]
AppId={#appId}
appName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
DefaultDirName={autopf}\{#appDisplayName}
DefaultGroupName={#appDisplayName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
PrivilegesRequired=lowest
AllowUNCPath=no
AllowNetworkDrive=no
WizardStyle=classic
WizardSizePercent=110,110
DirExistsWarning=yes
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/SudokuSolver/releases
ArchitecturesInstallIn64BitMode=x64 arm64
ArchitecturesAllowed=x86 x64 arm64

[Files]
Source: "..\bin\Release\win-x64\publish\*"; DestDir: "{app}"; Check: IsX64; Flags: recursesubdirs; 
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: IsARM64; Flags: recursesubdirs;
Source: "..\bin\Release\win-x86\publish\*"; DestDir: "{app}"; Check: IsX86; Flags: recursesubdirs;

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
Filename: powershell.exe; Parameters: "Get-Process '{#appName}' | where Path -eq '{app}\{#appExeName}' | kill -Force"; Flags: runhidden

[code]
function IsAppRunning: Boolean; forward;
function NewLine: String; forward;
function IsDowngradeInstall: Boolean; forward;

  
function InitializeSetup: Boolean;
var 
  Message: String;
begin
  Result := true;
  
  try
    if IsDowngradeInstall then
      RaiseException('Downgrading isn''t supported.' + NewLine + 'Please uninstall the current version first.');
    
  except
    Message := 'An error occured when checking install prerequesites:' + NewLine + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
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
  InstalledVersion: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) then
    Result := VersionComparer(InstalledVersion, '{#appVer}') > 0;
end;


procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else if CurPageID = wpFinished then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonFinish)
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;


function NewLine: String;
begin
  Result := #13#10;
end;


procedure InitializeUninstallProgressForm;
var
  PageText: TNewStaticText;
  PageNameLabel: string;
  PageDescriptionLabel: string;
  CancelButtonEnabled: Boolean;
  CancelButtonModalResult: Integer;
  NewPage: TNewNotebookPage;
  UninstallButton: TNewButton;
  CautionImage: TBitmapImage;
begin

  if (not UninstallSilent) and IsAppRunning then
  begin
    // create the page
    NewPage := TNewNotebookPage.Create(UninstallProgressForm);
    NewPage.Notebook := UninstallProgressForm.InnerNotebook;
    NewPage.Parent := UninstallProgressForm.InnerNotebook;
    NewPage.Align := alClient;
  
    // create page contents
    CautionImage := TBitmapImage.Create(UninstallProgressForm);
    CautionImage.Parent := NewPage;
    CautionImage.Bitmap.LoadFromFile(ExpandConstant('{app}\Resources\warning.bmp'));
    CautionImage.Bitmap.AlphaFormat := afPremultiplied;
    CautionImage.Width := ScaleX(80);
    CautionImage.Height := ScaleX(80);
    CautionImage.AutoSize := true;
    CautionImage.Top := ScaleX(10);
    CautionImage.Left := ScaleX(10);
        
    PageText := TNewStaticText.Create(UninstallProgressForm);
    PageText.Parent := NewPage;
    PageText.Top := CautionImage.Top;
    PageText.Left := CautionImage.Left + CautionImage.Width + ScaleX(20);
    PageText.Width := UninstallProgressForm.StatusLabel.Width - PageText.Left;
    PageText.Height := ScaleX(300);
    PageText.AutoSize := False;
    PageText.ShowAccelChar := False;
    PageText.WordWrap := True;
    PageText.Caption := 'Uninstall has detected that {#appDisplayName} is running. ' + NewLine + NewLine +
                        'Please close all {#appDisplayName} windows before continuing. ' + NewLine + NewLine +
                        'If you continue without closing, {#appDisplayName} will be terminated ' +
                        'and any unsaved changes will be lost.' 

             
    UninstallButton := TNewButton.Create(UninstallProgressForm);
    UninstallButton.Parent := UninstallProgressForm;
    UninstallButton.Left := UninstallProgressForm.CancelButton.Left - UninstallProgressForm.CancelButton.Width - ScaleX(10);
    UninstallButton.Top := UninstallProgressForm.CancelButton.Top;
    UninstallButton.Width := UninstallProgressForm.CancelButton.Width;
    UninstallButton.Height := UninstallProgressForm.CancelButton.Height;
    UninstallButton.ModalResult := mrOK; 
    UninstallButton.Caption := 'Uninstall';
    
    // adjust tab order
    UninstallButton.TabOrder := UninstallProgressForm.CancelButton.TabOrder;
    UninstallProgressForm.CancelButton.TabOrder := UninstallButton.TabOrder + 1;
    
    // store previous state
    CancelButtonEnabled := UninstallProgressForm.CancelButton.Enabled
    CancelButtonModalResult := UninstallProgressForm.CancelButton.ModalResult;
    PageNameLabel := UninstallProgressForm.PageNameLabel.Caption;
    PageDescriptionLabel := UninstallProgressForm.PageDescriptionLabel.Caption;
  
    // initialise content
    UninstallProgressForm.PageNameLabel.Caption := 'Caution';
    UninstallProgressForm.PageDescriptionLabel.Caption := 'A problem has been detected.';
    UninstallProgressForm.CancelButton.Enabled := true;    
    UninstallProgressForm.CancelButton.ModalResult := mrCancel;
    
    // add the new page
    UninstallProgressForm.InnerNotebook.ActivePage := NewPage;
    
    // run the page
    if UninstallProgressForm.ShowModal = mrCancel then Abort;

    // restore state
    UninstallButton.Visible := false
    UninstallProgressForm.CancelButton.Enabled := CancelButtonEnabled;
    UninstallProgressForm.CancelButton.ModalResult := CancelButtonModalResult
    UninstallProgressForm.PageNameLabel.Caption := PageNameLabel;
    UninstallProgressForm.PageDescriptionLabel.Caption := PageDescriptionLabel;
    UninstallProgressForm.InnerNotebook.ActivePage := UninstallProgressForm.InstallingPage;
  end;
end;


function IsAppRunning: Boolean;
var
  ResultCode: Integer;
  Params: String;
begin
  Params := ExpandConstant('Exit (Get-Process ''{#appName}'' | where Path -eq ''{app}\{#appExeName}'').Count');

  if Exec('powershell.exe', Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := ResultCode > 0
  else
  begin
    Log('Failed to start powershell, error: ' + IntToStr(ResultCode));
    Result := False;
  end;
end;
