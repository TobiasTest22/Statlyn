# FM26 Snapshot Audit Trail

Milestone 3.5 persists safe FM26 diagnostic snapshots in local SQLite so Statlyn can explain why live reading is blocked over time.

Persisted snapshots are safe diagnostic metadata only. No player data is stored. No hidden values, CA, PA, raw provider snapshots, memory addresses, raw offsets, native handles, raw memory-map internals or stack traces are stored.

## Stored Metadata

The audit trail stores:

- snapshot ID and generated timestamp
- snapshot status and safe message
- connector availability at the time
- platform, process detection and process status at the time
- read-only diagnostic status
- memory-map registry status and safe map counts
- selected safe map summary where available
- all-gates-passed flag
- blocking gate
- `liveReadingAllowed`, currently false
- safe next action
- warning and error counts
- ordered gate results with safe messages

The audit trail does not persist player rows, FM attributes, live squad data, memory field values or recruitment decisions.

## API

`GET /diagnostics/fm26/snapshot` returns a current preview and does not write history.

`POST /diagnostics/fm26/snapshots` creates and persists one safe diagnostic snapshot.

`GET /diagnostics/fm26/snapshots/latest` returns the latest persisted snapshot, if one exists.

`GET /diagnostics/fm26/snapshots` returns snapshot history summaries.

`GET /diagnostics/fm26/snapshots/{snapshotId}` returns one persisted safe snapshot by ID.

## UI Boundary

React/Tauri renders the audit trail from `Statlyn.Api` DTOs only. It must not read SQLite directly, call the native connector, parse memory-map files, inspect local processes or evaluate gates in TypeScript.

If no persisted snapshots exist, the desktop shows `No persisted snapshots yet` and does not create fake rows. Creating a snapshot through the UI calls the POST endpoint and then refreshes history through the API.

FM26 remains unsupported until validated maps and later reader milestones exist.
