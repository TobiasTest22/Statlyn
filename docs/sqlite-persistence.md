# SQLite Persistence

Milestone 1.7 adds a local SQLite foundation in `Statlyn.Data`.

## Scope

- Local-only database creation.
- In-memory SQLite for tests.
- File SQLite for future runtime use.
- Schema version tracking.
- Safe diagnostics.
- Repository layer for masked/source-tagged data.
- Import transaction boundary.
- Re-import duplicate prevention.

The Unity Data Sources page now calls the SQLite-backed workflow service when the required assemblies and SQLite dependencies are copied. Managed tests verify SQLite behavior. Unity Editor runtime loading of SQLite dependencies still requires manual validation.

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

## Transaction And Re-Import Behavior

One import uses a single SQLite connection and transaction for source metadata, players, visible fields, player stats, physical metrics, role scores, blocked audit rows, profile snapshots and the success audit row. If a fatal failure occurs during persistence, the transaction is rolled back and only a sanitized failure audit is written afterward.

Re-import currently uses snapshot replace for player-scoped rows. When the same player is imported again, Statlyn updates the player record, deletes current visible fields, player stats, physical metrics, role scores, blocked audit rows and profile snapshot rows for that player, then inserts the fresh masked snapshot. This keeps reload deterministic and prevents xG, TopSpeed or Finishing rows from doubling.

Unique indexes reinforce this behavior for visible fields, player stats, physical metrics and blocked audit rows.

## Diagnostics And Recommendations

Import audit diagnostics pass through a sanitizer before storage. Patterns such as `CurrentAbility: 200`, `Professionalism=19` and `PA 199` are redacted while field names, counts and source names remain readable.

Role scores persist their `Recommendation` value. Reload does not recompute the recommendation from role fit and confidence.

## Sample Minutes

If a safe visible `Minutes` player-stat field exists, `PlayerStat` rows store that value as sample minutes. If no safe minutes field exists, `Minutes` remains `0`, `SampleMinutesMissing` is true and `MinutesSource` is `missing`. Statlyn does not assume per-90 validity without sample minutes.

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

## Runtime Path

`StatlynDatabasePathResolver` resolves a default `Statlyn/statlyn.db` path under local app data, or under a caller-provided application data root. Tests use in-memory SQLite through `RuntimeDatabaseFactory.CreateInMemory()`. Unity uses `Application.persistentDataPath/statlyn.db` for the first Data Sources workflow path.
