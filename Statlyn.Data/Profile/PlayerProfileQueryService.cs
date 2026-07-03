using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;
using Statlyn.Data.Recruitment;
using Statlyn.Data.RoleLab;
using Statlyn.DataProviders;

namespace Statlyn.Data.Profile
{
    public sealed class PlayerProfileQueryService
    {
        private readonly PersistedMaskedPlayerLoader _loader;
        private readonly RoleOutputExpectationRepository _roleOutputExpectations;
        private readonly RoleLabOutputProfileBridge _roleLabBridge;
        private readonly RecruitmentOutputSummaryService _summaryService;
        private readonly BenchmarkWorkflowService _benchmarkWorkflow;

        public PlayerProfileQueryService(StatlynDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            _loader = new PersistedMaskedPlayerLoader(connectionFactory);
            _roleOutputExpectations = new RoleOutputExpectationRepository(connectionFactory);
            _roleLabBridge = new RoleLabOutputProfileBridge(connectionFactory);
            _summaryService = new RecruitmentOutputSummaryService();
            _benchmarkWorkflow = new BenchmarkWorkflowService(connectionFactory);
        }

        public PlayerProfileResult Query(PlayerProfileQuery? query)
        {
            query = query ?? new PlayerProfileQuery();
            if (string.IsNullOrWhiteSpace(query.StatlynPlayerId))
            {
                return PlayerProfileResult.NotFound(string.Empty);
            }

            var loaded = _loader.LoadByStatlynPlayerId(query.StatlynPlayerId);
            if (loaded == null)
            {
                return PlayerProfileResult.NotFound(query.StatlynPlayerId);
            }

            var roleScore = loaded.LatestRoleScore ?? CreateNotScoredRoleScore(loaded.Completeness, query.OptionalRoleName);
            var position = FindPrimaryPosition(loaded);
            var positionGroup = RecruitmentOutputSummaryService.ResolvePositionGroup(position);
            var persistedProfiles = _roleOutputExpectations.LoadAll();
            var selectedProfile = SelectProfile(query.OptionalRoleOutputProfileName, positionGroup, roleScore.RoleName, persistedProfiles);
            var summary = _summaryService.Build(position, loaded.PlayerStats, loaded.PhysicalMetrics, selectedProfile, roleScore);
            var benchmarkSummary = _benchmarkWorkflow.BuildPlayerBenchmarkSummary(loaded.Player.StatlynPlayerId);
            var warnings = BuildWarnings(loaded.PlayerStats, summary, loaded.MaskedPlayer.BlockedFields.Count, roleScore, loaded.SourceMetadata);
            var diagnostics = new List<string>
            {
                "Player Profile loaded persisted safe SQLite data only.",
                "No raw provider snapshots, hidden FM26 values or blocked raw values are returned.",
                selectedProfile == null
                    ? "Role output profile unavailable; output summary used no position-specific profile."
                    : selectedProfile.IsGenericTemplate
                        ? "Role output profile is generic/import-only and not FM26-verified."
                        : "Role output profile loaded from persisted SQLite data."
            };

            return PlayerProfileResult.Found(
                loaded.Player,
                loaded.SourceMetadata,
                loaded.MaskedPlayer,
                roleScore,
                summary,
                selectedProfile,
                loaded.PlayerStats,
                loaded.PhysicalMetrics,
                query.IncludeAttributes ? loaded.MaskedPlayer.Fields.Values.Where(field => field.CanDisplay).ToList() : new List<VisiblePlayerField>(),
                query.IncludeBlockedAudit ? loaded.MaskedPlayer.BlockedFields : new List<BlockedFieldNotice>(),
                loaded.Completeness,
                diagnostics,
                warnings,
                IsFixture(loaded.SourceMetadata),
                loaded.SourceMetadata.ProviderType == ProviderType.FM26LiveMemory && loaded.SourceMetadata.IsLive,
                metricsAreFm26Verified: false,
                benchmarkSummary);
        }

        private RoleOutputExpectationProfile? SelectProfile(
            string explicitProfileName,
            string positionGroup,
            string roleName,
            IReadOnlyList<RoleOutputExpectationProfile> persistedProfiles)
        {
            if (!string.IsNullOrWhiteSpace(explicitProfileName))
            {
                var roleLabProfile = _roleLabBridge.FindSelectedProfile(explicitProfileName);
                if (roleLabProfile != null)
                {
                    return roleLabProfile;
                }

                var explicitProfile = _roleOutputExpectations.FindByName(explicitProfileName);
                if (explicitProfile != null)
                {
                    return explicitProfile;
                }
            }

            return _summaryService.SelectProfile(positionGroup, roleName, persistedProfiles);
        }

        private static RoleScore CreateNotScoredRoleScore(DataCompletenessReport completeness, string optionalRoleName)
        {
            var safeRoleName = RoleNameSanitizer.SanitizeForDisplay(optionalRoleName, "Not scored");
            return new RoleScore(
                string.IsNullOrWhiteSpace(safeRoleName) ? "Not scored" : safeRoleName,
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

        private static string FindPrimaryPosition(PersistedMaskedPlayerData loaded)
        {
            foreach (var field in loaded.MaskedPlayer.Fields.Values)
            {
                if (field.Key == PlayerFieldKey.PrimaryPosition && field.IsKnown && field.CanDisplay && !string.IsNullOrWhiteSpace(field.DisplayValue))
                {
                    return field.DisplayValue;
                }
            }

            return string.Empty;
        }

        private static IReadOnlyList<string> BuildWarnings(
            IReadOnlyList<PlayerStatRecord> stats,
            RecruitmentOutputSummary summary,
            int blockedCount,
            RoleScore roleScore,
            SourceMetadata source)
        {
            var warnings = new List<string>();
            if (summary.MissingCoreMetrics.Count > 0)
            {
                warnings.Add("Missing core output metrics lower confidence: " + string.Join(", ", summary.MissingCoreMetrics.Take(4)) + ".");
            }

            if (stats.Count == 0 || stats.All(stat => stat.SampleMinutesMissing))
            {
                warnings.Add("Sample minutes are missing; per-90 interpretation is provisional.");
            }
            else
            {
                warnings.Add("Sample minutes available: " + stats.Max(stat => stat.Minutes).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
            }

            if (blockedCount > 0)
            {
                warnings.Add(blockedCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " blocked field audit notice(s) are included without raw values.");
            }

            if (roleScore.TacticalFit == null)
            {
                warnings.Add("Tactical fit is Unknown.");
            }

            if (source.SourceConfidence < 70)
            {
                warnings.Add("Source confidence is low; review source/licence context.");
            }

            return warnings;
        }

        private static bool IsFixture(SourceMetadata source)
        {
            return source.SourceName.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   source.AllowedUsage.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   source.ProviderType == ProviderType.Csv;
        }
    }
}
