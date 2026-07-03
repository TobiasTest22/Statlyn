using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class PerformanceMetricAlias
    {
        public PerformanceMetricAlias(string metricKey, ProviderType providerType, string aliasName, bool isVerifiedFm26Alias, string notes)
        {
            MetricKey = metricKey ?? string.Empty;
            ProviderType = providerType;
            AliasName = aliasName ?? string.Empty;
            IsVerifiedFm26Alias = isVerifiedFm26Alias;
            Notes = notes ?? string.Empty;
        }

        public string MetricKey { get; }

        public ProviderType ProviderType { get; }

        public string AliasName { get; }

        public bool IsVerifiedFm26Alias { get; }

        public string Notes { get; }
    }
}
