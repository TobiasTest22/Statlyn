# Memory Connector

The native connector is a Windows C++ DLL with a stable C ABI for Unity and C# bindings.

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

## Read-Only Policy

The connector opens FM26 with `PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ`. It must not allocate remote memory, create remote threads, suspend FM26, patch memory, inject code or write process memory.

Run the local guard script after connector edits:

```powershell
.\tools\check-native-readonly.ps1
```

## Current Behavior

The connector can search for `fm.exe`, query process metadata where Windows allows it and report unsupported build state. Snapshot reads currently return an unsupported status with an empty player array.

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
