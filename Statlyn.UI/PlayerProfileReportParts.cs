using System;
using System.Collections.Generic;
using System.Globalization;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;

namespace Statlyn.UI
{
    public sealed class PlayerProfileIdentityViewModel
    {
        public PlayerProfileIdentityViewModel(string playerName, string age, string nationality, string primaryPosition, string sourceName)
        {
            PlayerName = playerName ?? string.Empty;
            Age = age ?? string.Empty;
            Nationality = nationality ?? string.Empty;
            PrimaryPosition = primaryPosition ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
        }

        public string PlayerName { get; }

        public string Age { get; }

        public string Nationality { get; }

        public string PrimaryPosition { get; }

        public string SourceName { get; }
    }

    public sealed class PlayerProfileVerdictViewModel
    {
        public PlayerProfileVerdictViewModel(string recommendation, string roleFit, string confidence, string risk)
        {
            Recommendation = recommendation ?? string.Empty;
            RoleFit = roleFit ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Risk = risk ?? string.Empty;
        }

        public string Recommendation { get; }

        public string RoleFit { get; }

        public string Confidence { get; }

        public string Risk { get; }
    }

    public sealed class PlayerProfileOutputViewModel
    {
        public PlayerProfileOutputViewModel(string title, IReadOnlyList<PlayerProfileMetricTileViewModel> metrics, IReadOnlyList<string> missing)
        {
            Title = title ?? string.Empty;
            Metrics = metrics ?? new List<PlayerProfileMetricTileViewModel>();
            Missing = missing ?? new List<string>();
        }

        public string Title { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> Metrics { get; }

        public IReadOnlyList<string> Missing { get; }
    }

    public sealed class PlayerProfileMetricTileViewModel
    {
        public PlayerProfileMetricTileViewModel(string label, string value, string section, string source, string confidence, string sample, string verificationLabel)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Section = section ?? string.Empty;
            Source = source ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Sample = sample ?? string.Empty;
            VerificationLabel = verificationLabel ?? string.Empty;
        }

        public string Label { get; }

        public string Value { get; }

        public string Section { get; }

        public string Source { get; }

        public string Confidence { get; }

        public string Sample { get; }

        public string VerificationLabel { get; }

        public static PlayerProfileMetricTileViewModel FromStat(PlayerStatRecord stat, string section, bool isFm26Verified)
        {
            return new PlayerProfileMetricTileViewModel(
                stat.StatName,
                FormatNumber(stat.StatValue),
                section,
                stat.SourceName,
                stat.Confidence.ToString(CultureInfo.InvariantCulture) + "%",
                stat.SampleMinutesMissing ? "Minutes missing" : stat.Minutes.ToString(CultureInfo.InvariantCulture) + " minutes",
                isFm26Verified ? "FM26 verified" : "Generic/import metric - not FM26-verified");
        }

        public static PlayerProfileMetricTileViewModel FromPhysical(PhysicalMetricRecord metric, string section, bool isFm26Verified)
        {
            var unit = string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : " " + metric.Unit;
            return new PlayerProfileMetricTileViewModel(
                metric.MetricName,
                FormatNumber(metric.MetricValue) + unit,
                section,
                metric.SourceName,
                metric.Confidence.ToString(CultureInfo.InvariantCulture) + "%",
                "Physical sample",
                isFm26Verified ? "FM26 verified" : "Generic/import metric - not FM26-verified");
        }

        private static string FormatNumber(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.001
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    public sealed class PlayerProfileRoleEvidenceViewModel
    {
        public PlayerProfileRoleEvidenceViewModel(string category, string title, string body, string source, int confidence)
        {
            Category = category ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Source = source ?? string.Empty;
            Confidence = confidence.ToString(CultureInfo.InvariantCulture) + "%";
        }

        public string Category { get; }

        public string Title { get; }

        public string Body { get; }

        public string Source { get; }

        public string Confidence { get; }
    }

    public sealed class PlayerProfileDataQualityViewModel
    {
        public PlayerProfileDataQualityViewModel(string label, string value, string caption)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Caption = caption ?? string.Empty;
        }

        public string Label { get; }

        public string Value { get; }

        public string Caption { get; }
    }

    public sealed class PlayerProfileAttributeSupportViewModel
    {
        public PlayerProfileAttributeSupportViewModel(string label, string value, string confidence, string caption)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Caption = caption ?? string.Empty;
        }

        public string Label { get; }

        public string Value { get; }

        public string Confidence { get; }

        public string Caption { get; }
    }

    public sealed class PlayerProfileScoutActionViewModel
    {
        public PlayerProfileScoutActionViewModel(string title, string reason, string action)
        {
            Title = title ?? string.Empty;
            Reason = reason ?? string.Empty;
            Action = action ?? string.Empty;
        }

        public string Title { get; }

        public string Reason { get; }

        public string Action { get; }
    }

    public sealed class PlayerProfileBlockedDataViewModel
    {
        public PlayerProfileBlockedDataViewModel(int count, IReadOnlyList<string> categories, IReadOnlyList<string> reasons, string safeMessage)
        {
            Count = count;
            Categories = categories ?? new List<string>();
            Reasons = reasons ?? new List<string>();
            SafeMessage = safeMessage ?? string.Empty;
        }

        public int Count { get; }

        public IReadOnlyList<string> Categories { get; }

        public IReadOnlyList<string> Reasons { get; }

        public string SafeMessage { get; }
    }

    public sealed class PlayerProfileBenchmarkSectionViewModel
    {
        public PlayerProfileBenchmarkSectionViewModel(
            string benchmarkName,
            string status,
            string safeMessage,
            string comparisonGroup,
            IReadOnlyList<PlayerBenchmarkMetricViewModel> metrics,
            IReadOnlyList<string> warnings)
        {
            BenchmarkName = benchmarkName ?? string.Empty;
            Status = status ?? string.Empty;
            SafeMessage = safeMessage ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            Metrics = metrics ?? new List<PlayerBenchmarkMetricViewModel>();
            Warnings = warnings ?? new List<string>();
        }

        public string BenchmarkName { get; }

        public string Status { get; }

        public string SafeMessage { get; }

        public string ComparisonGroup { get; }

        public IReadOnlyList<PlayerBenchmarkMetricViewModel> Metrics { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PlayerProfileVisualSectionViewModel
    {
        public PlayerProfileVisualSectionViewModel(string title, string summary, IReadOnlyList<string> rows)
        {
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            Rows = rows ?? new List<string>();
        }

        public string Title { get; }

        public string Summary { get; }

        public IReadOnlyList<string> Rows { get; }
    }
}
