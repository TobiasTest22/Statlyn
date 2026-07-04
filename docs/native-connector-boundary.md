# Native Connector Boundary

`Statlyn.NativeConnector` is connector-only. Its current job is safe FM process diagnostics and future FM data access preparation:

- FM process detection
- read-only connector preparation
- connector version/build information
- connector diagnostics
- native build validation

It must not contain:

- recruitment logic
- role fit logic
- benchmark logic
- scouting firewall logic
- UI logic
- database business logic

The connector remains read-only. Statlyn does not write to FM process memory and does not fake FM26 support when no validated memory map exists.

The managed C# binding lives in `Statlyn.DataProviders/Fm26`. Its public `IFm26NativeConnector` surface exposes only availability, connector version/build info, safe FM process diagnostics and unsupported build validation. It does not expose native handles, module base addresses, memory addresses, raw snapshots, player-reading methods, CA, PA or hidden values.

React/Tauri never calls the native connector. The desktop receives connector status through `Statlyn.Api` endpoints such as `/connector/status`, and Rust code must not add process, memory or database access.
