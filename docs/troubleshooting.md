# Troubleshooting

## FM26 Not Detected

Open Football Manager 26 and load a save, then refresh diagnostics. The current connector searches for `fm.exe`.

## Access Denied

Windows may deny process query/read access depending on launch context and permissions. Statlyn should show access denied in diagnostics and must not retry with write or injection permissions.

## Unsupported Build

This is expected in the initial milestone. No FM26 memory maps are validated yet, so Statlyn returns no player data.

## Empty Squad Or Recruitment Views

Empty views are correct until a validated provider snapshot exists. Statlyn must not show demo players outside automated tests.

## Native Build Missing Compiler

Install Visual Studio Build Tools with C++ support, then rerun the CMake build from `docs/building.md`.
