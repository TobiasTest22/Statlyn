using System.Collections.Generic;

namespace Statlyn.Analytics
{
    public sealed class RoleScore
    {
        public RoleScore(
            string roleName,
            int roleFit,
            int confidence,
            RecruitmentRecommendation recommendation,
            IReadOnlyList<EvidenceItem> positiveEvidence,
            IReadOnlyList<EvidenceItem> negativeEvidence,
            IReadOnlyList<string> missingData)
        {
            RoleName = roleName ?? string.Empty;
            RoleFit = roleFit;
            Confidence = confidence;
            Recommendation = recommendation;
            PositiveEvidence = positiveEvidence;
            NegativeEvidence = negativeEvidence;
            MissingData = missingData;
        }

        public string RoleName { get; }

        public int RoleFit { get; }

        public int Confidence { get; }

        public RecruitmentRecommendation Recommendation { get; }

        public IReadOnlyList<EvidenceItem> PositiveEvidence { get; }

        public IReadOnlyList<EvidenceItem> NegativeEvidence { get; }

        public IReadOnlyList<string> MissingData { get; }
    }
}
