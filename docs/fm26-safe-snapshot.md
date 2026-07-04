# FM26 Safe Snapshot Foundation

Milestone 3.4 adds a safe FM26 snapshot foundation for diagnostic metadata only.

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

React/Tauri displays these DTOs only through `Statlyn.Api`. It must not call the native connector, parse memory-map files, read SQLite, inspect local processes or calculate recruitment decisions.

## Validation

Run:

```powershell
.\tools\run-safe-snapshot-diagnostics.ps1
```

The script builds the managed solution, runs the native read-only scan, validates memory-map metadata, starts `Statlyn.Api` temporarily, checks health/connector/map/snapshot endpoints, prints a safe status summary and stops the API process. It does not require FM26 and does not read player data.

First real memory field reads remain a future milestone after validated maps, field policy review and a reviewed safe reader path.
