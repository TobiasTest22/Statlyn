using System;
using System.Collections.Generic;

namespace Statlyn.Analytics.PlayerIntelligence
{
    public sealed class PlayerDataAvailabilityReport
    {
        public PlayerDataAvailabilityReport(
            bool available,
            string safeMessage,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public static PlayerDataAvailabilityReport Unavailable(string safeMessage, IReadOnlyList<string> requiredFieldsMissing)
        {
            return new PlayerDataAvailabilityReport(false, safeMessage, "Unavailable", 0, requiredFieldsMissing, new List<string>());
        }

        internal static int ClampPercent(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    public sealed class PlayerIntelligenceProfile
    {
        public PlayerIntelligenceProfile(
            bool available,
            string safeMessage,
            string statlynPlayerId,
            string displayName,
            string position,
            string role,
            string source,
            int? age,
            string nationality,
            string dataQuality,
            int confidence,
            int? roleFit,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> requiredFieldsMissing)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Position = position ?? "Unknown";
            Role = role ?? "Not assessed";
            Source = source ?? string.Empty;
            Age = age;
            Nationality = nationality ?? "Unknown";
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RoleFit = roleFit;
            Warnings = warnings ?? new List<string>();
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string StatlynPlayerId { get; }

        public string DisplayName { get; }

        public string Position { get; }

        public string Role { get; }

        public string Source { get; }

        public int? Age { get; }

        public string Nationality { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public int? RoleFit { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public static PlayerIntelligenceProfile Missing(string statlynPlayerId, string safeMessage)
        {
            return new PlayerIntelligenceProfile(
                false,
                safeMessage,
                statlynPlayerId,
                string.Empty,
                "Unknown",
                "Not assessed",
                string.Empty,
                null,
                "Unknown",
                "Unavailable",
                0,
                null,
                new List<string>(),
                new[] { "safe player profile" });
        }
    }

    public sealed class PlayerRadarAxis
    {
        public PlayerRadarAxis(string axisKey, string label, double? value, double? benchmarkValue, string sourceMetric, string dataQuality, int confidence)
        {
            AxisKey = axisKey ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value;
            BenchmarkValue = benchmarkValue;
            SourceMetric = sourceMetric ?? string.Empty;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
        }

        public string AxisKey { get; }

        public string Label { get; }

        public double? Value { get; }

        public double? BenchmarkValue { get; }

        public string SourceMetric { get; }

        public string DataQuality { get; }

        public int Confidence { get; }
    }

    public sealed class PlayerSkillRadar
    {
        public PlayerSkillRadar(
            bool available,
            string safeMessage,
            string profileType,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings,
            IReadOnlyList<PlayerRadarAxis> axes)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            ProfileType = profileType ?? "Role-specific";
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Axes = axes ?? new List<PlayerRadarAxis>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string ProfileType { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<PlayerRadarAxis> Axes { get; }
    }

    public sealed class PlayerPer90Metric
    {
        public PlayerPer90Metric(string metricKey, string label, double value, string unit, int minutes, string dataQuality, int confidence)
        {
            MetricKey = metricKey ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value;
            Unit = unit ?? "per 90";
            Minutes = minutes;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
        }

        public string MetricKey { get; }

        public string Label { get; }

        public double Value { get; }

        public string Unit { get; }

        public int Minutes { get; }

        public string DataQuality { get; }

        public int Confidence { get; }
    }

    public sealed class PlayerPer90Summary
    {
        public PlayerPer90Summary(
            bool available,
            string safeMessage,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings,
            IReadOnlyList<PlayerPer90Metric> metrics)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Metrics = metrics ?? new List<PlayerPer90Metric>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<PlayerPer90Metric> Metrics { get; }
    }

    public sealed class PlayerHeatmapPoint
    {
        public PlayerHeatmapPoint(string matchId, double minute, double x, double y, string actionType, int confidence)
        {
            MatchId = matchId ?? string.Empty;
            Minute = minute;
            X = x;
            Y = y;
            ActionType = actionType ?? string.Empty;
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
        }

        public string MatchId { get; }

        public double Minute { get; }

        public double X { get; }

        public double Y { get; }

        public string ActionType { get; }

        public int Confidence { get; }
    }

    public sealed class PlayerHeatmapSummary
    {
        public PlayerHeatmapSummary(
            bool available,
            string safeMessage,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings,
            IReadOnlyList<PlayerHeatmapPoint> points)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Points = points ?? new List<PlayerHeatmapPoint>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<PlayerHeatmapPoint> Points { get; }
    }

    public sealed class PlayerValueEstimate
    {
        public PlayerValueEstimate(
            bool available,
            string safeMessage,
            double? fairValueLow,
            double? fairValueMid,
            double? fairValueHigh,
            string currency,
            double? valueIndex,
            int confidence,
            string dataQuality,
            IReadOnlyList<string> keyValueDrivers,
            IReadOnlyList<string> keyDiscountDrivers,
            IReadOnlyList<string> missingInputs,
            string modelVersion)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            FairValueLow = fairValueLow;
            FairValueMid = fairValueMid;
            FairValueHigh = fairValueHigh;
            Currency = currency ?? string.Empty;
            ValueIndex = valueIndex;
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            DataQuality = dataQuality ?? "Unknown";
            KeyValueDrivers = keyValueDrivers ?? new List<string>();
            KeyDiscountDrivers = keyDiscountDrivers ?? new List<string>();
            MissingInputs = missingInputs ?? new List<string>();
            ModelVersion = modelVersion ?? "statlyn-fair-value-v0.1";
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public double? FairValueLow { get; }

        public double? FairValueMid { get; }

        public double? FairValueHigh { get; }

        public string Currency { get; }

        public double? ValueIndex { get; }

        public int Confidence { get; }

        public string DataQuality { get; }

        public IReadOnlyList<string> KeyValueDrivers { get; }

        public IReadOnlyList<string> KeyDiscountDrivers { get; }

        public IReadOnlyList<string> MissingInputs { get; }

        public string ModelVersion { get; }
    }

    public sealed class PlayerFitProjection
    {
        public PlayerFitProjection(
            bool available,
            string safeMessage,
            string dataQuality,
            int confidence,
            string roleFitSummary,
            string teamStyleSummary,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RoleFitSummary = roleFitSummary ?? string.Empty;
            TeamStyleSummary = teamStyleSummary ?? string.Empty;
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public string RoleFitSummary { get; }

        public string TeamStyleSummary { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PlayerArchetypeResult
    {
        public PlayerArchetypeResult(
            bool available,
            string safeMessage,
            string archetype,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> evidenceMetrics,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            Archetype = archetype ?? "Unavailable";
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            EvidenceMetrics = evidenceMetrics ?? new List<string>();
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string Archetype { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> EvidenceMetrics { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class SimilarPlayerCandidate
    {
        public SimilarPlayerCandidate(string statlynPlayerId, string displayName, string role, double similarityScore, int confidence, string dataQuality)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Role = role ?? string.Empty;
            SimilarityScore = similarityScore;
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            DataQuality = dataQuality ?? "Unknown";
        }

        public string StatlynPlayerId { get; }

        public string DisplayName { get; }

        public string Role { get; }

        public double SimilarityScore { get; }

        public int Confidence { get; }

        public string DataQuality { get; }
    }

    public sealed class PlayerSimilarityResult
    {
        public PlayerSimilarityResult(
            bool available,
            string safeMessage,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings,
            IReadOnlyList<SimilarPlayerCandidate> candidates)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Candidates = candidates ?? new List<SimilarPlayerCandidate>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<SimilarPlayerCandidate> Candidates { get; }
    }

    public sealed class LeagueAverageComparison
    {
        public LeagueAverageComparison(
            bool available,
            string safeMessage,
            string leagueKey,
            string comparisonGroup,
            int sampleSize,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings,
            IReadOnlyList<PlayerRadarAxis> comparisons)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            LeagueKey = leagueKey ?? string.Empty;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            SampleSize = sampleSize;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Comparisons = comparisons ?? new List<PlayerRadarAxis>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string LeagueKey { get; }

        public string ComparisonGroup { get; }

        public int SampleSize { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<PlayerRadarAxis> Comparisons { get; }
    }

    public sealed class RoleParameterMetric
    {
        public RoleParameterMetric(string metricKey, string label, string category, bool required, int minimumMinutes)
        {
            MetricKey = metricKey ?? string.Empty;
            Label = label ?? string.Empty;
            Category = category ?? string.Empty;
            Required = required;
            MinimumMinutes = minimumMinutes;
        }

        public string MetricKey { get; }

        public string Label { get; }

        public string Category { get; }

        public bool Required { get; }

        public int MinimumMinutes { get; }
    }

    public sealed class RoleParameterDefinition
    {
        public RoleParameterDefinition(
            string roleName,
            string roleFamily,
            IReadOnlyList<RoleParameterMetric> primaryMetrics,
            IReadOnlyList<RoleParameterMetric> secondaryMetrics,
            IReadOnlyList<RoleParameterMetric> riskMetrics,
            IReadOnlyList<string> styleTraits,
            int minimumMinutes,
            IReadOnlyList<string> unavailableConditions)
        {
            RoleName = roleName ?? string.Empty;
            RoleFamily = roleFamily ?? string.Empty;
            PrimaryMetrics = primaryMetrics ?? new List<RoleParameterMetric>();
            SecondaryMetrics = secondaryMetrics ?? new List<RoleParameterMetric>();
            RiskMetrics = riskMetrics ?? new List<RoleParameterMetric>();
            StyleTraits = styleTraits ?? new List<string>();
            MinimumMinutes = minimumMinutes;
            UnavailableConditions = unavailableConditions ?? new List<string>();
        }

        public string RoleName { get; }

        public string RoleFamily { get; }

        public IReadOnlyList<RoleParameterMetric> PrimaryMetrics { get; }

        public IReadOnlyList<RoleParameterMetric> SecondaryMetrics { get; }

        public IReadOnlyList<RoleParameterMetric> RiskMetrics { get; }

        public IReadOnlyList<string> StyleTraits { get; }

        public int MinimumMinutes { get; }

        public IReadOnlyList<string> UnavailableConditions { get; }
    }

    public sealed class RoleSpecificAssessment
    {
        public RoleSpecificAssessment(
            bool available,
            string safeMessage,
            string roleName,
            string dataQuality,
            int confidence,
            IReadOnlyList<string> requiredFieldsMissing,
            IReadOnlyList<string> warnings,
            RoleParameterDefinition? definition)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            RoleName = roleName ?? "Not assessed";
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
            RequiredFieldsMissing = requiredFieldsMissing ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Definition = definition;
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public string RoleName { get; }

        public string DataQuality { get; }

        public int Confidence { get; }

        public IReadOnlyList<string> RequiredFieldsMissing { get; }

        public IReadOnlyList<string> Warnings { get; }

        public RoleParameterDefinition? Definition { get; }
    }

    public sealed class PlayerStyleVector
    {
        public PlayerStyleVector(string vectorKey, IReadOnlyDictionary<string, double> metrics, int minutes, string dataQuality, int confidence)
        {
            VectorKey = vectorKey ?? string.Empty;
            Metrics = metrics ?? new Dictionary<string, double>();
            Minutes = minutes;
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
        }

        public string VectorKey { get; }

        public IReadOnlyDictionary<string, double> Metrics { get; }

        public int Minutes { get; }

        public string DataQuality { get; }

        public int Confidence { get; }
    }

    public sealed class TeamStyleModel
    {
        public TeamStyleModel(string teamStyleId, string displayName, string roleFocus, IReadOnlyDictionary<string, double> parameters, string dataQuality, int confidence)
        {
            TeamStyleId = teamStyleId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            RoleFocus = roleFocus ?? string.Empty;
            Parameters = parameters ?? new Dictionary<string, double>();
            DataQuality = dataQuality ?? "Unknown";
            Confidence = PlayerDataAvailabilityReport.ClampPercent(confidence);
        }

        public string TeamStyleId { get; }

        public string DisplayName { get; }

        public string RoleFocus { get; }

        public IReadOnlyDictionary<string, double> Parameters { get; }

        public string DataQuality { get; }

        public int Confidence { get; }
    }

    public sealed class PlayerIntelligenceResult
    {
        public PlayerIntelligenceResult(
            PlayerIntelligenceProfile profile,
            PlayerSkillRadar radar,
            PlayerPer90Summary per90,
            PlayerHeatmapSummary heatmap,
            PlayerValueEstimate valueEstimate,
            PlayerFitProjection fitProjection,
            PlayerArchetypeResult archetype,
            PlayerSimilarityResult similarPlayers,
            LeagueAverageComparison leagueComparison,
            RoleSpecificAssessment roleAssessment)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Radar = radar ?? throw new ArgumentNullException(nameof(radar));
            Per90 = per90 ?? throw new ArgumentNullException(nameof(per90));
            Heatmap = heatmap ?? throw new ArgumentNullException(nameof(heatmap));
            ValueEstimate = valueEstimate ?? throw new ArgumentNullException(nameof(valueEstimate));
            FitProjection = fitProjection ?? throw new ArgumentNullException(nameof(fitProjection));
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            SimilarPlayers = similarPlayers ?? throw new ArgumentNullException(nameof(similarPlayers));
            LeagueComparison = leagueComparison ?? throw new ArgumentNullException(nameof(leagueComparison));
            RoleAssessment = roleAssessment ?? throw new ArgumentNullException(nameof(roleAssessment));
        }

        public PlayerIntelligenceProfile Profile { get; }

        public PlayerSkillRadar Radar { get; }

        public PlayerPer90Summary Per90 { get; }

        public PlayerHeatmapSummary Heatmap { get; }

        public PlayerValueEstimate ValueEstimate { get; }

        public PlayerFitProjection FitProjection { get; }

        public PlayerArchetypeResult Archetype { get; }

        public PlayerSimilarityResult SimilarPlayers { get; }

        public LeagueAverageComparison LeagueComparison { get; }

        public RoleSpecificAssessment RoleAssessment { get; }
    }

    public sealed class PlayerIntelligenceReadiness
    {
        public PlayerIntelligenceReadiness(
            bool available,
            string safeMessage,
            int importedPlayers,
            int eventLocationRows,
            int marketContextRows,
            int teamStyleRows,
            int leagueAverageRows,
            int styleVectorRows,
            IReadOnlyList<string> warnings)
        {
            Available = available;
            SafeMessage = safeMessage ?? string.Empty;
            ImportedPlayers = importedPlayers;
            EventLocationRows = eventLocationRows;
            MarketContextRows = marketContextRows;
            TeamStyleRows = teamStyleRows;
            LeagueAverageRows = leagueAverageRows;
            StyleVectorRows = styleVectorRows;
            Warnings = warnings ?? new List<string>();
        }

        public bool Available { get; }

        public string SafeMessage { get; }

        public int ImportedPlayers { get; }

        public int EventLocationRows { get; }

        public int MarketContextRows { get; }

        public int TeamStyleRows { get; }

        public int LeagueAverageRows { get; }

        public int StyleVectorRows { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
