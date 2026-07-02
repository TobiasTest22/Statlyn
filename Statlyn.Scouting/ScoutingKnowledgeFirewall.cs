using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Core;

namespace Statlyn.Scouting
{
    public sealed class ScoutingKnowledgeFirewall
    {
        private readonly FieldPolicyRegistry _policyRegistry;
        private readonly FieldVisibilityEvaluator _visibilityEvaluator;

        public ScoutingKnowledgeFirewall()
            : this(new FieldPolicyRegistry())
        {
        }

        public ScoutingKnowledgeFirewall(FieldPolicyRegistry policyRegistry)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
            _visibilityEvaluator = new FieldVisibilityEvaluator(_policyRegistry);
        }

        public MaskedPlayer Mask(PlayerRawSnapshot raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException(nameof(raw));
            }

            var attributes = new Dictionary<string, VisibleField<int>>(StringComparer.OrdinalIgnoreCase);
            var facts = new Dictionary<string, VisibleField<string>>(StringComparer.OrdinalIgnoreCase);
            var fields = new Dictionary<PlayerFieldKey, VisiblePlayerField>();
            var blockedFields = new List<BlockedFieldNotice>();
            var normalizedFields = NormalizeFields(raw);

            foreach (var rawField in normalizedFields)
            {
                var decision = _visibilityEvaluator.Evaluate(rawField, raw.SourceContext, raw.ScoutContext);
                if (decision.DecisionKind == FieldDecisionKind.Blocked)
                {
                    blockedFields.Add(new BlockedFieldNotice(decision.RawField.Key, decision.RawField.RawName, decision.Reason, raw.SourceProvider));
                    continue;
                }

                var visibleField = ToVisibleField(decision, raw);
                fields[MakeUniqueFieldKey(fields, visibleField.Key)] = visibleField;

                if (decision.DecisionKind == FieldDecisionKind.Known && visibleField.CanDisplay)
                {
                    facts[visibleField.FieldName] = VisibleField<string>.Known(
                        visibleField.FieldName,
                        visibleField.DisplayValue,
                        canScore: visibleField.CanScore,
                        confidence: visibleField.Confidence,
                        category: decision.Policy.VisibilityCategory,
                        sourceProvider: raw.SourceProvider);
                }

                if (decision.RawField.Key == PlayerFieldKey.TechnicalAttribute && decision.RawField.NumericValue.HasValue)
                {
                    if (decision.DecisionKind == FieldDecisionKind.Known && visibleField.CanScore)
                    {
                        attributes[visibleField.FieldName] = VisibleField<int>.Known(
                            visibleField.FieldName,
                            (int)Math.Round(decision.RawField.NumericValue.Value),
                            canScore: true,
                            confidence: visibleField.Confidence,
                            category: decision.Policy.VisibilityCategory,
                            sourceProvider: raw.SourceProvider);
                    }
                    else if (decision.DecisionKind == FieldDecisionKind.Unknown)
                    {
                        attributes[visibleField.FieldName] = VisibleField<int>.Unknown(
                            visibleField.FieldName,
                            decision.Policy.VisibilityCategory,
                            raw.SourceProvider,
                            visibleField.MissingReason);
                    }
                }
                else if (decision.RawField.Key == PlayerFieldKey.TechnicalAttribute && decision.DecisionKind == FieldDecisionKind.Unknown)
                {
                    attributes[visibleField.FieldName] = VisibleField<int>.Unknown(
                        visibleField.FieldName,
                        decision.Policy.VisibilityCategory,
                        raw.SourceProvider,
                        visibleField.MissingReason);
                }
            }

            var confidence = CalculateMaskedConfidence(raw, fields.Values, blockedFields);

            return new MaskedPlayer(
                statlynPlayerId: raw.SourceProvider + ":" + raw.SourcePlayerId,
                displayName: raw.DisplayName,
                sourceProvider: raw.SourceProvider,
                providerType: raw.ProviderType,
                scoutKnowledgePercentage: raw.ScoutKnowledgePercentage,
                confidence: confidence,
                fields: fields,
                blockedFields: blockedFields,
                attributes: attributes,
                facts: facts);
        }

        private IEnumerable<RawFieldValue> NormalizeFields(PlayerRawSnapshot raw)
        {
            var fields = new List<RawFieldValue>();

            if (!string.IsNullOrWhiteSpace(raw.SourcePlayerId))
            {
                fields.Add(new RawFieldValue(PlayerFieldKey.SourcePlayerId, "SourcePlayerId", raw.SourcePlayerId, FieldValueKind.Text, raw.SourceConfidence));
            }

            if (!string.IsNullOrWhiteSpace(raw.DisplayName))
            {
                fields.Add(new RawFieldValue(PlayerFieldKey.DisplayName, "DisplayName", raw.DisplayName, FieldValueKind.Text, raw.SourceConfidence));
            }

            foreach (var field in raw.Fields.Values)
            {
                fields.Add(field);
            }

            foreach (var fact in raw.VisibleFacts)
            {
                var key = _policyRegistry.ResolveKey(fact.Key, PlayerFieldKey.Unknown);
                fields.Add(new RawFieldValue(key, fact.Key, fact.Value, FieldValueKind.Text, raw.SourceConfidence));
            }

            foreach (var attribute in raw.VisibleAttributes)
            {
                var known = raw.ProviderType != ProviderType.FM26LiveMemory
                    || raw.IsManagedClubPlayer
                    || raw.VisibleAttributeNames.Contains(attribute.Key);

                fields.Add(new RawFieldValue(
                    PlayerFieldKey.TechnicalAttribute,
                    attribute.Key,
                    known ? attribute.Value : null,
                    FieldValueKind.Number,
                    CalculateFieldConfidence(raw),
                    isKnown: known && attribute.Value.HasValue));
            }

            return fields;
        }

        private static VisiblePlayerField ToVisibleField(FieldVisibilityDecision decision, PlayerRawSnapshot raw)
        {
            var isKnown = decision.DecisionKind == FieldDecisionKind.Known;
            return new VisiblePlayerField(
                decision.RawField.Key,
                decision.RawField.RawName,
                isKnown ? decision.RawField.DisplayValue : string.Empty,
                isKnown ? decision.RawField.NumericValue : null,
                decision.RawField.ValueKind,
                isKnown,
                isBlocked: false,
                decision.CanDisplay,
                decision.CanScore,
                decision.CanStore,
                isKnown ? CalculateFieldConfidence(raw, decision.RawField) : 0,
                raw.SourceProvider,
                isKnown ? string.Empty : decision.Reason);
        }

        private static PlayerFieldKey MakeUniqueFieldKey(IDictionary<PlayerFieldKey, VisiblePlayerField> fields, PlayerFieldKey key)
        {
            if (!fields.ContainsKey(key))
            {
                return key;
            }

            // Grouped fields such as technical attributes remain addressable by name through Attributes.
            return key;
        }

        private static int CalculateFieldConfidence(PlayerRawSnapshot raw)
        {
            return CalculateFieldConfidence(raw, null);
        }

        private static int CalculateFieldConfidence(PlayerRawSnapshot raw, RawFieldValue? rawField)
        {
            var fieldConfidence = rawField == null ? raw.SourceConfidence : rawField.Confidence;
            if (raw.ProviderType == ProviderType.FM26LiveMemory)
            {
                return Clamp((raw.ScoutKnowledgePercentage + raw.SourceConfidence + fieldConfidence) / 3);
            }

            return Clamp((raw.SourceConfidence + fieldConfidence) / 2);
        }

        private static int CalculateMaskedConfidence(PlayerRawSnapshot raw, IEnumerable<VisiblePlayerField> fields, IReadOnlyCollection<BlockedFieldNotice> blockedFields)
        {
            var fieldList = fields.ToList();
            var expected = fieldList.Count == 0 ? 1 : fieldList.Count;
            var known = 0;
            var confidenceSum = 0;

            foreach (var field in fieldList)
            {
                if (field.IsKnown)
                {
                    known++;
                    confidenceSum += field.Confidence;
                }
            }

            var completeness = (int)Math.Round((double)known / expected * 100.0);
            var averageFieldConfidence = known == 0 ? 0 : confidenceSum / known;
            var providerFactor = raw.ProviderType == ProviderType.FM26LiveMemory ? raw.ScoutKnowledgePercentage : raw.SourceConfidence;
            var blockedPenalty = Math.Min(25, blockedFields.Count * 5);
            return Clamp(((completeness + averageFieldConfidence + raw.SourceConfidence + providerFactor) / 4) - blockedPenalty);
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
