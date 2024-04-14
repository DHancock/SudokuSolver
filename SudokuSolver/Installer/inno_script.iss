; This worker script is intended to be called from one of the build_XXX scripts
; where the platform variable is defined. It assumes that all release configurations 
; have been published and the WinAppSdk and .Net framework are self contained.
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
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_{#platform}_v{#appVer}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
PrivilegesRequired=lowest
WizardStyle=classic
WizardSizePercent=110,100
DirExistsWarning=yes
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/SudokuSolver/releases
ShowLanguageDialog=no

#if platform == "x64" || platform == "arm64"
  ArchitecturesAllowed={#platform} 
#endif

[Files]
#if platform == "x64"
  Source: "..\bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs; 
#elif platform == "arm64"
  Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs;
#elif platform == "x86"
  Source: "..\bin\Release\win-x86\publish\*"; DestDir: "{app}"; Flags: recursesubdirs;
#else
  #error unknown platform
#endif

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "zh_Hans"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "zh_Hant"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"

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


 