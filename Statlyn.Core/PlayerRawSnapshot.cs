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
            VisibleFacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            VisibleAttributes = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            HiddenAttributes = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            VisibleAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public string SourcePlayerId { get; }

        public string SourceProvider { get; }

        public ProviderType ProviderType { get; }

        public string DisplayName { get; set; } = string.Empty;

        public bool IsManagedClubPlayer { get; set; }

        public int ScoutKnowledgePercentage { get; set; }

        public bool HasScoutReport { get; set; }

        public int SourceConfidence { get; set; } = 100;

        public int? HiddenCurrentAbility { get; set; }

        public int? HiddenPotentialAbility { get; set; }

        public IDictionary<string, string> VisibleFacts { get; }

        public IDictionary<string, int?> VisibleAttributes { get; }

        public IDictionary<string, int?> HiddenAttributes { get; }

        public ISet<string> VisibleAttributeNames { get; }
    }
}
