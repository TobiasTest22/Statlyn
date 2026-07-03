# Testing

The current test project is `Statlyn.Tests`.

Run:

```powershell
dotnet test Statlyn.sln
```

## Safety Tests

The initial tests focus on data-protection behavior:

- Raw hidden CA does not appear in masked data.
- Raw hidden PA does not appear in masked data.
- Hidden personality values are blocked.
- Missing attributes reduce confidence.
- Low FM26 scout knowledge blocks overconfident recommendations.
- UI binding rejects raw entities.
- Scoring rejects raw entities.
- Unsupported FM26 builds return no fake player data.

Milestone 1.5 adds tests for:

- Mislabeled hidden fields in visible facts.
- Unknown fields denied by default.
- Licensed external fields blocked without source permission.
- Player image permission checks.
- Safe nationality flag permission checks.
- Scoring exclusion of non-scorable fields.
- Database schema hidden-field storage checks.
- CSV fixture import blocking a mislabeled ability column.

Milestone 1.6 adds tests for:

- Field instance cardinality across attributes, stats and physical metrics.
- Football field catalog mappings.
- CSV fixture imports with two synthetic players.
- CSV diagnostics and blocked-field counts.
- Split image, flag, badge and export permissions.
- Role scoring with zero values, missing groups and red flags.
- Visual intelligence model safety.
- `MaskedPlayerProfileViewModel` safety and fallback behavior.
- Schema support for `FieldInstanceKey`, `VisibleField`, `PhysicalMetric` and safe blocked-field audit.

Milestone 1.6.1 adds tests for:

- `FixtureProfileFactory` returning `MaskedPlayerProfileViewModel`, not raw snapshots.
- `UnityProfileRenderModel` being buildable only from `MaskedPlayerProfileViewModel`.
- Blocked-data notices and profile display strings excluding raw hidden values from synthetic fixture fields.
- Fixture mode, no live FM26 data, initials avatar fallback and bundled-safe flag behavior.
- Missing-data warnings and unknown tactical fit output.
- Nullable `RoleScore.TacticalFit` schema alignment.
- Split data-source permission columns for player images, provider flags, bundled safe flags, club badges and export.
- Blocked-field audit and visible-field schema safety.

Milestone 1.7 adds tests for:

- In-memory and file SQLite initialization.
- Idempotent schema creation and schema version tracking.
- Safe repository guardrails rejecting raw provider entities.
- Source metadata split permissions, nullable tactical fit and field instance persistence.
- Synthetic CSV import through firewall, scoring, SQLite persistence and import audit.
- Player stats and physical metrics persisting without overwriting one another.
- Generic performance metric definitions staying FM26-unverified by default.
- Position-specific role-output expectation profiles.
- Safe reload into `MaskedPlayer` and `MaskedPlayerProfileViewModel`.
- Database diagnostics counts without hidden or blocked raw values.

Milestone 1.7.1 adds tests for:

- Import transaction commit and fatal rollback.
- Raw-player rejection inside a transaction-aware persistence path.
- Re-import idempotency for visible fields, player stats, physical metrics, blocked audits and role scores.
- Diagnostic sanitization for hidden-value-looking patterns.
- Import audit storage of sanitized diagnostics.
- Persisted role-score recommendations.
- Safe Minutes sample handling for player stats.
- Duplicate-prevention indexes in the schema.

Milestone 1.8 adds tests for:

- `CsvPreviewService` row, column and mapping detection.
- Safe preview handling for Finishing, xG, TopSpeed, unknown fields and forbidden hidden fields.
- Preview remaining read-only and not storing import data.
- Permission-blocked image, flag, badge and licensed metric preview states.
- `DataSourceImportWorkflowService` preview/import counts and database diagnostics.
- Unlicensed source warnings and safe forbidden-field display.
- Idempotent double import through the UI workflow path.
- UI-safe preview/import view models excluding raw provider objects and hidden values.
- Default database path resolution, in-memory test mode and file database initialization.

Milestone 1.8.1 adds tests for:

- Unity runtime-check result safe formatting and hidden-value redaction.
- Temporary SQLite database initialization through the runtime dependency check.
- Runtime check independence from FM26.
- Synthetic fixture path fallback logic for repository and StreamingAssets paths.
- Preview still not storing data after UX hardening.
- Safe import still persisting idempotent masked data.
- Runtime/workflow diagnostics excluding hidden values and fake FM26 statuses.

Milestone 1.9 adds tests for:

- Recruitment Centre query service over persisted safe imports.
- Search, source, position and blocked-field filters.
- Safe role score, confidence, tactical-fit unknown and blocked-count display.
- Output-first summaries by position group.
- Missing core metrics appearing as missing rather than zero.
- Generic metrics remaining FM26-unverified.
- UI-safe Recruitment Centre view models.
- Persisted safe profile preview loading.
- Empty database state and double-import row stability.

Milestone 1.9.1 adds tests for:

- Role-score role name persistence, reload and hidden-value sanitization.
- Recruitment Centre display of persisted role names and `Not scored` fallback.
- Persisted role-output profile selection before generic fallback.
- Goalkeeper, wide attacker and centre-back output specificity.
- Missing output metrics remaining missing rather than zero.
- Generic profiles remaining `IsFm26Specific=false` with attributes as supporting evidence.
- Official logo resource copy/reference checks.
- Safe Recruitment Centre default/reset behavior and empty state.
- Safe profile preview labels, role name, output metrics, no-live-FM26 state and blocked-data notice.

Milestone 2.0 adds tests for:

- `PlayerProfileQueryService` loading persisted synthetic players safely.
- Safe not-found profile results.
- Missing role score and unknown tactical fit display.
- Fixture/import source and no-live-FM26 status.
- Blocked audit notices as count/category/reason only.
- `PlayerProfileReportViewModel` accepting only safe profile results.
- Output-first report sections and support-only attributes.
- Position-specific output expectations.
- Missing output warnings without zero-filling.
- Sample minutes and missing-minutes warnings.
- No fake percentile or comparison group without a benchmark.
- Low confidence scout actions.
- Recruitment Centre row to Player Profile report consistency.

Milestone 2.1 adds tests for:

- Refactored Player Profile report builders staying aligned with the public report.
- Safe visual analytics models rejecting raw provider entities.
- Output-first visual section ordering with Attribute Support after output/evidence.
- Benchmark status using `HasBenchmark=false`, `Percentile=null` and `No benchmark yet.`
- Missing output appearing as missing visuals, not zero values.
- Generic/import metric status becoming visual warnings.
- Recruitment Centre mini visuals binding only safe row view models.
- Legacy fixture visual bars no longer fabricating fixture comparison groups.

Milestone 2.2 adds tests for:

- shortlist status, priority and follow-up labels staying free of hidden-value terminology
- expanded shortlist schema columns, indexes and idempotent migrations
- repository create/add/update/remove/archive behavior
- idempotent double-add through `ShortlistId + StatlynPlayerId`
- raw provider entity rejection for shortlist adds
- workflow adds from default persisted data, Recruitment Centre rows and Player Profile results
- missing-player safe errors
- view-model safety, output metric preservation and no-live-FM26 labels
- shortlist decision helper suggestions for low confidence, missing output, strong targets and blocked-field warnings

Milestone 2.3 adds tests for:

- Scout Desk enums staying free of hidden-value terminology
- assignment/report/question schema, indexes and idempotent migrations
- `ScoutTextSanitizer` redacting hidden-looking exact assignments while preserving qualitative notes
- repository assignment creation, report submission, latest report loading, question answers, status updates and archive hiding
- missing-player and raw-provider rejection
- output-specific scout question generation for strikers, wide attackers, centre-backs and goalkeepers
- blocked-field and low-source-confidence scout prompts
- assignment creation from shortlist players with inherited priority and role
- optional shortlist status and follow-up updates from scout reports
- Unity-facing Scout Desk view-model safety, no-live-FM26 labels and latest report summaries
- latest report missing state for Player Profile and shortlist display
- absence of raw provider entities, raw blocked values, fake live FM26 data and hidden numeric values

Future tests should cover broader persistence migrations, real provider imports, Unity UI state transitions and native connector status parsing.

Unity editor validation remains manual. Run `.\tools\copy-managed-to-unity.ps1` before opening the project so the Unity assembly definition can resolve the shared managed Statlyn DLLs, SQLite dependencies and synthetic fixture CSV, then use the Data Sources runtime check inside Unity. Branding assets live directly under `Statlyn.UnityApp/Assets/Resources/Branding`.
