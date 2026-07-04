# Data Source Boundaries

All data must follow this route:

```text
Provider/source
-> mapper
-> normalized football model
-> scouting firewall/masking
-> analytics/application service
-> safe DTO
-> React/Tauri UI
```

Forbidden routes:

- provider/source to React
- raw FM data to analytics
- hidden values to DTOs
- raw database rows to frontend
- frontend-calculated recruitment decisions
- frontend/native connector direct process access

FM26 is only one possible source. CSV imports, manual club datasets, licensed APIs and other permitted sources can be added later through provider abstractions and mappers.

Unknown fields are denied by default. Hidden CA, hidden PA, hidden personality values, raw blocked values, memory addresses and raw provider dumps must never reach DTOs or UI.

FM26 connector diagnostics are allowed to reach DTOs only as safe status: connector availability, Windows/platform state, FM process detected/not detected, safe process ID if available, safe executable file/folder labels, read-only access status, product/file version where available, architecture, build support status, memory-map registry counts, selected-map safe summary, safe snapshot gates, blocking gate, next action and unsupported player-reading state. FM process detection, map selection and safe snapshots are diagnostics metadata only. No player data is read in 3.4. They must not include raw player snapshots, raw memory-map internals, native handles, module base addresses, memory addresses, raw offsets, hidden field names, CA or PA.

React/Tauri never calls native connector directly and never parses memory-map files. First real memory field reads remain a future milestone.
