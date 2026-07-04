# Memory Connector

The native connector is a Windows C++ DLL with a stable C ABI for managed C# bindings. Current production use is safe diagnostics only.

FM process detection is diagnostics only. Read-only access is diagnostics only. No player data is read in 3.2. FM26 remains unsupported without validated memory maps.

Required exports are present in `Statlyn.NativeConnector/include/StatlynNativeConnector.h`:

- `Statlyn_DetectFM26`
- `Statlyn_GetProcessInfo`
- `Statlyn_OpenReadOnly`
- `Statlyn_Close`
- `Statlyn_ValidateBuild`
- `Statlyn_ReadSnapshot`
- `Statlyn_GetLastError`
- `Statlyn_GetDiagnostics`
- `Statlyn_GetConnectorVersion`
- `StatlynConnector_GetVersion`
- `StatlynConnector_GetBuildInfo`
- `StatlynConnector_DetectFmProcess`
- `StatlynConnector_GetLastError`
- `StatlynConnector_ResetLastError`
- `StatlynConnector_OpenReadOnlyProcess`
- `StatlynConnector_CloseHandle`

The managed binding uses the `StatlynConnector_*` exports for status. Public C# diagnostics do not include native handles, module base addresses or memory addresses.

## Read-Only Policy

The connector opens FM26 with `PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ`. It must not allocate remote memory, create remote threads, suspend FM26, patch memory, inject code or write process memory.

Run the local guard script after connector edits:

```powershell
.\tools\check-native-readonly.ps1
```

## Current Behavior

The connector can search for `fm.exe`, query process metadata where Windows allows it and report unsupported build state. The safe API layer exposes connector availability, FM process detection, safe process labels, read-only access status, product/file version where available, architecture, build support status, map status and next action. Snapshot reads remain unsupported and are not used by the managed public connector interface.

Detecting an FM process is not the same as supporting that FM26 build. Until validated maps exist, Statlyn must show unsupported status and return no player data.

First memory-map work is later. First safe player snapshot is later. React/Tauri never calls native connector directly.

## Build-Specific Maps

Validated build maps belong under:

```text
memory-maps/fm26/{build}/players.map.json
memory-maps/fm26/{build}/clubs.map.json
memory-maps/fm26/{build}/nations.map.json
memory-maps/fm26/{build}/contracts.map.json
memory-maps/fm26/{build}/tactics.map.json
memory-maps/fm26/{build}/scout_knowledge.map.json
```

No map should be marked supported until it has been validated against the matching FM26 build.
