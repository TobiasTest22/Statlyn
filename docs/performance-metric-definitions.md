# Performance Metric Definitions

Milestone 1.7 adds generic performance metric definitions for future output-first recruitment analysis.

Seeded examples include attacking, creative, defensive, physical and goalkeeper metrics such as xG, xA, key passes, progressive passes, tackles, aerial duel success, TopSpeed, SprintDistance, saves, save percentage and keeper distribution accuracy.

## FM26 Status

Seeded metrics are generic/import-ready definitions. They are not official FM26 stat declarations.

`IsVerifiedFm26Metric` defaults to `false`. A metric can only be marked FM26-supported after validation from:

- visible FM26 UI
- supported FM26 export
- validated memory map
- another explicitly permitted supported FM26 source

Provider aliases can be stored without claiming FM26 support.

## Future Use

Definitions support:

- per-90 metrics
- raw totals
- percentage metrics
- goalkeeper-specific metrics
- defensive metrics
- creative metrics
- attacking metrics
- physical metrics

Attributes can support interpretation, but performance output should increasingly drive recruitment evidence.

Recruitment Centre v1 uses these generic/import-ready definitions for row summaries. xG, xA, progressive actions, defensive output, goalkeeper output and physical metrics can appear as safe labels/values when imported. They remain generic football metrics unless later FM26 validation marks them supported.

## Sample Size

Metrics that require minutes should not be treated as fully reliable unless safe sample minutes are present. The current persistence layer stores sample minutes on `PlayerStat` rows when a safe `Minutes` field exists. If it does not, the row is marked as sample-missing rather than inventing minutes.
