# Local CSV Release Candidate

Milestone 2.8 hardens Statlyn as a local CSV-only release-candidate workflow. This does not mean FM26 support is ready. It means the current safe local loop is easier to demo, diagnose and validate before starting the FM26 connector path.

## Scope

The supported release-candidate loop is:

1. Copy Unity dependencies with `tools/copy-managed-to-unity.ps1`.
2. Open Diagnostics.
3. Run Runtime Check.
4. Run Full Smoke Test.
5. Run Product Readiness Check.
6. Open Data Sources.
7. Preview a local CSV.
8. Run Safe Import.
9. Review Recruitment Centre, Player Profile, Shortlists, Scout Desk, Role Lab, Benchmarks and Dashboard.

CSV local import remains the only user-facing data source workflow. There is no live FM26 data, no external API integration, no scraping and no fake benchmark percentile generation.

React/Tauri is now the strategic desktop workspace, but it does not bypass this local CSV safety model. The desktop app reads safe DTOs from `Statlyn.Api`; imported data still flows through C# providers, the scouting firewall, SQLite repositories and API DTO mapping before it reaches the UI.

## Readiness Check

`LocalProductReadinessService` checks the local database, current schema version, fixture availability, SQLite runtime, import workflow construction, page/query services, smoke-test service availability and FM26 unsupported status.

Missing fixture data is reported as a warning with a clear next action. Empty database state is valid and honest: it should say `Awaiting local data.` or skip player-specific checks rather than creating fake players.

## Import Hardening

Preview and import surfaces now include these safe messages:

- Unknown columns are not stored unless mapped safely.
- Forbidden/hidden-looking fields are blocked.
- Missing metrics are not treated as zero.
- Re-import replaces current safe snapshot, not duplicate rows.

Import audit display rows expose counts only. Raw CSV rows, hidden values and raw blocked values are not displayed.

## Still Manual

Unity Editor validation is still manual unless explicitly reported. SQLite-in-Unity must be verified through the Diagnostics Runtime Check before relying on runtime import inside the Editor.
