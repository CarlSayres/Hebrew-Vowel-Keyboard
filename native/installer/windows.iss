; Compile with /DSourceDir=<package folder> /DOutputDir=<output folder>.
; Release signing additionally requires /DSignedBuild and /Spublisher=<command>.
#ifndef SourceDir
  #error SourceDir must point to the prepared native Windows package
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif
#ifndef AppVersion
  #error AppVersion is required
#endif
#ifdef SignedBuild
  #define PackageSuffix ""
#else
  #define PackageSuffix "-unsigned"
#endif

[Setup]
AppId={{78AC0E49-2F0A-4AE5-9F21-5FED1921074E}
AppName=Hebrew Marks
AppVersion={#AppVersion}
AppPublisher=Carl Sayres
AppPublisherURL=https://github.com/CarlSayres/Hebrew-Vowel-Keyboard
AppSupportURL=https://github.com/CarlSayres/Hebrew-Vowel-Keyboard/issues
DefaultDirName={localappdata}\Programs\Hebrew Marks
DefaultGroupName=Hebrew Marks
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.14393
WizardStyle=modern
OutputDir={#OutputDir}
OutputBaseFilename=HebrewMarks-{#AppVersion}-Setup{#PackageSuffix}
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\Hebrew Marks.exe
CloseApplications=yes
RestartApplications=no
#ifdef SignedBuild
SignTool=publisher
SignedUninstaller=yes
#endif

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
#ifdef SignedBuild
Source: "{#SourceDir}\Hebrew Marks.exe"; DestDir: "{app}"; Flags: ignoreversion signonce
#else
Source: "{#SourceDir}\Hebrew Marks.exe"; DestDir: "{app}"; Flags: ignoreversion
#endif
Source: "{#SourceDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\THIRD_PARTY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\JUCE-LICENSE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\JUCE.spdx.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Hebrew Marks"; Filename: "{app}\Hebrew Marks.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Hebrew Marks"; Filename: "{app}\Hebrew Marks.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Hebrew Marks.exe"; Description: "Open Hebrew Marks"; Flags: nowait postinstall skipifsilent runasoriginaluser
