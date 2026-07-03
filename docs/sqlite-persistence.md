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

The Unity Data Sources page now calls the SQLite-backed workflow service when the required assemblies and SQLite dependencies are copied. Managed tests verify SQLite behavior. Unity Editor runtime loading of SQLite dependencies still requires manual validation, assisted by the Data Sources `Run Runtime Check` button.

## Safety Boundary

SQLite stores only data that has passed the field policy registry and scouting knowledge firewall.

Stored:

- source metadata and split permissions
- masked player identity fields
- visible permitted fields
- player stats such as xG and xA
- physical metrics such as TopSpeed and SprintDistance
- persisted role name and nullable tactical fit in role scores
- shortlists and shortlist-player workflow labels
- scout assignments, qualitative scout reports and scout report question answers
- Role Lab tactical roles, role pairs, output metric requirements, scout questions and red flags
- blocked-field audit metadata
- import audit counts and safe diagnostics
- generic performance metric definitions
- generic role-output expectation profiles

Not stored:

- raw provider snapshots
- hidden FM26 values
- raw blocked values
- hidden-value-derived shortlist labels
- hidden-value-derived scout report fields
- hidden-value-derived Role Lab templates
- unmasked provider fields
- unlicensed image bytes, URLs, badges or flags

## Transaction And Re-Import Behavior

One import uses a single SQLite connection and transaction for source metadata, players, visible fields, player stats, physical metrics, role scores, blocked audit rows, profile snapshots and the success audit row. If a fatal failure occurs during persistence, the transaction is rolled back and only a sanitized failure audit is written afterward.

Re-import currently uses snapshot replace for player-scoped rows. When the same player is imported again, Statlyn updates the player record, deletes current visible fields, player stats, physical metrics, role scores, blocked audit rows and profile snapshot rows for that player, then inserts the fresh masked snapshot. This keeps reload deterministic and prevents xG, TopSpeed or Finishing rows from doubling.

Unique indexes reinforce this behavior for visible fields, player stats, physical metrics and blocked audit rows.

## Shortlists

Milestone 2.2 expands `Shortlist` and `ShortlistPlayer` for persisted recruitment decision tracking.

`Shortlist` stores:

- name and description
- created and updated timestamps
- active/archived state

`ShortlistPlayer` stores:

- `ShortlistId`
- persisted `PlayerId`
- persisted `StatlynPlayerId`
- status, priority and follow-up action workflow labels
- sanitized role name and recommendation text
- added reason and user note
- created and updated timestamps

The unique `ShortlistId + StatlynPlayerId` index keeps double-adds idempotent. These tables do not store raw provider snapshots, hidden FM26 values or raw blocked values. User notes are user-entered workflow text and pass through the same hidden-value-looking text sanitizer before storage.

## Scout Desk

Milestone 2.3 adds local human scouting tables:

`ScoutAssignment` stores:

- persisted `StatlynPlayerId`
- optional `ShortlistPlayerId` and `ShortlistId`
- persisted `PlayerId`
- assignment title
- sanitized role name
- position group
- priority
- assignment status
- assigned-to text
- created, updated, due and closed timestamps
- source name
- archive state

`ScoutReport` stores:

- optional assignment link
- persisted `PlayerId` and `StatlynPlayerId`
- report date
- role assessed
- qualitative observation ratings
- scout recommendation
- confidence
- strengths, weaknesses, risks and final summary
- follow-up action
- created and updated timestamps

`ScoutReportQuestion` stores role/output prompts and qualitative answers linked to a report.

Scout report text runs through `ScoutTextSanitizer` before storage. Normal qualitative language such as `looks professional` or `handles pressure well` is preserved. Hidden-looking exact assignments such as `CA 155`, `PA=180`, `Professionalism: 20`, `Pressure = 18` or `Consistency 17` are redacted.

Scout Desk indexes cover assignment player/status/shortlist lookups and report player/assignment/date lookups. Existing `ScoutReport` tables are expanded idempotently with `EnsureColumn` so older local databases keep their report rows.

## Role Lab

Milestone 2.4 adds editable phase-aware role modelling tables:

`TacticalRole` stores:

- role name
- tactical phase
- role family
- source
- `IsOfficialFm26Role` defaulting false
- optional future FM26 role id
- position group
- valid slots
- movement, build-up, final-third, pressing, defensive-block and transition behaviour
- timestamps and archive state

`TacticalRolePair` stores:

- in-possession and out-of-possession role links
- IP/OOP slots
- IP/OOP formation labels
- transition complexity
- tactical risk
- positional familiarity note

`RoleOutputMetricRequirement`, `RoleScoutQuestion` and `RoleRedFlag` store output-first expectations and qualitative prompts for roles or role pairs.

Role Lab text is sanitized before persistence. Built-in seed roles use `Source=BuiltInSeed`, `IsOfficialFm26Role=false` and generic/import-ready language. They are not official FM26 mappings and do not store old duty templates, CA, PA, hidden personality values, raw provider data or raw blocked values.

## Diagnostics And Recommendations

Import audit diagnostics pass through a sanitizer before storage. Patterns such as `CurrentAbility: 200`, `Professionalism=19` and `PA 199` are redacted while field names, counts and source names remain readable.

Role scores persist their `RoleName` and `Recommendation` values. Reload does not recompute the role name or recommendation from role fit and confidence. If an old score row has no role name, UI surfaces `Unknown role`; if a player has no score row, Recruitment Centre surfaces `Not scored`.

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

Recruitment Centre queries the same persisted safe tables. It reads `Player`, latest `RoleScore`, `PlayerStat`, `PhysicalMetric`, source metadata, role-output expectation profiles and blocked-field audit counts. It does not reconstruct raw provider snapshots or expose blocked raw values.

## Runtime Path

`StatlynDatabasePathResolver` resolves a default `Statlyn/statlyn.db` path under local app data, or under a caller-provided application data root. Tests use in-memory SQLite through `RuntimeDatabaseFactory.CreateInMemory()`. Unity uses `Application.persistentDataPath/statlyn.db` for the first Data Sources workflow path. Runtime self-checks use a temporary database under Unity's temporary cache path and clean it up after initialization.

File-backed SQLite connections use pooling disabled so temporary runtime-check databases can be cleaned up reliably and Unity file paths are easier to reason about.
## Benchmark Tables

Schema v5 adds `BenchmarkDefinition`, `BenchmarkRun` and `BenchmarkMetricSnapshot`. Definitions store scope, source, position group, role/profile selectors, metric keys and sample thresholds. Runs store summary counts and a safe message.

Snapshots store aggregate safe values only: sample size, median, average, minimum and maximum. They do not store selected player raw values, fake percentiles, hidden FM26 values, raw blocked values or raw provider entities.
