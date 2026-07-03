# Unity Validation

Unity CI is not configured yet. Use this manual checklist after opening `Statlyn.UnityApp` in Unity 6 or newer.

Before opening Unity, run:

```powershell
.\tools\copy-managed-to-unity.ps1
```

This copies the managed Statlyn assemblies and SQLite managed/native dependencies used by the Player Profile and Data Sources bridge into `Assets/Plugins/Managed/Statlyn`.

## Checklist

- Project opens without package resolution errors.
- `Assets/Scenes/Main.unity` opens.
- No C# compile errors in Unity.
- Main shell loads.
- Navigation is visible.
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
- Manual CSV path entry works.
- `Use synthetic fixture CSV` fills a local fixture path when the repository layout is available.
- `Preview CSV` shows file readability, column count, row count, mapped fields, unknown fields and forbidden fields.
- Preview does not create players, stats or visible fields in SQLite.
- `Run Safe Import` shows accepted/rejected rows and database counts when SQLite dependencies load successfully.
- Forbidden fields are shown by safe name/category only.
- No fake live FM26 data is shown.
- No real player images, club badges or unlicensed flags are shown.
- Empty/unsupported states remain honest.

If the Unity Editor cannot load SQLite native dependencies, record the error and keep managed `dotnet test` as the source of truth until Unity packaging is adjusted.

Record the Unity version and screenshots when validating a release candidate.
