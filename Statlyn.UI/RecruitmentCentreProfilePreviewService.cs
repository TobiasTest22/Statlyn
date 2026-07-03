using System;
using Statlyn.Analytics;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders;

namespace Statlyn.UI
{
    public sealed class RecruitmentCentreProfilePreviewService
    {
        private readonly PersistedMaskedPlayerLoader _loader;

        public RecruitmentCentreProfilePreviewService(StatlynDbConnectionFactory connectionFactory)
        {
            _loader = new PersistedMaskedPlayerLoader(connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory)));
        }

        public MaskedPlayerProfileViewModel? LoadProfile(string statlynPlayerId)
        {
            var loaded = _loader.LoadByStatlynPlayerId(statlynPlayerId, "Generic performance preview");
            if (loaded == null)
            {
                return null;
            }

            var roleScore = loaded.LatestRoleScore ?? CreateNotScoredRoleScore(loaded.Completeness);
            return MaskedPlayerProfileViewModel.From(loaded.MaskedPlayer, roleScore, loaded.SourceMetadata, loaded.Completeness);
        }

        private static RoleScore CreateNotScoredRoleScore(DataCompletenessReport completeness)
        {
            return new RoleScore(
                "Not scored",
                0,
                0,
                0,
                0,
                null,
                0,
                0,
                RecruitmentRecommendation.ScoutFurther,
                Array.Empty<EvidenceItem>(),
                Array.Empty<EvidenceItem>(),
                completeness == null ? Array.Empty<string>() : completeness.MissingFields,
                "No role score is stored for this persisted player yet.");
        }
    }
}
