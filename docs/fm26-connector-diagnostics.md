# FM26 Connector Diagnostics

Milestone 3.1 adds a safe managed bridge over the native connector. This is a diagnostics foundation, not live FM26 data support.

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

The endpoints return the same safe status shape: connector availability, connector version/build info, platform state, FM process detected/not detected, read-only access status, product version, architecture, support message and safe error text.

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

## Validation

Run:

```powershell
.\tools\check-native-readonly.ps1
.\tools\run-connector-diagnostics.ps1
```

The diagnostics script does not require FM26. If the native DLL is absent, the API should return a safe unavailable status rather than crashing.
