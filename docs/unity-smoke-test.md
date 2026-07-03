# Unity Smoke Test

Milestone 2.6 adds a full local smoke test for the Unity runtime path. Milestone 2.7 keeps the same smoke-test workflow while moving the UI toward the dark command-center baseline. It does not require FM26, network access, scraping, external APIs or real player data.

## Before Opening Unity

Run:

```powershell
.\tools\copy-managed-to-unity.ps1
```

This copies managed Statlyn assemblies, SQLite dependencies, the Windows `e_sqlite3` plugin and the synthetic fixture CSV to:

```text
Statlyn.UnityApp\Assets\StreamingAssets\Statlyn\Fixtures\players.sample.csv
```

## In Unity

Open `Statlyn.UnityApp` in Unity 6 or newer. Confirm there are no compile errors and the Statlyn logo appears.

Open `Diagnostics`.

Run:

- `Run Runtime Check`
- `Run Full Smoke Test`

## Smoke Test Steps

The full smoke test validates:

1. Managed Statlyn assemblies are available.
2. SQLite managed dependencies are available.
3. SQLite can initialize a temp database.
4. A smoke-test database path resolves.
5. The smoke-test database initializes.
6. The synthetic fixture CSV is found.
7. The fixture can be previewed.
8. The fixture can be safely imported.
9. Recruitment Centre can query imported players.
10. Player Profile can load the first player.
11. The first player can be added to a shortlist.
12. A scout assignment can be created.
13. A sanitized scout report can be submitted.
14. Role Lab roles can be seeded.
15. Benchmark definitions can be seeded and run.

## Database Modes

The main Unity database uses `RuntimeMain` and lives under `Application.persistentDataPath`.

The smoke test uses `RuntimeSmokeTest` and lives under `Application.temporaryCachePath\StatlynSmokeTest\statlyn-smoke-test.db`. It is cleared before the smoke test runs by default, so it should not pollute the main app database.

## Expected Limitations

FM26 remains unsupported until validated memory maps exist. CSV local import is the only user-facing data source workflow. Benchmarks are generic/import only and not official FM26. Percentiles are shown only when a real comparison group is available.

The Home dashboard reads local SQLite counts and may show `Awaiting local data.` before Data Sources import or the smoke test has created data. That is expected and should not be replaced with fake counts.

If SQLite fails in Unity, rerun `tools/copy-managed-to-unity.ps1`, confirm `Microsoft.Data.Sqlite.dll`, `SQLitePCLRaw.*.dll` and `Assets\Plugins\x86_64\e_sqlite3.dll` exist, then run the Runtime Check again.
