namespace Statlyn.Core
{
    public sealed class FieldVisibilityDecision
    {
        public FieldVisibilityDecision(
            RawFieldValue rawField,
            FieldPolicy policy,
            FieldDecisionKind decisionKind,
            bool canDisplay,
            bool canScore,
            bool canStore,
            string reason)
        {
            RawField = rawField;
            Policy = policy;
            DecisionKind = decisionKind;
            CanDisplay = canDisplay;
            CanScore = canScore;
            CanStore = canStore;
            Reason = reason ?? string.Empty;
        }

        public RawFieldValue RawField { get; }

        public FieldPolicy Policy { get; }

        public FieldDecisionKind DecisionKind { get; }

        public bool CanDisplay { get; }

        public bool CanScore { get; }

        public bool CanStore { get; }

        public string Reason { get; }
    }
}
