; This script assumes that all release configurations have been published
; and is framework dependent targeting a minimum WinAppSdk version of 1.3
; but will roll forward to any later 1.n version.
; Inno 6.2.2

#define appDisplayName "Sudoku Solver"
#define appVer "1.6.0"
#define appName "SudokuSolver"
#define appExeName appName + ".exe"
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
Source: "..\installer\tools\NetCoreCheck.exe"; Flags: dontcopy;
Source: "..\installer\tools\CheckWinAppSdk.exe"; Flags: dontcopy;
Source: "..\bin\x64\Release\publish\*"; DestDir: "{app}"; Check: IsX64; Flags: recursesubdirs; 
Source: "..\bin\x86\Release\publish\*"; DestDir: "{app}"; Check: IsX86; Flags: recursesubdirs solidbreak; 
Source: "..\bin\arm64\Release\publish\*"; DestDir: "{app}"; Check: IsARM64; Flags: recursesubdirs solidbreak;

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
type
  TCheckFunc = function(): Boolean;
  
  TDependencyItem = record
    Url: String;
    Title: String;
    CheckFunction: TCheckFunc;
  end;
  
var
  DownloadsList: array of TDependencyItem;

  
function IsWinAppSdkInstalled(): Boolean; forward;
function IsNetDesktopInstalled(): Boolean; forward;
function GetPlatformStr(): String; forward;
function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean; forward;
procedure AddDownload(const Url, Title: String; const CheckFunction: TCheckFunc); forward;
function VersionComparer(const A, B: String): Integer; forward;
function IsSelfcontained(const Version: String): Boolean; forward;
function IsAppRunning: Boolean; forward;
  
  
function InitializeSetup(): Boolean;
var 
  UpdateNet, UpdateWinAppSdk: Boolean;
  IniFile, DownloadUrl, Message: String;
begin
  Result := true;
  
  try
    UpdateNet := not IsNetDesktopInstalled;
    UpdateWinAppSdk := not IsWinAppSdkInstalled;

    if UpdateNet or UpdateWinAppSdk then  
    begin
      Result := false;
     
      if DownloadTemporaryFile('https://raw.githubusercontent.com/DHancock/Common/main/versions.ini', 'versions.ini', '', @OnDownloadProgress) > 0 then
      begin
        IniFile := ExpandConstant('{tmp}\versions.ini');

        if UpdateNet then
        begin
          DownloadUrl := GetIniString('NetDesktopRuntime', GetPlatformStr, '', IniFile);
          Result := Length(DownloadUrl) > 0;
          AddDownload(DownloadUrl, 'Net Desktop Runtime', @IsNetDesktopInstalled);
        end;

        if UpdateWinAppSdk then
        begin
          DownloadUrl := GetIniString('WinAppSdk', GetPlatformStr, '', IniFile);
          Result := Length(DownloadUrl) > 0;
          AddDownload(DownloadUrl, 'Windows App SDK', @IsWinAppSdkInstalled);
        end;
      end;
  
      if not Result then
      begin
        Message := 'Setup has detected that a prerequisite SDK needs to be installed but cannot determine the download Url.';
        SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
      end;
    end;
    
  except
    Message := 'An fatal error occured when checking install prerequesites: '#13#10 + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
  end;
end;


function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Retry: Boolean;
  ExeFilePath: String;
  Dependency: TDependencyItem;
  ResultCode, Count, Index: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  NeedsRestart := false;
  Result := ''; 
  Count := GetArrayLength(DownloadsList);
  
  if Count > 0 then
  begin
    DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
    DownloadPage.Show;       
    Index := 0;
    
    try
      try
        repeat
          Dependency := DownloadsList[Index];

          DownloadPage.Clear;
          DownloadPage.Add(Dependency.Url, ExtractFileName(Dependency.Url), '');
          
          repeat 
            Retry := false;
            try
              DownloadPage.Download;
            except
            
              if DownloadPage.AbortedByUser then
              begin
                Result := 'Download of ' + Dependency.Title + ' was cancelled.';
                Index := Count;
                break;
              end
              else
              begin
                case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
                  IDABORT: begin
                    Result := 'Download of ' + Dependency.Title + ' was cancelled.';
                    Index := Count;
                    break;
                  end;
                  IDRETRY: begin
                    Retry := True;
                  end;
                end;
              end; 
            end;
          until not Retry;

          if Result = '' then
          begin
            DownloadPage.AbortButton.Hide;
            DownloadPage.SetText('Installing the ' + Dependency.Title, '');
            DownloadPage.ProgressBar.Style := npbstMarquee;
            
            ExeFilePath := ExpandConstant('{tmp}\') + ExtractFileName(Dependency.Url);

            if not Exec(ExeFilePath, '', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
            begin
              Result := 'An error occured installing ' + Dependency.Title + '.'#13#10 + SysErrorMessage(ResultCode);
              break;
            end;

            DeleteFile(ExeFilePath);
            
            if not Dependency.CheckFunction() then
            begin
              Result := 'Installation of ' + Dependency.Title + ' failed.';
              break;
            end;
            
            DownloadPage.ProgressBar.Style := npbstNormal;
            DownloadPage.AbortButton.Show;
          end;

          Index := Index + 1;
          
        until Index >= Count;
      except
        Result := 'Installing prerequesites failed.'#13#10 + GetExceptionMessage;
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;                                              


procedure AddDownload(const Url, Title: String; const CheckFunction: TCheckFunc); 
var
  Dependency: TDependencyItem;
  Count: Integer;
begin
  Dependency.Url := Url;
  Dependency.Title := Title;
  Dependency.CheckFunction := CheckFunction;

  // a linked list isn't possible because forward type declarations arn't supported 
  Count := GetArrayLength(DownloadsList);
  SetArrayLength(DownloadsList, Count + 1);
  DownloadsList[Count] := Dependency;
end;


function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log('Successfully downloaded file: ' + FileName + ' size: ' + IntToStr(ProgressMax));

  Result := True;
end;
 

function GetPlatformStr(): String;
begin
  case ProcessorArchitecture of
    paX86: Result := 'x86';
    paX64: Result := 'x64';
    paARM64: Result := 'arm64';
  end;
end;


// returns a Windows.System.ProcessorArchitecture enum value
function GetPlatformParamStr(): String;
begin
  case ProcessorArchitecture of
    paX86: Result := '0';
    paX64: Result := '9';
    paARM64: Result := '12';
  end;
end;


function IsWinAppSdkInstalled(): Boolean;
var
  ExeFilePath: String;
  ResultCode: Integer;
begin
  ExeFilePath := ExpandConstant('{tmp}\CheckWinAppSdk.exe');

  if not FileExists(ExeFilePath) then
    ExtractTemporaryFile('CheckWinAppSdk.exe');

  if not Exec(ExeFilePath, '3000 ' + GetPlatformParamStr, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Log('Exec CheckWinAppSdk.exe failed: ' + SysErrorMessage(ResultCode));    

  Result := ResultCode = 0;
end;


function IsNetDesktopInstalled(): Boolean;
var
  ExeFilePath: String;
  ResultCode: Integer;
begin
  ExeFilePath := ExpandConstant('{tmp}\NetCoreCheck.exe');

  if not FileExists(ExeFilePath) then
    ExtractTemporaryFile('NetCoreCheck.exe');

  if not Exec(ExeFilePath, '-n Microsoft.WindowsDesktop.App -v 6.0.16 -r LatestMajor', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Log('Exec NetCoreCheck.exe failed: ' + SysErrorMessage(ResultCode));    

  Result := ResultCode = 0;
end;


// The remnants of a self contained install dlls will cause a framework dependent 
// app to trap on start. Have to uninstall first. Down grading from framework
// dependent to an old self contained version also causes the app to fail. 
// The old installer releases will be removed from GitHub.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode, Attempts: Integer;
  RegKey, InstalledVersion, UninstallerPath: String;
  ProgressPage: TOutputMarqueeProgressWizardPage;
begin
  if (CurStep = ssInstall) then
  begin
    RegKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#appId}_is1';

    if RegQueryStringValue(HKCU, RegKey, 'DisplayVersion', InstalledVersion) and IsSelfcontained(InstalledVersion) then
    begin
      if RegQueryStringValue(HKCU, RegKey, 'UninstallString', UninstallerPath) then
      begin
        ResultCode := 1;

        ProgressPage := CreateOutputMarqueeProgressPage('Uninstall', 'Uninstalling version ' + InstalledVersion);
        ProgressPage.Animate;
        ProgressPage.Show;

        try
          try 
            UninstallerPath := RemoveQuotes(UninstallerPath);
            
            Exec(UninstallerPath, '/VERYSILENT /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
            Log('Uninstall version: ' + InstalledVersion + ' returned: ' + IntToStr(ResultCode));
            
            if ResultCode = 0 then // wait until the uninstall has completed
            begin
              Attempts := 8 * 30;
               
              while FileExists(UninstallerPath) and (Attempts > 0) do
              begin
                Sleep(125);
                Attempts := Attempts - 1;
              end;
                
              Log('Uninstall completed, attempts remaining: ' + IntToStr(Attempts));
            end;
          except
          end;
        finally
          ProgressPage.Hide;
        end;

        if (ResultCode <> 0) or FileExists(UninstallerPath) then
        begin
          SuppressibleMsgBox('Setup failed to uninstall a previous version.', mbCriticalError, MB_OK, IDOK);
          Abort;
        end;
      end;
    end;
  end;
end;


function IsSelfcontained(const Version: String): Boolean;
begin
  Result := VersionComparer(Version, '1.6') < 0;
end;
  

// A < B returns -ve
// A = B returns 0
// A > B returns +ve
function VersionComparer(const A, B: String): Integer;
var
  X, Y: Int64;
begin

  if not StrToVersion(A, X) then
    Log('StrToVersion() failed for A: ' + A);
    
  if not StrToVersion(B, Y) then
    Log('StrToVersion() failed for B: ' + B);
  
  Result := ComparePackedVersion(X, Y);
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


procedure InitializeUninstallProgressForm();
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
    PageText.Caption := 'Uninstall has detected that {#appDisplayName} is running. '#13#10#13#10 +
                        'Please close all {#appDisplayName} windows before continuing. '#13#10#13#10 +
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
