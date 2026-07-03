using System;
using System.Globalization;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Recruitment;
using Statlyn.DataProviders;

namespace Statlyn.UI
{
    public sealed class RecruitmentCentreProfilePreviewService
    {
        private readonly RoleOutputExpectationRepository _roleOutputExpectations;
        private readonly PersistedMaskedPlayerLoader _loader;
        private readonly RecruitmentOutputSummaryService _summaryService;

        public RecruitmentCentreProfilePreviewService(StatlynDbConnectionFactory connectionFactory)
        {
            connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _roleOutputExpectations = new RoleOutputExpectationRepository(connectionFactory);
            _loader = new PersistedMaskedPlayerLoader(connectionFactory);
            _summaryService = new RecruitmentOutputSummaryService();
        }

        public MaskedPlayerProfileViewModel? LoadProfile(string statlynPlayerId)
        {
            var preview = LoadProfilePreview(statlynPlayerId);
            return preview == null ? null : preview.MaskedProfile;
        }

        public RecruitmentCentreProfilePreviewViewModel? LoadProfilePreview(string statlynPlayerId)
        {
            var loaded = _loader.LoadByStatlynPlayerId(statlynPlayerId);
            if (loaded == null)
            {
                return null;
            }

            var roleScore = loaded.LatestRoleScore ?? CreateNotScoredRoleScore(loaded.Completeness);
            var maskedProfile = MaskedPlayerProfileViewModel.From(loaded.MaskedPlayer, roleScore, loaded.SourceMetadata, loaded.Completeness);
            var positionGroup = RecruitmentOutputSummaryService.ResolvePositionGroup(maskedProfile.PositionDisplay);
            var profile = _summaryService.SelectProfile(positionGroup, string.Empty, _roleOutputExpectations.LoadAll());
            var summary = _summaryService.Build(maskedProfile.PositionDisplay, loaded.PlayerStats, loaded.PhysicalMetrics, profile, roleScore);
            var outputMetrics = summary.CoreMetrics.Concat(summary.SupportingMetrics).Take(6).ToList();
            if (outputMetrics.Count == 0)
            {
                outputMetrics.Add("Output metrics missing");
            }

            var blockedNotice = maskedProfile.BlockedDataNotice.Count == 0
                ? maskedProfile.BlockedDataNotice.SafeMessage
                : maskedProfile.BlockedDataNotice.Count.ToString(CultureInfo.InvariantCulture) + " blocked field category/categories excluded safely. Raw values are not shown.";
            var missingWarning = summary.MissingCoreMetrics.Count == 0 && maskedProfile.MissingDataWarnings.Count == 0
                ? "No core output metrics are missing from the persisted preview."
                : summary.ConfidenceImpactText;

            return new RecruitmentCentreProfilePreviewViewModel(
                maskedProfile,
                maskedProfile.PlayerName,
                maskedProfile.SourceName,
                maskedProfile.IsFixtureMode ? "Fixture/import mode" : "Persisted import mode",
                maskedProfile.IsFixtureMode,
                maskedProfile.IsLiveFm26Data,
                RoleNameSanitizer.SanitizeForDisplay(roleScore.RoleName, loaded.LatestRoleScore == null ? "Not scored" : "Unknown role"),
                roleScore.RoleFit.ToString(CultureInfo.InvariantCulture),
                roleScore.Confidence.ToString(CultureInfo.InvariantCulture),
                roleScore.RiskScore.ToString(CultureInfo.InvariantCulture),
                outputMetrics,
                missingWarning,
                blockedNotice);
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
