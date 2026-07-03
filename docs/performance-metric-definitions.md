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

Recruitment Centre v1 and Player Profile v1 use these generic/import-ready definitions for summaries and report metric tiles. xG, xA, progressive actions, defensive output, goalkeeper output and physical metrics can appear as safe labels/values when imported. They remain generic football metrics unless later FM26 validation marks them supported.

## Sample Size

Metrics that require minutes should not be treated as fully reliable unless safe sample minutes are present. The current persistence layer stores sample minutes on `PlayerStat` rows when a safe `Minutes` field exists. If it does not, the row is marked as sample-missing rather than inventing minutes.

Player Profile v1 surfaces minutes in the Data Quality section. If minutes are missing, the profile shows a sample-size warning and keeps the output interpretation provisional. Visual metric tiles carry source, confidence, sample and generic/import verification labels so users can see whether an output metric is available, missing or not FM26-verified.

## Benchmarks

No benchmark means no percentile claim. Player Profile v1 uses data-driven visual sections and says `No benchmark yet.` until a real comparison group exists. The 2.1 benchmark status model currently uses `HasBenchmark=false`, `Percentile=null` and no comparison group.
