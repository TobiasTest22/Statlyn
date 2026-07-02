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

Future tests should cover SQLite persistence, real provider imports, Unity UI state transitions and native connector status parsing.
