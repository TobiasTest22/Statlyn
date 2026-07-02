using System;
using System.Collections.Generic;
using Statlyn.Core.Abstractions;

namespace Statlyn.Core
{
    public sealed class PlayerRawSnapshot : IRawFootballEntity
    {
        public PlayerRawSnapshot(string sourcePlayerId, string sourceProvider, ProviderType providerType)
        {
            SourcePlayerId = sourcePlayerId ?? throw new ArgumentNullException(nameof(sourcePlayerId));
            SourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
            ProviderType = providerType;
            SourceContext = Statlyn.Core.SourceContext.ForProvider(sourceProvider, providerType, 100);
            ScoutContext = Statlyn.Core.ScoutContext.Unknown;
            Fields = new Dictionary<PlayerFieldKey, RawFieldValue>();
            VisibleFacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            VisibleAttributes = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            HiddenAttributes = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            VisibleAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public string SourcePlayerId { get; }

        public string SourceProvider { get; }

        public ProviderType ProviderType { get; }

        public string DisplayName { get; set; } = string.Empty;

        public bool IsManagedClubPlayer
        {
            get { return ScoutContext.IsManagedClubPlayer; }
            set { ScoutContext = new ScoutContext(value, ScoutKnowledgePercentage, HasScoutReport); }
        }

        public int ScoutKnowledgePercentage
        {
            get { return ScoutContext.ScoutKnowledgePercentage; }
            set { ScoutContext = new ScoutContext(IsManagedClubPlayer, value, HasScoutReport); }
        }

        public bool HasScoutReport
        {
            get { return ScoutContext.HasScoutReport; }
            set { ScoutContext = new ScoutContext(IsManagedClubPlayer, ScoutKnowledgePercentage, value); }
        }

        public int SourceConfidence
        {
            get { return SourceContext.SourceConfidence; }
            set
            {
                SourceContext = new SourceContext(
                    SourceContext.SourceName,
                    SourceProvider,
                    ProviderType,
                    SourceContext.IsLicensed,
                    SourceContext.AllowsPlayerImages,
                    SourceContext.AllowsNationalityFlags,
                    SourceContext.UsesBundledSafeFlagAssets,
                    value,
                    SourceContext.AllowedUsage);
            }
        }

        public SourceContext SourceContext { get; set; }

        public ScoutContext ScoutContext { get; set; }

        public IDictionary<PlayerFieldKey, RawFieldValue> Fields { get; }

        public int? HiddenCurrentAbility { get; set; }

        public int? HiddenPotentialAbility { get; set; }

        // Transitional compatibility for the initial milestone. New providers should use Fields.
        public IDictionary<string, string> VisibleFacts { get; }

        // Transitional compatibility for the initial milestone. New providers should use Fields.
        public IDictionary<string, int?> VisibleAttributes { get; }

        // Raw hidden values remain provider-only and must never be routed to UI, scoring or storage.
        public IDictionary<string, int?> HiddenAttributes { get; }

        public ISet<string> VisibleAttributeNames { get; }
    }
}
