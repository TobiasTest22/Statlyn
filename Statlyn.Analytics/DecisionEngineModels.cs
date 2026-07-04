using System.Collections.Generic;

namespace Statlyn.Analytics
{
    public enum DecisionStatus
    {
        Available = 0,
        InsufficientData = 1,
        Unsupported = 2
    }

    public sealed class DecisionEvidence
    {
        public DecisionEvidence(string label, string message, bool isPositive)
        {
            Label = label ?? string.Empty;
            Message = message ?? string.Empty;
            IsPositive = isPositive;
        }

        public string Label { get; }

        public string Message { get; }

        public bool IsPositive { get; }
    }

    public sealed class DecisionResult
    {
        public DecisionResult(
            DecisionStatus status,
            string safeSummary,
            int? score,
            int confidence,
            IReadOnlyList<DecisionEvidence> evidence,
            IReadOnlyList<string> missingData,
            IReadOnlyList<string> warnings)
        {
            Status = status;
            SafeSummary = safeSummary ?? string.Empty;
            Score = score;
            Confidence = confidence < 0 ? 0 : confidence > 100 ? 100 : confidence;
            Evidence = evidence ?? new List<DecisionEvidence>();
            MissingData = missingData ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public DecisionStatus Status { get; }

        public string SafeSummary { get; }

        public int? Score { get; }

        public int Confidence { get; }

        public IReadOnlyList<DecisionEvidence> Evidence { get; }

        public IReadOnlyList<string> MissingData { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class PlayerComparisonInput
    {
        public PlayerComparisonInput(string label, int? roleFit, int? confidence, int missingDataCount, int blockedFieldCount)
        {
            Label = label ?? string.Empty;
            RoleFit = roleFit;
            Confidence = confidence;
            MissingDataCount = missingDataCount < 0 ? 0 : missingDataCount;
            BlockedFieldCount = blockedFieldCount < 0 ? 0 : blockedFieldCount;
        }

        public string Label { get; }

        public int? RoleFit { get; }

        public int? Confidence { get; }

        public int MissingDataCount { get; }

        public int BlockedFieldCount { get; }
    }
}
