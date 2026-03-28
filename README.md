# CopySave

CopySave is a native background utility for Windows and macOS.

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

- `CopySave-windows-x64.exe`
- `CopySave-macos.dmg`

The Windows build is a self-contained single-file executable.
The macOS build is packaged as a self-contained `.app` and then wrapped into a `.dmg`.

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
zsh scripts/build-macos.sh
```

Outputs:

```text
dist/macos/CopySave.app
dist/macos/CopySave-1.0.0.dmg
```

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

- macOS binaries and DMG packaging must be built on macOS
- unsigned or ad-hoc signed builds may still trigger OS warnings depending on system settings
