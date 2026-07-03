# Player Profile v1

Milestone 2.0 adds the first persisted-safe Player Profile report. Milestone 2.1 adds reusable safe visual analytics models and Unity UI Toolkit component builders for the report. Milestone 2.2 adds a safe add-to-shortlist action and membership panel. Milestone 2.3 adds latest scout report summary and a create-assignment action. Milestone 2.4 adds an optional Role Lab role/pair selector foundation.

## Flow

1. Open Data Sources.
2. Select or quick-fill a local synthetic fixture CSV.
3. Preview the CSV.
4. Run Safe Import.
5. Open Player Profile.
6. Load the first imported player or enter a `StatlynPlayerId`.
7. Add the player to the Main Recruitment List when the profile is worth tracking.
8. Create a Scout Desk assignment or review the latest qualitative scout report.
9. Optionally enter a Role Lab role or pair name to test a phase-aware output profile.

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

The shortlist action uses `StatlynPlayerId` plus the safe persisted profile context. It does not add fake players and does not store raw provider data.

The Scout Desk action also uses only `StatlynPlayerId` and safe role labels. It does not pass raw provider objects, ask for CA/PA, or treat scout notes as exact hidden personality attributes.

The Role Lab selector resolves a persisted `TacticalRole` or `TacticalRolePair` through `RoleLabOutputProfileBridge`. If no Role Lab match exists, Player Profile keeps using persisted role-output profiles and generic/import fallbacks.

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
- benchmark status
- shortlist membership/add action
- latest scout report summary and create-assignment action
- reusable visual analytics components

Attributes are support-only and should not lead the report. Missing output metrics lower confidence and are displayed as missing, not zero.

Milestone 2.1 splits the report construction into focused builders for metric tiles, evidence, data quality, attribute support, scout actions, blocked-data notices and visual sections. The public report model still accepts only `PlayerProfileResult`; raw provider entities are rejected before any UI binding can occur.

## Visual Analytics Components

`StatlynVisualAnalyticsBuilder` converts `PlayerProfileReportViewModel` into safe visuals only. It builds:

- verdict score cards
- role/output bars
- metric groups for core, supporting and physical output
- data-quality tiles
- warning, evidence and scout-action cards
- missing-data and blocked-data panels
- support-only attribute tiles
- benchmark status

The Unity Player Profile page consumes those visuals through UI Toolkit component builders. It keeps the visible order as identity/source, verdict score cards, role/output, core output, supporting output, physical output, data quality, missing data, warnings, evidence, scout actions, attribute support, blocked data and benchmark status.

## Shortlist Action

The profile page can add the loaded player to `Main Recruitment List`. Default shortlist labels are suggested by `ShortlistDecisionHelper` from visible context:

- low confidence or missing output prefers `ScoutFurther`
- strong visible role fit and confidence can suggest `Shortlist` or `StrongTarget`
- no path automatically stores a `Sign` decision
- blocked-field counts add warnings but do not expose raw values

## Scout Report Summary

Player Profile shows the latest scout recommendation, confidence, report date and short safe summary. If no report exists, it says `No scout report yet.`

Scout reports are local-only qualitative observations. They can say things like `looks composed` or `handles pressure well`, but hidden-looking numeric assignments such as `CA 155`, `PA=180` or `Professionalism: 20` are redacted before storage.

## Generic Metric Status

Current metric and role-output profiles are generic/import-ready. They are not official FM26 metrics or official FM26 role templates. The profile displays generic/import metric status until a later milestone validates FM26-visible data, exports, or memory maps.

## Benchmarks

Player Profile v1 does not create fake benchmark percentiles. The 2.1 benchmark foundation sets `HasBenchmark=false`, `Percentile=null` and `SafeMessage=No benchmark yet.` until a real comparison group exists.

## Unity Status

The Unity page is manually validated through `docs/unity-validation.md`. Unity Editor validation and SQLite-in-Unity loading remain manual unless a release note explicitly says the Editor was opened and checked.
