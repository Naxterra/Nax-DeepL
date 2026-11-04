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
; Updates keep the folder of the installed version, so a folder the user chose stays in use. Only
; a folder named after the old name moves to one named Nax-Translate, see InitializeWizard.
UsePreviousAppDir=yes
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
; --show opens the window, because the app otherwise starts hidden in the tray.
Filename: "{app}\{#AppExe}"; Parameters: "--show"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Removes the "Start with Windows" entry and undoes the deepl:// handler of an unfinished login
; while the app's files are still there.
Filename: "{app}\{#AppExe}"; Parameters: "--uninstall-cleanup"; Flags: runhidden waituntilterminated; RunOnceId: "RegistryCleanup"

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

var
  OldInstallDir: String;
  OldUninstaller: String;

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
  // Where the installed version lives and which uninstaller it uses, read before this setup
  // replaces the uninstall entry. In a folder shared with other programs the uninstaller is not
  // always unins000, because Inno Setup picks the next free number.
  if not RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{AB237F8A-B239-458C-BFC9-625A415608D7}_is1',
                             'InstallLocation', OldInstallDir) then
    OldInstallDir := '';
  if not RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{AB237F8A-B239-458C-BFC9-625A415608D7}_is1',
                             'UninstallString', OldUninstaller) then
    OldUninstaller := '';
  OldUninstaller := RemoveQuotes(OldUninstaller);
  Result := DesktopRuntimeInstalled or
    (SuppressibleMsgBox(CustomMessage('RuntimeMissing'), mbConfirmation, MB_YESNO, IDNO) = IDYES);
end;

procedure InitializeWizard;
var
  Prev: String;
begin
  // The installed version's folder is kept (UsePreviousAppDir). Only when that folder carries the
  // old name, as the default folder of Nax-DeepL does, the update goes to a folder of the new name
  // next to it. A folder given with /DIR= on the command line is always respected.
  Prev := RemoveBackslash(OldInstallDir);
  if (Prev <> '') and (ExpandConstant('{param:DIR}') = '') and
     (CompareText(ExtractFileName(Prev), '{#OldName}') = 0) then
    WizardForm.DirEdit.Text := AddBackslash(ExtractFileDir(Prev)) + '{#AppName}';
end;

procedure MoveStartEntry;
var
  Old: String;
  Found: Boolean;
begin
  // A "Start with Windows" entry of an old name ("Nax-DeepL" up to 1.1.x, "DeepL-Translator"
  // before that, the same names as StartupHelper.LegacyNames) points at a program file that no
  // longer runs. It is moved here, because after a silent update nobody starts the new app to move it.
  Found := RegQueryStringValue(HKCU, RunKey, '{#OldName}', Old);
  Found := RegQueryStringValue(HKCU, RunKey, 'DeepL-Translator', Old) or Found;
  if Found then
  begin
    // --autostart matches StartupHelper.AutostartSwitch, so the start at sign-in stays quiet.
    RegWriteStringValue(HKCU, RunKey, '{#AppName}', '"' + ExpandConstant('{app}\{#AppExe}') + '" --autostart');
    RegDeleteValue(HKCU, RunKey, '{#OldName}');
    RegDeleteValue(HKCU, RunKey, 'DeepL-Translator');
  end;
end;

procedure RemoveOldProgramFiles;
var
  OldDir: String;
begin
  // Only the files that Nax-DeepL installed are deleted, by name, never anything else that may be
  // in the folder. The folder itself is removed only when the update moved to another folder and
  // nothing else is left in it.
  OldDir := RemoveBackslash(OldInstallDir);
  if OldDir = '' then
    exit;
  DeleteFile(OldDir + '\{#OldName}.exe');
  DeleteFile(OldDir + '\{#OldName}.dll');
  DeleteFile(OldDir + '\{#OldName}.deps.json');
  DeleteFile(OldDir + '\{#OldName}.runtimeconfig.json');
  DeleteFile(OldDir + '\{#OldName}.pdb');
  if CompareText(OldDir, RemoveBackslash(ExpandConstant('{app}'))) <> 0 then
  begin
    // The new uninstaller lives in the new folder, so the old one is no longer used. Only the one
    // named in the old uninstall entry is deleted, and only when it sits in the old folder, so
    // another program's uninstaller in a shared folder is never touched.
    if (OldUninstaller <> '') and (CompareText(RemoveBackslash(ExtractFileDir(OldUninstaller)), OldDir) = 0) then
    begin
      DeleteFile(OldUninstaller);
      DeleteFile(ChangeFileExt(OldUninstaller, '.dat'));
      DeleteFile(ChangeFileExt(OldUninstaller, '.msg'));
    end;
    RemoveDir(OldDir);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MoveStartEntry;
    RemoveOldProgramFiles;
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
