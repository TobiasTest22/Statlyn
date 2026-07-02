# Data Providers

Statlyn providers implement a common contract so FM26, CSV, JSON, manual datasets and future licensed APIs can enter the same pipeline.

```text
provider -> source metadata -> raw snapshot -> field policy registry -> masked data -> storage/scoring/UI
```

## Provider Contract

`IDataProvider` now exposes:

- `ValidateAccess()`
- `ReadSourceMetadata()`
- `ReadPlayers()`
- `ReadTeams()`
- `ReadMatches()`
- `ReadPlayerStats()`
- `ReadScoutReports()`
- `ReadPlayerImages()`
- `ReadNationalityFlags()`
- `GetDataCompleteness()`
- `GetDiagnostics()`

## Current Providers

- `Fm26LiveMemoryProvider`: live-process facade, unsupported until validated memory maps exist.
- `CsvImportProvider`: local file import skeleton, no network calls.
- `JsonImportProvider`: local file skeleton, no network calls.
- `ExternalFootballDataProviderTemplate`: future provider template only.

There is no FotMob scraper. Any FotMob-style provider would require licensed, exported, user-provided or otherwise permitted data.

## Source Metadata

Every source records licence status, allowed usage, image permission, flag permission, confidence and completeness. Field policies use this metadata before anything reaches scoring or UI.

## CSV Mapping

CSV imports use `FootballFieldCatalog` plus optional explicit mappings. The catalog maps common columns such as `Finishing`, `Pace`, `xG`, `xA`, `TopSpeed` and `SprintDistance` into safe field instances.

Diagnostics report file readability, licence state, row count, players imported, mapped field count, unknown/forbidden field counts, image permission, flag permission and completeness. Diagnostics may include field names and counts, but not raw hidden values.
