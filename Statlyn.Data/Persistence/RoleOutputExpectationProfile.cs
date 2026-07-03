using System.Collections.Generic;

namespace Statlyn.Data.Persistence
{
    public sealed class RoleOutputExpectationProfile
    {
        public RoleOutputExpectationProfile(
            string profileName,
            string positionGroup,
            string roleFamily,
            string tacticalPhase,
            bool isFm26Specific,
            bool isGenericTemplate,
            IReadOnlyList<MetricExpectation> metricExpectations,
            string attributeSupportWeights,
            string scoutQuestionPrompts,
            string redFlagRules,
            string minimumSampleRules,
            string notes)
        {
            ProfileName = profileName ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            RoleFamily = roleFamily ?? string.Empty;
            TacticalPhase = tacticalPhase ?? string.Empty;
            IsFm26Specific = isFm26Specific;
            IsGenericTemplate = isGenericTemplate;
            MetricExpectations = metricExpectations ?? new List<MetricExpectation>();
            AttributeSupportWeights = attributeSupportWeights ?? string.Empty;
            ScoutQuestionPrompts = scoutQuestionPrompts ?? string.Empty;
            RedFlagRules = redFlagRules ?? string.Empty;
            MinimumSampleRules = minimumSampleRules ?? string.Empty;
            Notes = notes ?? string.Empty;
        }

        public string ProfileName { get; }

        public string PositionGroup { get; }

        public string RoleFamily { get; }

        public string TacticalPhase { get; }

        public bool IsFm26Specific { get; }

        public bool IsGenericTemplate { get; }

        public IReadOnlyList<MetricExpectation> MetricExpectations { get; }

        public string AttributeSupportWeights { get; }

        public string ScoutQuestionPrompts { get; }

        public string RedFlagRules { get; }

        public string MinimumSampleRules { get; }

        public string Notes { get; }
    }
}
