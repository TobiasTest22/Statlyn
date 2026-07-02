namespace Statlyn.Core
{
    public sealed class ScoutingReportSnapshot
    {
        public ScoutingReportSnapshot(string sourcePlayerId, string recommendation, int confidence, string notes)
        {
            SourcePlayerId = sourcePlayerId ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            Confidence = Clamp(confidence);
            Notes = notes ?? string.Empty;
        }

        public string SourcePlayerId { get; }

        public string Recommendation { get; }

        public int Confidence { get; }

        public string Notes { get; }

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
