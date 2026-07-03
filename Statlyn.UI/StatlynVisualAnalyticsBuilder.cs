using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Core.Abstractions;
using Statlyn.Data.Recruitment;

namespace Statlyn.UI
{
    public static class StatlynVisualAnalyticsBuilder
    {
        public static StatlynVisualAnalyticsViewModel Build(object source)
        {
            if (source is IRawFootballEntity)
            {
                throw new InvalidOperationException("Visual analytics cannot be created from raw provider data.");
            }

            if (!(source is PlayerProfileReportViewModel report))
            {
                throw new InvalidOperationException("Visual analytics require a safe PlayerProfileReportViewModel.");
            }

            return Build(report);
        }

        public static StatlynVisualAnalyticsViewModel Build(PlayerProfileReportViewModel report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var coreMissing = BuildMissingData(report.MissingOutputMetrics).ToList();
            var metricGroups = new List<StatlynMetricGroupVisual>
            {
                BuildMetricGroup("Core Role Output", "Primary role-output evidence", report.CoreOutputMetrics, coreMissing),
                BuildMetricGroup("Supporting Output", "Secondary output context", report.SupportingOutputMetrics, new List<StatlynMissingDataVisual>()),
                BuildMetricGroup("Physical Output", "Physical metrics when safely imported", report.PhysicalOutputMetrics, new List<StatlynMissingDataVisual>())
            };

            return new StatlynVisualAnalyticsViewModel(
                new[]
                {
                    "Identity/Header",
                    "Verdict Score Cards",
                    "Role/Output",
                    "Core Role Output",
                    "Supporting Output",
                    "Physical Output",
                    "Data Quality",
                    "Missing Data",
                    "Evidence",
                    "Scout Actions",
                    "Attribute Support",
                    "Blocked Data",
                    "Benchmark"
                },
                BuildScoreCards(report),
                BuildRoleOutput(report),
                metricGroups,
                report.DataQualityCards.Select(ToDataQualityVisual).ToList(),
                BuildWarnings(report).ToList(),
                report.EvidenceCards.Select(ToEvidenceVisual).ToList(),
                report.ScoutActionCards.Select(ToScoutActionVisual).ToList(),
                report.AttributeSupportCards.Select(ToAttributeTile).ToList(),
                coreMissing,
                BuildBlockedData(report),
                BuildBenchmarkStatus(report));
        }

        public static StatlynBenchmarkStatusVisual BuildBenchmarkStatus()
        {
            return new StatlynBenchmarkStatusVisual(
                false,
                "No benchmark yet.",
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                null);
        }

        public static StatlynBenchmarkStatusVisual BuildBenchmarkStatus(PlayerProfileReportViewModel report)
        {
            if (report == null || report.BenchmarkSection == null || report.BenchmarkSection.Metrics.Count == 0)
            {
                return BuildBenchmarkStatus();
            }

            var availableMetrics = report.BenchmarkSection.Metrics
                .Where(metric => string.Equals(metric.Status, "Available", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var first = availableMetrics.FirstOrDefault() ?? report.BenchmarkSection.Metrics.First();
            var percentile = string.Equals(first.Status, "Available", StringComparison.OrdinalIgnoreCase)
                ? TryParseInt(first.Percentile)
                : null;

            return new StatlynBenchmarkStatusVisual(
                availableMetrics.Count > 0,
                report.BenchmarkSection.SafeMessage,
                percentile,
                report.BenchmarkSection.BenchmarkName,
                report.BenchmarkSection.ComparisonGroup,
                first.MetricKey,
                first.SampleSize,
                report.BenchmarkSection.Metrics.Select(ToBenchmarkMetricVisual).ToList());
        }

        private static IReadOnlyList<StatlynScoreCardVisual> BuildScoreCards(PlayerProfileReportViewModel report)
        {
            return new[]
            {
                new StatlynScoreCardVisual("Role fit", DisplayScore(report.RoleFit), "Persisted role-fit score; not a benchmark percentile.", TryParseScore(report.RoleFit), report.OutputFitLabel),
                new StatlynScoreCardVisual("Confidence", DisplayScore(report.Confidence), "Confidence from safe profile evidence.", TryParseScore(report.Confidence), SourceStatus(report)),
                new StatlynScoreCardVisual("Risk", DisplayPlain(report.Risk), "Risk score from persisted role scoring.", TryParseScore(report.Risk), report.Recommendation),
                new StatlynScoreCardVisual("Tactical fit", DisplayPlain(report.TacticalFitDisplay), "Unknown means unavailable, not zero.", TryParseScore(report.TacticalFitDisplay), report.TacticalFitDisplay)
            };
        }

        private static StatlynRoleOutputVisual BuildRoleOutput(PlayerProfileReportViewModel report)
        {
            return new StatlynRoleOutputVisual(
                report.RoleName,
                report.OutputFitLabel,
                report.TacticalFitDisplay,
                new[]
                {
                    new StatlynHorizontalBarVisual("Role fit", DisplayScore(report.RoleFit), ClampScore(TryParseScore(report.RoleFit)), "Role-score output, not percentile.", TryParseScore(report.RoleFit).HasValue),
                    new StatlynHorizontalBarVisual("Confidence", DisplayScore(report.Confidence), ClampScore(TryParseScore(report.Confidence)), "Evidence confidence from persisted safe data.", TryParseScore(report.Confidence).HasValue),
                    new StatlynHorizontalBarVisual("Risk", DisplayPlain(report.Risk), ClampScore(TryParseScore(report.Risk)), "Risk indicator only; review evidence before deciding.", TryParseScore(report.Risk).HasValue)
                });
        }

        private static StatlynMetricGroupVisual BuildMetricGroup(
            string title,
            string summary,
            IReadOnlyList<PlayerProfileMetricTileViewModel> sourceMetrics,
            IReadOnlyList<StatlynMissingDataVisual> missing)
        {
            var metrics = (sourceMetrics ?? new List<PlayerProfileMetricTileViewModel>()).Select(ToMetricVisual).ToList();
            var safeSummary = metrics.Count == 0
                ? "Output metrics missing; missing values are not treated as zero."
                : summary;
            return new StatlynMetricGroupVisual(title, safeSummary, metrics, missing);
        }

        private static IEnumerable<StatlynMissingDataVisual> BuildMissingData(IReadOnlyList<string> missingOutputMetrics)
        {
            if (missingOutputMetrics == null || missingOutputMetrics.Count == 0)
            {
                yield return new StatlynMissingDataVisual("Core output", "No core output missing.", false, "No zero value is inferred.");
                yield break;
            }

            foreach (var metric in missingOutputMetrics)
            {
                yield return new StatlynMissingDataVisual(metric, "Missing output lowers confidence and is not treated as zero.", true, "Collect before relying on the profile.");
            }
        }

        private static IEnumerable<StatlynWarningVisual> BuildWarnings(PlayerProfileReportViewModel report)
        {
            foreach (var warning in report.KeyWarnings)
            {
                yield return new StatlynWarningVisual("Warning", warning, "Warning", new List<string>());
            }
        }

        private static StatlynWarningVisual BuildBlockedData(PlayerProfileReportViewModel report)
        {
            var rows = new List<string>();
            if (report.BlockedDataNotice.Categories.Count == 0)
            {
                rows.Add("Categories: None");
            }
            else
            {
                rows.Add("Categories: " + string.Join(", ", report.BlockedDataNotice.Categories));
            }

            foreach (var reason in report.BlockedDataNotice.Reasons.Take(4))
            {
                rows.Add("Reason: " + reason);
            }

            rows.Add("Count: " + report.BlockedDataNotice.Count.ToString(CultureInfo.InvariantCulture));
            return new StatlynWarningVisual("Blocked Data Safe Notice", report.BlockedDataNotice.SafeMessage, report.BlockedDataNotice.Count > 0 ? "Guardrail" : "Info", rows);
        }

        private static StatlynMetricTileVisual ToMetricVisual(PlayerProfileMetricTileViewModel metric)
        {
            return new StatlynMetricTileVisual(
                metric.Label,
                metric.Value,
                metric.Section,
                metric.Source,
                metric.Confidence,
                metric.Sample,
                metric.VerificationLabel,
                metric.VerificationLabel.IndexOf("not FM26-verified", StringComparison.OrdinalIgnoreCase) >= 0,
                false);
        }

        private static StatlynMetricTileVisual ToAttributeTile(PlayerProfileAttributeSupportViewModel card)
        {
            return new StatlynMetricTileVisual(
                card.Label,
                card.Value,
                "Attribute Support",
                "Masked profile",
                card.Confidence,
                card.Caption,
                "Supporting evidence only",
                false,
                false);
        }

        private static StatlynBenchmarkMetricVisual ToBenchmarkMetricVisual(Statlyn.Data.Benchmarks.PlayerBenchmarkMetricViewModel metric)
        {
            var percentile = string.Equals(metric.Status, "Available", StringComparison.OrdinalIgnoreCase)
                ? TryParseInt(metric.Percentile)
                : null;
            return new StatlynBenchmarkMetricVisual(
                metric.MetricKey,
                metric.PlayerValue,
                metric.Median,
                metric.Average,
                percentile,
                metric.SampleSize,
                metric.Status,
                metric.SourceName,
                metric.ComparisonGroup,
                metric.VerificationLabel);
        }

        private static StatlynDataQualityVisual ToDataQualityVisual(PlayerProfileDataQualityViewModel card)
        {
            var text = card.Value + " " + card.Caption;
            var isWarning = text.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            text.IndexOf("not FM26-verified", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            text.IndexOf("unsupported", StringComparison.OrdinalIgnoreCase) >= 0;
            return new StatlynDataQualityVisual(card.Label, card.Value, card.Caption, isWarning);
        }

        private static StatlynEvidenceVisual ToEvidenceVisual(PlayerProfileRoleEvidenceViewModel card)
        {
            return new StatlynEvidenceVisual(card.Category, card.Title, card.Body, card.Source, card.Confidence);
        }

        private static StatlynEvidenceVisual ToScoutActionVisual(PlayerProfileScoutActionViewModel card)
        {
            return new StatlynEvidenceVisual("Scout Action", card.Title, card.Reason + " " + card.Action, "Statlyn safe report", "n/a");
        }

        private static string SourceStatus(PlayerProfileReportViewModel report)
        {
            return report.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data";
        }

        private static string DisplayScore(string value)
        {
            var parsed = TryParseScore(value);
            return parsed.HasValue
                ? parsed.Value.ToString("0.##", CultureInfo.InvariantCulture) + "/100"
                : DisplayPlain(value);
        }

        private static string DisplayPlain(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;
        }

        private static double? ClampScore(double? score)
        {
            if (!score.HasValue)
            {
                return null;
            }

            if (score.Value < 0)
            {
                return 0;
            }

            if (score.Value > 100)
            {
                return 100;
            }

            return score.Value;
        }

        private static double? TryParseScore(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Replace("%", string.Empty).Trim();
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static int? TryParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? (int?)Convert.ToInt32(Math.Round(parsed), CultureInfo.InvariantCulture)
                : null;
        }
    }

    public static class RecruitmentCentreMiniVisualBuilder
    {
        public static RecruitmentCentreMiniVisuals Build(object source)
        {
            if (source is IRawFootballEntity)
            {
                throw new InvalidOperationException("Recruitment Centre visuals cannot be created from raw provider data.");
            }

            if (!(source is RecruitmentCentrePlayerRowViewModel row))
            {
                throw new InvalidOperationException("Recruitment Centre visuals require a safe row view model.");
            }

            return Build(row);
        }

        public static RecruitmentCentreMiniVisuals Build(RecruitmentCentrePlayerRowViewModel row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var output = row.KeyOutputMetrics.Count == 0
                ? new[] { new StatlynMetricTileVisual("Output metrics", "Missing", "Output Mini List", row.Source, row.SourceConfidence, "Recruitment row", "Missing output is not treated as zero", false, true) }.ToList()
                : row.KeyOutputMetrics.Take(4).Select(metric => new StatlynMetricTileVisual(metric, "Available", "Output Mini List", row.Source, row.SourceConfidence, "Recruitment row", "Generic/import metric - not FM26-verified", true, false)).ToList();

            var badges = new List<string>
            {
                "Missing data: " + row.MissingDataCount.ToString(CultureInfo.InvariantCulture),
                "Blocked fields: " + row.BlockedFieldCount.ToString(CultureInfo.InvariantCulture)
            };

            if (!row.IsLiveFm26Data)
            {
                badges.Add("No live FM26 data");
            }

            return new RecruitmentCentreMiniVisuals(
                new StatlynScoreCardVisual("Role fit", DisplayScore(row.RoleFit), "Role-fit score, not a percentile.", TryParseScore(row.RoleFit), row.RoleName),
                new StatlynHorizontalBarVisual("Confidence", DisplayScore(row.Confidence), ClampScore(TryParseScore(row.Confidence)), "Persisted confidence.", TryParseScore(row.Confidence).HasValue),
                new StatlynHorizontalBarVisual("Completeness", DisplayScore(row.DataCompleteness), ClampScore(TryParseScore(row.DataCompleteness)), "Persisted data completeness.", TryParseScore(row.DataCompleteness).HasValue),
                new StatlynWarningVisual("Risk", "Risk: " + row.Risk + " | Recommendation: " + row.Recommendation, RiskSeverity(row.Risk), row.Warnings.Take(2).ToList()),
                output,
                BuildRecruitmentBenchmark(row),
                badges,
                row.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data");
        }

        private static StatlynBenchmarkStatusVisual BuildRecruitmentBenchmark(RecruitmentCentrePlayerRowViewModel row)
        {
            var indicator = row.BenchmarkIndicator ?? RecruitmentBenchmarkIndicatorViewModel.NoBenchmark();
            var hasBenchmark = string.Equals(indicator.Status, "Available", StringComparison.OrdinalIgnoreCase);
            var percentile = hasBenchmark ? TryParseInt(indicator.Percentile) : null;
            return new StatlynBenchmarkStatusVisual(
                hasBenchmark,
                indicator.SafeMessage,
                percentile,
                string.Empty,
                string.Empty,
                indicator.KeyMetric,
                indicator.SampleSize);
        }

        private static string RiskSeverity(string risk)
        {
            var score = TryParseScore(risk);
            if (!score.HasValue)
            {
                return "Unknown";
            }

            if (score.Value >= 70)
            {
                return "High";
            }

            return score.Value >= 40 ? "Moderate" : "Low";
        }

        private static string DisplayScore(string value)
        {
            var parsed = TryParseScore(value);
            return parsed.HasValue
                ? parsed.Value.ToString("0.##", CultureInfo.InvariantCulture) + "/100"
                : (string.IsNullOrWhiteSpace(value) ? "Unavailable" : value);
        }

        private static double? ClampScore(double? score)
        {
            if (!score.HasValue)
            {
                return null;
            }

            if (score.Value < 0)
            {
                return 0;
            }

            if (score.Value > 100)
            {
                return 100;
            }

            return score.Value;
        }

        private static double? TryParseScore(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Replace("%", string.Empty).Trim();
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static int? TryParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? (int?)Convert.ToInt32(Math.Round(parsed), CultureInfo.InvariantCulture)
                : null;
        }
    }
}
