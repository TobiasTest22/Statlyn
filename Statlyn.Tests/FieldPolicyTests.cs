using System;
using System.IO;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.DataProviders.Import;
using Statlyn.Scouting;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class FieldPolicyTests
    {
        [Theory]
        [InlineData("CurrentAbility", PlayerFieldKey.CurrentAbility)]
        [InlineData("PotentialAbility", PlayerFieldKey.PotentialAbility)]
        [InlineData("InjuryProneness", PlayerFieldKey.InjuryProneness)]
        public void MislabeledHiddenVisibleFactIsBlocked(string fieldName, PlayerFieldKey blockedKey)
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            raw.VisibleFacts[fieldName] = "20";

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.BlockedFields, field => field.Key == blockedKey);
            Assert.DoesNotContain(fieldName, masked.Facts.Keys);
            Assert.DoesNotContain(masked.Fields.Values, field => field.FieldName == fieldName && field.IsKnown);
        }

        [Fact]
        public void ProfessionalismVisibleFactIsBlockedButQualitativeScoutNoteCanDisplay()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            raw.VisibleFacts["Professionalism"] = "20";
            raw.AddField(new RawFieldValue(
                PlayerFieldKey.ScoutVisiblePersonalityNote,
                "VisiblePersonalityNote",
                "Scout report describes the player as a committed trainer.",
                FieldValueKind.Text,
                70));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.Professionalism);
            Assert.Contains(masked.Facts.Values, field => field.Value == "Scout report describes the player as a committed trainer.");
        }

        [Fact]
        public void UnknownFieldKeyIsDeniedByDefault()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.Unknown, "MysteryMetric", 99, FieldValueKind.Number, 90));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.Unknown);
        }

        [Fact]
        public void LicensedExternalDataIsBlockedIfSourceIsNotLicensed()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: false);
            raw.AddField(new RawFieldValue(PlayerFieldKey.LicensedExternalData, "xG", 0.42, FieldValueKind.Number, 80));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.LicensedExternalData);
        }

        [Fact]
        public void PlayerFaceImageIsBlockedIfSourceDoesNotPermitImages()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: false);
            raw.AddField(new RawFieldValue(PlayerFieldKey.PlayerFaceImage, "PlayerFaceImage", "faces/player.png", FieldValueKind.ImageReference, 80));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.PlayerFaceImage);
        }

        [Fact]
        public void NationalityFlagCanDisplayIfBundledSafeFlagAssetsAreAllowed()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: false, permitsImages: false, permitsFlags: false, usesBundledFlags: true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.NationalityFlag, "NationalityFlag", "flags/RO.svg", FieldValueKind.FlagReference, 90));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.Facts.Values, field => field.Value == "flags/RO.svg");
            Assert.DoesNotContain(masked.BlockedFields, field => field.Key == PlayerFieldKey.NationalityFlag);
        }

        [Fact]
        public void ScoringCannotUseFieldsWithCanScoreFalse()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.DisplayName, "DisplayName", "Synthetic Name", FieldValueKind.Text, 100));
            raw.AddField(new RawFieldValue(PlayerFieldKey.TechnicalAttribute, "Finishing", 18, FieldValueKind.Number, 100));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var score = new RoleScoringEngine().ScorePlayer(masked, new RoleModel("Name Is Not A Role").RequireAttribute("DisplayName", 10));

            Assert.Equal(0, score.RoleFit);
            Assert.Contains("DisplayName", score.MissingData);
        }

        [Fact]
        public void DatabaseSchemaDoesNotStoreHiddenFields()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);

            Assert.DoesNotContain("CurrentAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("InjuryProneness", schema, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CsvFixtureImportsSyntheticRowsAndBlocksCurrentAbilityColumn()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
            var metadata = new SourceMetadata("Synthetic CSV fixture", ProviderType.Csv, false, true, "synthetic test fixture", "development tests only", false, true, DateTimeOffset.UtcNow, 80);
            var provider = new CsvImportProvider(path, metadata, new FieldMappingSet(Array.Empty<FieldMapping>()));

            var players = provider.ReadPlayers();
            var masked = new ScoutingKnowledgeFirewall().Mask(players.Value!.First());

            Assert.True(players.Success);
            Assert.Equal(2, players.Value!.Count);
            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.CurrentAbility);
        }

        [Fact]
        public void RawProtectionsStillRejectRawObjects()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            var role = new RoleModel("Winger").RequireAttribute("Pace", 1);

            Assert.Throws<InvalidOperationException>(() => new RoleScoringEngine().ScorePlayer(raw, role));
            Assert.Throws<InvalidOperationException>(() => BindingPolicy.AssertBindable(raw));
        }
    }
}
