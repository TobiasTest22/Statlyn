namespace Statlyn.Core
{
    public sealed class FieldVisibilityEvaluator
    {
        private readonly FieldPolicyRegistry _registry;

        public FieldVisibilityEvaluator(FieldPolicyRegistry registry)
        {
            _registry = registry;
        }

        public FieldVisibilityDecision Evaluate(RawFieldValue rawField, SourceContext sourceContext, ScoutContext scoutContext)
        {
            var resolvedKey = _registry.ResolveKey(rawField.RawName, rawField.Key);
            var normalizedField = rawField.WithKey(resolvedKey);
            var policy = _registry.GetPolicy(resolvedKey);

            if (resolvedKey == PlayerFieldKey.Unknown)
            {
                return Block(normalizedField, policy, "Unknown fields are denied by default.");
            }

            if (policy.IsFm26HiddenValue || policy.VisibilityCategory == FieldVisibilityCategory.NeverVisible)
            {
                return Block(normalizedField, policy, policy.MissingReason);
            }

            if (!policy.CanDisplay && !policy.CanScore && !policy.CanStore)
            {
                return Block(normalizedField, policy, policy.MissingReason);
            }

            if (policy.RequiresLicensedSource && !sourceContext.IsLicensed)
            {
                return Block(normalizedField, policy, "This field requires a licensed, permitted or user-provided source.");
            }

            if (resolvedKey == PlayerFieldKey.PlayerFaceImage && !sourceContext.PermitsPlayerImages)
            {
                return Block(normalizedField, policy, "The source does not permit player image display.");
            }

            if (resolvedKey == PlayerFieldKey.NationalityFlag && !sourceContext.PermitsProviderFlags && !sourceContext.UsesBundledSafeFlagAssets)
            {
                return Block(normalizedField, policy, "The source does not permit flags and no bundled safe flag asset is available.");
            }

            if (resolvedKey == PlayerFieldKey.ClubBadge && !sourceContext.PermitsClubBadges)
            {
                return Block(normalizedField, policy, "The source does not permit club badge display.");
            }

            if (sourceContext.ProviderType == ProviderType.FM26LiveMemory && policy.RequiresScoutReport && !scoutContext.HasScoutReport && !scoutContext.IsManagedClubPlayer)
            {
                return Unknown(normalizedField, policy, "Requires an in-game scout report or managed-club visibility.");
            }

            if (sourceContext.ProviderType == ProviderType.FM26LiveMemory && scoutContext.ScoutKnowledgePercentage < policy.MinimumScoutKnowledge && !scoutContext.IsManagedClubPlayer)
            {
                return Unknown(normalizedField, policy, "Requires scout knowledge of at least " + policy.MinimumScoutKnowledge + "%.");
            }

            if (!normalizedField.IsKnown || normalizedField.Value == null)
            {
                return Unknown(normalizedField, policy, policy.MissingReason);
            }

            return new FieldVisibilityDecision(
                normalizedField,
                policy,
                FieldDecisionKind.Known,
                policy.CanDisplay,
                policy.CanScore,
                policy.CanStore,
                string.Empty);
        }

        private static FieldVisibilityDecision Block(RawFieldValue rawField, FieldPolicy policy, string reason)
        {
            return new FieldVisibilityDecision(rawField, policy, FieldDecisionKind.Blocked, false, false, false, reason);
        }

        private static FieldVisibilityDecision Unknown(RawFieldValue rawField, FieldPolicy policy, string reason)
        {
            return new FieldVisibilityDecision(rawField, policy, FieldDecisionKind.Unknown, false, false, false, reason);
        }
    }
}
