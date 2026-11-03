# Nax-Translate

Nax-Translate is a fast, lightweight Windows desktop translator that works with your **DeepL Pro** subscription or your **Claude** subscription. It was called Nax-DeepL before version 1.2.0. It starts quickly, stays small in the tray and keeps working when your network changes.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-11-purple)
[![License: Nax Source-Available](https://img.shields.io/badge/license-Nax%20Source--Available-blue.svg)](LICENSE)

## Features

- **Instant translation:** type or paste, and the translation appears after a short pause.
- **Global hotkey:** press `Ctrl+C+C` anywhere to translate the copied text.
- **Survives network switches:** when Windows switches networks (Wi-Fi, LAN or VPN), the app drops its old connections and retries on the new network instead of going blank.
- **Login once:** you sign in with your DeepL Pro account in the browser. The session is renewed automatically and stored encrypted for your Windows user.
- **Automatic target language:** when the text is already in the target language, German switches to English (US) and every other language switches to German.
- **Classic, Next-gen or Claude:** a toggle in the title bar picks the DeepL model, or translates with Claude through your own Claude subscription instead of DeepL. The status line shows which engine translated and how long it took.
- **Language swap:** one click flips source and target.
- **System tray:** the window hides to the tray and is one click away.
- **Start with Windows:** an optional entry starts the app when you sign in to Windows.
- **German and English interface** with a dark theme.

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 11 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) (x64)
- An active **DeepL Pro** subscription for the Classic and Next-gen engines
- For the Claude engine: [Claude Code](https://claude.com/claude-code) installed (`winget install Anthropic.ClaudeCode`) and signed in once by running `claude`

## The Claude engine

With **Claude** picked in the title bar, the app sends the text to the installed Claude Code tool (`claude -p`) instead of DeepL. It needs no DeepL login and no API key, because it uses the subscription you are signed in with in Claude Code. An `ANTHROPIC_API_KEY` or similar variable in your environment is not passed on, so translations are never billed to an API key. Each translation counts against that subscription's usage limit. The translation appears word by word as it arrives. While Claude is picked, the app keeps one Claude Code process started and waiting, so a translation does not wait for the tool to start. A waiting process sends nothing and uses no quota, but it takes about 200 MB of memory. Every process handles exactly one translation and then ends, so no translation sees an earlier text. The waiting process is ended when you pick DeepL again or quit the app. The app waits for a longer pause in typing before it sends, and it stops a request at once when newer text is typed.

While Claude is picked, a switch next to the target language sets the tone. **Natural** (the default) translates the way people in Germany would naturally say it and picks du or Sie to suit the text. **Formal** always uses Sie, and **Informal** always uses du. The choice is saved, and changing it translates the current text again.

Claude Code runs in safe mode for these requests, so your own hooks, plugins, CLAUDE.md files and MCP servers are not loaded, no tools are offered and no session is saved. The model is Sonnet by default. To use another one, set `"claude_model"` in `%APPDATA%\Nax-Translate\config.json` to an alias such as `"haiku"`.

## Installation

1. Download `Nax-Translate-<version>-setup.exe` from the [latest release](https://github.com/Naxterra/Nax-Translate/releases/latest).
2. Run it. It installs for your Windows user only, into `%LOCALAPPDATA%\Programs\Nax-Translate`, so no administrator rights are needed. It adds a Start menu entry and, if you choose, a desktop shortcut.
3. Click **⚙ Verbindung → Anmelden** to sign in. This opens the DeepL login in your browser.

After the first login the session is saved, and later starts connect automatically.

To uninstall, use **Settings → Apps → Installed apps**. The uninstaller removes the "Start with Windows" entry and asks whether to delete your settings and the saved login as well.

An installed Nax-DeepL is updated in place: the setup moves the program to the new folder and replaces the old shortcuts. Settings from the earlier names "Nax-DeepL" and "Better DeepL Translator" are taken over automatically on the first start, including the login and the "Start with Windows" entry.

## Usage

| Action | How |
|---|---|
| Translate | Type in the left box. The translation appears after a short pause. |
| Force translate | `Ctrl+Enter` |
| Translate clipboard | Press `Ctrl+C+C` anywhere on the system. |
| Switch languages | Use the dropdowns or click ⇌ to swap. |
| Copy result | Click **📋 Kopieren / Copy**. |
| Switch engine | Use the Classic, Next-gen and Claude toggle in the title bar. |
| Log out or switch account | ⚙ Verbindung → Abmelden |

## Building from source

```
git clone https://github.com/Naxterra/Nax-Translate.git
cd Nax-Translate
dotnet publish Nax-Translate.csproj -c Release -r win-x64 --no-self-contained -o dist
```

Run the tests with `dotnet test`. Building requires the .NET 11 SDK.

To build the installer, run `installer\build.ps1` in PowerShell. It needs Inno Setup 7 (`winget install JRSoftware.InnoSetup.7`) and writes the setup file to `artifacts\installer`.

## Disclaimer

This app uses DeepL's internal web API on behalf of your own authenticated DeepL Pro account. It is not affiliated with or endorsed by DeepL SE. Use it in accordance with DeepL's Terms of Service.

## Author

**Naxterra**, [github.com/Naxterra](https://github.com/Naxterra)

## License

This software is under the Nax Source-Available License 1.0. You may install and use the official release builds, privately and at work, and you may read the source code. Modifying the software, redistributing it and selling it need the written permission of the owner. See [LICENSE](LICENSE).
