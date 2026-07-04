# Unity Legacy Transition

`Statlyn.UnityApp` is preserved as the current prototype shell and validation surface. It is no longer the strategic UI direction.

Going forward:

- new strategic desktop UI work belongs in `Statlyn.Desktop`
- C# logic must stay independent of Unity
- Unity should not receive new major product modules
- Unity can remain useful for smoke tests and prototype validation while the React/Tauri workspace matures

Unity still uses copied managed assemblies and SQLite dependencies through `tools/copy-managed-to-unity.ps1`. If Unity is opened manually, validate Runtime Check and Full Smoke Test before claiming SQLite-in-Unity works.

React/Tauri is the future analyst workspace. Unity is legacy/current shell only.
