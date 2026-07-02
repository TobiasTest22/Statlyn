using Statlyn.Analytics;

namespace Statlyn.UI.Visuals
{
    public sealed class RoleFitVisual
    {
        public RoleFitVisual(string roleName, int score, int confidence, RecruitmentRecommendation recommendation, string statusLabel, string missingDataWarning)
        {
            RoleName = roleName ?? string.Empty;
            Score = score;
            Confidence = confidence;
            Recommendation = recommendation;
            StatusLabel = statusLabel ?? string.Empty;
            MissingDataWarning = missingDataWarning ?? string.Empty;
        }

        public string RoleName { get; }

        public int Score { get; }

        public int Confidence { get; }

        public RecruitmentRecommendation Recommendation { get; }

        public string StatusLabel { get; }

        public string MissingDataWarning { get; }
    }
}
