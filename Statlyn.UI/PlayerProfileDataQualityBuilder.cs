using System.Globalization;
using System.Linq;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileDataQualityBuilder
    {
        public static PlayerProfileDataQualityViewModel[] Build(PlayerProfileResult result)
        {
            var minutesMissing = result.PlayerStats.Count == 0 || result.PlayerStats.All(stat => stat.SampleMinutesMissing);
            var minutes = minutesMissing ? "Missing" : result.PlayerStats.Max(stat => stat.Minutes).ToString(CultureInfo.InvariantCulture);
            return new[]
            {
                new PlayerProfileDataQualityViewModel("Source", result.SourceMetadata!.SourceName, result.IsFixtureMode ? "Fixture/import mode" : "Persisted source"),
                new PlayerProfileDataQualityViewModel("Live FM26", result.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data", "FM26 unsupported until validated memory maps exist."),
                new PlayerProfileDataQualityViewModel("Metric status", result.MetricsAreFm26Verified ? "FM26 verified" : "Generic/import, not FM26-verified", "Do not treat generic metric names as official FM26 stats."),
                new PlayerProfileDataQualityViewModel("Sample minutes", minutes, minutesMissing ? "Sample-size warning: minutes missing." : "Minutes are available for output interpretation."),
                new PlayerProfileDataQualityViewModel("Missing output", result.RoleOutputSummary!.MissingCoreMetrics.Count.ToString(CultureInfo.InvariantCulture), "Missing output lowers confidence."),
                new PlayerProfileDataQualityViewModel("Blocked audit", result.BlockedFields.Count.ToString(CultureInfo.InvariantCulture), "Counts/categories only; no raw blocked values."),
                new PlayerProfileDataQualityViewModel("Tactical fit", result.TacticalFitDisplay, result.TacticalFitDisplay == "Unknown" ? "Tactical fit is unknown, not zero." : "Persisted tactical fit is available.")
            };
        }
    }
}
