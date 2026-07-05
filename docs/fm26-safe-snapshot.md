# FM26 Safe Snapshot Foundation

Milestone 3.4 adds a safe FM26 snapshot foundation for diagnostic metadata only. Milestone 3.5 adds a persisted audit trail for those same safe diagnostics.

This is not player reading, not live squad data, not memory-map field reading and not hidden value access. The snapshot answers whether the local FM26 environment is ready for future work and which safety gate currently blocks live reading.

## Snapshot Contents

The safe snapshot may include:

- snapshot ID and generated timestamp
- connector availability
- platform status
- FM process detected or not detected
- safe process/build metadata when available
- read-only diagnostic status
- memory-map registry status
- map counts for found, validated, template and invalid metadata
- selected map ID/display/build summary when safe
- gate results and the first blocking gate
- safe next action, warnings and errors

The safe snapshot must not include:

- player data or squad data
- attributes or scout values from FM memory
- hidden FM values, CA or PA
- raw provider snapshots
- raw offsets, memory addresses, module bases or handles
- raw memory-map field internals
- stack traces or exception details

## Gate Order

`SafeFm26SnapshotService` combines connector diagnostics and memory-map registry metadata. `Fm26SnapshotGateEvaluator` checks:

1. connector availability
2. platform support
3. FM process detection
4. read-only diagnostics
5. memory-map registry
6. validated map availability
7. selected map/build match
8. live reader implementation
9. field policy

Current expected behavior is blocked. With template-only maps, the snapshot blocks at validated-map readiness. With a synthetic validated map in tests, the snapshot still blocks at `BlockedReaderNotImplemented`. `IsFm26Supported` and `IsLiveReadingAvailable` remain false.

## API Surface

Safe snapshot DTOs are exposed through:

- `/diagnostics/fm26/snapshot`
- `/connector/fm26/snapshot`
- `/diagnostics/fm26/snapshot/readiness`

The preview endpoint remains read-only from an audit perspective: `GET /diagnostics/fm26/snapshot` creates an in-memory preview and does not append history.

Persisted snapshots are safe diagnostic metadata only. No player data is stored, no hidden values are stored and no raw memory details are stored. The audit API is:

- `POST /diagnostics/fm26/snapshots` creates and stores one safe diagnostic snapshot
- `GET /diagnostics/fm26/snapshots/latest` returns the latest persisted safe snapshot
- `GET /diagnostics/fm26/snapshots` lists persisted safe snapshot summaries
- `GET /diagnostics/fm26/snapshots/{snapshotId}` returns one persisted safe snapshot

Persisted rows may include generated time, snapshot status, connector status, process status, read-only status, memory-map registry status, map counts, blocking gate, live-reading allowed flag, next action and gate results. They must not include player data, attributes, hidden values, CA, PA, memory addresses, raw offsets, native handles, raw provider snapshots or raw memory-map internals.

React/Tauri displays these DTOs only through `Statlyn.Api`. It must not call the native connector, parse memory-map files, read SQLite, inspect local processes or calculate recruitment decisions.

## Validation

Run:

```powershell
.\tools\run-safe-snapshot-diagnostics.ps1
```

The script builds the managed solution, runs the native read-only scan, validates memory-map metadata, starts `Statlyn.Api` temporarily, checks health/connector/map/snapshot endpoints, creates a persisted safe snapshot by default, checks latest/history endpoints, prints a safe status summary and stops the API process. Use `-NoPersist` for preview-only validation. It does not require FM26 and does not read player data.

First real memory field reads remain a future milestone after validated maps, field policy review and a reviewed safe reader path.
