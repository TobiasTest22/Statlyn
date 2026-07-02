namespace Statlyn.UI.Visuals
{
    public sealed class PercentileBar
    {
        public PercentileBar(string label, double value, int percentile, string comparisonGroup, int confidence, string sourceName, bool isMissing, string missingReason)
        {
            Label = label ?? string.Empty;
            Value = value;
            Percentile = percentile < 0 ? 0 : percentile > 100 ? 100 : percentile;
            ComparisonGroup = comparisonGroup ?? string.Empty;
            Confidence = confidence < 0 ? 0 : confidence > 100 ? 100 : confidence;
            SourceName = sourceName ?? string.Empty;
            IsMissing = isMissing;
            MissingReason = missingReason ?? string.Empty;
        }

        public string Label { get; }

        public double Value { get; }

        public int Percentile { get; }

        public string ComparisonGroup { get; }

        public int Confidence { get; }

        public string SourceName { get; }

        public bool IsMissing { get; }

        public string MissingReason { get; }
    }
}
