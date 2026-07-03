namespace Statlyn.Data.Persistence
{
    public sealed class MetricExpectation
    {
        public MetricExpectation(
            string metricKey,
            string fieldName,
            double weight,
            string importance,
            string direction,
            int minimumSampleMinutes,
            bool per90Required,
            string normalizationHint,
            string evidenceTemplate,
            string missingDataImpact)
        {
            MetricKey = metricKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Weight = weight;
            Importance = importance ?? string.Empty;
            Direction = direction ?? string.Empty;
            MinimumSampleMinutes = minimumSampleMinutes;
            Per90Required = per90Required;
            NormalizationHint = normalizationHint ?? string.Empty;
            EvidenceTemplate = evidenceTemplate ?? string.Empty;
            MissingDataImpact = missingDataImpact ?? string.Empty;
        }

        public string MetricKey { get; }

        public string FieldName { get; }

        public double Weight { get; }

        public string Importance { get; }

        public string Direction { get; }

        public int MinimumSampleMinutes { get; }

        public bool Per90Required { get; }

        public string NormalizationHint { get; }

        public string EvidenceTemplate { get; }

        public string MissingDataImpact { get; }
    }
}
