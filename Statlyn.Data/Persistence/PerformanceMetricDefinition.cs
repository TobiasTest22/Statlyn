using System.Collections.Generic;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class PerformanceMetricDefinition
    {
        public PerformanceMetricDefinition(
            string metricKey,
            string displayName,
            string description,
            PlayerFieldKey fieldKey,
            string fieldName,
            ProviderType providerType,
            bool isGenericFootballMetric,
            bool isVerifiedFm26Metric,
            bool isPer90Capable,
            string defaultUnit,
            bool higherIsBetter,
            bool lowerIsBetter,
            bool requiresMinutes,
            int minimumMinutesRecommended,
            IReadOnlyList<string> positionGroups,
            IReadOnlyList<string> roleFamilies,
            int sourceConfidenceRequired,
            bool canScore,
            bool canStore,
            string notes)
        {
            MetricKey = metricKey ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            FieldKey = fieldKey;
            FieldName = fieldName ?? string.Empty;
            ProviderType = providerType;
            IsGenericFootballMetric = isGenericFootballMetric;
            IsVerifiedFm26Metric = isVerifiedFm26Metric;
            IsPer90Capable = isPer90Capable;
            DefaultUnit = defaultUnit ?? string.Empty;
            HigherIsBetter = higherIsBetter;
            LowerIsBetter = lowerIsBetter;
            RequiresMinutes = requiresMinutes;
            MinimumMinutesRecommended = minimumMinutesRecommended;
            PositionGroups = positionGroups ?? new List<string>();
            RoleFamilies = roleFamilies ?? new List<string>();
            SourceConfidenceRequired = sourceConfidenceRequired;
            CanScore = canScore;
            CanStore = canStore;
            Notes = notes ?? string.Empty;
        }

        public string MetricKey { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public PlayerFieldKey FieldKey { get; }

        public string FieldName { get; }

        public ProviderType ProviderType { get; }

        public bool IsGenericFootballMetric { get; }

        public bool IsVerifiedFm26Metric { get; }

        public bool IsPer90Capable { get; }

        public string DefaultUnit { get; }

        public bool HigherIsBetter { get; }

        public bool LowerIsBetter { get; }

        public bool RequiresMinutes { get; }

        public int MinimumMinutesRecommended { get; }

        public IReadOnlyList<string> PositionGroups { get; }

        public IReadOnlyList<string> RoleFamilies { get; }

        public int SourceConfidenceRequired { get; }

        public bool CanScore { get; }

        public bool CanStore { get; }

        public string Notes { get; }
    }
}
