; Installer for Nax-Translate. Build it with installer\build.ps1, which publishes the app
; and passes AppVersion and PublishDir to the compiler.

#ifndef AppVersion
  #error AppVersion is not defined. Build the installer with installer\build.ps1.
#endif
#ifndef PublishDir
  #error PublishDir is not defined. Build the installer with installer\build.ps1.
#endif

#define AppName "Nax-Translate"
#define AppExe  "Nax-Translate.exe"
; The name of versions up to 1.1.x. Their program folder and shortcuts are removed on update.
#define OldName "Nax-DeepL"

[Setup]
; The ID ties updates and the uninstall entry together. It must never change, so an installed
; Nax-DeepL is updated in place and its uninstall entry becomes the one for Nax-Translate.
AppId={{AB237F8A-B239-458C-BFC9-625A415608D7}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Naxterra
AppPublisherURL=https://github.com/Naxterra/Nax-Translate
VersionInfoVersion={#AppVersion}
; Installs for the current user only, so no administrator rights are needed.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
; An update from Nax-DeepL moves to the folder of the new name instead of reusing the old one.
UsePreviousAppDir=no
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Setup and the uninstaller wait until the running app is closed. The names match the
; single-instance mutex of this app and of versions before the rename.
AppMutex=Nax-Translate-7F3A,Nax-DeepL-7F3A
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern dynamic
Compression=lzma2
SolidCompression=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=Nax-Translate-{#AppVersion}-setup

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
german.RuntimeMissing=Die .NET 11 Desktop Runtime (x64) wurde nicht gefunden. Ohne sie startet die App nicht.%n%nSie erhalten sie unter https://dotnet.microsoft.com/download/dotnet/11.0%n%nTrotzdem installieren?
english.RuntimeMissing=The .NET 11 Desktop Runtime (x64) was not found. The app does not start without it.%n%nYou can get it from https://dotnet.microsoft.com/download/dotnet/11.0%n%nInstall anyway?
german.DeleteSettings=Sollen auch Ihre Einstellungen und die gespeicherte DeepL-Anmeldung gelöscht werden?
english.DeleteSettings=Do you also want to delete your settings and the saved DeepL login?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
; Shortcuts of the old name would otherwise stay behind and point at a program file that is gone.
Type: files; Name: "{autoprograms}\{#OldName}.lnk"
Type: files; Name: "{autodesktop}\{#OldName}.lnk"

[Files]
Source: "{#PublishDir}\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Removes the "Start with Windows" entry and undoes the deepl:// handler of an unfinished login
; while the app's files are still there.
Filename: "{app}\{#AppExe}"; Parameters: "--uninstall-cleanup"; Flags: runhidden waituntilterminated; RunOnceId: "RegistryCleanup"

[Code]
var
  OldInstallDir: String;

function DesktopRuntimeInstalled: Boolean;
var
  FindRec: TFindRec;
begin
  Result := FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\11.*'), FindRec);
  if Result then
    FindClose(FindRec);
end;

function InitializeSetup: Boolean;
begin
  // Where the installed version lives, read before this setup replaces the uninstall entry.
  if not RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{AB237F8A-B239-458C-BFC9-625A415608D7}_is1',
                             'InstallLocation', OldInstallDir) then
    OldInstallDir := '';
  Result := DesktopRuntimeInstalled or
    (SuppressibleMsgBox(CustomMessage('RuntimeMissing'), mbConfirmation, MB_YESNO, IDNO) = IDYES);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  OldDir: String;
begin
  // After an update from Nax-DeepL the old program folder is no longer used. It is removed only
  // when it is a different folder and still holds the old program file, so nothing else is touched.
  if CurStep = ssPostInstall then
  begin
    OldDir := RemoveBackslash(OldInstallDir);
    if (OldDir <> '') and (CompareText(OldDir, RemoveBackslash(ExpandConstant('{app}'))) <> 0) and
       FileExists(AddBackslash(OldDir) + '{#OldName}.exe') then
      DelTree(OldDir, True, True, True);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  // Settings and the encrypted login are the user's data, so they are only deleted on request.
  // A silent uninstall keeps them. The folders of the old app names are included in case the app
  // was never started after a rename and so never moved them.
  if (CurUninstallStep = usPostUninstall) and
     (SuppressibleMsgBox(CustomMessage('DeleteSettings'), mbConfirmation, MB_YESNO, IDNO) = IDYES) then
  begin
    DelTree(ExpandConstant('{userappdata}\Nax-Translate'), True, True, True);
    DelTree(ExpandConstant('{userappdata}\Nax-DeepL'), True, True, True);
    DelTree(ExpandConstant('{userappdata}\DeepL-Translator'), True, True, True);
  end;
end;
