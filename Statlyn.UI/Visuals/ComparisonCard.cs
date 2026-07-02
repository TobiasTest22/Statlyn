namespace Statlyn.UI.Visuals
{
    public sealed class ComparisonCard
    {
        public ComparisonCard(string comparedTo, string metric, double playerValue, double benchmarkValue, double difference, string interpretation, int confidence)
        {
            ComparedTo = comparedTo ?? string.Empty;
            Metric = metric ?? string.Empty;
            PlayerValue = playerValue;
            BenchmarkValue = benchmarkValue;
            Difference = difference;
            Interpretation = interpretation ?? string.Empty;
            Confidence = confidence;
        }

        public string ComparedTo { get; }

        public string Metric { get; }

        public double PlayerValue { get; }

        public double BenchmarkValue { get; }

        public double Difference { get; }

        public string Interpretation { get; }

        public int Confidence { get; }
    }
}
