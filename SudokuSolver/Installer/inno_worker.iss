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
#define appVer "1.5.1"
#define appName "SudokuSolver"
#define appExeName appName + ".exe"
#define appId "sudukosolver.8628521D92E74106"

[Setup]
AppId={#appId}
appName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
VersionInfoVersion={#appVer}
DefaultDirName={autopf}\{#appDisplayName}
DefaultGroupName={#appDisplayName}
SourceDir=..\bin\{#platform}\Release\publish
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}_{#platform}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
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
Source: "*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Parameters: "/register"; 
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appDisplayName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#appExeName}"; Parameters: "/unregister"; 
Filename: powershell.exe; Parameters: "Get-Process '{#appName}' | where Path -eq '{app}\{#appExeName}' | kill -Force"; Flags: runhidden

[code]
// code based on this excellent stackoverflow answer:
// https://stackoverflow.com/questions/7415457/custom-inno-setup-uninstall-page-not-msgbox#42550055

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
  
  if (not UninstallSilent) and CheckForMutexes('{#appId}') then
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
    PageText.Left := CautionImage.Left + CautionImage.Width + ScaleX(20) ;
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
