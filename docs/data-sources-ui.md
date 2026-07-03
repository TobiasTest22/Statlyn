# Data Sources UI

Milestone 1.8 adds the first user-facing Data Sources workflow in the Unity shell. Milestone 1.8.1 adds runtime dependency diagnostics so SQLite and copied plugin dependencies can be checked before import.

## Scope

- Local CSV files only.
- Manual path entry, plus a synthetic fixture quick-fill button.
- Source name, licence status, allowed usage and confidence inputs.
- Permission toggles for player images, provider flags, bundled safe flag placeholders, club badges and export.
- Read-only CSV preview before import.
- Safe import through the existing provider/firewall/persistence pipeline.
- Database diagnostics after import.
- Runtime dependency check button.

No network sources, scraping, FotMob integration, external APIs, fake live FM26 data, unlicensed player images, badges or provider flags are included.

## Runtime Path

Unity uses `Application.persistentDataPath/statlyn.db` for the first local SQLite path. The runtime self-check uses a temporary database under Unity's temporary cache path and deletes it after the check. Tests use in-memory SQLite. A fuller settings screen can add database path override later.

## Runtime Check

Click `Run Runtime Check` on the Data Sources page after running `tools/copy-managed-to-unity.ps1`. The check verifies:

- Statlyn managed assemblies can resolve.
- Microsoft.Data.Sqlite and SQLitePCLRaw managed assemblies can resolve.
- SQLite can open a test connection.
- A temporary SQLite database can initialize.
- `DataSourceImportWorkflowService` and `CsvPreviewService` can be constructed.

If SQLite fails in Unity, the page should show the dependency/path failure instead of crashing or showing fake success. Confirm that `Assets/Plugins/x86_64/e_sqlite3.dll` exists on Windows and rerun the copy script.

## Synthetic Fixture CSV

The quick-fill button checks the repository development fixture path, `Assets/Fixtures/players.sample.csv`, and `StreamingAssets/Statlyn/Fixtures/players.sample.csv`. The copy script copies the synthetic fixture to `Statlyn.UnityApp/Assets/StreamingAssets/Statlyn/Fixtures/players.sample.csv`. The fixture is development/test data only and is not live FM26 data.

## Safety

Preview shows column names, mapping status and counts only. It does not store data. Import stores only masked, permitted fields after the scouting firewall. Forbidden fields appear by safe field name/category and reason only; raw blocked values and hidden numeric values are not displayed or stored.

SQLite behavior is verified by managed tests. SQLite dependency loading inside the Unity Editor remains a manual validation step until the Unity Editor is opened and the runtime check is run successfully.

After a safe import, open Recruitment Centre to query the persisted players. Recruitment Centre does not read CSV files directly; it reads only the masked SQLite rows created by this workflow.
