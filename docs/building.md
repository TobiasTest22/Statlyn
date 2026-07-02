# Building

## Managed Projects

```powershell
dotnet build Statlyn.sln
```

## Tests

```powershell
dotnet test Statlyn.sln
```

## Native Connector

Requires CMake and a Windows C++ compiler.

```powershell
cmake -S Statlyn.NativeConnector -B build\native
cmake --build build\native --config Release
```

The release DLL should later be copied into the Unity plugin folder and final desktop build output.

## Unity

Open `Statlyn.UnityApp` in Unity 6 or newer. The shell is intentionally minimal and data-free until a validated provider is connected.

Use `docs/unity-validation.md` for the current manual Unity checklist.
