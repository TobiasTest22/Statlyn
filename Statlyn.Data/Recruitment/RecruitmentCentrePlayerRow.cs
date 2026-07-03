using System.Collections.Generic;
using Statlyn.Analytics;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentrePlayerRow
    {
        public RecruitmentCentrePlayerRow(
            string statlynPlayerId,
            string displayName,
            string ageDisplay,
            string nationality,
            string positionGroup,
            string primaryPosition,
            string sourceName,
            int sourceConfidence,
            int dataCompleteness,
            string latestRoleName,
            int? roleFit,
            int? technicalFit,
            int? statisticalFit,
            int? physicalFit,
            string tacticalFitDisplay,
            int? riskScore,
            int? confidence,
            RecruitmentRecommendation? recommendation,
            int blockedFieldCount,
            int missingDataCount,
            IReadOnlyList<string> keyOutputMetrics,
            IReadOnlyList<string> keyWarnings,
            bool isFixtureMode,
            bool isLiveFm26Data)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AgeDisplay = ageDisplay ?? string.Empty;
            Nationality = nationality ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            PrimaryPosition = primaryPosition ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceConfidence = sourceConfidence;
            DataCompleteness = dataCompleteness;
            LatestRoleName = string.IsNullOrWhiteSpace(latestRoleName) ? "Not scored" : latestRoleName;
            RoleFit = roleFit;
            TechnicalFit = technicalFit;
            StatisticalFit = statisticalFit;
            PhysicalFit = physicalFit;
            TacticalFitDisplay = tacticalFitDisplay ?? "Unknown";
            RiskScore = riskScore;
            Confidence = confidence;
            Recommendation = recommendation;
            BlockedFieldCount = blockedFieldCount;
            MissingDataCount = missingDataCount;
            KeyOutputMetrics = keyOutputMetrics ?? new List<string>();
            KeyWarnings = keyWarnings ?? new List<string>();
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
        }

        public string StatlynPlayerId { get; }

        public string DisplayName { get; }

        public string AgeDisplay { get; }

        public string Nationality { get; }

        public string PositionGroup { get; }

        public string PrimaryPosition { get; }

        public string SourceName { get; }

        public int SourceConfidence { get; }

        public int DataCompleteness { get; }

        public string LatestRoleName { get; }

        public int? RoleFit { get; }

        public int? TechnicalFit { get; }

        public int? StatisticalFit { get; }

        public int? PhysicalFit { get; }

        public string TacticalFitDisplay { get; }

        public int? RiskScore { get; }

        public int? Confidence { get; }

        public RecruitmentRecommendation? Recommendation { get; }

        public int BlockedFieldCount { get; }

        public int MissingDataCount { get; }

        public IReadOnlyList<string> KeyOutputMetrics { get; }

        public IReadOnlyList<string> KeyWarnings { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }
    }
}
