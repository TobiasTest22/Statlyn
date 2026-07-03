# Building

## Managed Projects

```powershell
dotnet build Statlyn.sln
```

## Tests

```powershell
dotnet test Statlyn.sln
```

The test suite uses in-memory SQLite for persistence tests and the synthetic CSV fixture for safe import/reload coverage.

## Native Connector

Requires CMake and a Windows C++ compiler.

```powershell
cmake -S Statlyn.NativeConnector -B build\native
cmake --build build\native --config Release
```

The release DLL should later be copied into the Unity plugin folder and final desktop build output.

## Unity

Copy the shared managed assemblies before opening the project:

```powershell
.\tools\copy-managed-to-unity.ps1
```

The copy script now includes Statlyn managed assemblies, Microsoft.Data.Sqlite/SQLitePCL managed dependencies, the Windows x64 `e_sqlite3.dll` native dependency under `Assets/Plugins/x86_64`, a compatibility runtime copy under the managed plugin folder, and the synthetic fixture CSV under `Assets/StreamingAssets/Statlyn/Fixtures`. Official logo files live in `Assets/Resources/Branding`; see `docs/branding.md`.

Open `Statlyn.UnityApp` in Unity 6 or newer. The shell includes the Data Sources page for local CSV preview/import and the first Recruitment Centre page for persisted safe players. Click `Run Runtime Check` before preview/import to verify copied assemblies, SQLite loading, temporary database initialization and workflow construction. SQLite persistence is verified in managed tests; SQLite runtime loading inside Unity still needs manual Editor validation.

Use `docs/unity-validation.md` for the current manual Unity checklist.
## Unity Runtime Preparation

Before opening the Unity desktop app, run:

```powershell
.\tools\copy-managed-to-unity.ps1
```

This copies the Statlyn managed assemblies, SQLite dependencies, `e_sqlite3` native plugin and the synthetic fixture CSV into the Unity project.

After opening `Statlyn.UnityApp` in Unity 6 or newer, use the Diagnostics page to run `Run Runtime Check` and `Run Full Smoke Test`. The smoke test uses a separate smoke-test database and does not require FM26.
