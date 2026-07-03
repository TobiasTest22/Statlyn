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
- Profile slice shows source confidence, data completeness, role fit, confidence, risk, masked evidence, benchmark-unavailable status, evidence cards and missing/blocked-data warnings.
- Blocked-data notice does not show raw hidden values.
- Player Profile navigation opens the Player Profile v1 page.
- Player Profile page shows persisted-safe/no-live-FM26 header copy and the official Statlyn logo mark.
- `Load First Imported Player` loads a profile after a safe CSV import.
- Manual `StatlynPlayerId` loading works for an imported player.
- Profile report shows identity, source, verdict score cards, role/output fit, core output, supporting output, physical output, data quality/sample size, missing output, warnings, evidence, scout actions, attribute support, blocked-data safe notice and benchmark status.
- Profile report visual components render score cards, horizontal bars, metric tiles, data-quality tiles, evidence cards, warning panels, missing-data panels, blocked-data panels and benchmark status from safe view models only.
- Profile report says generic/import metrics are not FM26-verified.
- Profile report says `No benchmark yet.` and benchmark percentile is unavailable until a real comparison group exists.
- Profile report shows shortlist membership status and `Add to Main Recruitment List`.
- Adding from Player Profile creates or updates the Main Recruitment List safely.
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
- Player cards show source confidence, completeness, persisted role name, role fit score, tactical-fit status, confidence/completeness bars, risk indicator, output mini list, blocked-field badge, missing-data badge and no-live-FM26 label.
- Player cards show `Add to Main Recruitment List`.
- Player cards show a `Shortlisted` badge after the player belongs to an active shortlist.
- `Open Profile` shows the full persisted safe Player Profile report or a safe error card.
- Profile report from Recruitment Centre shows selected player name, source, fixture/import mode, no live FM26 data, role name, role fit/confidence/risk, output metrics, missing-data warning and blocked-data safe notice.
- Shortlists navigation opens the Shortlists page.
- Create shortlist form creates a persisted shortlist.
- Shortlists overview shows shortlist name, player count, active/archive status and updated time.
- Shortlist detail shows persisted players with status, priority, follow-up, role fit, confidence, key output metrics, warnings and no-live-FM26 state.
- Status, priority, follow-up and user note can be updated safely.
- Remove button removes a player from the shortlist.
- No fake live FM26 data is shown.
- No real player images, club badges or unlicensed flags are shown.
- Empty/unsupported states remain honest.

If the Unity Editor cannot load SQLite native dependencies, record the runtime-check error, verify `Assets/Plugins/x86_64/e_sqlite3.dll`, rerun `tools/copy-managed-to-unity.ps1`, and keep managed `dotnet test` as the source of truth until Unity packaging is adjusted.

Record the Unity version and screenshots when validating a release candidate.
