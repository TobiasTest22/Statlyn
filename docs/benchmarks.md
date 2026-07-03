# Benchmarks

Milestone 2.5 adds the first benchmark foundation for persisted SQLite data.

Benchmarks are calculated from imported safe player records only. Statlyn does not seed fake benchmark results, does not call external APIs, does not scrape providers, and does not claim generic/import metrics are official FM26 metrics.

## What Is Stored

Statlyn stores:

- benchmark definitions
- benchmark run summaries
- aggregate metric snapshots

Metric snapshots contain aggregate values only: sample size, median, average, minimum, and maximum. They do not store selected player raw values or benchmark percentiles.

## Percentiles

Percentiles are nullable. A percentile is shown only when:

- a benchmark definition exists
- a real comparison group exists
- the metric exists for enough players
- the selected player has the metric
- the benchmark status is `Available`

No benchmark means no percentile. Insufficient sample means no percentile. Missing metrics are not treated as zero.

## Comparison Groups

Comparison groups are explicit and can be scoped by source, position group, role output profile, tactical role, tactical role pair, or a custom definition. Statlyn does not create fake comparison groups.

## Metric Safety

The benchmark calculator uses safe `PlayerStat` and `PhysicalMetric` rows first. Attribute support can be benchmarked only when explicitly requested as support-only data. Hidden FM values, blocked raw values, raw provider entities, and unverified FM26 fields are not used.

Default benchmark definitions are generic/import definitions and are not FM26-verified.
