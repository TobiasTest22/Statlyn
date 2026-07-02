using System;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.DataProviders.Fm26;
using Statlyn.Scouting;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class FirewallTests
    {
        [Fact]
        public void HiddenCaAndPaPresentInRawFixtureNeverAppearInMaskedPlayer()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            raw.HiddenCurrentAbility = 153;
            raw.HiddenPotentialAbility = 181;

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var propertyNames = typeof(MaskedPlayer).GetProperties().Select(property => property.Name).ToArray();

            Assert.DoesNotContain("HiddenCurrentAbility", propertyNames);
            Assert.DoesNotContain("HiddenPotentialAbility", propertyNames);
            Assert.DoesNotContain("CurrentAbility", propertyNames);
            Assert.DoesNotContain("PotentialAbility", propertyNames);
            Assert.DoesNotContain(masked.Facts.Keys, key => key.Contains("Ability", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(masked.Attributes.Keys, key => key.Contains("Ability", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void HiddenPersonalityValuesNeverAppearInMaskedPlayer()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            raw.HiddenAttributes["Professionalism"] = 20;
            raw.HiddenAttributes["Pressure"] = 17;

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            Assert.DoesNotContain("Professionalism", masked.Attributes.Keys);
            Assert.DoesNotContain("Pressure", masked.Attributes.Keys);
            Assert.DoesNotContain("Professionalism", masked.Facts.Keys);
            Assert.DoesNotContain("Pressure", masked.Facts.Keys);
        }

        [Fact]
        public void UnknownAttributeReducesScoringConfidence()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            raw.VisibleAttributes["Finishing"] = 16;
            raw.VisibleAttributes["OffTheBall"] = 15;
            raw.VisibleAttributeNames.Add("Finishing");

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var role = new RoleModel("Advanced Forward").RequireAttribute("Finishing", 2).RequireAttribute("OffTheBall", 2);
            var score = new RoleScoringEngine().ScorePlayer(masked, role);

            Assert.Contains("OffTheBall", score.MissingData);
            Assert.True(score.Confidence < 80);
        }

        [Fact]
        public void LowScoutKnowledgePreventsAutomaticSignVerdict()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            raw.ScoutKnowledgePercentage = 30;
            raw.SourceConfidence = 55;
            raw.VisibleAttributes["Finishing"] = 19;
            raw.VisibleAttributes["Pace"] = 18;
            raw.VisibleAttributeNames.Add("Finishing");
            raw.VisibleAttributeNames.Add("Pace");

            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var role = new RoleModel("Pressing Forward").RequireAttribute("Finishing", 1).RequireAttribute("Pace", 1);
            var score = new RoleScoringEngine().ScorePlayer(masked, role);

            Assert.NotEqual(RecruitmentRecommendation.Sign, score.Recommendation);
            Assert.Equal(RecruitmentRecommendation.ScoutFurther, score.Recommendation);
        }

        [Fact]
        public void UiCannotBindRawPlayerEntity()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();

            Assert.Throws<InvalidOperationException>(() => BindingPolicy.AssertBindable(raw));
        }

        [Fact]
        public void ScoringEngineRejectsRawEntityInput()
        {
            var raw = TestPlayers.CreateScoutedFm26Player();
            var role = new RoleModel("Winger").RequireAttribute("Pace", 1);

            Assert.Throws<InvalidOperationException>(() => new RoleScoringEngine().ScorePlayer(raw, role));
        }

        [Fact]
        public void UnsupportedFm26BuildReturnsNoFakePlayers()
        {
            var provider = new Fm26LiveMemoryProvider(new UnsupportedBuildConnector());
            var connect = provider.Connect();
            var players = provider.ReadPlayers();

            Assert.False(connect.Success);
            Assert.True(players.Success);
            Assert.Empty(players.Value!);
            Assert.Equal(Core.Diagnostics.DiagnosticStatus.Unsupported, players.Diagnostics.OverallStatus);
        }
    }
}
