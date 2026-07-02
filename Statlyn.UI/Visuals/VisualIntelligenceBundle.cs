using System.Collections.Generic;

namespace Statlyn.UI.Visuals
{
    public sealed class VisualIntelligenceBundle
    {
        public VisualIntelligenceBundle(
            IReadOnlyList<RadarMetric> radarMetrics,
            IReadOnlyList<PercentileBar> percentileBars,
            RoleFitVisual roleFitVisual,
            ConfidenceVisual confidenceVisual,
            RiskVisual riskVisual,
            IReadOnlyList<EvidenceCard> evidenceCards,
            IReadOnlyList<TrendVisual> trendVisuals,
            IReadOnlyList<ComparisonCard> comparisonCards,
            IReadOnlyList<MissingDataWarning> missingDataWarnings,
            BlockedDataNoticeView blockedDataNotice)
        {
            RadarMetrics = radarMetrics;
            PercentileBars = percentileBars;
            RoleFitVisual = roleFitVisual;
            ConfidenceVisual = confidenceVisual;
            RiskVisual = riskVisual;
            EvidenceCards = evidenceCards;
            TrendVisuals = trendVisuals;
            ComparisonCards = comparisonCards;
            MissingDataWarnings = missingDataWarnings;
            BlockedDataNotice = blockedDataNotice;
        }

        public IReadOnlyList<RadarMetric> RadarMetrics { get; }

        public IReadOnlyList<PercentileBar> PercentileBars { get; }

        public RoleFitVisual RoleFitVisual { get; }

        public ConfidenceVisual ConfidenceVisual { get; }

        public RiskVisual RiskVisual { get; }

        public IReadOnlyList<EvidenceCard> EvidenceCards { get; }

        public IReadOnlyList<TrendVisual> TrendVisuals { get; }

        public IReadOnlyList<ComparisonCard> ComparisonCards { get; }

        public IReadOnlyList<MissingDataWarning> MissingDataWarnings { get; }

        public BlockedDataNoticeView BlockedDataNotice { get; }
    }
}
