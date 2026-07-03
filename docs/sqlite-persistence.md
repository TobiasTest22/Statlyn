# SQLite Persistence

Milestone 1.7 adds a local SQLite foundation in `Statlyn.Data`.

## Scope

- Local-only database creation.
- In-memory SQLite for tests.
- File SQLite for future runtime use.
- Schema version tracking.
- Safe diagnostics.
- Repository layer for masked/source-tagged data.

Unity is not bound to SQLite yet. The Unity Player Profile slice can keep rendering the fixture factory profile until a later UI milestone wires persistence into the app workflow.

## Safety Boundary

SQLite stores only data that has passed the field policy registry and scouting knowledge firewall.

Stored:

- source metadata and split permissions
- masked player identity fields
- visible permitted fields
- player stats such as xG and xA
- physical metrics such as TopSpeed and SprintDistance
- nullable tactical fit in role scores
- blocked-field audit metadata
- import audit counts and safe diagnostics
- generic performance metric definitions
- generic role-output expectation profiles

Not stored:

- raw provider snapshots
- hidden FM26 values
- raw blocked values
- unmasked provider fields
- unlicensed image bytes, URLs, badges or flags

## Reload Flow

Persisted data reloads into a `MaskedPlayer` shape, not an original raw provider snapshot.

```text
SQLite visible fields
-> Stored masked player reconstruction
-> latest safe role score
-> source metadata
-> DataCompletenessReport
-> MaskedPlayerProfileViewModel
```

FM26 remains unsupported until validated memory maps exist. No fake live data is stored or generated.
