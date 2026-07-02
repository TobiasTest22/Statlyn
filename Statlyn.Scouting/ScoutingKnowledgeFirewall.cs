using System;
using System.Collections.Generic;
using Statlyn.Core;

namespace Statlyn.Scouting
{
    public sealed class ScoutingKnowledgeFirewall
    {
        private const int AttributeKnowledgeThreshold = 50;

        public MaskedPlayer Mask(PlayerRawSnapshot raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException(nameof(raw));
            }

            var attributes = new Dictionary<string, VisibleField<int>>(StringComparer.OrdinalIgnoreCase);
            var facts = new Dictionary<string, VisibleField<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var fact in raw.VisibleFacts)
            {
                facts[fact.Key] = VisibleField<string>.Known(
                    fact.Key,
                    fact.Value,
                    canScore: false,
                    confidence: raw.SourceConfidence,
                    category: FieldVisibilityCategory.AlwaysVisible,
                    sourceProvider: raw.SourceProvider);
            }

            foreach (var attribute in raw.VisibleAttributes)
            {
                if (CanDisplayAttribute(raw, attribute.Key) && attribute.Value.HasValue)
                {
                    attributes[attribute.Key] = VisibleField<int>.Known(
                        attribute.Key,
                        attribute.Value.Value,
                        canScore: true,
                        confidence: CalculateFieldConfidence(raw),
                        category: FieldVisibilityCategory.VisibleIfScouted,
                        sourceProvider: raw.SourceProvider);
                }
                else
                {
                    attributes[attribute.Key] = VisibleField<int>.Unknown(
                        attribute.Key,
                        FieldVisibilityCategory.VisibleIfScouted,
                        raw.SourceProvider,
                        "Not scouted or not visible in the current FM26 knowledge state.");
                }
            }

            var confidence = CalculateMaskedConfidence(raw, attributes);

            return new MaskedPlayer(
                statlynPlayerId: raw.SourceProvider + ":" + raw.SourcePlayerId,
                displayName: raw.DisplayName,
                sourceProvider: raw.SourceProvider,
                providerType: raw.ProviderType,
                scoutKnowledgePercentage: raw.ScoutKnowledgePercentage,
                confidence: confidence,
                attributes: attributes,
                facts: facts);
        }

        private static bool CanDisplayAttribute(PlayerRawSnapshot raw, string attributeName)
        {
            if (raw.ProviderType != ProviderType.FM26LiveMemory)
            {
                return true;
            }

            if (raw.IsManagedClubPlayer)
            {
                return true;
            }

            if (!raw.HasScoutReport)
            {
                return false;
            }

            if (raw.ScoutKnowledgePercentage < AttributeKnowledgeThreshold)
            {
                return raw.VisibleAttributeNames.Contains(attributeName);
            }

            return raw.VisibleAttributeNames.Contains(attributeName);
        }

        private static int CalculateFieldConfidence(PlayerRawSnapshot raw)
        {
            if (raw.ProviderType == ProviderType.FM26LiveMemory)
            {
                return Clamp((raw.ScoutKnowledgePercentage + raw.SourceConfidence) / 2);
            }

            return Clamp(raw.SourceConfidence);
        }

        private static int CalculateMaskedConfidence(PlayerRawSnapshot raw, IDictionary<string, VisibleField<int>> attributes)
        {
            var expected = attributes.Count == 0 ? 1 : attributes.Count;
            var known = 0;
            var confidenceSum = 0;

            foreach (var field in attributes.Values)
            {
                if (field.IsKnown && field.CanScore)
                {
                    known++;
                    confidenceSum += field.Confidence;
                }
            }

            var completeness = (int)Math.Round((double)known / expected * 100.0);
            var averageFieldConfidence = known == 0 ? 0 : confidenceSum / known;
            return Clamp((completeness + averageFieldConfidence + raw.SourceConfidence) / 3);
        }

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
