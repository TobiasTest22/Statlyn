# Unity Validation

Unity CI is not configured yet. Use this manual checklist after opening `Statlyn.UnityApp` in Unity 6 or newer.

## Checklist

- Project opens without package resolution errors.
- `Assets/Scenes/Main.unity` opens.
- No C# compile errors in Unity.
- Main shell loads.
- Navigation is visible.
- Player Profile slice loads.
- Profile slice clearly shows fixture mode.
- Diagnostics panel loads.
- No fake live FM26 data is shown.
- No real player images, club badges or unlicensed flags are shown.
- Empty/unsupported states remain honest.

Record the Unity version and screenshots when validating a release candidate.
