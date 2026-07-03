# Data Providers

Statlyn providers implement a common contract so FM26, CSV, JSON, manual datasets and future licensed APIs can enter the same pipeline.

```text
provider -> source metadata -> raw snapshot -> field policy registry -> masked data -> storage/scoring/UI
```

## Provider Contract

`IDataProvider` exposes:

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

Every source records licence status, allowed usage, confidence and explicit permissions:

- `PermitsPlayerImages`
- `PermitsProviderFlags`
- `UsesBundledSafeFlagAssets`
- `PermitsClubBadges`
- `AllowsExport`

Field policies use this metadata before anything reaches scoring or UI. Player images and club badges default to blocked. Provider flags require source permission, while bundled safe flag assets can be used only when that mode is explicitly enabled.

## CSV Mapping

CSV imports use `FootballFieldCatalog` plus optional explicit mappings. The catalog maps common columns such as `Finishing`, `Pace`, `xG`, `xA`, `TopSpeed` and `SprintDistance` into safe field instances.

`CsvPreviewService` runs before import for the Data Sources UI. It reads only the local CSV header and row count, maps columns through the same catalog/mapping set, and reports safe, unknown, forbidden and permission-blocked columns. Preview does not store data and does not expose cell values from hidden or blocked fields.

Diagnostics report file readability, licence state, row count, players imported, mapped field count, unknown/forbidden field counts, image permission, flag permission and completeness. Diagnostics may include field names and counts, but not raw hidden values.

CSV and JSON remain local import skeletons. They do not create live FM26 data and do not assume image URLs, badge URLs or provider flags are licensed.

## Import Pipeline

The current safe import pipeline is:

```text
IDataProvider.ValidateAccess
-> ReadSourceMetadata
-> ReadPlayers
-> ScoutingKnowledgeFirewall.Mask
-> RoleScoringEngine.ScorePlayer
-> SQLite repositories
-> ImportAudit
```

Only masked, permitted and source-tagged data is stored. Blocked fields create audit rows with source name, entity id, field key, field name and reason only. Raw blocked values are not stored.

Player stats such as xG and xA are persisted as `PlayerStat` output records. Physical outputs such as TopSpeed and SprintDistance are persisted as `PhysicalMetric` records. This keeps performance output separate from attributes for future role-specific scoring.
