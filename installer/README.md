# EA Playmate Group Installer

This folder contains the Inno Setup script for building a Windows installer.

## Build

1. Install Inno Setup 6.
2. Run from the project root:

```powershell
.\Scripts\Build-Installer.ps1
```

The installer will be created under:

```text
artifacts\installer\
```

## Update Behavior

The installer is designed for overwrite updates:

- Application files are replaced with the latest published version.
- `appsettings.json` is copied only when it does not already exist.
- Existing database data is not touched because it lives in SQL Server.
- The installer asks Windows to close the running app before replacing files.

Version number is controlled by `MyAppVersion` in `EAPlaymateGroup.iss`.
