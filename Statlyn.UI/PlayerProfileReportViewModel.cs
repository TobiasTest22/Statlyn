using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Core;
using Statlyn.Core.Abstractions;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;

namespace Statlyn.UI
{
    public sealed class PlayerProfileReportViewModel
    {
        private PlayerProfileReportViewModel(
            string statlynPlayerId,
            string playerName,
            string age,
            string nationality,
            string primaryPosition,
            string positionGroup,
            string sourceName,
            string sourceConfidence,
            string dataCompleteness,
            bool isFixtureMode,
            bool isLiveFm26Data,
            string roleName,
            string roleFit,
            string outputFitLabel,
            string confidence,
            string risk,
            string recommendation,
            string tacticalFitDisplay,
            IReadOnlyList<PlayerProfileMetricTileViewModel> coreOutputMetrics,
            IReadOnlyList<PlayerProfileMetricTileViewModel> supportingOutputMetrics,
            IReadOnlyList<string> missingOutputMetrics,
            IReadOnlyList<PlayerProfileMetricTileViewModel> physicalOutputMetrics,
            IReadOnlyList<string> keyWarnings,
            IReadOnlyList<PlayerProfileRoleEvidenceViewModel> evidenceCards,
            IReadOnlyList<PlayerProfileDataQualityViewModel> dataQualityCards,
            IReadOnlyList<PlayerProfileAttributeSupportViewModel> attributeSupportCards,
            IReadOnlyList<PlayerProfileScoutActionViewModel> scoutActionCards,
            PlayerProfileBlockedDataViewModel blockedDataNotice,
            IReadOnlyList<PlayerProfileVisualSectionViewModel> visualSections)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            Age = age ?? string.Empty;
            Nationality = nationality ?? string.Empty;
            PrimaryPosition = primaryPosition ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceConfidence = sourceConfidence ?? string.Empty;
            DataCompleteness = dataCompleteness ?? string.Empty;
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
            RoleName = roleName ?? string.Empty;
            RoleFit = roleFit ?? string.Empty;
            OutputFitLabel = outputFitLabel ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Risk = risk ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            TacticalFitDisplay = tacticalFitDisplay ?? "Unknown";
            CoreOutputMetrics = coreOutputMetrics ?? new List<PlayerProfileMetricTileViewModel>();
            SupportingOutputMetrics = supportingOutputMetrics ?? new List<PlayerProfileMetricTileViewModel>();
            MissingOutputMetrics = missingOutputMetrics ?? new List<string>();
            PhysicalOutputMetrics = physicalOutputMetrics ?? new List<PlayerProfileMetricTileViewModel>();
            KeyWarnings = keyWarnings ?? new List<string>();
            EvidenceCards = evidenceCards ?? new List<PlayerProfileRoleEvidenceViewModel>();
            DataQualityCards = dataQualityCards ?? new List<PlayerProfileDataQualityViewModel>();
            AttributeSupportCards = attributeSupportCards ?? new List<PlayerProfileAttributeSupportViewModel>();
            ScoutActionCards = scoutActionCards ?? new List<PlayerProfileScoutActionViewModel>();
            BlockedDataNotice = blockedDataNotice;
            VisualSections = visualSections ?? new List<PlayerProfileVisualSectionViewModel>();
        }

        public string StatlynPlayerId { get; }

        public string PlayerName { get; }

        public string Age { get; }

        public string Nationality { get; }

        public string PrimaryPosition { get; }

        public string PositionGroup { get; }

        public string SourceName { get; }

        public string SourceConfidence { get; }

        public string DataCompleteness { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public string RoleName { get; }

        public string RoleFit { get; }

        public string OutputFitLabel { get; }

        public string Confidence { get; }

        public string Risk { get; }

        public string Recommendation { get; }

        public string TacticalFitDisplay { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> CoreOutputMetrics { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> SupportingOutputMetrics { get; }

        public IReadOnlyList<string> MissingOutputMetrics { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> PhysicalOutputMetrics { get; }

        public IReadOnlyList<string> KeyWarnings { get; }

        public IReadOnlyList<PlayerProfileRoleEvidenceViewModel> EvidenceCards { get; }

        public IReadOnlyList<PlayerProfileDataQualityViewModel> DataQualityCards { get; }

        public IReadOnlyList<PlayerProfileAttributeSupportViewModel> AttributeSupportCards { get; }

        public IReadOnlyList<PlayerProfileScoutActionViewModel> ScoutActionCards { get; }

        public PlayerProfileBlockedDataViewModel BlockedDataNotice { get; }

        public IReadOnlyList<PlayerProfileVisualSectionViewModel> VisualSections { get; }

        public static PlayerProfileReportViewModel From(object source)
        {
            if (source is IRawFootballEntity)
            {
                throw new InvalidOperationException("Player Profile report cannot be created from raw provider data.");
            }

            if (!(source is PlayerProfileResult result))
            {
                throw new InvalidOperationException("Player Profile report requires a persisted-safe PlayerProfileResult.");
            }

            return From(result);
        }

        public static PlayerProfileReportViewModel From(PlayerProfileResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Success || result.Player == null || result.SourceMetadata == null || result.MaskedPlayer == null || result.LatestRoleScore == null || result.RoleOutputSummary == null)
            {
                return Empty(result);
            }

            var age = ValueOrUnknown(result.VisibleFields, PlayerFieldKey.Age);
            var nationality = ValueOrUnknown(result.VisibleFields, PlayerFieldKey.Nationality);
            var position = ValueOrUnknown(result.VisibleFields, PlayerFieldKey.PrimaryPosition);
            var core = BuildMetricTiles(result, "Core").ToList();
            var supporting = BuildMetricTiles(result, "Supporting").ToList();
            var physical = result.PhysicalMetrics
                .Select(metric => PlayerProfileMetricTileViewModel.FromPhysical(metric, "Physical Output", result.MetricsAreFm26Verified))
                .ToList();
            var warnings = result.Warnings.ToList();
            if (!result.MetricsAreFm26Verified)
            {
                warnings.Add("Metrics are generic/import metrics and not FM26-verified.");
            }

            if (core.Count == 0 && result.RoleOutputSummary.MissingCoreMetrics.Count > 0)
            {
                warnings.Add("Core output metrics are missing; do not treat missing metrics as zero.");
            }

            var report = new PlayerProfileReportViewModel(
                result.Player.StatlynPlayerId,
                result.Player.DisplayName,
                age,
                nationality,
                position,
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
                BuildEvidence(result),
                BuildDataQuality(result),
                BuildAttributeSupport(result),
                BuildScoutActions(result),
                BuildBlockedNotice(result),
                BuildVisualSections(result, core, supporting));

            return report;
        }

        private static PlayerProfileReportViewModel Empty(PlayerProfileResult result)
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
                new[] { new PlayerProfileVisualSectionViewModel("Benchmark", "No benchmark yet.", new[] { "No percentile is displayed without a real comparison group." }) });
        }

        private static IEnumerable<PlayerProfileMetricTileViewModel> BuildMetricTiles(PlayerProfileResult result, string section)
        {
            var expectations = result.RoleOutputExpectationProfile == null
                ? new List<MetricExpectation>()
                : result.RoleOutputExpectationProfile.MetricExpectations;
            var requiredImportance = string.Equals(section, "Core", StringComparison.OrdinalIgnoreCase)
                ? "Core"
                : string.Empty;

            foreach (var expectation in expectations)
            {
                var isCore = string.Equals(expectation.Importance, "Core", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(requiredImportance) != isCore)
                {
                    continue;
                }

                if (TryFindStat(result.PlayerStats, expectation.FieldName, out var stat))
                {
                    yield return PlayerProfileMetricTileViewModel.FromStat(stat, isCore ? "Core Role Output" : "Supporting Output", result.MetricsAreFm26Verified);
                    continue;
                }

                if (TryFindPhysical(result.PhysicalMetrics, expectation.FieldName, out var physical))
                {
                    yield return PlayerProfileMetricTileViewModel.FromPhysical(physical, isCore ? "Core Role Output" : "Supporting Output", result.MetricsAreFm26Verified);
                }
            }
        }

        private static string BuildOutputFitLabel(PlayerProfileResult result)
        {
            if (result.RoleOutputSummary == null || result.RoleOutputSummary.CoreMetrics.Count == 0)
            {
                return "Output incomplete";
            }

            return "Output evidence available";
        }

        private static IReadOnlyList<PlayerProfileRoleEvidenceViewModel> BuildEvidence(PlayerProfileResult result)
        {
            var cards = new List<PlayerProfileRoleEvidenceViewModel>();
            foreach (var item in result.LatestRoleScore!.PositiveEvidence)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Positive", item.FieldName, item.Message, result.SourceMetadata!.SourceName, result.LatestRoleScore.Confidence));
            }

            foreach (var item in result.LatestRoleScore.NegativeEvidence)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Risk", item.FieldName, item.Message, result.SourceMetadata!.SourceName, result.LatestRoleScore.Confidence));
            }

            foreach (var missing in result.RoleOutputSummary!.MissingCoreMetrics)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Missing Output", missing, "Missing output lowers confidence and is not treated as zero.", result.SourceMetadata!.SourceName, 0));
            }

            if (cards.Count == 0)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Evidence", "Role evidence", "Evidence is provisional until more output data and scouting context are available.", result.SourceMetadata!.SourceName, result.LatestRoleScore.Confidence));
            }

            return cards;
        }

        private static IReadOnlyList<PlayerProfileDataQualityViewModel> BuildDataQuality(PlayerProfileResult result)
        {
            var minutesMissing = result.PlayerStats.Count == 0 || result.PlayerStats.All(stat => stat.SampleMinutesMissing);
            var minutes = minutesMissing ? "Missing" : result.PlayerStats.Max(stat => stat.Minutes).ToString(CultureInfo.InvariantCulture);
            return new[]
            {
                new PlayerProfileDataQualityViewModel("Source", result.SourceMetadata!.SourceName, result.IsFixtureMode ? "Fixture/import mode" : "Persisted source"),
                new PlayerProfileDataQualityViewModel("Live FM26", result.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data", "FM26 unsupported until validated memory maps exist."),
                new PlayerProfileDataQualityViewModel("Metric status", result.MetricsAreFm26Verified ? "FM26 verified" : "Generic/import, not FM26-verified", "Do not treat generic metric names as official FM26 stats."),
                new PlayerProfileDataQualityViewModel("Sample minutes", minutes, minutesMissing ? "Sample-size warning: minutes missing." : "Minutes are available for output interpretation."),
                new PlayerProfileDataQualityViewModel("Missing output", result.RoleOutputSummary!.MissingCoreMetrics.Count.ToString(CultureInfo.InvariantCulture), "Missing output lowers confidence."),
                new PlayerProfileDataQualityViewModel("Blocked audit", result.BlockedFields.Count.ToString(CultureInfo.InvariantCulture), "Counts/categories only; no raw blocked values."),
                new PlayerProfileDataQualityViewModel("Tactical fit", result.TacticalFitDisplay, result.TacticalFitDisplay == "Unknown" ? "Tactical fit is unknown, not zero." : "Persisted tactical fit is available.")
            };
        }

        private static IReadOnlyList<PlayerProfileAttributeSupportViewModel> BuildAttributeSupport(PlayerProfileResult result)
        {
            return result.VisibleFields
                .Where(field => field.Key == PlayerFieldKey.TechnicalAttribute && field.CanDisplay && field.NumericValue.HasValue)
                .Take(8)
                .Select(field => new PlayerProfileAttributeSupportViewModel(field.FieldName, FormatNumber(field.NumericValue!.Value), field.Confidence.ToString(CultureInfo.InvariantCulture) + "%", "Supporting evidence only"))
                .ToList();
        }

        private static IReadOnlyList<PlayerProfileScoutActionViewModel> BuildScoutActions(PlayerProfileResult result)
        {
            var actions = new List<PlayerProfileScoutActionViewModel>();
            if (result.LatestRoleScore!.Confidence < 55 || result.SourceMetadata!.SourceConfidence < 70)
            {
                actions.Add(new PlayerProfileScoutActionViewModel("Scout further", "Confidence is limited.", "Collect scouting context before making a stronger call."));
            }

            if (result.RoleOutputSummary!.MissingCoreMetrics.Count > 0)
            {
                actions.Add(new PlayerProfileScoutActionViewModel("Collect missing output", "Core output metrics are missing: " + string.Join(", ", result.RoleOutputSummary.MissingCoreMetrics.Take(4)) + ".", "Scout or import the missing output before deciding."));
            }

            if (!result.MetricsAreFm26Verified)
            {
                actions.Add(new PlayerProfileScoutActionViewModel("Treat metrics as generic/import", "Metrics are not FM26-verified.", "Do not claim official FM26 support."));
            }

            actions.Add(new PlayerProfileScoutActionViewModel("Benchmark status", "No benchmark yet.", "Do not show percentiles until a real comparison group exists."));
            if (actions.Count == 1)
            {
                actions.Insert(0, new PlayerProfileScoutActionViewModel("Review role profile", "Output fit is provisional.", "Confirm the role-output profile matches the scouting question."));
            }

            return actions;
        }

        private static PlayerProfileBlockedDataViewModel BuildBlockedNotice(PlayerProfileResult result)
        {
            var categories = result.BlockedFields.Select(field => field.Key.ToString()).Distinct().OrderBy(value => value).ToList();
            var reasons = result.BlockedFields.Select(field => field.Reason).Where(reason => !string.IsNullOrWhiteSpace(reason)).Distinct().OrderBy(value => value).ToList();
            var message = result.BlockedFields.Count == 0
                ? "No blocked fields were present."
                : result.BlockedFields.Count.ToString(CultureInfo.InvariantCulture) + " blocked field(s) excluded. Raw values are not shown.";
            return new PlayerProfileBlockedDataViewModel(result.BlockedFields.Count, categories, reasons, message);
        }

        private static IReadOnlyList<PlayerProfileVisualSectionViewModel> BuildVisualSections(
            PlayerProfileResult result,
            IReadOnlyList<PlayerProfileMetricTileViewModel> core,
            IReadOnlyList<PlayerProfileMetricTileViewModel> supporting)
        {
            var sections = new List<PlayerProfileVisualSectionViewModel>
            {
                new PlayerProfileVisualSectionViewModel("Score cards", "Role fit " + result.LatestRoleScore!.RoleFit.ToString(CultureInfo.InvariantCulture) + " | Confidence " + result.LatestRoleScore.Confidence.ToString(CultureInfo.InvariantCulture), new[] { "Source: " + result.SourceMetadata!.SourceName, "Tactical fit: " + result.TacticalFitDisplay }),
                new PlayerProfileVisualSectionViewModel("Core Role Output", core.Count == 0 ? "Output metrics missing" : "Output metrics available", core.Select(metric => metric.Label + " " + metric.Value).ToList()),
                new PlayerProfileVisualSectionViewModel("Supporting Output", supporting.Count == 0 ? "No supporting output available yet." : "Supporting output available", supporting.Select(metric => metric.Label + " " + metric.Value).ToList()),
                new PlayerProfileVisualSectionViewModel("Missing Data", result.RoleOutputSummary!.MissingCoreMetrics.Count == 0 ? "No core output missing." : "Missing output lowers confidence.", result.RoleOutputSummary.MissingCoreMetrics),
                new PlayerProfileVisualSectionViewModel("Blocked Data", result.BlockedFields.Count == 0 ? "No blocked fields were present." : "Blocked values excluded safely.", result.BlockedFields.Select(field => field.Key.ToString()).Distinct().ToList()),
                new PlayerProfileVisualSectionViewModel("Benchmark", "No benchmark yet.", new[] { "No fake percentile or comparison group is shown." })
            };

            if (result.VisibleFields.Any(field => field.Key == PlayerFieldKey.TechnicalAttribute))
            {
                sections.Add(new PlayerProfileVisualSectionViewModel("Attribute Support", "Attributes are supporting evidence only.", result.VisibleFields.Where(field => field.Key == PlayerFieldKey.TechnicalAttribute).Take(6).Select(field => field.FieldName).ToList()));
            }

            return sections;
        }

        private static bool TryFindStat(IReadOnlyList<PlayerStatRecord> stats, string name, out PlayerStatRecord stat)
        {
            stat = stats.FirstOrDefault(item => string.Equals(item.StatName, name, StringComparison.OrdinalIgnoreCase));
            return stat != null;
        }

        private static bool TryFindPhysical(IReadOnlyList<PhysicalMetricRecord> metrics, string name, out PhysicalMetricRecord metric)
        {
            metric = metrics.FirstOrDefault(item => string.Equals(item.MetricName, name, StringComparison.OrdinalIgnoreCase));
            return metric != null;
        }

        private static string ValueOrUnknown(IReadOnlyList<VisiblePlayerField> fields, PlayerFieldKey key)
        {
            var field = fields.FirstOrDefault(item => item.Key == key && item.CanDisplay && item.IsKnown && !string.IsNullOrWhiteSpace(item.DisplayValue));
            return field == null ? "Unknown" : field.DisplayValue;
        }

        private static string FormatNumber(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.001
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

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
