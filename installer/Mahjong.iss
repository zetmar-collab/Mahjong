#ifndef AppVersion
  #define AppVersion "1.1.0"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\artifacts\Mahjong-win-x64"
#endif

[Setup]
AppId={{469D0595-E34B-4A30-93A3-89D31AD4D134}
AppName=Mahjong Solitaire
AppVersion={#AppVersion}
AppPublisher=Mahjong contributors
AppCopyright=Copyright (c) 2026 Mahjong contributors
DefaultDirName={localappdata}\Programs\QuietMahjong
DefaultGroupName=Mahjong Solitaire
DisableProgramGroupPage=auto
AllowNoIcons=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupArchitecture=x64
MinVersion=10.0
OutputDir=..\artifacts\installer
OutputBaseFilename=Mahjong-Solitaire-{#AppVersion}-Setup-x64
SetupIconFile=..\assets\mahjong.ico
UninstallDisplayIcon={app}\Mahjong.exe
UninstallDisplayName=Mahjong Solitaire
WizardStyle=modern
WizardImageFile=wizard-large.png
WizardSmallImageFile=..\assets\mahjong-icon.png
WizardImageBackColor=$2B2D15
DisableWelcomePage=no
LicenseFile=..\LICENSE
Compression=lzma2/max
SolidCompression=yes
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes
CloseApplications=yes
CloseApplicationsFilter=Mahjong.exe
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#AppVersion}
VersionInfoDescription=Mahjong Solitaire Setup
VersionInfoProductName=Mahjong Solitaire
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
polish.WelcomeLabel2=Ten kreator zainstaluje grę Mahjong Solitaire na Twoim komputerze.%n%nWszystkie potrzebne składniki są dołączone. Gra i instalacja działają bez internetu.%n%nPrzed kontynuowaniem zamknij uruchomioną grę. Kliknij Dalej, aby rozpocząć.
english.WelcomeLabel2=This wizard will install Mahjong Solitaire on your computer.%n%nAll required components are included. The game and installer work offline.%n%nClose the game before continuing. Click Next to begin.

[CustomMessages]
polish.DesktopShortcut=Utwórz skrót na pulpicie
english.DesktopShortcut=Create a desktop shortcut
polish.ShortcutGroup=Skróty:
english.ShortcutGroup=Shortcuts:
polish.HelpShortcut=Instrukcja gry
english.HelpShortcut=How to play
polish.RemoveShortcut=Odinstaluj Mahjong Solitaire
english.RemoveShortcut=Uninstall Mahjong Solitaire

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:ShortcutGroup}"

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "SmokeData\*,SmokeResults\*,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Mahjong Solitaire"; Filename: "{app}\Mahjong.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Mahjong.exe"
Name: "{group}\{cm:HelpShortcut}"; Filename: "{app}\USER_GUIDE.txt"; WorkingDir: "{app}"
Name: "{group}\{cm:RemoveShortcut}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Mahjong Solitaire"; Filename: "{app}\Mahjong.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Mahjong.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Mahjong.exe"; Description: "{cm:LaunchProgram,Mahjong Solitaire}"; Flags: nowait postinstall skipifsilent
