using Statlyn.Analytics;

namespace Statlyn.UI.Visuals
{
    public sealed class RoleFitVisual
    {
        public RoleFitVisual(
            string roleName,
            int score,
            int confidence,
            RecruitmentRecommendation recommendation,
            string statusLabel,
            string missingDataWarning,
            bool isTacticalFitUnknown = false,
            string tacticalFitLabel = "")
        {
            RoleName = roleName ?? string.Empty;
            Score = score;
            Confidence = confidence;
            Recommendation = recommendation;
            StatusLabel = statusLabel ?? string.Empty;
            MissingDataWarning = missingDataWarning ?? string.Empty;
            IsTacticalFitUnknown = isTacticalFitUnknown;
            TacticalFitLabel = string.IsNullOrWhiteSpace(tacticalFitLabel)
                ? isTacticalFitUnknown ? "Tactical fit unknown" : "Tactical fit available"
                : tacticalFitLabel;
        }

        public string RoleName { get; }

        public int Score { get; }

        public int Confidence { get; }

        public RecruitmentRecommendation Recommendation { get; }

        public string StatusLabel { get; }

        public string MissingDataWarning { get; }

        public bool IsTacticalFitUnknown { get; }

        public string TacticalFitLabel { get; }
    }
}
