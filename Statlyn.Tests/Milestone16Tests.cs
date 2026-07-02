using System;
using System.IO;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Statlyn.Scouting;
using Statlyn.UI;
using Statlyn.UI.Visuals;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone16Tests
    {
        [Fact]
        public void MultipleGroupedFieldsSurviveMasking()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Finishing", 14));
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Pace", 16));
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xG", 44));
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xA", 21));
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "TopSpeed", 33));
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "SprintDistance", 810));
            raw.AddField(Number(PlayerFieldKey.Professionalism, "Professionalism", 20));
            raw.AddField(Number(PlayerFieldKey.CurrentAbility, "CurrentAbility", 200));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            AssertVisible(masked, PlayerFieldKey.TechnicalAttribute, "Finishing");
            AssertVisible(masked, PlayerFieldKey.TechnicalAttribute, "Pace");
            AssertVisible(masked, PlayerFieldKey.PlayerStat, "xG");
            AssertVisible(masked, PlayerFieldKey.PlayerStat, "xA");
            AssertVisible(masked, PlayerFieldKey.PhysicalData, "TopSpeed");
            AssertVisible(masked, PlayerFieldKey.PhysicalData, "SprintDistance");
            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.Professionalism);
            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.CurrentAbility);
        }

        [Theory]
        [InlineData("Finishing", PlayerFieldKey.TechnicalAttribute, "Finishing")]
        [InlineData("Pace", PlayerFieldKey.TechnicalAttribute, "Pace")]
        [InlineData("xG", PlayerFieldKey.PlayerStat, "xG")]
        [InlineData("xA", PlayerFieldKey.PlayerStat, "xA")]
        [InlineData("TopSpeed", PlayerFieldKey.PhysicalData, "TopSpeed")]
        [InlineData("CurrentAbility", PlayerFieldKey.CurrentAbility, "CurrentAbility")]
        [InlineData("Professionalism", PlayerFieldKey.Professionalism, "Professionalism")]
        [InlineData("UnknownMetric", PlayerFieldKey.Unknown, "UnknownMetric")]
        public void FootballFieldCatalogMapsKnownAndForbiddenColumns(string column, PlayerFieldKey expectedKey, string expectedName)
        {
            var mapping = new FootballFieldCatalog(new FieldPolicyRegistry()).Resolve(column);

            Assert.Equal(expectedKey, mapping.FieldKey);
            Assert.Equal(expectedName, mapping.FieldName);
        }

        [Fact]
        public void ExplicitMappingWorksButForbiddenRawNameCannotBeRescued()
        {
            var registry = new FieldPolicyRegistry();
            var mappingSet = new FieldMappingSet(new[]
            {
                new FieldMapping("CustomFinish", PlayerFieldKey.TechnicalAttribute, "Finishing", FieldValueKind.Number),
                new FieldMapping("CurrentAbility", PlayerFieldKey.TechnicalAttribute, "Finishing", FieldValueKind.Number)
            });

            Assert.Equal(PlayerFieldKey.TechnicalAttribute, mappingSet.Resolve("CustomFinish", registry).FieldKey);
            Assert.Equal(PlayerFieldKey.CurrentAbility, mappingSet.Resolve("CurrentAbility", registry).FieldKey);
        }

        [Fact]
        public void CsvFixtureImportsTwoRowsAndPreservesSafeFields()
        {
            var provider = CreateCsvFixtureProvider();
            var result = provider.ReadPlayers();

            Assert.True(result.Success);
            Assert.Equal(2, result.Value!.Count);

            foreach (var player in result.Value!)
            {
                var masked = new ScoutingKnowledgeFirewall().Mask(player);
                AssertVisible(masked, PlayerFieldKey.TechnicalAttribute, "Finishing");
                AssertVisible(masked, PlayerFieldKey.TechnicalAttribute, "Pace");
                AssertVisible(masked, PlayerFieldKey.TechnicalAttribute, "Acceleration");
                AssertVisible(masked, PlayerFieldKey.PlayerStat, "xG");
                AssertVisible(masked, PlayerFieldKey.PlayerStat, "xA");
                AssertVisible(masked, PlayerFieldKey.PlayerStat, "Goals");
                AssertVisible(masked, PlayerFieldKey.PlayerStat, "Assists");
                AssertVisible(masked, PlayerFieldKey.PhysicalData, "TopSpeed");
                AssertVisible(masked, PlayerFieldKey.PhysicalData, "SprintDistance");
                Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.CurrentAbility);
                Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.Professionalism);
            }
        }

        [Fact]
        public void RoleScoringUsesMultipleAttributesAndStatsFromCsv()
        {
            var player = new ScoutingKnowledgeFirewall().Mask(CreateCsvFixtureProvider().ReadPlayers().Value!.First());
            var role = new RoleModel("Pressing Forward")
                .RequireAttribute("Finishing", 2)
                .RequireAttribute("Pace", 1)
                .RequireStat("xG", 1);

            var score = new RoleScoringEngine().ScorePlayer(player, role);

            Assert.True(score.RoleFit > 0);
            Assert.True(score.TechnicalFit > 0);
            Assert.True(score.StatisticalFit > 0);
            Assert.DoesNotContain("Finishing", score.MissingData);
            Assert.DoesNotContain("xG", score.MissingData);
        }

        [Fact]
        public void SourcePermissionsSplitImagesFlagsBadgesAndExport()
        {
            var noFlags = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: false, permitsFlags: false, usesBundledFlags: false);
            noFlags.AddField(new RawFieldValue(PlayerFieldKey.NationalityFlag, "NationalityFlag", "flags/RO.svg", FieldValueKind.FlagReference, 90));
            Assert.Contains(new ScoutingKnowledgeFirewall().Mask(noFlags).BlockedFields, field => field.Key == PlayerFieldKey.NationalityFlag);

            var bundled = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: false, permitsFlags: false, usesBundledFlags: true);
            bundled.AddField(new RawFieldValue(PlayerFieldKey.NationalityFlag, "NationalityFlag", "flags/RO.svg", FieldValueKind.FlagReference, 90));
            AssertVisible(new ScoutingKnowledgeFirewall().Mask(bundled), PlayerFieldKey.NationalityFlag, "NationalityFlag");

            var imageBlocked = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: false);
            imageBlocked.AddField(new RawFieldValue(PlayerFieldKey.PlayerFaceImage, "PlayerFaceImage", "face.png", FieldValueKind.ImageReference, 90));
            Assert.Contains(new ScoutingKnowledgeFirewall().Mask(imageBlocked).BlockedFields, field => field.Key == PlayerFieldKey.PlayerFaceImage);

            var imageAllowed = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: true);
            imageAllowed.AddField(new RawFieldValue(PlayerFieldKey.PlayerFaceImage, "PlayerFaceImage", "face.png", FieldValueKind.ImageReference, 90));
            AssertVisible(new ScoutingKnowledgeFirewall().Mask(imageAllowed), PlayerFieldKey.PlayerFaceImage, "PlayerFaceImage");

            var badge = TestPlayers.CreateExternalPlayer(isLicensed: true);
            badge.AddField(new RawFieldValue(PlayerFieldKey.ClubBadge, "ClubBadge", "badge.png", FieldValueKind.ImageReference, 90));
            Assert.Contains(new ScoutingKnowledgeFirewall().Mask(badge).BlockedFields, field => field.Key == PlayerFieldKey.ClubBadge);

            var metadata = new SourceMetadata("Exportable fixture", ProviderType.Csv, false, true, "synthetic", "test", false, false, false, false, true, DateTimeOffset.UtcNow, 80);
            Assert.True(metadata.AllowsExport);
        }

        [Fact]
        public void ScoringTreatsZeroAsPoorAndHandlesMissingGroupsHonestly()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            raw.SourceContext = new SourceContext("Low confidence fixture", "CSV fixture", ProviderType.Csv, true, false, false, false, false, false, 40, "test");
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xG", 0, 40));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            var score = new RoleScoringEngine().ScorePlayer(masked, new RoleModel("Striker").RequireStat("xG", 1));

            Assert.Equal(0, score.StatisticalFit);
            Assert.DoesNotContain("xG", score.MissingData);
            Assert.True(score.Confidence < 60);
            Assert.NotEqual(RecruitmentRecommendation.Sign, score.Recommendation);
            Assert.Null(score.TacticalFit);
        }

        [Fact]
        public void RedFlagsTriggerOnlyWhenFieldExistsAndMeetsCondition()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "SprintDistance", 500));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var role = new RoleModel("Pressing Forward")
                .RequirePhysicalMetric("SprintDistance", 1)
                .AddRedFlag("SprintDistance", RedFlagOperator.LessThan, 700, "Sprint distance is below role threshold.", "Physical");

            var score = new RoleScoringEngine().ScorePlayer(masked, role);
            var missingScore = new RoleScoringEngine().ScorePlayer(TestPlayers.CreateMaskedExternalPlayer(), role);

            Assert.Contains(score.NegativeEvidence, evidence => evidence.Message.Contains("Sprint distance", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(missingScore.NegativeEvidence, evidence => evidence.Message.Contains("Sprint distance", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void VisualModelsAndProfileViewModelAreSafeAndHonest()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: false, permitsFlags: false, usesBundledFlags: true);
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Finishing", 16));
            raw.AddField(Number(PlayerFieldKey.CurrentAbility, "CurrentAbility", 200));
            raw.AddField(new RawFieldValue(PlayerFieldKey.NationalityFlag, "NationalityFlag", "flags/RO.svg", FieldValueKind.FlagReference, 80));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var roleScore = new RoleScoringEngine().ScorePlayer(masked, new RoleModel("Forward").RequireAttribute("Finishing", 1).RequireAttribute("Pace", 1));
            var metadata = new SourceMetadata("Synthetic fixture source", ProviderType.Csv, false, true, "synthetic", "fixture mode", false, false, true, false, false, DateTimeOffset.UtcNow, 80);
            var completeness = new DataCompletenessReport(1, 2, new[] { "Pace" });

            var visuals = new VisualIntelligenceBuilder().Build(masked, roleScore, metadata, completeness);
            var profile = MaskedPlayerProfileViewModel.From(masked, roleScore, metadata, completeness);

            Assert.Throws<InvalidOperationException>(() => new VisualIntelligenceBuilder().Build(raw, roleScore, metadata, completeness));
            Assert.Throws<InvalidOperationException>(() => MaskedPlayerProfileViewModel.From(raw, roleScore, metadata, completeness));
            Assert.DoesNotContain("200", string.Join(" ", visuals.BlockedDataNotice.Categories) + visuals.BlockedDataNotice.SafeMessage);
            Assert.Contains(visuals.MissingDataWarnings, warning => warning.FieldName == "Pace");
            Assert.True(visuals.ConfidenceVisual.Score < 100);
            Assert.NotEqual("Sign", profile.RoleFitVisual.StatusLabel);
            Assert.All(visuals.PercentileBars, bar => Assert.False(string.IsNullOrWhiteSpace(bar.ComparisonGroup)));
            Assert.All(visuals.RadarMetrics, metric => Assert.False(string.IsNullOrWhiteSpace(metric.SourceName)));
            Assert.Equal(AvatarDisplayMode.Initials, profile.AvatarMode);
            Assert.Equal(FlagDisplayMode.BundledSafeFlag, profile.FlagDisplayMode);
            Assert.True(profile.IsFixtureMode);
            Assert.False(profile.IsLiveFm26Data);
        }

        [Fact]
        public void ProfileUsesPermittedImageOnlyWhenSourceAllowsIt()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsImages: true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.PlayerFaceImage, "PlayerFaceImage", "face.png", FieldValueKind.ImageReference, 80));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var roleScore = new RoleScoringEngine().ScorePlayer(masked, new RoleModel("Profile"));
            var metadata = new SourceMetadata("Image fixture", ProviderType.Csv, false, true, "synthetic", "fixture mode", true, false, false, false, false, DateTimeOffset.UtcNow, 80);

            var profile = MaskedPlayerProfileViewModel.From(masked, roleScore, metadata, new DataCompletenessReport(1, 1, Array.Empty<string>()));

            Assert.Equal(AvatarDisplayMode.PermittedImage, profile.AvatarMode);
            Assert.Equal("face.png", profile.AvatarReference);
        }

        [Fact]
        public void SchemaSupportsFieldInstancesAndDoesNotStoreRawBlockedValues()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);

            Assert.DoesNotContain("CurrentAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("InjuryProneness", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FieldInstanceKey", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CREATE TABLE IF NOT EXISTS VisibleField", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CREATE TABLE IF NOT EXISTS PhysicalMetric", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CREATE TABLE IF NOT EXISTS PlayerStat", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Reason TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RawValue", schema, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CsvDiagnosticsReportCountsWithoutHiddenRawValues()
        {
            var result = CreateCsvFixtureProvider().ReadPlayers();
            var diagnostics = string.Join(" ", result.Diagnostics.Items.Select(item => item.Key + " " + item.Message + " " + item.TechnicalDetail));

            Assert.Contains("CSV rows were read", diagnostics);
            Assert.Contains("Forbidden fields:", diagnostics);
            Assert.Contains("Source metadata says this import is permitted", diagnostics);
            Assert.Contains("CSV data completeness calculated", diagnostics);
            Assert.DoesNotContain("200", diagnostics);
            Assert.DoesNotContain("199", diagnostics);
        }

        private static CsvImportProvider CreateCsvFixtureProvider()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
            var metadata = new SourceMetadata("Synthetic CSV fixture", ProviderType.Csv, false, true, "synthetic test fixture", "development fixture only", false, true, true, false, true, DateTimeOffset.UtcNow, 80);
            return new CsvImportProvider(path, metadata, new FieldMappingSet(Array.Empty<FieldMapping>()));
        }

        private static RawFieldValue Number(PlayerFieldKey key, string fieldName, double value, int confidence = 90)
        {
            return new RawFieldValue(key, fieldName, fieldName, value, FieldValueKind.Number, confidence);
        }

        private static void AssertVisible(MaskedPlayer masked, PlayerFieldKey key, string fieldName)
        {
            Assert.Contains(masked.Fields.Values, field => field.Key == key && field.FieldName == fieldName && field.IsKnown && field.CanDisplay);
        }
    }
}
