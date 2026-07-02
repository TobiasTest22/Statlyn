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

Future tests should cover SQLite persistence, real provider imports, Unity UI state transitions and native connector status parsing.
