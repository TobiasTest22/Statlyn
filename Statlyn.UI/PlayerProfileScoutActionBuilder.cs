using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileScoutActionBuilder
    {
        public static IReadOnlyList<PlayerProfileScoutActionViewModel> Build(PlayerProfileResult result)
        {
            var actions = new List<PlayerProfileScoutActionViewModel>();
            if (result.LatestRoleScore!.Confidence < 55 || result.SourceMetadata!.SourceConfidence < 70)
            {
                actions.Add(new PlayerProfileScoutActionViewModel("Scout further", "Confidence is limited.", "Collect scouting context before making a stronger call."));
            }

            if (result.RoleOutputSummary!.MissingCoreMetrics.Count > 0)
            {
                actions.Add(new PlayerProfileScoutActionViewModel("Collect missing output", "Core output metrics are missing: " + string.Join(", ", result.RoleOutputSummary.MissingCoreMetrics.Take(4)) + ".", "Scout or import the missing output before deciding."));
            }

            if (!result.MetricsAreFm26Verified)
            {
                actions.Add(new PlayerProfileScoutActionViewModel("Treat metrics as generic/import", "Metrics are not FM26-verified.", "Do not claim official FM26 support."));
            }

            actions.Add(new PlayerProfileScoutActionViewModel("Benchmark status", "No benchmark yet.", "Do not show percentiles until a real comparison group exists."));
            if (actions.Count == 1)
            {
                actions.Insert(0, new PlayerProfileScoutActionViewModel("Review role profile", "Output fit is provisional.", "Confirm the role-output profile matches the scouting question."));
            }

            return actions;
        }
    }
}
