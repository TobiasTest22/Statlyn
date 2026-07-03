using System.Collections.Generic;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data.Persistence;
using Statlyn.Data.Recruitment;
using Statlyn.DataProviders;

namespace Statlyn.Data.Profile
{
    public sealed class PlayerProfileResult
    {
        private PlayerProfileResult(
            bool success,
            string safeMessage,
            StoredPlayerRecord? player,
            SourceMetadata? sourceMetadata,
            MaskedPlayer? maskedPlayer,
            RoleScore? latestRoleScore,
            RecruitmentOutputSummary? roleOutputSummary,
            RoleOutputExpectationProfile? roleOutputExpectationProfile,
            IReadOnlyList<PlayerStatRecord> playerStats,
            IReadOnlyList<PhysicalMetricRecord> physicalMetrics,
            IReadOnlyList<VisiblePlayerField> visibleFields,
            IReadOnlyList<BlockedFieldNotice> blockedFields,
            DataCompletenessReport? dataCompleteness,
            IReadOnlyList<string> diagnostics,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors,
            bool isFixtureMode,
            bool isLiveFm26Data,
            string tacticalFitDisplay,
            bool metricsAreFm26Verified)
        {
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            Player = player;
            SourceMetadata = sourceMetadata;
            MaskedPlayer = maskedPlayer;
            LatestRoleScore = latestRoleScore;
            RoleOutputSummary = roleOutputSummary;
            RoleOutputExpectationProfile = roleOutputExpectationProfile;
            PlayerStats = playerStats ?? new List<PlayerStatRecord>();
            PhysicalMetrics = physicalMetrics ?? new List<PhysicalMetricRecord>();
            VisibleFields = visibleFields ?? new List<VisiblePlayerField>();
            BlockedFields = blockedFields ?? new List<BlockedFieldNotice>();
            DataCompleteness = dataCompleteness;
            Diagnostics = diagnostics ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
            TacticalFitDisplay = tacticalFitDisplay ?? "Unknown";
            MetricsAreFm26Verified = metricsAreFm26Verified;
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public StoredPlayerRecord? Player { get; }

        public SourceMetadata? SourceMetadata { get; }

        public MaskedPlayer? MaskedPlayer { get; }

        public RoleScore? LatestRoleScore { get; }

        public RecruitmentOutputSummary? RoleOutputSummary { get; }

        public RoleOutputExpectationProfile? RoleOutputExpectationProfile { get; }

        public IReadOnlyList<PlayerStatRecord> PlayerStats { get; }

        public IReadOnlyList<PhysicalMetricRecord> PhysicalMetrics { get; }

        public IReadOnlyList<VisiblePlayerField> VisibleFields { get; }

        public IReadOnlyList<BlockedFieldNotice> BlockedFields { get; }

        public DataCompletenessReport? DataCompleteness { get; }

        public IReadOnlyList<string> Diagnostics { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public string TacticalFitDisplay { get; }

        public bool MetricsAreFm26Verified { get; }

        public static PlayerProfileResult NotFound(string statlynPlayerId)
        {
            return new PlayerProfileResult(
                false,
                "Player profile was not found in persisted safe data.",
                null,
                null,
                null,
                null,
                null,
                null,
                new List<PlayerStatRecord>(),
                new List<PhysicalMetricRecord>(),
                new List<VisiblePlayerField>(),
                new List<BlockedFieldNotice>(),
                null,
                new[] { "Player Profile query searched persisted safe SQLite data only." },
                new List<string>(),
                new[] { "No persisted player matched StatlynPlayerId '" + (statlynPlayerId ?? string.Empty) + "'." },
                false,
                false,
                "Unknown",
                false);
        }

        public static PlayerProfileResult Found(
            StoredPlayerRecord player,
            SourceMetadata sourceMetadata,
            MaskedPlayer maskedPlayer,
            RoleScore latestRoleScore,
            RecruitmentOutputSummary roleOutputSummary,
            RoleOutputExpectationProfile? roleOutputExpectationProfile,
            IReadOnlyList<PlayerStatRecord> playerStats,
            IReadOnlyList<PhysicalMetricRecord> physicalMetrics,
            IReadOnlyList<VisiblePlayerField> visibleFields,
            IReadOnlyList<BlockedFieldNotice> blockedFields,
            DataCompletenessReport dataCompleteness,
            IReadOnlyList<string> diagnostics,
            IReadOnlyList<string> warnings,
            bool isFixtureMode,
            bool isLiveFm26Data,
            bool metricsAreFm26Verified)
        {
            return new PlayerProfileResult(
                true,
                "Player Profile loaded from persisted safe data.",
                player,
                sourceMetadata,
                maskedPlayer,
                latestRoleScore,
                roleOutputSummary,
                roleOutputExpectationProfile,
                playerStats,
                physicalMetrics,
                visibleFields,
                blockedFields,
                dataCompleteness,
                diagnostics,
                warnings,
                new List<string>(),
                isFixtureMode,
                isLiveFm26Data,
                latestRoleScore.TacticalFit.HasValue ? latestRoleScore.TacticalFit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "Unknown",
                metricsAreFm26Verified);
        }
    }
}
