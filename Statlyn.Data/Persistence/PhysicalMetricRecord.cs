namespace Statlyn.Data.Persistence
{
    public sealed class PhysicalMetricRecord
    {
        public PhysicalMetricRecord(long playerId, string fieldInstanceKey, string metricName, double metricValue, string unit, string sourceName, int confidence)
        {
            PlayerId = playerId;
            FieldInstanceKey = fieldInstanceKey ?? string.Empty;
            MetricName = metricName ?? string.Empty;
            MetricValue = metricValue;
            Unit = unit ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            Confidence = confidence;
        }

        public long PlayerId { get; }

        public string FieldInstanceKey { get; }

        public string MetricName { get; }

        public double MetricValue { get; }

        public string Unit { get; }

        public string SourceName { get; }

        public int Confidence { get; }
    }
}
