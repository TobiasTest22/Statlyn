using System.Collections.Generic;

namespace Statlyn.UI
{
    public sealed class StatlynVisualAnalyticsViewModel
    {
        public StatlynVisualAnalyticsViewModel(
            IReadOnlyList<string> sectionOrder,
            IReadOnlyList<StatlynScoreCardVisual> scoreCards,
            StatlynRoleOutputVisual roleOutput,
            IReadOnlyList<StatlynMetricGroupVisual> metricGroups,
            IReadOnlyList<StatlynDataQualityVisual> dataQuality,
            IReadOnlyList<StatlynWarningVisual> warnings,
            IReadOnlyList<StatlynEvidenceVisual> evidence,
            IReadOnlyList<StatlynEvidenceVisual> scoutActions,
            IReadOnlyList<StatlynMetricTileVisual> attributeSupport,
            IReadOnlyList<StatlynMissingDataVisual> missingData,
            StatlynWarningVisual blockedData,
            StatlynBenchmarkStatusVisual benchmarkStatus)
        {
            SectionOrder = sectionOrder ?? new List<string>();
            ScoreCards = scoreCards ?? new List<StatlynScoreCardVisual>();
            RoleOutput = roleOutput;
            MetricGroups = metricGroups ?? new List<StatlynMetricGroupVisual>();
            DataQuality = dataQuality ?? new List<StatlynDataQualityVisual>();
            Warnings = warnings ?? new List<StatlynWarningVisual>();
            Evidence = evidence ?? new List<StatlynEvidenceVisual>();
            ScoutActions = scoutActions ?? new List<StatlynEvidenceVisual>();
            AttributeSupport = attributeSupport ?? new List<StatlynMetricTileVisual>();
            MissingData = missingData ?? new List<StatlynMissingDataVisual>();
            BlockedData = blockedData;
            BenchmarkStatus = benchmarkStatus;
        }

        public IReadOnlyList<string> SectionOrder { get; }

        public IReadOnlyList<StatlynScoreCardVisual> ScoreCards { get; }

        public StatlynRoleOutputVisual RoleOutput { get; }

        public IReadOnlyList<StatlynMetricGroupVisual> MetricGroups { get; }

        public IReadOnlyList<StatlynDataQualityVisual> DataQuality { get; }

        public IReadOnlyList<StatlynWarningVisual> Warnings { get; }

        public IReadOnlyList<StatlynEvidenceVisual> Evidence { get; }

        public IReadOnlyList<StatlynEvidenceVisual> ScoutActions { get; }

        public IReadOnlyList<StatlynMetricTileVisual> AttributeSupport { get; }

        public IReadOnlyList<StatlynMissingDataVisual> MissingData { get; }

        public StatlynWarningVisual BlockedData { get; }

        public StatlynBenchmarkStatusVisual BenchmarkStatus { get; }
    }

    public sealed class StatlynScoreCardVisual
    {
        public StatlynScoreCardVisual(string title, string value, string caption, double? score, string status)
        {
            Title = title ?? string.Empty;
            Value = value ?? string.Empty;
            Caption = caption ?? string.Empty;
            Score = score;
            Status = status ?? string.Empty;
        }

        public string Title { get; }

        public string Value { get; }

        public string Caption { get; }

        public double? Score { get; }

        public string Status { get; }
    }

    public sealed class StatlynMetricTileVisual
    {
        public StatlynMetricTileVisual(
            string label,
            string value,
            string section,
            string source,
            string confidence,
            string sample,
            string verificationLabel,
            bool isGenericImportMetric,
            bool isMissing)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Section = section ?? string.Empty;
            Source = source ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Sample = sample ?? string.Empty;
            VerificationLabel = verificationLabel ?? string.Empty;
            IsGenericImportMetric = isGenericImportMetric;
            IsMissing = isMissing;
        }

        public string Label { get; }

        public string Value { get; }

        public string Section { get; }

        public string Source { get; }

        public string Confidence { get; }

        public string Sample { get; }

        public string VerificationLabel { get; }

        public bool IsGenericImportMetric { get; }

        public bool IsMissing { get; }
    }

    public sealed class StatlynHorizontalBarVisual
    {
        public StatlynHorizontalBarVisual(string label, string value, double? percent, string caption, bool isAvailable)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Percent = percent;
            Caption = caption ?? string.Empty;
            IsAvailable = isAvailable;
        }

        public string Label { get; }

        public string Value { get; }

        public double? Percent { get; }

        public string Caption { get; }

        public bool IsAvailable { get; }
    }

    public sealed class StatlynMetricGroupVisual
    {
        public StatlynMetricGroupVisual(string title, string summary, IReadOnlyList<StatlynMetricTileVisual> metrics, IReadOnlyList<StatlynMissingDataVisual> missingMetrics)
        {
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            Metrics = metrics ?? new List<StatlynMetricTileVisual>();
            MissingMetrics = missingMetrics ?? new List<StatlynMissingDataVisual>();
        }

        public string Title { get; }

        public string Summary { get; }

        public IReadOnlyList<StatlynMetricTileVisual> Metrics { get; }

        public IReadOnlyList<StatlynMissingDataVisual> MissingMetrics { get; }
    }

    public sealed class StatlynDataQualityVisual
    {
        public StatlynDataQualityVisual(string label, string value, string caption, bool isWarning)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Caption = caption ?? string.Empty;
            IsWarning = isWarning;
        }

        public string Label { get; }

        public string Value { get; }

        public string Caption { get; }

        public bool IsWarning { get; }
    }

    public sealed class StatlynWarningVisual
    {
        public StatlynWarningVisual(string title, string message, string severity, IReadOnlyList<string> rows)
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Severity = severity ?? string.Empty;
            Rows = rows ?? new List<string>();
        }

        public string Title { get; }

        public string Message { get; }

        public string Severity { get; }

        public IReadOnlyList<string> Rows { get; }
    }

    public sealed class StatlynEvidenceVisual
    {
        public StatlynEvidenceVisual(string category, string title, string body, string source, string confidence)
        {
            Category = category ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Source = source ?? string.Empty;
            Confidence = confidence ?? string.Empty;
        }

        public string Category { get; }

        public string Title { get; }

        public string Body { get; }

        public string Source { get; }

        public string Confidence { get; }
    }

    public sealed class StatlynRoleOutputVisual
    {
        public StatlynRoleOutputVisual(string roleName, string outputFitLabel, string tacticalFitDisplay, IReadOnlyList<StatlynHorizontalBarVisual> bars)
        {
            RoleName = roleName ?? string.Empty;
            OutputFitLabel = outputFitLabel ?? string.Empty;
            TacticalFitDisplay = tacticalFitDisplay ?? string.Empty;
            Bars = bars ?? new List<StatlynHorizontalBarVisual>();
        }

        public string RoleName { get; }

        public string OutputFitLabel { get; }

        public string TacticalFitDisplay { get; }

        public IReadOnlyList<StatlynHorizontalBarVisual> Bars { get; }
    }

    public sealed class StatlynBenchmarkStatusVisual
    {
        public StatlynBenchmarkStatusVisual(
            bool hasBenchmark,
            string safeMessage,
            int? percentile,
            string benchmarkName,
            string comparisonGroup,
            string metricKey,
            int? sampleSize)
        {
            HasBenchmark = hasBenchmark;
            SafeMessage = safeMessage ?? string.Empty;
            Percentile = percentile;
            BenchmarkName = benchmarkName ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            MetricKey = metricKey ?? string.Empty;
            SampleSize = sampleSize;
        }

        public bool HasBenchmark { get; }

        public string SafeMessage { get; }

        public int? Percentile { get; }

        public string BenchmarkName { get; }

        public string ComparisonGroup { get; }

        public string MetricKey { get; }

        public int? SampleSize { get; }
    }

    public sealed class StatlynMissingDataVisual
    {
        public StatlynMissingDataVisual(string label, string safeMessage, bool isMissing, string caption)
        {
            Label = label ?? string.Empty;
            SafeMessage = safeMessage ?? string.Empty;
            IsMissing = isMissing;
            Caption = caption ?? string.Empty;
        }

        public string Label { get; }

        public string SafeMessage { get; }

        public bool IsMissing { get; }

        public string Caption { get; }
    }

    public sealed class RecruitmentCentreMiniVisuals
    {
        public RecruitmentCentreMiniVisuals(
            StatlynScoreCardVisual roleFitScore,
            StatlynHorizontalBarVisual confidenceBar,
            StatlynHorizontalBarVisual dataCompletenessBar,
            StatlynWarningVisual riskIndicator,
            IReadOnlyList<StatlynMetricTileVisual> outputMiniList,
            IReadOnlyList<string> badges,
            string noLiveDataLabel)
        {
            RoleFitScore = roleFitScore;
            ConfidenceBar = confidenceBar;
            DataCompletenessBar = dataCompletenessBar;
            RiskIndicator = riskIndicator;
            OutputMiniList = outputMiniList ?? new List<StatlynMetricTileVisual>();
            Badges = badges ?? new List<string>();
            NoLiveDataLabel = noLiveDataLabel ?? string.Empty;
        }

        public StatlynScoreCardVisual RoleFitScore { get; }

        public StatlynHorizontalBarVisual ConfidenceBar { get; }

        public StatlynHorizontalBarVisual DataCompletenessBar { get; }

        public StatlynWarningVisual RiskIndicator { get; }

        public IReadOnlyList<StatlynMetricTileVisual> OutputMiniList { get; }

        public IReadOnlyList<string> Badges { get; }

        public string NoLiveDataLabel { get; }
    }
}
