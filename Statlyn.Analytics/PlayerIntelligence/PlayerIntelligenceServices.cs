using System;
using System.Collections.Generic;
using System.Linq;

namespace Statlyn.Analytics.PlayerIntelligence
{
    public sealed class SafeMetricInput
    {
        public SafeMetricInput(string metricKey, string label, double value, int minutes, string unit, int confidence)
        {
            MetricKey = metricKey ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value;
            Minutes = minutes;
            Unit = unit ?? string.Empty;
            Confidence = confidence;
        }

        public string MetricKey { get; }

        public string Label { get; }

        public double Value { get; }

        public int Minutes { get; }

        public string Unit { get; }

        public int Confidence { get; }
    }

    public sealed class FairValueInput
    {
        public string Currency { get; set; } = string.Empty;

        public double? AnchorValue { get; set; }

        public double? AskingPrice { get; set; }

        public IReadOnlyList<double> ComparableValues { get; set; } = new List<double>();

        public int? Age { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string PositionGroup { get; set; } = string.Empty;

        public int? ContractMonthsRemaining { get; set; }

        public int Minutes { get; set; }

        public double? PerformanceIndex { get; set; }

        public int? RoleFit { get; set; }

        public double? LeagueStrengthMultiplier { get; set; }

        public int? TacticalFit { get; set; }

        public int DataCompleteness { get; set; }

        public IReadOnlyList<string> SafeRiskFlags { get; set; } = new List<string>();
    }

    public sealed class PlayerValueEstimateService
    {
        public const string ModelVersion = "statlyn-fair-value-v0.1";

        public PlayerValueEstimate Estimate(FairValueInput input)
        {
            input = input ?? new FairValueInput();
            var missing = new List<string>();
            var drivers = new List<string>();
            var discounts = new List<string>();
            var comparableValues = input.ComparableValues == null ? new List<double>() : input.ComparableValues.Where(value => value > 0).ToList();
            var anchor = ResolveAnchor(input, comparableValues);

            if (!anchor.HasValue)
            {
                return new PlayerValueEstimate(
                    false,
                    "Fair value unavailable. Missing valuation anchor, contract context or comparable player sample.",
                    null,
                    null,
                    null,
                    input.Currency,
                    null,
                    0,
                    "Unavailable",
                    drivers,
                    discounts,
                    new[] { "valuation anchor", "safe comparable player sample" },
                    ModelVersion);
            }

            if (!input.PerformanceIndex.HasValue)
            {
                missing.Add("role-specific performance sample");
                discounts.Add("Missing role-specific performance lowers confidence.");
            }

            if (!input.Age.HasValue)
            {
                missing.Add("age");
                discounts.Add("Missing age removes age-curve adjustment.");
            }

            if (!input.ContractMonthsRemaining.HasValue)
            {
                missing.Add("contract context");
                discounts.Add("Missing contract context lowers confidence.");
            }

            if (!input.LeagueStrengthMultiplier.HasValue)
            {
                missing.Add("league strength");
                discounts.Add("Missing league strength keeps league adjustment neutral.");
            }

            if (!input.TacticalFit.HasValue)
            {
                missing.Add("tactical fit");
                discounts.Add("Missing team style keeps tactical fit neutral.");
            }

            if (input.Minutes < 900)
            {
                discounts.Add("Weak sample size widens the range.");
            }

            var performance = PerformanceMultiplier(input.PerformanceIndex);
            var ageCurve = AgeCurveMultiplier(input.Age, input.PositionGroup, input.RoleName);
            var contract = ContractMultiplier(input.ContractMonthsRemaining);
            var scarcity = RoleScarcityMultiplier(input.RoleName, input.RoleFit, input.PerformanceIndex);
            var league = input.LeagueStrengthMultiplier.HasValue ? Clamp(input.LeagueStrengthMultiplier.Value, 0.82, 1.18) : 1.0;
            var tactical = TacticalFitMultiplier(input.TacticalFit);
            var risk = RiskAdjustment(input);
            var confidenceAdjustment = DataConfidenceAdjustment(input.DataCompleteness, missing.Count);

            if (input.AskingPrice.HasValue)
            {
                drivers.Add("Safe asking price is the anchor.");
            }
            else if (input.AnchorValue.HasValue)
            {
                drivers.Add("Safe imported value is the anchor.");
            }
            else
            {
                drivers.Add("Safe comparable player values are the anchor.");
            }

            if (performance > 1.03)
            {
                drivers.Add("Role output supports an upward performance adjustment.");
            }

            if (ageCurve > 1.03)
            {
                drivers.Add("Age curve adds upside for this role.");
            }

            if (scarcity > 1.01)
            {
                drivers.Add("Role scarcity is applied from supported role evidence.");
            }

            if (tactical > 1.01)
            {
                drivers.Add("Defined tactical fit supports the estimate.");
            }

            var midpoint = anchor.Value * performance * ageCurve * contract * scarcity * league * tactical * risk * confidenceAdjustment;
            var rangeWidth = RangeWidth(input, missing.Count);
            var low = midpoint * (1.0 - rangeWidth);
            var high = midpoint * (1.0 + rangeWidth);
            var confidence = Confidence(input, missing.Count);
            var dataQuality = confidence >= 70 ? "Medium" : confidence >= 45 ? "Limited" : "Low";

            return new PlayerValueEstimate(
                true,
                "Statlyn Fair Value Estimate is available from safe imported valuation inputs. Treat this as an estimate, not truth.",
                Math.Round(low, 2),
                Math.Round(midpoint, 2),
                Math.Round(high, 2),
                input.Currency,
                null,
                confidence,
                dataQuality,
                drivers,
                discounts,
                missing,
                ModelVersion);
        }

        private static double? ResolveAnchor(FairValueInput input, IReadOnlyList<double> comparableValues)
        {
            if (input.AskingPrice.HasValue && input.AskingPrice.Value > 0)
            {
                return input.AskingPrice.Value;
            }

            if (input.AnchorValue.HasValue && input.AnchorValue.Value > 0)
            {
                return input.AnchorValue.Value;
            }

            if (comparableValues.Count < 5)
            {
                return null;
            }

            var ordered = comparableValues.OrderBy(value => value).ToList();
            return ordered[ordered.Count / 2];
        }

        private static double PerformanceMultiplier(double? performanceIndex)
        {
            if (!performanceIndex.HasValue)
            {
                return 1.0;
            }

            return 1.0 + Clamp((performanceIndex.Value - 50.0) / 100.0, -0.18, 0.22);
        }

        private static double AgeCurveMultiplier(int? age, string positionGroup, string roleName)
        {
            if (!age.HasValue)
            {
                return 1.0;
            }

            var slowerCurve = Contains(positionGroup, "keeper") || Contains(roleName, "Goalkeeper") || Contains(roleName, "Centre-back");
            if (slowerCurve)
            {
                if (age.Value <= 24)
                {
                    return 1.06;
                }

                if (age.Value <= 30)
                {
                    return 1.02;
                }

                return age.Value >= 34 ? 0.88 : 0.96;
            }

            if (age.Value <= 21)
            {
                return 1.1;
            }

            if (age.Value <= 25)
            {
                return 1.05;
            }

            return age.Value >= 31 ? 0.84 : age.Value >= 29 ? 0.93 : 1.0;
        }

        private static double ContractMultiplier(int? monthsRemaining)
        {
            if (!monthsRemaining.HasValue)
            {
                return 1.0;
            }

            if (monthsRemaining.Value < 12)
            {
                return 0.82;
            }

            if (monthsRemaining.Value < 24)
            {
                return 0.95;
            }

            return monthsRemaining.Value >= 42 ? 1.12 : 1.04;
        }

        private static double RoleScarcityMultiplier(string roleName, int? roleFit, double? performanceIndex)
        {
            if (!roleFit.HasValue || roleFit.Value < 70 || !performanceIndex.HasValue)
            {
                return 1.0;
            }

            var scarce =
                Contains(roleName, "Ball-carrying") ||
                Contains(roleName, "Take-on") ||
                Contains(roleName, "Creative") ||
                Contains(roleName, "Controller") ||
                Contains(roleName, "Sweeper") ||
                Contains(roleName, "Progression");

            return scarce ? 1.1 : 1.04;
        }

        private static double TacticalFitMultiplier(int? tacticalFit)
        {
            if (!tacticalFit.HasValue)
            {
                return 1.0;
            }

            return 1.0 + Clamp((tacticalFit.Value - 50.0) / 300.0, -0.12, 0.12);
        }

        private static double RiskAdjustment(FairValueInput input)
        {
            var adjustment = 1.0;
            if (input.Minutes < 900)
            {
                adjustment -= 0.04;
            }

            if (input.SafeRiskFlags != null && input.SafeRiskFlags.Count > 0)
            {
                adjustment -= Math.Min(0.12, input.SafeRiskFlags.Count * 0.03);
            }

            return Clamp(adjustment, 0.82, 1.0);
        }

        private static double DataConfidenceAdjustment(int dataCompleteness, int missingCount)
        {
            var adjustment = 1.0;
            if (dataCompleteness < 60)
            {
                adjustment -= 0.04;
            }

            adjustment -= Math.Min(0.08, missingCount * 0.015);
            return Clamp(adjustment, 0.86, 1.0);
        }

        private static double RangeWidth(FairValueInput input, int missingCount)
        {
            var width = 0.14 + missingCount * 0.035;
            if (input.Minutes < 900)
            {
                width += 0.1;
            }

            if (input.DataCompleteness < 60)
            {
                width += 0.06;
            }

            return Clamp(width, 0.14, 0.46);
        }

        private static int Confidence(FairValueInput input, int missingCount)
        {
            var confidence = 78;
            confidence -= missingCount * 8;
            if (input.Minutes < 900)
            {
                confidence -= 15;
            }

            if (input.DataCompleteness < 60)
            {
                confidence -= 10;
            }

            if (!input.AnchorValue.HasValue && !input.AskingPrice.HasValue)
            {
                confidence -= 8;
            }

            return PlayerDataAvailabilityReport.ClampPercent(confidence);
        }

        private static bool Contains(string value, string pattern)
        {
            return !string.IsNullOrWhiteSpace(value) && value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }

    public sealed class PlayerPer90Service
    {
        public PlayerPer90Summary Build(IReadOnlyList<SafeMetricInput> metrics)
        {
            metrics = metrics ?? new List<SafeMetricInput>();
            var usable = metrics.Where(metric => metric.Minutes > 0).ToList();
            if (usable.Count == 0)
            {
                return new PlayerPer90Summary(
                    false,
                    "Performance per 90 unavailable. Missing minutes or safe performance metrics.",
                    "Unavailable",
                    0,
                    new[] { "minutes", "safe performance metrics" },
                    new List<string>(),
                    new List<PlayerPer90Metric>());
            }

            var converted = usable.Select(metric => new PlayerPer90Metric(
                metric.MetricKey,
                metric.Label,
                Math.Round(metric.Value * 90.0 / metric.Minutes, 3),
                string.IsNullOrWhiteSpace(metric.Unit) ? "per 90" : metric.Unit,
                metric.Minutes,
                "Imported",
                metric.Confidence)).ToList();

            return new PlayerPer90Summary(
                true,
                "Performance per 90 uses safe imported minutes and metric totals.",
                "Imported",
                converted.Count == 0 ? 0 : (int)converted.Average(metric => metric.Confidence),
                new List<string>(),
                new List<string>(),
                converted);
        }
    }

    public sealed class PlayerHeatmapService
    {
        public PlayerHeatmapSummary Build(IReadOnlyList<PlayerHeatmapPoint> points)
        {
            points = points ?? new List<PlayerHeatmapPoint>();
            if (points.Count == 0)
            {
                return new PlayerHeatmapSummary(
                    false,
                    "Heatmap unavailable. No safe event-location data has been imported.",
                    "Unavailable",
                    0,
                    new[] { "matchId", "playerId", "eventType", "x", "y", "minute" },
                    new List<string>(),
                    new List<PlayerHeatmapPoint>());
            }

            return new PlayerHeatmapSummary(
                true,
                "Heatmap uses imported event-location points only.",
                "Imported",
                (int)points.Average(point => point.Confidence),
                new List<string>(),
                new List<string>(),
                points);
        }
    }

    public sealed class PlayerRadarService
    {
        public PlayerSkillRadar Build(IReadOnlyList<PlayerRadarAxis> axes, string profileType)
        {
            axes = axes ?? new List<PlayerRadarAxis>();
            if (axes.Count < 3)
            {
                return new PlayerSkillRadar(
                    false,
                    "Skill radar unavailable. Missing enough safe visible or imported metrics.",
                    profileType,
                    "Unavailable",
                    0,
                    new[] { "safe visible attributes or imported role metrics" },
                    new List<string>(),
                    new List<PlayerRadarAxis>());
            }

            return new PlayerSkillRadar(
                true,
                "Skill radar uses safe visible/imported data only.",
                profileType,
                "Imported",
                (int)axes.Average(axis => axis.Confidence),
                new List<string>(),
                new List<string>(),
                axes);
        }
    }

    public sealed class PlayerSimilarityService
    {
        public PlayerSimilarityResult Build(IReadOnlyList<SimilarPlayerCandidate> candidates, int comparableSample)
        {
            candidates = candidates ?? new List<SimilarPlayerCandidate>();
            if (comparableSample < 5 || candidates.Count == 0)
            {
                return new PlayerSimilarityResult(
                    false,
                    "Similar player search unavailable. Not enough comparable safe player data.",
                    "Unavailable",
                    0,
                    new[] { "comparable sample", "common metric set", "position or role", "minutes threshold" },
                    new List<string>(),
                    new List<SimilarPlayerCandidate>());
            }

            return new PlayerSimilarityResult(
                true,
                "Similar player search uses safe statistical/style vectors.",
                "Imported",
                (int)candidates.Average(candidate => candidate.Confidence),
                new List<string>(),
                new List<string>(),
                candidates);
        }
    }

    public sealed class LeagueAverageComparisonService
    {
        public LeagueAverageComparison Build(string leagueKey, string comparisonGroup, int sampleSize, IReadOnlyList<PlayerRadarAxis> comparisons)
        {
            comparisons = comparisons ?? new List<PlayerRadarAxis>();
            if (sampleSize < 10 || comparisons.Count == 0)
            {
                return new LeagueAverageComparison(
                    false,
                    "League comparison unavailable. Not enough league sample data.",
                    leagueKey,
                    comparisonGroup,
                    sampleSize,
                    "Unavailable",
                    0,
                    new[] { "league sample", "common metric set", "sample size" },
                    new List<string>(),
                    new List<PlayerRadarAxis>());
            }

            return new LeagueAverageComparison(
                true,
                "League comparison uses safe imported league averages.",
                leagueKey,
                comparisonGroup,
                sampleSize,
                "Imported",
                (int)comparisons.Average(item => item.Confidence),
                new List<string>(),
                new List<string>(),
                comparisons);
        }
    }

    public sealed class PlayerFitProjectionService
    {
        public PlayerFitProjection Build(bool hasTeamStyle, bool hasRoleModel, int? roleFit, string roleName)
        {
            if (!hasTeamStyle || !hasRoleModel)
            {
                return new PlayerFitProjection(
                    false,
                    "Fit projection unavailable. Team style model or squad need has not been defined.",
                    "Unavailable",
                    0,
                    roleFit.HasValue ? "Role fit exists, but team style is missing." : string.Empty,
                    string.Empty,
                    new[] { "team style model", "squad need", "role-specific parameters" },
                    new List<string>());
            }

            return new PlayerFitProjection(
                true,
                "Fit projection uses backend role and team style evidence.",
                "Imported",
                roleFit ?? 0,
                roleName,
                "Team style model available.",
                new List<string>(),
                new List<string>());
        }
    }

    public sealed class PlayerArchetypeService
    {
        public PlayerArchetypeResult Build(IReadOnlyList<SafeMetricInput> metrics)
        {
            metrics = metrics ?? new List<SafeMetricInput>();
            if (metrics.Count < 4)
            {
                return new PlayerArchetypeResult(
                    false,
                    "Archetype unavailable. Safe style vector metrics are missing.",
                    "Unavailable",
                    "Unavailable",
                    0,
                    new List<string>(),
                    new[] { "style vector metrics", "role metrics", "minutes threshold" },
                    new List<string>());
            }

            return new PlayerArchetypeResult(
                true,
                "Archetype uses safe imported style metrics.",
                "Role style cluster",
                "Imported",
                (int)metrics.Average(metric => metric.Confidence),
                metrics.Select(metric => metric.Label).ToList(),
                new List<string>(),
                new List<string>());
        }
    }
}
