using System.Collections.Generic;

namespace Statlyn.Analytics
{
    public sealed class RoleScore
    {
        public RoleScore(
            string roleName,
            int roleFit,
            int technicalFit,
            int statisticalFit,
            int physicalFit,
            int tacticalFit,
            int riskScore,
            int confidence,
            RecruitmentRecommendation recommendation,
            IReadOnlyList<EvidenceItem> positiveEvidence,
            IReadOnlyList<EvidenceItem> negativeEvidence,
            IReadOnlyList<string> missingData,
            string blockedDataNotice)
        {
            RoleName = roleName ?? string.Empty;
            RoleFit = roleFit;
            TechnicalFit = technicalFit;
            StatisticalFit = statisticalFit;
            PhysicalFit = physicalFit;
            TacticalFit = tacticalFit;
            RiskScore = riskScore;
            Confidence = confidence;
            Recommendation = recommendation;
            PositiveEvidence = positiveEvidence;
            NegativeEvidence = negativeEvidence;
            MissingData = missingData;
            BlockedDataNotice = blockedDataNotice ?? string.Empty;
        }

        public string RoleName { get; }

        public int RoleFit { get; }

        public int TechnicalFit { get; }

        public int StatisticalFit { get; }

        public int PhysicalFit { get; }

        public int TacticalFit { get; }

        public int RiskScore { get; }

        public int Confidence { get; }

        public RecruitmentRecommendation Recommendation { get; }

        public IReadOnlyList<EvidenceItem> PositiveEvidence { get; }

        public IReadOnlyList<EvidenceItem> NegativeEvidence { get; }

        public IReadOnlyList<string> MissingData { get; }

        public string BlockedDataNotice { get; }
    }
}
