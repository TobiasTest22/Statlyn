# Player Profile v1

Milestone 2.0 adds the first persisted-safe Player Profile report.

## Flow

1. Open Data Sources.
2. Select or quick-fill a local synthetic fixture CSV.
3. Preview the CSV.
4. Run Safe Import.
5. Open Player Profile.
6. Load the first imported player or enter a `StatlynPlayerId`.

Recruitment Centre also uses the same Player Profile report pipeline when `Open Profile` is clicked.

## Safety Boundary

Player Profile v1 reads only persisted safe SQLite data:

- masked player identity and visible fields
- latest persisted role score
- persisted role-output expectation profile, or generic fallback
- player stats and physical metrics
- source metadata
- blocked-field audit notices
- data completeness

It does not reconstruct raw provider snapshots, expose `PlayerRawSnapshot`, expose hidden FM26 values, show raw blocked values, use player images, display club badges, or claim live FM26 support.

CSV/fixture/import sources are labelled as no live FM26 data. FM26 remains unsupported until validated memory maps exist.

## Report Shape

The report is output-first:

- identity and source context
- verdict summary
- role/output fit
- core role output metrics
- supporting output metrics
- physical output
- missing output metrics
- data quality and sample-size status
- attribute support
- evidence cards
- scout/recruitment actions
- blocked-data safe notice
- simple visual-section foundation

Attributes are support-only and should not lead the report. Missing output metrics lower confidence and are displayed as missing, not zero.

## Generic Metric Status

Current metric and role-output profiles are generic/import-ready. They are not official FM26 metrics or official FM26 role templates. The profile displays generic/import metric status until a later milestone validates FM26-visible data, exports, or memory maps.

## Benchmarks

Player Profile v1 does not create fake benchmark percentiles. If no real comparison group exists, the visual section says `No benchmark yet.`

## Unity Status

The Unity page is manually validated through `docs/unity-validation.md`. Unity Editor validation and SQLite-in-Unity loading remain manual unless a release note explicitly says the Editor was opened and checked.
