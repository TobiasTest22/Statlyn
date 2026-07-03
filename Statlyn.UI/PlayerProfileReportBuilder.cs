using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Core;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    internal static class PlayerProfileReportBuilder
    {
        public static PlayerProfileReportViewModel Build(PlayerProfileResult result)
        {
            if (!result.Success || result.Player == null || result.SourceMetadata == null || result.MaskedPlayer == null || result.LatestRoleScore == null || result.RoleOutputSummary == null)
            {
                return BuildEmpty(result);
            }

            var core = PlayerProfileMetricTileBuilder.BuildCore(result).ToList();
            var supporting = PlayerProfileMetricTileBuilder.BuildSupporting(result).ToList();
            var physical = PlayerProfileMetricTileBuilder.BuildPhysical(result).ToList();
            var warnings = result.Warnings.ToList();
            if (!result.MetricsAreFm26Verified)
            {
                warnings.Add("Metrics are generic/import metrics and not FM26-verified.");
            }

            if (core.Count == 0 && result.RoleOutputSummary.MissingCoreMetrics.Count > 0)
            {
                warnings.Add("Core output metrics are missing; do not treat missing metrics as zero.");
            }

            return new PlayerProfileReportViewModel(
                result.Player.StatlynPlayerId,
                result.Player.DisplayName,
                ValueOrUnknown(result.VisibleFields, PlayerFieldKey.Age),
                ValueOrUnknown(result.VisibleFields, PlayerFieldKey.Nationality),
                ValueOrUnknown(result.VisibleFields, PlayerFieldKey.PrimaryPosition),
                result.RoleOutputSummary.PositionGroup,
                result.SourceMetadata.SourceName,
                result.SourceMetadata.SourceConfidence.ToString(CultureInfo.InvariantCulture) + "%",
                (result.DataCompleteness == null ? 0 : result.DataCompleteness.CompletenessPercentage).ToString(CultureInfo.InvariantCulture) + "%",
                result.IsFixtureMode,
                result.IsLiveFm26Data,
                RoleNameSanitizer.SanitizeForDisplay(result.LatestRoleScore.RoleName, "Not scored"),
                result.LatestRoleScore.RoleFit.ToString(CultureInfo.InvariantCulture),
                BuildOutputFitLabel(result),
                result.LatestRoleScore.Confidence.ToString(CultureInfo.InvariantCulture),
                result.LatestRoleScore.RiskScore.ToString(CultureInfo.InvariantCulture),
                result.LatestRoleScore.Recommendation.ToString(),
                result.TacticalFitDisplay,
                core,
                supporting,
                result.RoleOutputSummary.MissingCoreMetrics,
                physical,
                warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                PlayerProfileEvidenceBuilder.Build(result),
                PlayerProfileDataQualityBuilder.Build(result),
                PlayerProfileAttributeSupportBuilder.Build(result),
                PlayerProfileScoutActionBuilder.Build(result),
                PlayerProfileBlockedDataBuilder.Build(result),
                BuildBenchmarkSection(result.BenchmarkSummary),
                PlayerProfileVisualSectionBuilder.Build(result, core, supporting));
        }

        private static PlayerProfileReportViewModel BuildEmpty(PlayerProfileResult result)
        {
            return new PlayerProfileReportViewModel(
                string.Empty,
                "Not found",
                "Unknown",
                "Unknown",
                "Unknown",
                "Unknown",
                string.Empty,
                "0%",
                "0%",
                false,
                false,
                "Not scored",
                "0",
                "Unavailable",
                "0",
                "0",
                "ScoutFurther",
                "Unknown",
                new List<PlayerProfileMetricTileViewModel>(),
                new List<PlayerProfileMetricTileViewModel>(),
                new List<string>(),
                new List<PlayerProfileMetricTileViewModel>(),
                result.Errors.Concat(result.Warnings).ToList(),
                new List<PlayerProfileRoleEvidenceViewModel>(),
                new List<PlayerProfileDataQualityViewModel>(),
                new List<PlayerProfileAttributeSupportViewModel>(),
                new[] { new PlayerProfileScoutActionViewModel("Find persisted player", result.SafeMessage, "Import a CSV or open an existing Recruitment Centre row.") },
                new PlayerProfileBlockedDataViewModel(0, new List<string>(), new List<string>(), "No blocked data is loaded for this profile."),
                BuildBenchmarkSection(null),
                new[] { new PlayerProfileVisualSectionViewModel("Benchmark", "No benchmark yet.", new[] { "No percentile is displayed without a real comparison group." }) });
        }

        private static PlayerProfileBenchmarkSectionViewModel BuildBenchmarkSection(BenchmarkPlayerSummary? summary)
        {
            if (summary == null)
            {
                return new PlayerProfileBenchmarkSectionViewModel(
                    string.Empty,
                    BenchmarkStatus.NoBenchmark.ToString(),
                    "No benchmark yet.",
                    string.Empty,
                    new List<PlayerBenchmarkMetricViewModel>(),
                    new[] { "No percentile is displayed without a real comparison group." });
            }

            return new PlayerProfileBenchmarkSectionViewModel(
                summary.BenchmarkName,
                summary.OverallStatus.ToString(),
                summary.SafeMessage,
                summary.ComparisonGroup,
                summary.Results.Select(ToBenchmarkMetricViewModel).ToList(),
                summary.Warnings);
        }

        private static PlayerBenchmarkMetricViewModel ToBenchmarkMetricViewModel(BenchmarkMetricResult result)
        {
            return new PlayerBenchmarkMetricViewModel(
                result.MetricKey,
                result.FieldName,
                result.MetricType.ToString(),
                FormatNullable(result.PlayerValue),
                FormatNullable(result.BenchmarkMedian),
                FormatNullable(result.BenchmarkAverage),
                result.Percentile.HasValue ? result.Percentile.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty,
                result.SampleSize,
                result.Status.ToString(),
                result.SourceName,
                result.ComparisonGroup,
                result.IsGenericImportMetric && !result.IsVerifiedFm26Metric
                    ? "Generic/import metric - not FM26-verified"
                    : "Not FM26-verified");
        }

        private static string BuildOutputFitLabel(PlayerProfileResult result)
        {
            return result.RoleOutputSummary == null || result.RoleOutputSummary.CoreMetrics.Count == 0
                ? "Output incomplete"
                : "Output evidence available";
        }

        private static string ValueOrUnknown(IReadOnlyList<VisiblePlayerField> fields, PlayerFieldKey key)
        {
            var field = fields.FirstOrDefault(item => item.Key == key && item.CanDisplay && item.IsKnown && !string.IsNullOrWhiteSpace(item.DisplayValue));
            return field == null ? "Unknown" : field.DisplayValue;
        }

        private static string FormatNullable(double? value)
        {
            if (!value.HasValue)
            {
                return "Unavailable";
            }

            return Math.Abs(value.Value - Math.Round(value.Value)) < 0.001
                ? ((int)Math.Round(value.Value)).ToString(CultureInfo.InvariantCulture)
                : value.Value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
