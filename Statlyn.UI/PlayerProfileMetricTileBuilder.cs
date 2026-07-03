using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileMetricTileBuilder
    {
        public static IReadOnlyList<PlayerProfileMetricTileViewModel> BuildCore(PlayerProfileResult result)
        {
            return BuildFromExpectations(result, coreOnly: true).ToList();
        }

        public static IReadOnlyList<PlayerProfileMetricTileViewModel> BuildSupporting(PlayerProfileResult result)
        {
            return BuildFromExpectations(result, coreOnly: false).ToList();
        }

        public static IReadOnlyList<PlayerProfileMetricTileViewModel> BuildPhysical(PlayerProfileResult result)
        {
            return result.PhysicalMetrics
                .Select(metric => PlayerProfileMetricTileViewModel.FromPhysical(metric, "Physical Output", result.MetricsAreFm26Verified))
                .ToList();
        }

        private static IEnumerable<PlayerProfileMetricTileViewModel> BuildFromExpectations(PlayerProfileResult result, bool coreOnly)
        {
            var expectations = result.RoleOutputExpectationProfile == null
                ? new List<MetricExpectation>()
                : result.RoleOutputExpectationProfile.MetricExpectations;

            foreach (var expectation in expectations)
            {
                var isCore = string.Equals(expectation.Importance, "Core", StringComparison.OrdinalIgnoreCase);
                if (isCore != coreOnly)
                {
                    continue;
                }

                if (TryFindStat(result.PlayerStats, expectation.FieldName, out var stat))
                {
                    yield return PlayerProfileMetricTileViewModel.FromStat(stat, isCore ? "Core Role Output" : "Supporting Output", result.MetricsAreFm26Verified);
                    continue;
                }

                if (TryFindPhysical(result.PhysicalMetrics, expectation.FieldName, out var physical))
                {
                    yield return PlayerProfileMetricTileViewModel.FromPhysical(physical, isCore ? "Core Role Output" : "Supporting Output", result.MetricsAreFm26Verified);
                }
            }
        }

        private static bool TryFindStat(IReadOnlyList<PlayerStatRecord> stats, string name, out PlayerStatRecord stat)
        {
            stat = stats.FirstOrDefault(item => string.Equals(item.StatName, name, StringComparison.OrdinalIgnoreCase));
            return stat != null;
        }

        private static bool TryFindPhysical(IReadOnlyList<PhysicalMetricRecord> metrics, string name, out PhysicalMetricRecord metric)
        {
            metric = metrics.FirstOrDefault(item => string.Equals(item.MetricName, name, StringComparison.OrdinalIgnoreCase));
            return metric != null;
        }
    }
}
