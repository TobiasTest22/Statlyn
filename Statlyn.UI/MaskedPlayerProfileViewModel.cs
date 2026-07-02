using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Core.Abstractions;
using Statlyn.DataProviders;
using Statlyn.UI.Visuals;

namespace Statlyn.UI
{
    public sealed class MaskedPlayerProfileViewModel
    {
        private MaskedPlayerProfileViewModel(
            string playerName,
            AvatarDisplayMode avatarMode,
            string avatarReference,
            string initials,
            string ageDisplay,
            string nationalityDisplay,
            FlagDisplayMode flagDisplayMode,
            string flagReference,
            string clubDisplay,
            string positionDisplay,
            string sourceName,
            int sourceConfidence,
            int dataCompleteness,
            int scoutKnowledge,
            int roleFit,
            int confidence,
            int risk,
            string recommendation,
            VisualIntelligenceBundle visualBundle,
            bool isFixtureMode,
            bool isLiveFm26Data)
        {
            PlayerName = playerName;
            AvatarMode = avatarMode;
            AvatarReference = avatarReference;
            Initials = initials;
            AgeDisplay = ageDisplay;
            NationalityDisplay = nationalityDisplay;
            FlagDisplayMode = flagDisplayMode;
            FlagReference = flagReference;
            ClubDisplay = clubDisplay;
            PositionDisplay = positionDisplay;
            SourceName = sourceName;
            SourceConfidence = sourceConfidence;
            DataCompleteness = dataCompleteness;
            ScoutKnowledge = scoutKnowledge;
            RoleFit = roleFit;
            Confidence = confidence;
            Risk = risk;
            Recommendation = recommendation;
            RadarMetrics = visualBundle.RadarMetrics;
            PercentileBars = visualBundle.PercentileBars;
            RoleFitVisual = visualBundle.RoleFitVisual;
            ConfidenceVisual = visualBundle.ConfidenceVisual;
            RiskVisual = visualBundle.RiskVisual;
            EvidenceCards = visualBundle.EvidenceCards;
            TrendVisuals = visualBundle.TrendVisuals;
            ComparisonCards = visualBundle.ComparisonCards;
            MissingDataWarnings = visualBundle.MissingDataWarnings;
            BlockedDataNotice = visualBundle.BlockedDataNotice;
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
        }

        public string PlayerName { get; }

        public AvatarDisplayMode AvatarMode { get; }

        public string AvatarReference { get; }

        public string Initials { get; }

        public string AgeDisplay { get; }

        public string NationalityDisplay { get; }

        public FlagDisplayMode FlagDisplayMode { get; }

        public string FlagReference { get; }

        public string ClubDisplay { get; }

        public string PositionDisplay { get; }

        public string SourceName { get; }

        public int SourceConfidence { get; }

        public int DataCompleteness { get; }

        public int ScoutKnowledge { get; }

        public int RoleFit { get; }

        public int Confidence { get; }

        public int Risk { get; }

        public string Recommendation { get; }

        public IReadOnlyList<RadarMetric> RadarMetrics { get; }

        public IReadOnlyList<PercentileBar> PercentileBars { get; }

        public RoleFitVisual RoleFitVisual { get; }

        public ConfidenceVisual ConfidenceVisual { get; }

        public RiskVisual RiskVisual { get; }

        public IReadOnlyList<EvidenceCard> EvidenceCards { get; }

        public IReadOnlyList<TrendVisual> TrendVisuals { get; }

        public IReadOnlyList<ComparisonCard> ComparisonCards { get; }

        public IReadOnlyList<MissingDataWarning> MissingDataWarnings { get; }

        public BlockedDataNoticeView BlockedDataNotice { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public static MaskedPlayerProfileViewModel From(
            object player,
            RoleScore roleScore,
            SourceMetadata sourceMetadata,
            DataCompletenessReport completeness)
        {
            if (player is IRawFootballEntity)
            {
                throw new InvalidOperationException("Profile view models cannot be created from raw provider data.");
            }

            if (!(player is MaskedPlayer maskedPlayer))
            {
                throw new InvalidOperationException("Profile view models require masked player data.");
            }

            var visuals = new VisualIntelligenceBuilder().Build(maskedPlayer, roleScore, sourceMetadata, completeness);
            var imageField = FindField(maskedPlayer, PlayerFieldKey.PlayerFaceImage);
            var flagField = FindField(maskedPlayer, PlayerFieldKey.NationalityFlag);

            var avatarMode = imageField != null && imageField.CanDisplay && sourceMetadata.PermitsPlayerImages
                ? AvatarDisplayMode.PermittedImage
                : AvatarDisplayMode.Initials;
            var flagMode = flagField == null || !flagField.CanDisplay
                ? FlagDisplayMode.None
                : sourceMetadata.PermitsProviderFlags
                    ? FlagDisplayMode.ProviderFlag
                    : sourceMetadata.UsesBundledSafeFlagAssets
                        ? FlagDisplayMode.BundledSafeFlag
                        : FlagDisplayMode.None;

            var playerName = string.IsNullOrWhiteSpace(maskedPlayer.DisplayName) ? ValueOrUnknown(maskedPlayer, PlayerFieldKey.DisplayName) : maskedPlayer.DisplayName;

            var model = new MaskedPlayerProfileViewModel(
                playerName,
                avatarMode,
                avatarMode == AvatarDisplayMode.PermittedImage && imageField != null ? imageField.DisplayValue : string.Empty,
                BuildInitials(playerName),
                ValueOrUnknown(maskedPlayer, PlayerFieldKey.Age),
                ValueOrUnknown(maskedPlayer, PlayerFieldKey.Nationality),
                flagMode,
                flagMode == FlagDisplayMode.None || flagField == null ? string.Empty : flagField.DisplayValue,
                ValueOrUnknown(maskedPlayer, PlayerFieldKey.Club),
                ValueOrUnknown(maskedPlayer, PlayerFieldKey.PrimaryPosition),
                sourceMetadata.SourceName,
                sourceMetadata.SourceConfidence,
                completeness.CompletenessPercentage,
                maskedPlayer.ScoutKnowledgePercentage,
                roleScore.RoleFit,
                roleScore.Confidence,
                roleScore.RiskScore,
                roleScore.Recommendation.ToString(),
                visuals,
                sourceMetadata.SourceName.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 || sourceMetadata.AllowedUsage.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0,
                sourceMetadata.ProviderType == ProviderType.FM26LiveMemory && sourceMetadata.IsLive);

            BindingPolicy.AssertProfileBindable(model);
            return model;
        }

        private static VisiblePlayerField? FindField(MaskedPlayer player, PlayerFieldKey key)
        {
            return player.Fields.Values.FirstOrDefault(field => field.Key == key && field.IsKnown && field.CanDisplay);
        }

        private static string ValueOrUnknown(MaskedPlayer player, PlayerFieldKey key)
        {
            var field = FindField(player, key);
            return field == null || string.IsNullOrWhiteSpace(field.DisplayValue) ? "Unknown" : field.DisplayValue;
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "??";
            }

            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            }

            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}
