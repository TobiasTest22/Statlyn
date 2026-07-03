using System.Collections.Generic;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileEvidenceBuilder
    {
        public static IReadOnlyList<PlayerProfileRoleEvidenceViewModel> Build(PlayerProfileResult result)
        {
            var cards = new List<PlayerProfileRoleEvidenceViewModel>();
            foreach (var item in result.LatestRoleScore!.PositiveEvidence)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Positive", item.FieldName, item.Message, result.SourceMetadata!.SourceName, result.LatestRoleScore.Confidence));
            }

            foreach (var item in result.LatestRoleScore.NegativeEvidence)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Risk", item.FieldName, item.Message, result.SourceMetadata!.SourceName, result.LatestRoleScore.Confidence));
            }

            foreach (var missing in result.RoleOutputSummary!.MissingCoreMetrics)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Missing Output", missing, "Missing output lowers confidence and is not treated as zero.", result.SourceMetadata!.SourceName, 0));
            }

            if (cards.Count == 0)
            {
                cards.Add(new PlayerProfileRoleEvidenceViewModel("Evidence", "Role evidence", "Evidence is provisional until more output data and scouting context are available.", result.SourceMetadata!.SourceName, result.LatestRoleScore.Confidence));
            }

            return cards;
        }
    }
}
