using System.Collections.Generic;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentOutputSummary
    {
        public RecruitmentOutputSummary(
            string positionGroup,
            string roleFamily,
            IReadOnlyList<string> coreMetrics,
            IReadOnlyList<string> supportingMetrics,
            IReadOnlyList<string> missingCoreMetrics,
            string outputSummaryText,
            string confidenceImpactText)
        {
            PositionGroup = positionGroup ?? string.Empty;
            RoleFamily = roleFamily ?? string.Empty;
            CoreMetrics = coreMetrics ?? new List<string>();
            SupportingMetrics = supportingMetrics ?? new List<string>();
            MissingCoreMetrics = missingCoreMetrics ?? new List<string>();
            OutputSummaryText = outputSummaryText ?? string.Empty;
            ConfidenceImpactText = confidenceImpactText ?? string.Empty;
        }

        public string PositionGroup { get; }

        public string RoleFamily { get; }

        public IReadOnlyList<string> CoreMetrics { get; }

        public IReadOnlyList<string> SupportingMetrics { get; }

        public IReadOnlyList<string> MissingCoreMetrics { get; }

        public string OutputSummaryText { get; }

        public string ConfidenceImpactText { get; }
    }
}
