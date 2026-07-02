using System.Collections.Generic;

namespace Statlyn.UI.Visuals
{
    public sealed class RiskVisual
    {
        public RiskVisual(int score, string label, IReadOnlyList<string> mainRiskReasons, bool isLowSample, bool isLowConfidence, bool hasBlockedFields)
        {
            Score = score;
            Label = label ?? string.Empty;
            MainRiskReasons = mainRiskReasons ?? new List<string>();
            IsLowSample = isLowSample;
            IsLowConfidence = isLowConfidence;
            HasBlockedFields = hasBlockedFields;
        }

        public int Score { get; }

        public string Label { get; }

        public IReadOnlyList<string> MainRiskReasons { get; }

        public bool IsLowSample { get; }

        public bool IsLowConfidence { get; }

        public bool HasBlockedFields { get; }
    }
}
