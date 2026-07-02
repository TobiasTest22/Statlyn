namespace Statlyn.UI.Visuals
{
    public sealed class RadarMetric
    {
        public RadarMetric(string label, double value, double maximumValue, int confidence, string sourceName, bool isMissing, string missingReason)
        {
            Label = label ?? string.Empty;
            Value = value;
            MaximumValue = maximumValue <= 0 ? 100 : maximumValue;
            Confidence = Clamp(confidence);
            SourceName = sourceName ?? string.Empty;
            IsMissing = isMissing;
            MissingReason = missingReason ?? string.Empty;
        }

        public string Label { get; }

        public double Value { get; }

        public double MaximumValue { get; }

        public int Confidence { get; }

        public string SourceName { get; }

        public bool IsMissing { get; }

        public string MissingReason { get; }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }
}
