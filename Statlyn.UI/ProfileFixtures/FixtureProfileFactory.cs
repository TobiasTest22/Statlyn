using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.DataProviders;
using Statlyn.Scouting;

namespace Statlyn.UI.ProfileFixtures
{
    public static class FixtureProfileFactory
    {
        private static readonly string[] ExpectedDevelopmentFields =
        {
            "Finishing",
            "Pace",
            "Acceleration",
            "xG",
            "xA",
            "TopSpeed",
            "SprintDistance",
            "OffTheBall"
        };

        public static MaskedPlayerProfileViewModel CreateDevelopmentPreviewProfile()
        {
            var metadata = CreateDevelopmentSourceMetadata();
            var raw = CreateDevelopmentRawPlayer(metadata);
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var roleScore = new RoleScoringEngine().ScorePlayer(masked, CreateDevelopmentRoleModel());
            var completeness = CreateCompletenessReport(masked);

            return MaskedPlayerProfileViewModel.From(masked, roleScore, metadata, completeness);
        }

        private static PlayerRawSnapshot CreateDevelopmentRawPlayer(SourceMetadata metadata)
        {
            var raw = new PlayerRawSnapshot("fixture-dev-forward-001", "Synthetic development fixture", ProviderType.Csv)
            {
                DisplayName = "Synthetic Forward",
                SourceContext = metadata.ToSourceContext("Synthetic CSV fixture"),
                ScoutContext = new ScoutContext(false, 65, true)
            };

            raw.AddField(new RawFieldValue(PlayerFieldKey.DisplayName, "DisplayName", "Synthetic Forward", FieldValueKind.Text, 90));
            raw.AddField(new RawFieldValue(PlayerFieldKey.Age, "Age", 22, FieldValueKind.Number, 90));
            raw.AddField(new RawFieldValue(PlayerFieldKey.Nationality, "Nationality", "Romania", FieldValueKind.Text, 90));
            raw.AddField(new RawFieldValue(PlayerFieldKey.NationalityFlag, "NationalityFlag", "bundled-safe:RO", FieldValueKind.FlagReference, 90));
            raw.AddField(new RawFieldValue(PlayerFieldKey.PrimaryPosition, "PrimaryPosition", "ST", FieldValueKind.Text, 90));
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Finishing", 15));
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Pace", 14));
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Acceleration", 16));
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xG", 0.48));
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xA", 0.18));
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "TopSpeed", 33.4));
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "SprintDistance", 845));
            raw.AddField(Number(PlayerFieldKey.CurrentAbility, "CurrentAbility", 200));
            raw.AddField(Number(PlayerFieldKey.Professionalism, "Professionalism", 19));

            return raw;
        }

        private static SourceMetadata CreateDevelopmentSourceMetadata()
        {
            return new SourceMetadata(
                "Synthetic development fixture",
                ProviderType.Csv,
                isLive: false,
                isLicensed: true,
                licenceStatus: "synthetic fixture",
                allowedUsage: "development fixture mode only; no live FM26 data",
                permitsPlayerImages: false,
                permitsProviderFlags: false,
                usesBundledSafeFlagAssets: true,
                permitsClubBadges: false,
                allowsExport: true,
                importedAtUtc: DateTimeOffset.UtcNow,
                sourceConfidence: 80);
        }

        private static RoleModel CreateDevelopmentRoleModel()
        {
            return new RoleModel("Pressing Forward")
                .RequireAttribute("Finishing", 2)
                .RequireAttribute("Pace", 1)
                .RequireAttribute("Acceleration", 1)
                .RequireAttribute("OffTheBall", 1)
                .RequireStat("xG", 1)
                .RequireStat("xA", 1)
                .RequirePhysicalMetric("TopSpeed", 1)
                .RequirePhysicalMetric("SprintDistance", 1);
        }

        private static DataCompletenessReport CreateCompletenessReport(MaskedPlayer masked)
        {
            var known = new HashSet<string>(
                masked.Fields.Values
                    .Where(field => field.IsKnown && field.CanScore)
                    .Select(field => field.FieldName),
                StringComparer.OrdinalIgnoreCase);

            var missing = ExpectedDevelopmentFields
                .Where(field => !known.Contains(field))
                .ToArray();

            return new DataCompletenessReport(
                ExpectedDevelopmentFields.Length - missing.Length,
                ExpectedDevelopmentFields.Length,
                missing);
        }

        private static RawFieldValue Number(PlayerFieldKey key, string fieldName, double value)
        {
            return new RawFieldValue(key, fieldName, fieldName, value, FieldValueKind.Number, 90);
        }
    }
}
