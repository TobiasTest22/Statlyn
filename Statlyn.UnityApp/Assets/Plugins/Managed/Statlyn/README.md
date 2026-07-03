# Statlyn managed plugin assemblies

This folder is populated by `tools/copy-managed-to-unity.ps1` after the managed solution builds.

The Unity Player Profile slice and Data Sources page reference the shared Statlyn managed assemblies from here. The copy script also places SQLite managed dependencies here, a Windows x64 native copy under `Assets/Plugins/x86_64`, and the synthetic fixture CSV under `Assets/StreamingAssets/Statlyn/Fixtures`. Do not commit generated DLLs, runtime folders or copied fixture files; rebuild and rerun the copy script before manual Unity editor validation.
