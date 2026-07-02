# Unity Validation

Unity CI is not configured yet. Use this manual checklist after opening `Statlyn.UnityApp` in Unity 6 or newer.

Before opening Unity, run:

```powershell
.\tools\copy-managed-to-unity.ps1
```

This copies the managed Statlyn assemblies used by the Player Profile bridge into `Assets/Plugins/Managed/Statlyn`.

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
- No fake live FM26 data is shown.
- No real player images, club badges or unlicensed flags are shown.
- Empty/unsupported states remain honest.

Record the Unity version and screenshots when validating a release candidate.
