using System;
using System.Collections.Generic;
using Statlyn.Core.Abstractions;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public sealed class PlayerProfileReportViewModel
    {
        internal PlayerProfileReportViewModel(
            string statlynPlayerId,
            string playerName,
            string age,
            string nationality,
            string primaryPosition,
            string positionGroup,
            string sourceName,
            string sourceConfidence,
            string dataCompleteness,
            bool isFixtureMode,
            bool isLiveFm26Data,
            string roleName,
            string roleFit,
            string outputFitLabel,
            string confidence,
            string risk,
            string recommendation,
            string tacticalFitDisplay,
            IReadOnlyList<PlayerProfileMetricTileViewModel> coreOutputMetrics,
            IReadOnlyList<PlayerProfileMetricTileViewModel> supportingOutputMetrics,
            IReadOnlyList<string> missingOutputMetrics,
            IReadOnlyList<PlayerProfileMetricTileViewModel> physicalOutputMetrics,
            IReadOnlyList<string> keyWarnings,
            IReadOnlyList<PlayerProfileRoleEvidenceViewModel> evidenceCards,
            IReadOnlyList<PlayerProfileDataQualityViewModel> dataQualityCards,
            IReadOnlyList<PlayerProfileAttributeSupportViewModel> attributeSupportCards,
            IReadOnlyList<PlayerProfileScoutActionViewModel> scoutActionCards,
            PlayerProfileBlockedDataViewModel blockedDataNotice,
            IReadOnlyList<PlayerProfileVisualSectionViewModel> visualSections)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            Age = age ?? string.Empty;
            Nationality = nationality ?? string.Empty;
            PrimaryPosition = primaryPosition ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceConfidence = sourceConfidence ?? string.Empty;
            DataCompleteness = dataCompleteness ?? string.Empty;
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
            RoleName = roleName ?? string.Empty;
            RoleFit = roleFit ?? string.Empty;
            OutputFitLabel = outputFitLabel ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Risk = risk ?? string.Empty;
            Recommendation = recommendation ?? string.Empty;
            TacticalFitDisplay = tacticalFitDisplay ?? "Unknown";
            CoreOutputMetrics = coreOutputMetrics ?? new List<PlayerProfileMetricTileViewModel>();
            SupportingOutputMetrics = supportingOutputMetrics ?? new List<PlayerProfileMetricTileViewModel>();
            MissingOutputMetrics = missingOutputMetrics ?? new List<string>();
            PhysicalOutputMetrics = physicalOutputMetrics ?? new List<PlayerProfileMetricTileViewModel>();
            KeyWarnings = keyWarnings ?? new List<string>();
            EvidenceCards = evidenceCards ?? new List<PlayerProfileRoleEvidenceViewModel>();
            DataQualityCards = dataQualityCards ?? new List<PlayerProfileDataQualityViewModel>();
            AttributeSupportCards = attributeSupportCards ?? new List<PlayerProfileAttributeSupportViewModel>();
            ScoutActionCards = scoutActionCards ?? new List<PlayerProfileScoutActionViewModel>();
            BlockedDataNotice = blockedDataNotice ?? new PlayerProfileBlockedDataViewModel(0, new List<string>(), new List<string>(), "No blocked data is loaded for this profile.");
            VisualSections = visualSections ?? new List<PlayerProfileVisualSectionViewModel>();
        }

        public string StatlynPlayerId { get; }

        public string PlayerName { get; }

        public string Age { get; }

        public string Nationality { get; }

        public string PrimaryPosition { get; }

        public string PositionGroup { get; }

        public string SourceName { get; }

        public string SourceConfidence { get; }

        public string DataCompleteness { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public string RoleName { get; }

        public string RoleFit { get; }

        public string OutputFitLabel { get; }

        public string Confidence { get; }

        public string Risk { get; }

        public string Recommendation { get; }

        public string TacticalFitDisplay { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> CoreOutputMetrics { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> SupportingOutputMetrics { get; }

        public IReadOnlyList<string> MissingOutputMetrics { get; }

        public IReadOnlyList<PlayerProfileMetricTileViewModel> PhysicalOutputMetrics { get; }

        public IReadOnlyList<string> KeyWarnings { get; }

        public IReadOnlyList<PlayerProfileRoleEvidenceViewModel> EvidenceCards { get; }

        public IReadOnlyList<PlayerProfileDataQualityViewModel> DataQualityCards { get; }

        public IReadOnlyList<PlayerProfileAttributeSupportViewModel> AttributeSupportCards { get; }

        public IReadOnlyList<PlayerProfileScoutActionViewModel> ScoutActionCards { get; }

        public PlayerProfileBlockedDataViewModel BlockedDataNotice { get; }

        public IReadOnlyList<PlayerProfileVisualSectionViewModel> VisualSections { get; }

        public static PlayerProfileReportViewModel From(object source)
        {
            if (source is IRawFootballEntity)
            {
                throw new InvalidOperationException("Player Profile report cannot be created from raw provider data.");
            }

            if (!(source is PlayerProfileResult result))
            {
                throw new InvalidOperationException("Player Profile report requires a persisted-safe PlayerProfileResult.");
            }

            return From(result);
        }

        public static PlayerProfileReportViewModel From(PlayerProfileResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return PlayerProfileReportBuilder.Build(result);
        }
    }
}
