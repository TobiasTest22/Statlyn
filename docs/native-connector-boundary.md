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

The connector remains read-only. Statlyn does not write to FM process memory and does not fake FM26 player-reading support when no validated memory-map metadata exists.

FM process detection is diagnostics only. Read-only access is diagnostics only. Memory-map registry selection is metadata only. Safe FM26 snapshots are diagnostic metadata only. No player data is read in 3.4. FM26 remains unsupported for player reading.

The managed C# binding lives in `Statlyn.DataProviders/Fm26`. Its public `IFm26NativeConnector` surface exposes only availability, connector version/build info, safe FM process diagnostics and unsupported build validation. It does not expose native handles, module base addresses, memory addresses, raw snapshots, player-reading methods, CA, PA or hidden values.

React/Tauri never calls native connector directly. The desktop receives connector and safe snapshot status through `Statlyn.Api` endpoints such as `/connector/status` and `/diagnostics/fm26/snapshot`, and Rust code must not add process, memory or database access.

Milestone 3.3 adds metadata-only memory-map registry validation. Milestone 3.4 adds `SafeFm26SnapshotService`, which combines connector diagnostics and map metadata into a gate report. Template-only maps block live reading, validated test maps still block because no live reader exists, and first real memory field reads remain a future milestone.
