; Ninja Strike — Windows Installer
; Build this with Inno Setup 6: https://jrsoftware.org/isinfo.php
; Run from the project root after building with build_win.ps1

#define AppName      "Ninja Strike"
#define AppVersion   "1.0"
#define AppPublisher "Your Studio Name"
#define AppExeName   "NinjaStrike.exe"
#define BuildDir     "Builds\Windows"

[Setup]
AppId={{B8A2C3F1-4D7E-4A2B-9C6F-1E3D5A7B9C2E}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://yourwebsite.com
AppSupportURL=https://yourwebsite.com/support
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=Builds\Installer
OutputBaseFilename=NinjaStrike_Setup_v{#AppVersion}
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
DisableWelcomePage=no
DisableReadyPage=no
ShowLanguageDialog=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayName={#AppName}

; Minimum Windows version: Windows 10
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";   Description: "Create a &desktop shortcut";   GroupDescription: "Additional icons:"; Flags: checked
Name: "quicklaunch";   Description: "Create a &Quick Launch shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; Copy everything from the Windows build directory
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu
Name: "{group}\{#AppName}";             Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}";   Filename: "{uninstallexe}"

; Desktop shortcut (if task selected)
Name: "{autodesktop}\{#AppName}";       Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

; Quick Launch (if task selected)
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: quicklaunch

[Run]
; Offer to launch the game after installation
Filename: "{app}\{#AppExeName}"; \
  Description: "Launch {#AppName} now!"; \
  Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
; Clean up any generated files the game creates in its folder
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\crash.dmp"

[Code]
// ── Custom wizard pages ──────────────────────────────────────────────────
var
  WelcomeBitmap: TBitmapImage;

// Show a colourful welcome message on the first wizard page
procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel1.Caption :=
    'Welcome to the ' + ExpandConstant('{#AppName}') + ' Installer!';
  WizardForm.WelcomeLabel2.Caption :=
    'This wizard will install ' + ExpandConstant('{#AppName}') + ' version ' +
    ExpandConstant('{#AppVersion}') + ' on your computer.' + #13#10 + #13#10 +
    'Defend your island! Battle endless waves of sky-diving fireball demons.' + #13#10 +
    'Supports keyboard (2-player) and Xbox/PlayStation gamepads.' + #13#10 + #13#10 +
    'Click Next to continue.';
end;

// Confirm before uninstalling
function InitializeUninstall(): Boolean;
begin
  Result := MsgBox(
    'Are you sure you want to uninstall ' + ExpandConstant('{#AppName}') + '?',
    mbConfirmation, MB_YESNO) = IDYES;
end;
