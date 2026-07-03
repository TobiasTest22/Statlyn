using System.Collections.Generic;

namespace Statlyn.Data.Benchmarks
{
    public sealed class BenchmarksPageViewModel
    {
        public BenchmarksPageViewModel(string safeMessage, IReadOnlyList<BenchmarkDefinitionCardViewModel> definitions)
        {
            SafeMessage = safeMessage ?? string.Empty;
            Definitions = definitions ?? new List<BenchmarkDefinitionCardViewModel>();
        }

        public string SafeMessage { get; }

        public IReadOnlyList<BenchmarkDefinitionCardViewModel> Definitions { get; }
    }

    public sealed class BenchmarkDefinitionCardViewModel
    {
        public BenchmarkDefinitionCardViewModel(
            long definitionId,
            string benchmarkName,
            string scope,
            string sourceName,
            string positionGroup,
            IReadOnlyList<string> metricKeys,
            int minimumSampleSize,
            int minimumMinutes,
            string verificationLabel,
            BenchmarkRunSummaryViewModel? latestRun,
            IReadOnlyList<BenchmarkMetricSnapshotViewModel> snapshots)
        {
            DefinitionId = definitionId;
            BenchmarkName = benchmarkName ?? string.Empty;
            Scope = scope ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            MetricKeys = metricKeys ?? new List<string>();
            MinimumSampleSize = minimumSampleSize;
            MinimumMinutes = minimumMinutes;
            VerificationLabel = verificationLabel ?? string.Empty;
            LatestRun = latestRun;
            Snapshots = snapshots ?? new List<BenchmarkMetricSnapshotViewModel>();
        }

        public long DefinitionId { get; }

        public string BenchmarkName { get; }

        public string Scope { get; }

        public string SourceName { get; }

        public string PositionGroup { get; }

        public IReadOnlyList<string> MetricKeys { get; }

        public int MinimumSampleSize { get; }

        public int MinimumMinutes { get; }

        public string VerificationLabel { get; }

        public BenchmarkRunSummaryViewModel? LatestRun { get; }

        public IReadOnlyList<BenchmarkMetricSnapshotViewModel> Snapshots { get; }
    }

    public sealed class BenchmarkRunSummaryViewModel
    {
        public BenchmarkRunSummaryViewModel(long runId, long definitionId, string ranAtUtc, int playerCount, int metricCount, string safeMessage)
        {
            RunId = runId;
            DefinitionId = definitionId;
            RanAtUtc = ranAtUtc ?? string.Empty;
            PlayerCount = playerCount;
            MetricCount = metricCount;
            SafeMessage = safeMessage ?? string.Empty;
        }

        public long RunId { get; }

        public long DefinitionId { get; }

        public string RanAtUtc { get; }

        public int PlayerCount { get; }

        public int MetricCount { get; }

        public string SafeMessage { get; }
    }

    public sealed class BenchmarkMetricSnapshotViewModel
    {
        public BenchmarkMetricSnapshotViewModel(
            string metricKey,
            string fieldName,
            string metricType,
            int sampleSize,
            string median,
            string average,
            string min,
            string max,
            string sourceName,
            string comparisonGroup,
            string verificationLabel)
        {
            MetricKey = metricKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            MetricType = metricType ?? string.Empty;
            SampleSize = sampleSize;
            Median = median ?? string.Empty;
            Average = average ?? string.Empty;
            Min = min ?? string.Empty;
            Max = max ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            VerificationLabel = verificationLabel ?? string.Empty;
        }

        public string MetricKey { get; }

        public string FieldName { get; }

        public string MetricType { get; }

        public int SampleSize { get; }

        public string Median { get; }

        public string Average { get; }

        public string Min { get; }

        public string Max { get; }

        public string SourceName { get; }

        public string ComparisonGroup { get; }

        public string VerificationLabel { get; }
    }

    public sealed class PlayerBenchmarkSummaryViewModel
    {
        public PlayerBenchmarkSummaryViewModel(
            string statlynPlayerId,
            string benchmarkName,
            string comparisonGroup,
            string status,
            string safeMessage,
            IReadOnlyList<PlayerBenchmarkMetricViewModel> metrics,
            IReadOnlyList<string> warnings)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            BenchmarkName = benchmarkName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            Status = status ?? string.Empty;
            SafeMessage = safeMessage ?? string.Empty;
            Metrics = metrics ?? new List<PlayerBenchmarkMetricViewModel>();
            Warnings = warnings ?? new List<string>();
        }

        public string StatlynPlayerId { get; }

        public string BenchmarkName { get; }

        public string ComparisonGroup { get; }

        public string Status { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<PlayerBenchmarkMetricViewModel> Metrics { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PlayerBenchmarkMetricViewModel
    {
        public PlayerBenchmarkMetricViewModel(
            string metricKey,
            string fieldName,
            string metricType,
            string playerValue,
            string median,
            string average,
            string percentile,
            int sampleSize,
            string status,
            string sourceName,
            string comparisonGroup,
            string verificationLabel)
        {
            MetricKey = metricKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            MetricType = metricType ?? string.Empty;
            PlayerValue = playerValue ?? string.Empty;
            Median = median ?? string.Empty;
            Average = average ?? string.Empty;
            Percentile = percentile ?? string.Empty;
            SampleSize = sampleSize;
            Status = status ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            VerificationLabel = verificationLabel ?? string.Empty;
        }

        public string MetricKey { get; }

        public string FieldName { get; }

        public string MetricType { get; }

        public string PlayerValue { get; }

        public string Median { get; }

        public string Average { get; }

        public string Percentile { get; }

        public int SampleSize { get; }

        public string Status { get; }

        public string SourceName { get; }

        public string ComparisonGroup { get; }

        public string VerificationLabel { get; }
    }

    public sealed class BenchmarkRunBatchResult
    {
        public BenchmarkRunBatchResult(int definitionsRun, string safeMessage, IReadOnlyList<BenchmarkRunSummaryViewModel> runs)
        {
            DefinitionsRun = definitionsRun < 0 ? 0 : definitionsRun;
            SafeMessage = safeMessage ?? string.Empty;
            Runs = runs ?? new List<BenchmarkRunSummaryViewModel>();
        }

        public int DefinitionsRun { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<BenchmarkRunSummaryViewModel> Runs { get; }
    }
}
