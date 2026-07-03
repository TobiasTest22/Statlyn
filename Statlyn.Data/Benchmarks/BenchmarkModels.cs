using System;
using System.Collections.Generic;

namespace Statlyn.Data.Benchmarks
{
    public enum BenchmarkScope
    {
        GlobalDataset = 0,
        Source = 1,
        PositionGroup = 2,
        RoleOutputProfile = 3,
        TacticalRole = 4,
        TacticalRolePair = 5,
        Custom = 6
    }

    public enum BenchmarkMetricType
    {
        PlayerStat = 0,
        PhysicalMetric = 1,
        AttributeSupport = 2
    }

    public enum BenchmarkStatus
    {
        Available = 0,
        InsufficientSample = 1,
        MissingMetric = 2,
        NoBenchmark = 3,
        NotApplicable = 4
    }

    public sealed class BenchmarkDefinition
    {
        public BenchmarkDefinition(
            long id,
            string benchmarkName,
            BenchmarkScope scope,
            string sourceName,
            string positionGroup,
            string roleProfileName,
            string tacticalRoleName,
            string tacticalRolePairName,
            IReadOnlyList<string> metricKeys,
            int minimumSampleSize,
            int minimumMinutes,
            bool includeFixtureData,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc,
            bool isArchived)
        {
            Id = id;
            BenchmarkName = benchmarkName ?? string.Empty;
            Scope = scope;
            SourceName = sourceName ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            RoleProfileName = roleProfileName ?? string.Empty;
            TacticalRoleName = tacticalRoleName ?? string.Empty;
            TacticalRolePairName = tacticalRolePairName ?? string.Empty;
            MetricKeys = metricKeys ?? new List<string>();
            MinimumSampleSize = minimumSampleSize < 1 ? 1 : minimumSampleSize;
            MinimumMinutes = minimumMinutes < 0 ? 0 : minimumMinutes;
            IncludeFixtureData = includeFixtureData;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            IsArchived = isArchived;
        }

        public long Id { get; }

        public string BenchmarkName { get; }

        public BenchmarkScope Scope { get; }

        public string SourceName { get; }

        public string PositionGroup { get; }

        public string RoleProfileName { get; }

        public string TacticalRoleName { get; }

        public string TacticalRolePairName { get; }

        public IReadOnlyList<string> MetricKeys { get; }

        public int MinimumSampleSize { get; }

        public int MinimumMinutes { get; }

        public bool IncludeFixtureData { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }

        public bool IsArchived { get; }
    }

    public sealed class BenchmarkRunRecord
    {
        public BenchmarkRunRecord(long id, long benchmarkDefinitionId, DateTimeOffset ranAtUtc, int playerCount, int metricCount, string safeMessage)
        {
            Id = id;
            BenchmarkDefinitionId = benchmarkDefinitionId;
            RanAtUtc = ranAtUtc;
            PlayerCount = playerCount < 0 ? 0 : playerCount;
            MetricCount = metricCount < 0 ? 0 : metricCount;
            SafeMessage = safeMessage ?? string.Empty;
        }

        public long Id { get; }

        public long BenchmarkDefinitionId { get; }

        public DateTimeOffset RanAtUtc { get; }

        public int PlayerCount { get; }

        public int MetricCount { get; }

        public string SafeMessage { get; }
    }

    public sealed class BenchmarkMetricSnapshot
    {
        public BenchmarkMetricSnapshot(
            long id,
            long benchmarkRunId,
            string metricKey,
            string fieldName,
            BenchmarkMetricType metricType,
            int sampleSize,
            double? medianValue,
            double? averageValue,
            double? minimumValue,
            double? maximumValue,
            string sourceName,
            string comparisonGroup,
            bool isGenericImportMetric,
            bool isVerifiedFm26Metric)
        {
            Id = id;
            BenchmarkRunId = benchmarkRunId;
            MetricKey = metricKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            MetricType = metricType;
            SampleSize = sampleSize < 0 ? 0 : sampleSize;
            MedianValue = medianValue;
            AverageValue = averageValue;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
            SourceName = sourceName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            IsGenericImportMetric = isGenericImportMetric;
            IsVerifiedFm26Metric = isVerifiedFm26Metric;
        }

        public long Id { get; }

        public long BenchmarkRunId { get; }

        public string MetricKey { get; }

        public string FieldName { get; }

        public BenchmarkMetricType MetricType { get; }

        public int SampleSize { get; }

        public double? MedianValue { get; }

        public double? AverageValue { get; }

        public double? MinimumValue { get; }

        public double? MaximumValue { get; }

        public string SourceName { get; }

        public string ComparisonGroup { get; }

        public bool IsGenericImportMetric { get; }

        public bool IsVerifiedFm26Metric { get; }
    }

    public sealed class BenchmarkMetricResult
    {
        public BenchmarkMetricResult(
            string metricKey,
            string fieldName,
            BenchmarkMetricType metricType,
            double? playerValue,
            double? benchmarkMedian,
            double? benchmarkAverage,
            double? benchmarkMin,
            double? benchmarkMax,
            double? percentile,
            int sampleSize,
            BenchmarkStatus status,
            string safeMessage,
            string sourceName,
            string comparisonGroup,
            bool isGenericImportMetric = true,
            bool isVerifiedFm26Metric = false)
        {
            MetricKey = metricKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            MetricType = metricType;
            PlayerValue = playerValue;
            BenchmarkMedian = benchmarkMedian;
            BenchmarkAverage = benchmarkAverage;
            BenchmarkMin = benchmarkMin;
            BenchmarkMax = benchmarkMax;
            Percentile = status == BenchmarkStatus.Available ? percentile : null;
            SampleSize = sampleSize < 0 ? 0 : sampleSize;
            Status = status;
            SafeMessage = safeMessage ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            IsGenericImportMetric = isGenericImportMetric;
            IsVerifiedFm26Metric = isVerifiedFm26Metric;
        }

        public string MetricKey { get; }

        public string FieldName { get; }

        public BenchmarkMetricType MetricType { get; }

        public double? PlayerValue { get; }

        public double? BenchmarkMedian { get; }

        public double? BenchmarkAverage { get; }

        public double? BenchmarkMin { get; }

        public double? BenchmarkMax { get; }

        public double? Percentile { get; }

        public int SampleSize { get; }

        public BenchmarkStatus Status { get; }

        public string SafeMessage { get; }

        public string SourceName { get; }

        public string ComparisonGroup { get; }

        public bool IsGenericImportMetric { get; }

        public bool IsVerifiedFm26Metric { get; }
    }

    public sealed class BenchmarkPlayerSummary
    {
        public BenchmarkPlayerSummary(
            string statlynPlayerId,
            string benchmarkName,
            string comparisonGroup,
            IReadOnlyList<BenchmarkMetricResult> results,
            BenchmarkStatus overallStatus,
            string safeMessage,
            IReadOnlyList<string> warnings)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            BenchmarkName = benchmarkName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            Results = results ?? new List<BenchmarkMetricResult>();
            OverallStatus = overallStatus;
            SafeMessage = safeMessage ?? string.Empty;
            Warnings = warnings ?? new List<string>();
        }

        public string StatlynPlayerId { get; }

        public string BenchmarkName { get; }

        public string ComparisonGroup { get; }

        public IReadOnlyList<BenchmarkMetricResult> Results { get; }

        public BenchmarkStatus OverallStatus { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
