using System.Collections.Generic;
using System.Globalization;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentrePlayerRowViewModel
    {
        public RecruitmentCentrePlayerRowViewModel(
            string statlynPlayerId,
            string name,
            string age,
            string nationality,
            string position,
            string source,
            string sourceConfidence,
            string dataCompleteness,
            string roleFit,
            string confidence,
            string recommendation,
            string risk,
            IReadOnlyList<string> keyOutputMetrics,
            int blockedFieldCount,
            int missingDataCount,
            IReadOnlyList<string> warnings,
            bool isFixtureMode,
            bool isLiveFm26Data)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            Name = name ?? string.Empty;
            Age = age ?? string.Empty;
            Nationality = nationality ?? string.Empty;
            Position = position ?? string.Empty;
            Source = source ?? string.Empty;
            SourceConfidence = sourceConfidence ?? string.Empty;
            DataCompleteness = dataCompleteness ?? string.Empty;
            RoleFit = roleFit ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            Risk = risk ?? string.Empty;
            KeyOutputMetrics = keyOutputMetrics ?? new List<string>();
            BlockedFieldCount = blockedFieldCount;
            MissingDataCount = missingDataCount;
            Warnings = warnings ?? new List<string>();
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
        }

        public string StatlynPlayerId { get; }

        public string Name { get; }

        public string Age { get; }

        public string Nationality { get; }

        public string Position { get; }

        public string Source { get; }

        public string SourceConfidence { get; }

        public string DataCompleteness { get; }

        public string RoleFit { get; }

        public string Confidence { get; }

        public string Recommendation { get; }

        public string Risk { get; }

        public IReadOnlyList<string> KeyOutputMetrics { get; }

        public int BlockedFieldCount { get; }

        public int MissingDataCount { get; }

        public IReadOnlyList<string> Warnings { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public static RecruitmentCentrePlayerRowViewModel From(RecruitmentCentrePlayerRow row)
        {
            return new RecruitmentCentrePlayerRowViewModel(
                row.StatlynPlayerId,
                row.DisplayName,
                row.AgeDisplay,
                row.Nationality,
                row.PrimaryPosition,
                row.SourceName,
                row.SourceConfidence.ToString(CultureInfo.InvariantCulture) + "%",
                row.DataCompleteness.ToString(CultureInfo.InvariantCulture) + "%",
                row.RoleFit.HasValue ? row.RoleFit.Value.ToString(CultureInfo.InvariantCulture) : "Not scored",
                row.Confidence.HasValue ? row.Confidence.Value.ToString(CultureInfo.InvariantCulture) : "Unknown",
                row.Recommendation.HasValue ? row.Recommendation.Value.ToString() : "Not scored",
                row.RiskScore.HasValue ? row.RiskScore.Value.ToString(CultureInfo.InvariantCulture) : "Unknown",
                row.KeyOutputMetrics,
                row.BlockedFieldCount,
                row.MissingDataCount,
                row.KeyWarnings,
                row.IsFixtureMode,
                row.IsLiveFm26Data);
        }
    }
}
