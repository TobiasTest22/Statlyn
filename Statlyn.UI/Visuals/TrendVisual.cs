using System.Collections.Generic;

namespace Statlyn.UI.Visuals
{
    public sealed class TrendVisual
    {
        public TrendVisual(string label, IReadOnlyList<double> points, string timeRange, string sourceName, bool isAvailable, string missingReason)
        {
            Label = label ?? string.Empty;
            Points = points ?? new List<double>();
            TimeRange = timeRange ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            IsAvailable = isAvailable;
            MissingReason = missingReason ?? string.Empty;
        }

        public string Label { get; }

        public IReadOnlyList<double> Points { get; }

        public string TimeRange { get; }

        public string SourceName { get; }

        public bool IsAvailable { get; }

        public string MissingReason { get; }
    }
}
