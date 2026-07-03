# Unity Validation

Unity CI is not configured yet. Use this manual checklist after opening `Statlyn.UnityApp` in Unity 6 or newer.

Before opening Unity, run:

```powershell
.\tools\copy-managed-to-unity.ps1
```

This copies the managed Statlyn assemblies, SQLite managed dependencies, Windows x64 SQLite native plugin and the synthetic fixture CSV used by the Player Profile and Data Sources bridge.

## Checklist

- Project opens without package resolution errors.
- `Assets/Scenes/Main.unity` opens.
- No C# compile errors in Unity.
- Main shell loads.
- Navigation is visible.
- Sidebar shows the official Statlyn logo asset from `Assets/Resources/Branding`.
- Dashboard header shows a small official Statlyn logo mark.
- Player Profile slice loads.
- Player Profile slice renders from `MaskedPlayerProfileViewModel` or `UnityProfileRenderModel` built only from it.
- Profile slice clearly shows fixture mode.
- Profile slice clearly shows no live FM26 data.
- Profile slice keeps FM26 memory maps marked unsupported until validated maps exist.
- Profile slice shows source confidence, data completeness, role fit, confidence, risk, radar placeholder, percentile bars, evidence cards and missing/blocked-data warnings.
- Blocked-data notice does not show raw hidden values.
- Diagnostics panel loads.
- Data Sources navigation opens a CSV-only page.
- Data Sources page shows the active local SQLite path.
- `Run Runtime Check` reports managed assembly, SQLite managed, SQLite native, database init and workflow service status.
- Runtime check uses a temporary database and does not write to the main `statlyn.db`.
- Manual CSV path entry works.
- `Use synthetic fixture CSV` fills a local fixture path from the repository or StreamingAssets copy.
- `Preview CSV` shows file readability, column count, row count, mapped fields, unknown fields and forbidden fields.
- Preview does not create players, stats or visible fields in SQLite.
- `Run Safe Import` shows accepted/rejected rows and database counts when SQLite dependencies load successfully.
- Safe import shows blocked-field and unknown-field counts.
- Forbidden fields are shown by safe name/category only.
- Recruitment navigation opens the Recruitment Centre page.
- Imported players appear after a safe CSV import.
- Search/source/position/minimum filters refresh the player cards.
- Reset filters returns search/source/position/minimum filters to defaults.
- Sort selector works for role fit, confidence, data completeness, source and position.
- Player cards show source confidence, completeness, persisted role name, role fit, tactical-fit status, confidence, recommendation, risk, output metrics, blocked-field count and missing-data count.
- `Open Profile` shows a persisted safe profile preview or a safe error card.
- Profile preview shows selected player name, source, fixture/import mode, no live FM26 data, role name, role fit/confidence/risk, output metrics, missing-data warning and blocked-data safe notice.
- No fake live FM26 data is shown.
- No real player images, club badges or unlicensed flags are shown.
- Empty/unsupported states remain honest.

If the Unity Editor cannot load SQLite native dependencies, record the runtime-check error, verify `Assets/Plugins/x86_64/e_sqlite3.dll`, rerun `tools/copy-managed-to-unity.ps1`, and keep managed `dotnet test` as the source of truth until Unity packaging is adjusted.

Record the Unity version and screenshots when validating a release candidate.
