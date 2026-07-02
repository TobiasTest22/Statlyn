using System;
using System.Collections.Generic;

namespace Statlyn.Core
{
    public sealed class MaskedPlayer
    {
        public MaskedPlayer(
            string statlynPlayerId,
            string displayName,
            string sourceProvider,
            ProviderType providerType,
            int scoutKnowledgePercentage,
            int confidence,
            IDictionary<PlayerFieldKey, VisiblePlayerField> fields,
            IReadOnlyList<BlockedFieldNotice> blockedFields,
            IDictionary<string, VisibleField<int>> attributes,
            IDictionary<string, VisibleField<string>> facts)
        {
            StatlynPlayerId = statlynPlayerId ?? throw new ArgumentNullException(nameof(statlynPlayerId));
            DisplayName = displayName ?? string.Empty;
            SourceProvider = sourceProvider ?? string.Empty;
            ProviderType = providerType;
            ScoutKnowledgePercentage = Clamp(scoutKnowledgePercentage);
            Confidence = Clamp(confidence);
            Fields = new Dictionary<PlayerFieldKey, VisiblePlayerField>(fields ?? new Dictionary<PlayerFieldKey, VisiblePlayerField>());
            BlockedFields = blockedFields ?? new List<BlockedFieldNotice>();
            Attributes = new Dictionary<string, VisibleField<int>>(attributes ?? new Dictionary<string, VisibleField<int>>(), StringComparer.OrdinalIgnoreCase);
            Facts = new Dictionary<string, VisibleField<string>>(facts ?? new Dictionary<string, VisibleField<string>>(), StringComparer.OrdinalIgnoreCase);
        }

        public MaskedPlayer(
            string statlynPlayerId,
            string displayName,
            string sourceProvider,
            ProviderType providerType,
            int scoutKnowledgePercentage,
            int confidence,
            IDictionary<string, VisibleField<int>> attributes,
            IDictionary<string, VisibleField<string>> facts)
            : this(
                statlynPlayerId,
                displayName,
                sourceProvider,
                providerType,
                scoutKnowledgePercentage,
                confidence,
                new Dictionary<PlayerFieldKey, VisiblePlayerField>(),
                new List<BlockedFieldNotice>(),
                attributes,
                facts)
        {
        }

        public string StatlynPlayerId { get; }

        public string DisplayName { get; }

        public string SourceProvider { get; }

        public ProviderType ProviderType { get; }

        public int ScoutKnowledgePercentage { get; }

        public int Confidence { get; }

        public IReadOnlyDictionary<PlayerFieldKey, VisiblePlayerField> Fields { get; }

        public IReadOnlyList<BlockedFieldNotice> BlockedFields { get; }

        public IReadOnlyDictionary<string, VisibleField<int>> Attributes { get; }

        public IReadOnlyDictionary<string, VisibleField<string>> Facts { get; }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }
    }
}
