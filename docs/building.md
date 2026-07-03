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

The copy script now includes Statlyn managed assemblies plus Microsoft.Data.Sqlite/SQLitePCL managed dependencies and the Windows x64 `e_sqlite3.dll` native dependency under the managed plugin folder.

Open `Statlyn.UnityApp` in Unity 6 or newer. The shell includes the first Data Sources page for local CSV preview/import. SQLite persistence is verified in managed tests; SQLite runtime loading inside Unity still needs manual Editor validation.

Use `docs/unity-validation.md` for the current manual Unity checklist.
