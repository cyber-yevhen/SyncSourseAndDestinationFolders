# SyncSourseAndDestinationFolders

`SyncSourseAndDestinationFolders` is a small .NET 8 console app for keeping one **sourse** folder in sync with one or more **destination** folders.

The short executable name is `ssad.exe`.

## What the app does

The app works with:

- 1 sourse folder
- 1 or more destination folders

Main behavior:

1. On start, it copies files from sourse to destinations.
2. If a file is identical, it is not copied again.
3. It keeps watching the sourse and destination folders for changes.
4. If the sourse changes, the app copies that change to the destinations.
5. If a destination changes, the app updates the sourse **only if that file or folder already exists in the sourse**.
6. After the sourse is updated from a destination, the app syncs that result to the other destinations.

Important rule:

- New files that appear only in a destination are ignored and are **not** copied into the sourse.

## Requirements

- Windows
- .NET 8 SDK

## Build

Open a terminal in the project folder and run:

```powershell
dotnet build -c Release
```

The executable will be created here:

```text
bin\Release\net8.0\ssad.exe
```

## Run

Start with interactive input:

```powershell
.\bin\Release\net8.0\ssad.exe
```

Start with source and destinations from CLI:

```powershell
.\bin\Release\net8.0\ssad.exe -s "C:\MySource" -d "C:\Dest1" -d "C:\Dest2"
```

Other supported source forms:

```powershell
.\bin\Release\net8.0\ssad.exe s:"C:\MySource"
.\bin\Release\net8.0\ssad.exe -sourse "C:\MySource"
```

## Presets

The app stores sync presets in:

```text
sync-settings.json
```

This file is created near the executable after the user confirms startup with a valid sourse and at least one valid destination.

To list saved presets and exit:

```powershell
.\bin\Release\net8.0\ssad.exe -p
.\bin\Release\net8.0\ssad.exe "preset:anything"
```

Any preset key currently shows saved presets and exits.

## Console output

When a file is copied, the app writes one log line like this:

```text
[2026-05-14 10:03:53.568] [s >> d] /file.txt
[2026-05-14 10:03:55.014] [s << d] /file.txt
```

Meaning:

- `[s >> d]` = copied from sourse to destination(s)
- `[s << d]` = copied from destination to sourse

## Notes for developers

- `.vs/`, `bin/`, and `obj/` are ignored in Git.
- The project is intentionally small and split into simple classes under `Application`, `Services`, `Models`, and `Abstractions`.
- Press `Ctrl+C` to stop monitoring.
