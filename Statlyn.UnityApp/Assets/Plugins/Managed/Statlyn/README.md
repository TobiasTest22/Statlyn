# Statlyn managed plugin assemblies

This folder is populated by `tools/copy-managed-to-unity.ps1` after the managed solution builds.

The Unity Player Profile slice and Data Sources page reference the shared Statlyn managed assemblies from here. The copy script also places SQLite managed/native dependencies here for the local CSV import workflow. Do not commit generated DLLs or runtime folders; rebuild and rerun the copy script before manual Unity editor validation.
