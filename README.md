# CopySave

CopySave is a native background utility for Windows and macOS.

I made it for a very specific workflow: when you copy code or text from an LLM and want to save it straight into the current folder without first pasting it into Notepad or another editor just to create a file.

When you press paste in the file manager:

- On Windows it intercepts `Ctrl+V` only when File Explorer is focused on the file area
- On macOS it intercepts `Cmd+V` only when Finder is focused on the file area
- It opens a small save dialog with two fields: `name` and `extension`
- It saves clipboard text into the currently open folder
- If the file already exists, it creates `name_2`, `name_3`, and so on

Outside Explorer and Finder, normal paste should continue unchanged.

## Platforms

- `windows/CopySave.Windows`: native Windows app on `C#` + `WinForms`
- `macos/CopySaveMac`: native macOS app on `Swift` + `AppKit`

## Release Artifacts

GitHub Releases are designed to publish:

- `CopySave-<version>-windows-x64.exe`
- `CopySave-<version>-macos-app.zip`

The Windows build is a self-contained single-file executable.
The macOS build is packaged as a self-contained `.app` bundle and uploaded as a zip archive.

## Local Build

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-windows.ps1
```

Output:

```text
dist/windows/CopySave.exe
```

macOS:

```bash
bash scripts/build-macos.sh
```

Outputs:

```text
dist/macos/CopySave.app
```

The release workflow packages the macOS app bundle into a zip asset for GitHub Releases.

## GitHub Release Flow

Tag the repository with a version like:

```text
v1.0.0
```

The release workflow will:

- build Windows on `windows-latest`
- build macOS on `macos-latest`
- create a GitHub Release
- upload the built assets to that release

Workflow:

```text
.github/workflows/release.yml
```

## Permissions

Windows:

- registers global paste interception only for Explorer
- writes autostart entry to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

macOS:

- uses Accessibility APIs for the hotkey monitor
- uses Finder automation to resolve the front folder
- writes a LaunchAgent to `~/Library/LaunchAgents/com.copysave.mac.plist`

## Notes

- macOS binaries must be built on macOS
- unsigned or ad-hoc signed builds may still trigger OS warnings depending on system settings
