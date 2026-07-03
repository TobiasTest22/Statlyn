using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentOutputSummaryService
    {
        public RecruitmentOutputSummary Build(
            string primaryPosition,
            IReadOnlyList<PlayerStatRecord> playerStats,
            IReadOnlyList<PhysicalMetricRecord> physicalMetrics,
            RoleOutputExpectationProfile? profile,
            RoleScore? latestRoleScore)
        {
            var positionGroup = ResolvePositionGroup(primaryPosition);
            var effectiveProfile = profile ?? FindDefaultProfile(positionGroup);
            var roleFamily = effectiveProfile == null ? "Generic" : effectiveProfile.RoleFamily;
            var statsByName = (playerStats ?? new List<PlayerStatRecord>()).ToDictionary(stat => stat.StatName, StringComparer.OrdinalIgnoreCase);
            var physicalByName = (physicalMetrics ?? new List<PhysicalMetricRecord>()).ToDictionary(metric => metric.MetricName, StringComparer.OrdinalIgnoreCase);
            var core = new List<string>();
            var supporting = new List<string>();
            var missing = new List<string>();

            foreach (var expectation in (effectiveProfile == null ? DefaultExpectations(positionGroup) : effectiveProfile.MetricExpectations))
            {
                var value = FormatMetric(expectation.FieldName, statsByName, physicalByName);
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (string.Equals(expectation.Importance, "Core", StringComparison.OrdinalIgnoreCase))
                    {
                        missing.Add(expectation.FieldName);
                    }

                    continue;
                }

                if (string.Equals(expectation.Importance, "Core", StringComparison.OrdinalIgnoreCase))
                {
                    core.Add(value);
                }
                else
                {
                    supporting.Add(value);
                }
            }

            var summary = core.Count == 0
                ? "No core output metrics available yet."
                : "Core output: " + string.Join(", ", core.Take(3)) + ".";
            var confidence = missing.Count == 0
                ? "Core output sample is available; still generic/import-only until provider validation."
                : "Missing core output lowers confidence: " + string.Join(", ", missing.Take(4)) + ".";

            if (latestRoleScore != null && latestRoleScore.MissingData.Count > 0)
            {
                confidence += " Role score also reports missing data.";
            }

            return new RecruitmentOutputSummary(positionGroup, roleFamily, core, supporting, missing, summary, confidence);
        }

        public static string ResolvePositionGroup(string primaryPosition)
        {
            var normalized = (primaryPosition ?? string.Empty).Trim().ToUpperInvariant();
            switch (normalized)
            {
                case "GK":
                case "GOALKEEPER":
                    return "Goalkeeper";
                case "CB":
                case "DC":
                case "CENTREBACK":
                case "CENTRE-BACK":
                    return "CentreBack";
                case "RB":
                case "LB":
                case "RWB":
                case "LWB":
                case "FB":
                case "WB":
                    return "FullBackWingBack";
                case "DM":
                case "DMC":
                    return "DefensiveMidfield";
                case "CM":
                case "MC":
                case "MIDFIELDER":
                    return "CentralMidfield";
                case "AM":
                case "AMC":
                    return "AttackingMidfield";
                case "RW":
                case "LW":
                case "AML":
                case "AMR":
                case "W":
                    return "WingerWideForward";
                case "ST":
                case "CF":
                case "FW":
                case "STRIKER":
                    return "StrikerForward";
                default:
                    return string.IsNullOrWhiteSpace(primaryPosition) ? "Unknown" : primaryPosition.Trim();
            }
        }

        public RoleOutputExpectationProfile? FindDefaultProfile(string positionGroup)
        {
            return GenericRoleOutputExpectationSeed.Create()
                .FirstOrDefault(profile => string.Equals(profile.PositionGroup, positionGroup, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<MetricExpectation> DefaultExpectations(string positionGroup)
        {
            var profile = GenericRoleOutputExpectationSeed.Create()
                .FirstOrDefault(item => string.Equals(item.PositionGroup, positionGroup, StringComparison.OrdinalIgnoreCase));
            return profile == null ? new List<MetricExpectation>() : profile.MetricExpectations;
        }

        private static string FormatMetric(string fieldName, IReadOnlyDictionary<string, PlayerStatRecord> stats, IReadOnlyDictionary<string, PhysicalMetricRecord> physical)
        {
            if (stats.TryGetValue(fieldName, out var stat))
            {
                return fieldName + " " + FormatNumber(stat.StatValue);
            }

            if (physical.TryGetValue(fieldName, out var metric))
            {
                var unit = string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : " " + metric.Unit;
                return fieldName + " " + FormatNumber(metric.MetricValue) + unit;
            }

            return string.Empty;
        }

        private static string FormatNumber(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.001
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
