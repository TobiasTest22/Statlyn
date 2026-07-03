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

Future tests should cover broader persistence migrations, real provider imports, Unity UI state transitions and native connector status parsing.

Unity editor validation remains manual. Run `.\tools\copy-managed-to-unity.ps1` before opening the project so the Unity assembly definition can resolve the shared managed Statlyn DLLs.
