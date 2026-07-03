# Recruitment Centre

Milestone 1.9 adds the first Recruitment Centre page powered by persisted safe SQLite data.

## Flow

1. Open Data Sources.
2. Select or quick-fill a local synthetic fixture CSV.
3. Preview the CSV.
4. Run Safe Import.
5. Open Recruitment Centre.
6. Refresh/search/filter persisted players.
7. Open a safe profile preview.

Recruitment Centre does not parse CSV files directly. It reads only the masked SQLite rows created by the safe import pipeline.

## Data Safety

Recruitment Centre uses:

- `Player`
- latest `RoleScore`
- `PlayerStat`
- `PhysicalMetric`
- source metadata
- blocked-field audit counts

It does not use `PlayerRawSnapshot`, raw provider entities, hidden FM26 values or raw blocked values. Blocked fields appear only as counts or safe warnings. No player images, badges or provider flags are displayed.

## Output Summaries

Rows are output-first and position-specific:

- goalkeepers prefer goalkeeper output
- centre-backs prefer defensive/aerial/build-up output
- wide attackers prefer creative/carrying/threat output
- strikers prefer xG, shots and goals
- central midfielders prefer progression, chance creation and defensive contribution

Attributes can support interpretation, but they are not the Recruitment Centre's primary model. Missing core metrics are shown as missing warnings, not zero values.

## Current Limits

CSV local import is the only supported data path. FM26 live data remains unsupported until validated memory maps exist. Output metrics are generic/import-ready and are not official FM26 stats unless later validation marks them supported.

If SQLite fails inside Unity, run the Data Sources runtime check and review the copied dependency paths before using Recruitment Centre.
