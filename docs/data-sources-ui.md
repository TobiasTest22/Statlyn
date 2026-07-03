# Data Sources UI

Milestone 1.8 adds the first user-facing Data Sources workflow in the Unity shell.

## Scope

- Local CSV files only.
- Manual path entry, plus a synthetic fixture quick-fill button.
- Source name, licence status, allowed usage and confidence inputs.
- Permission toggles for player images, provider flags, bundled safe flag placeholders, club badges and export.
- Read-only CSV preview before import.
- Safe import through the existing provider/firewall/persistence pipeline.
- Database diagnostics after import.

No network sources, scraping, FotMob integration, external APIs, fake live FM26 data, unlicensed player images, badges or provider flags are included.

## Runtime Path

Unity uses `Application.persistentDataPath/statlyn.db` for the first local SQLite path. Tests use in-memory SQLite. A fuller settings screen can add database path override later.

## Safety

Preview shows column names, mapping status and counts only. It does not store data. Import stores only masked, permitted fields after the scouting firewall. Forbidden fields appear by safe field name/category and reason only; raw blocked values and hidden numeric values are not displayed or stored.

SQLite behavior is verified by managed tests. SQLite dependency loading inside the Unity Editor remains a manual validation step.
