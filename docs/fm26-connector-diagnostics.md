# FM26 Connector Diagnostics

Milestone 3.1 added a safe managed bridge over the native connector. Milestone 3.2 improves the FM26 diagnostics surface across the native connector, C# API and React/Tauri UI. This is a diagnostics foundation, not live FM26 data support.

FM process detection is diagnostics only. Read-only access is diagnostics only. No player data is read in 3.2. FM26 remains unsupported without validated memory maps.

## Flow

```text
Statlyn.NativeConnector
-> Statlyn.DataProviders/Fm26/NativeFm26Connector
-> SafeFm26ConnectorService
-> Statlyn.Api DTO
-> React/Tauri Connector Status panel
```

React/Tauri calls the API only. It must not load the native DLL, inspect processes, open SQLite, read memory or call provider code.

## API Endpoints

- `/connector/status`
- `/connector/fm26`
- `/diagnostics/fm26`
- `/diagnostics/fm26/summary`

The endpoints return the same safe status shape: connector availability, connector version/build info, platform state, FM process detected/not detected, process ID if safely available, safe executable file/folder labels, read-only access status, product/file version where available, architecture, build support status, map support status, next action message, warnings and safe error text.

The expected 3.2 map state is `MapMissing` or equivalent. The support message must continue to say that FM26 is unsupported until validated maps exist.

## Safety Limits

Diagnostics must not include:

- raw player snapshots
- hidden values
- CA or PA
- native handles
- module base addresses
- memory addresses
- memory maps
- stack traces or exception details

Detecting `fm.exe` is not build support. `IsFm26Supported` remains false until a later milestone validates a build map and adds a reviewed player-reading path.

First memory-map work is later. First safe player snapshot is later. React/Tauri never calls native connector directly; it reads these diagnostics through `Statlyn.Api` only.

## Validation

Run:

```powershell
.\tools\check-native-readonly.ps1
.\tools\run-connector-diagnostics.ps1
```

The diagnostics script does not require FM26. If the native DLL is absent, the API should return a safe unavailable status rather than crashing.
