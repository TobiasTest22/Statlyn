# Visual Analytics Components

Milestone 2.1 adds a safe visual analytics layer for Player Profile and Recruitment Centre.

## Safety Boundary

Visual analytics are built from safe view models only:

- `PlayerProfileReportViewModel`
- `RecruitmentCentrePlayerRowViewModel`

`StatlynVisualAnalyticsBuilder` and `RecruitmentCentreMiniVisualBuilder` reject raw provider entities. They do not expose raw snapshots, hidden FM26 values, raw blocked values, fake live FM26 data, images, badges, flags, scraped data or external API data.

## Player Profile Visuals

The Player Profile builder creates:

- `StatlynScoreCardVisual`
- `StatlynMetricTileVisual`
- `StatlynHorizontalBarVisual`
- `StatlynMetricGroupVisual`
- `StatlynDataQualityVisual`
- `StatlynWarningVisual`
- `StatlynEvidenceVisual`
- `StatlynRoleOutputVisual`
- `StatlynBenchmarkStatusVisual`
- `StatlynMissingDataVisual`

The Unity page renders those through UI Toolkit component builders for score cards, horizontal bars, metric tiles, data quality, evidence cards, warning panels, missing data, blocked data and benchmark status.

## Benchmark Status

Benchmarking is foundation-only in Milestone 2.1:

- `HasBenchmark=false`
- `SafeMessage=No benchmark yet.`
- `Percentile=null`
- no comparison group

The UI may show benchmark status as unavailable. It must not show fake percentiles or fixture comparison groups.

## Recruitment Centre Mini Visuals

Recruitment Centre cards use safe mini visuals for:

- role-fit score
- confidence bar
- data-completeness bar
- risk indicator
- output mini list
- missing-data badge
- blocked-field badge
- no-live-FM26 label

Missing output remains missing and is never converted to zero. Attribute support remains secondary to output, evidence, data quality and scout actions.
