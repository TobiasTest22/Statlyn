# Recruitment Centre

Milestone 1.9 adds the first Recruitment Centre page powered by persisted safe SQLite data. Milestone 1.9.1 hardens role names, output-profile selection, branding and preview labels. Milestone 2.1 adds safe mini visuals for scanning imported players. Milestone 2.2 adds add-to-shortlist workflow actions. Milestone 2.3 extends the downstream loop through Shortlists and Scout Desk.

## Flow

1. Open Data Sources.
2. Select or quick-fill a local synthetic fixture CSV.
3. Preview the CSV.
4. Run Safe Import.
5. Open Recruitment Centre.
6. Refresh/search/filter persisted players.
7. Open the full safe Player Profile report.
8. Add persisted safe players to the Main Recruitment List.
9. Create scout assignments from Shortlists or Player Profile when human validation is needed.

Recruitment Centre does not parse CSV files directly. It reads only the masked SQLite rows created by the safe import pipeline. Its `Open Profile` action now reuses the same `PlayerProfileQueryService` and `PlayerProfileReportViewModel` pipeline as the Player Profile page.

## Data Safety

Recruitment Centre uses:

- `Player`
- latest `RoleScore`
- `PlayerStat`
- `PhysicalMetric`
- source metadata
- blocked-field audit counts

It does not use `PlayerRawSnapshot`, raw provider entities, hidden FM26 values or raw blocked values. Blocked fields appear only as counts or safe warnings. No player images, badges or provider flags are displayed.

Shortlist actions pass `StatlynPlayerId` and safe row labels only. They do not pass raw player objects, hidden FM26 fields or raw blocked values.

Scout Desk continues the same safety boundary. Assignment and report creation use persisted safe IDs, not raw provider rows, and report summaries do not expose hidden values.

`RoleScore.RoleName` is persisted and reloaded. When a score exists but an old row has no role name, the UI shows `Unknown role`; when no score exists, it shows `Not scored`. Hidden-value-looking role labels are not surfaced.

## Output Summaries

Rows are output-first and position-specific:

- goalkeepers prefer goalkeeper output
- centre-backs prefer defensive/aerial/build-up output
- wide attackers prefer creative/carrying/threat output
- strikers prefer xG, shots and goals
- central midfielders prefer progression, chance creation and defensive contribution

Attributes can support interpretation, but they are not the Recruitment Centre's primary model. Missing core metrics are shown as missing warnings, not zero values.

Recruitment Centre loads persisted `RoleOutputExpectationProfile` rows from SQLite and selects the best match by position group, then role family where available. If no persisted profile matches, it falls back to the generic import-only seed profiles. Those profiles are not official FM26 role templates.

## Unity UX

The Unity page shows a persisted-safe-data banner, no-live-FM26 label, active database path/status, player count, source list, reset filters button, sort selector, role name and tactical-fit status. Player cards now bind through `RecruitmentCentreMiniVisualBuilder`, which accepts only `RecruitmentCentrePlayerRowViewModel` and rejects raw provider entities.

Mini visuals include:

- role-fit score
- confidence and data-completeness bars
- risk indicator
- output mini list
- missing-data and blocked-field badges
- no-live-FM26 label

`Open Profile` renders the Player Profile v1 report below the Recruitment Centre results using persisted safe data only.

Milestone 2.2 adds a `Add to Main Recruitment List` button and a `Shortlisted` badge when the player belongs to an active shortlist. Double-adds are idempotent and refresh safe workflow labels instead of duplicating rows.

Milestone 2.3 keeps Recruitment Centre focused on scanning and shortlist entry. Scout assignments are created from Shortlists or Player Profile so the workflow remains explicit.

## Current Limits

CSV local import is the only supported data path. FM26 live data remains unsupported until validated memory maps exist. Output metrics are generic/import-ready and are not official FM26 stats unless later validation marks them supported.

If SQLite fails inside Unity, run the Data Sources runtime check and review the copied dependency paths before using Recruitment Centre.
