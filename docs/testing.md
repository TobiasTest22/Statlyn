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

Future tests should cover SQLite persistence, real provider imports, Unity UI state transitions and native connector status parsing.

Unity editor validation remains manual. Run `.\tools\copy-managed-to-unity.ps1` before opening the project so the Unity assembly definition can resolve the shared managed Statlyn DLLs.
