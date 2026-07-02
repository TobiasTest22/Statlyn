namespace Statlyn.UI.Visuals
{
    public sealed class ConfidenceVisual
    {
        public ConfidenceVisual(int score, string label, string reason, int sourceConfidence, int scoutKnowledge, int dataCompleteness)
        {
            Score = score;
            Label = label ?? string.Empty;
            Reason = reason ?? string.Empty;
            SourceConfidence = sourceConfidence;
            ScoutKnowledge = scoutKnowledge;
            DataCompleteness = dataCompleteness;
        }

        public int Score { get; }

        public string Label { get; }

        public string Reason { get; }

        public int SourceConfidence { get; }

        public int ScoutKnowledge { get; }

        public int DataCompleteness { get; }
    }
}
