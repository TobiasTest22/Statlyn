# Memory-Map Registry

Milestone 3.3 adds a metadata-only FM26 memory-map registry. It answers which local map files exist, whether they are valid JSON, whether they are templates, whether they are validated, and whether any validated metadata matches detected FM process build metadata. Milestone 3.4 consumes that registry inside safe FM26 diagnostic snapshots.

The registry does not read player data. It does not expose memory addresses, raw offsets, process handles, raw provider snapshots, hidden FM values, CA or PA. React/Tauri does not parse map files; it reads only safe DTO summaries from `Statlyn.Api`.

## Current File Shape

The current `memory-maps/fm26/templates/*.map.json` files are templates. They use the legacy template shape:

- `game`
- `build`
- `entity`
- `isTemplate`
- `supported`
- `fields`

Field entries may include:

- `fieldName`
- `dataType`
- `visibilityCategory`
- `canDisplay`
- `canScore`
- `canStore`
- `notes`

Templates are allowed to contain blocked field definitions only when those fields cannot display, store or score. Templates are not usable and must not be marked validated.

Future non-template metadata can include a richer `buildTarget` object with game version, build number, platform and architecture. A map becomes usable only when `isValidated=true`, `isTemplate=false`, usage is read-only/metadata-only, and all guardrail validation passes.

## API Surface

Safe registry status is exposed through:

- `/diagnostics/memory-maps`
- `/connector/memory-maps`
- `/connector/status`
- `/diagnostics/fm26`
- `/diagnostics/fm26/snapshot`
- `/connector/fm26/snapshot`

The API exposes counts, statuses, safe messages, selected-map summary and snapshot gate outcomes only. It does not expose field internals, offsets, addresses, player values or hidden field names.

## Validation Rules

- Missing registry directory is safe and returns an empty/missing status.
- Invalid JSON is reported as an invalid map diagnostic, not as an API crash.
- Template maps are loaded but never usable.
- Unvalidated maps are loaded but never usable.
- Write-enabled maps fail validation.
- Unknown field visibility is denied by default.
- Hidden fields must be blocked from display, storage and scoring.
- A validated matching map means map metadata is available; player reading is still not implemented.
- Safe FM26 snapshots block at validated-map readiness when only template maps exist.
- If tests supply a synthetic validated matching map, safe snapshots still block at reader-not-implemented.

First real memory field reading remains a later milestone after validated maps, a reviewed reader and field policy checks.
