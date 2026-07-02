using System;
using System.Linq;
using System.Reflection;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.DataProviders;
using Statlyn.Scouting;
using Statlyn.UI;
using Statlyn.UI.ProfileFixtures;
using Statlyn.UI.UnityBridge;
using Statlyn.UI.Visuals;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone161Tests
    {
        [Fact]
        public void FixtureFactoryReturnsMaskedProfileViewModelOnly()
        {
            var profile = FixtureProfileFactory.CreateDevelopmentPreviewProfile();

            Assert.IsType<MaskedPlayerProfileViewModel>(profile);
            Assert.False(profile.GetType().IsAssignableFrom(typeof(PlayerRawSnapshot)));

            var publicFactoryMethods = typeof(FixtureProfileFactory).GetMethods(BindingFlags.Public | BindingFlags.Static);
            Assert.All(publicFactoryMethods, method => Assert.NotEqual(typeof(PlayerRawSnapshot), method.ReturnType));
        }

        [Fact]
        public void UnityRenderModelIsBuiltOnlyFromMaskedProfileViewModel()
        {
            var fromMethods = typeof(UnityProfileRenderModel)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "From")
                .ToArray();

            Assert.Single(fromMethods);
            Assert.Equal(typeof(MaskedPlayerProfileViewModel), fromMethods[0].GetParameters()[0].ParameterType);

            var profile = FixtureProfileFactory.CreateDevelopmentPreviewProfile();
            var renderModel = UnityProfileRenderModel.From(profile);

            Assert.Equal(profile.PlayerName, renderModel.PlayerName);
            Assert.True(renderModel.IsFixtureMode);
            Assert.False(renderModel.IsLiveFm26Data);
        }

        [Fact]
        public void FixtureProfileDoesNotExposeBlockedRawValues()
        {
            var profile = FixtureProfileFactory.CreateDevelopmentPreviewProfile();
            var renderModel = UnityProfileRenderModel.From(profile);
            var blockedText = string.Join(" ", profile.BlockedDataNotice.Categories) + " " + profile.BlockedDataNotice.SafeMessage + " " + renderModel.BlockedDataMessage;
            var displayText = string.Join(" ", new[]
            {
                renderModel.PlayerName,
                renderModel.Initials,
                renderModel.DetailLine,
                renderModel.FlagLine,
                renderModel.SourceName,
                renderModel.DataCompletenessCaption,
                renderModel.RoleFitCaption,
                renderModel.Confidence,
                renderModel.ConfidenceCaption,
                renderModel.Risk,
                renderModel.RiskCaption,
                renderModel.MissingDataMessage,
                renderModel.BlockedDataMessage,
                string.Join(" ", renderModel.EvidenceCards.Select(card => card.Title + " " + card.Body))
            });

            Assert.Contains("CurrentAbility", blockedText);
            Assert.Contains("Professionalism", blockedText);
            Assert.DoesNotContain("200", blockedText);
            Assert.DoesNotContain("19", blockedText);
            Assert.DoesNotContain("200", displayText);
            Assert.DoesNotContain("Professionalism: 19", displayText);
            Assert.DoesNotContain("CurrentAbility: 200", displayText);
        }

        [Fact]
        public void FixtureProfileKeepsModeAssetsWarningsAndUnknownTacticalFitHonest()
        {
            var profile = FixtureProfileFactory.CreateDevelopmentPreviewProfile();
            var renderModel = UnityProfileRenderModel.From(profile);

            Assert.True(profile.IsFixtureMode);
            Assert.False(profile.IsLiveFm26Data);
            Assert.Equal(AvatarDisplayMode.Initials, profile.AvatarMode);
            Assert.Equal(FlagDisplayMode.BundledSafeFlag, profile.FlagDisplayMode);
            Assert.NotEmpty(profile.MissingDataWarnings);
            Assert.True(profile.RoleFitVisual.IsTacticalFitUnknown);
            Assert.Equal("Tactical fit unknown", profile.RoleFitVisual.TacticalFitLabel);
            Assert.Contains("Tactical fit unknown", renderModel.RoleFitCaption);
            Assert.DoesNotContain("Tactical fit: 0", renderModel.RoleFitCaption, StringComparison.OrdinalIgnoreCase);
            Assert.All(profile.PercentileBars, bar => Assert.Equal("Fixture comparison group", bar.ComparisonGroup));
            Assert.All(profile.RadarMetrics, metric => Assert.False(string.IsNullOrWhiteSpace(metric.SourceName)));
        }

        [Fact]
        public void VisualBuilderRejectsRawPlayersAndShowsLowConfidenceReasons()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsFlags: false, usesBundledFlags: true);
            raw.SourceContext = new SourceContext("Low confidence fixture", "CSV fixture", ProviderType.Csv, true, false, false, true, false, false, 40, "fixture mode");
            raw.ScoutContext = new ScoutContext(false, 20, true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.CurrentAbility, "CurrentAbility", 200, FieldValueKind.Number, 90));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var role = new RoleModel("Forward").RequireAttribute("Finishing", 1).RequireStat("xG", 1);
            var score = new RoleScoringEngine().ScorePlayer(masked, role);
            var metadata = new SourceMetadata("Low confidence fixture", ProviderType.Csv, false, true, "synthetic", "fixture mode", false, false, true, false, false, DateTimeOffset.UtcNow, 40);
            var completeness = new DataCompletenessReport(0, 2, new[] { "Finishing", "xG" });

            Assert.Throws<InvalidOperationException>(() => new VisualIntelligenceBuilder().Build(raw, score, metadata, completeness));
            Assert.Throws<InvalidOperationException>(() => MaskedPlayerProfileViewModel.From(raw, score, metadata, completeness));

            var visuals = new VisualIntelligenceBuilder().Build(masked, score, metadata, completeness);

            Assert.Equal("Low", visuals.ConfidenceVisual.Label);
            Assert.Contains("missing role data", visuals.ConfidenceVisual.Reason);
            Assert.Contains("source confidence", visuals.ConfidenceVisual.Reason);
            Assert.Contains("scout knowledge", visuals.ConfidenceVisual.Reason);
            Assert.Equal("Directional", visuals.RiskVisual.Label);
            Assert.Contains(visuals.EvidenceCards, card => card.Category == EvidenceCategory.Risk && card.Body.Contains("directional", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void RoleScoreTacticalFitAndSchemaAreNullable()
        {
            Assert.Equal(typeof(int), Nullable.GetUnderlyingType(typeof(RoleScore).GetProperty(nameof(RoleScore.TacticalFit))!.PropertyType));

            var schema = Schema();

            Assert.Contains("TacticalFit INTEGER NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TacticalFit INTEGER NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DataSourceSchemaUsesSplitPermissionColumnsWithBlockedDefaults()
        {
            var schema = Schema();

            Assert.Contains("PermitsPlayerImages INTEGER NOT NULL DEFAULT 0", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PermitsProviderFlags INTEGER NOT NULL DEFAULT 0", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UsesBundledSafeFlagAssets INTEGER NOT NULL DEFAULT 0", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PermitsClubBadges INTEGER NOT NULL DEFAULT 0", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AllowsExport INTEGER NOT NULL DEFAULT 0", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PermitsImages INTEGER", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PermitsFlags INTEGER", schema, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ClubBadgesRemainBlockedWithoutExplicitPermission()
        {
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.ClubBadge, "ClubBadge", "badge.png", FieldValueKind.ImageReference, 90));

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.Contains(masked.BlockedFields, field => field.Key == PlayerFieldKey.ClubBadge);
            Assert.DoesNotContain(masked.Fields.Values, field => field.Key == PlayerFieldKey.ClubBadge && field.CanDisplay);
        }

        [Fact]
        public void SchemaAuditExcludesHiddenAndRawValueColumns()
        {
            var schema = Schema();

            foreach (var forbidden in new[]
            {
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "InjuryProneness",
                "Consistency",
                "ImportantMatches",
                "Pressure",
                "Ambition",
                "Loyalty",
                "Adaptability",
                "Temperament",
                "RawValue"
            })
            {
                Assert.DoesNotContain(forbidden, schema, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Contains("CREATE TABLE IF NOT EXISTS BlockedFieldAudit", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SourceName TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SourceEntityId TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FieldKey TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FieldName TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Reason TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CreatedAtUtc TEXT NOT NULL", schema, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void VisibleFieldSchemaStoresOnlyMaskedFieldValues()
        {
            var schema = Schema();

            foreach (var required in new[]
            {
                "FieldInstanceKey TEXT NOT NULL",
                "FieldKey TEXT NOT NULL",
                "FieldName TEXT NOT NULL",
                "SourceFieldName TEXT NOT NULL",
                "DisplayValue TEXT NULL",
                "NumericValue REAL NULL",
                "CanDisplay INTEGER NOT NULL",
                "CanScore INTEGER NOT NULL",
                "CanStore INTEGER NOT NULL",
                "Confidence INTEGER NOT NULL",
                "SourceName TEXT NOT NULL",
                "LastUpdatedUtc TEXT NOT NULL"
            })
            {
                Assert.Contains(required, schema, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string Schema()
        {
            return string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);
        }
    }
}
