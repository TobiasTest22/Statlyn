using System;
using System.Linq;
using Statlyn.Data.Workflow;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone27Tests
    {
        [Fact]
        public void ThemeSystemExposesLightFallbackAndDarkCommandCenter()
        {
            var modes = Enum.GetNames(typeof(ThemeMode));

            Assert.Contains(nameof(ThemeMode.LightGlass), modes);
            Assert.Contains(nameof(ThemeMode.DarkCommandCenter), modes);
            Assert.Equal(ThemeMode.DarkCommandCenter, ThemeTokens.For(ThemeMode.DarkCommandCenter).Mode);
            Assert.Contains("legacy", ThemeTokens.SafeModeLabel(ThemeMode.LightGlass), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DarkCommandCenterTokensIncludeStatusAndAccentPalette()
        {
            var tokens = ThemeTokens.DarkCommandCenter;
            var values = new[]
            {
                tokens.Background,
                tokens.Panel,
                tokens.ElevatedPanel,
                tokens.Border,
                tokens.PrimaryAccent,
                tokens.SecondaryAccent,
                tokens.Success,
                tokens.Warning,
                tokens.Danger,
                tokens.MutedText,
                tokens.MainText,
                tokens.SubtleText
            };

            Assert.All(values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            Assert.StartsWith("#", tokens.PrimaryAccent, StringComparison.Ordinal);
            Assert.StartsWith("#", tokens.SecondaryAccent, StringComparison.Ordinal);
            Assert.StartsWith("#", tokens.Success, StringComparison.Ordinal);
            Assert.StartsWith("#", tokens.Warning, StringComparison.Ordinal);
            Assert.StartsWith("#", tokens.Danger, StringComparison.Ordinal);

            var joined = string.Join(" ", values.Append(tokens.Name));
            Assert.DoesNotContain("CurrentAbility", joined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", joined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", joined, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StatusLabelsMapToSafeCommandCategories()
        {
            Assert.Equal(CommandStatusCategory.Success, ThemeTokens.ResolveStatusCategory("Passed"));
            Assert.Equal(CommandStatusCategory.Success, ThemeTokens.ResolveStatusCategory("Scouting firewall active"));
            Assert.Equal(CommandStatusCategory.Warning, ThemeTokens.ResolveStatusCategory("Unsupported until validated"));
            Assert.Equal(CommandStatusCategory.Warning, ThemeTokens.ResolveStatusCategory("Not checked"));
            Assert.Equal(CommandStatusCategory.Danger, ThemeTokens.ResolveStatusCategory("Runtime error"));
            Assert.Equal(CommandStatusCategory.Accent, ThemeTokens.ResolveStatusCategory("No live FM26 data"));
            Assert.Equal("status-warning", ThemeTokens.StatusClassFor(CommandStatusCategory.Warning));
        }

        [Fact]
        public void SafeUiStateCopyDoesNotInventLiveData()
        {
            var global = ThemeTokens.GlobalSafetyLabel(false);
            var empty = ThemeTokens.EmptyStateMessage("Settings");
            var error = ThemeTokens.ErrorStateMessage("Data Sources");

            Assert.Equal("No live FM26 data", global);
            Assert.Contains("not built yet", empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No fake data", empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("could not load safely", error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No raw provider data", error, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FM26 supported", global + empty + error, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fake live", global + empty + error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CommandCenterNavigationKeepsBuiltPagesAndSafePlaceholders()
        {
            var items = UnityNavigationCatalog.Items;
            var names = items.Select(item => item.Name).ToArray();

            Assert.Contains("Home", names);
            Assert.Contains("Recruitment", names);
            Assert.Contains("Player Profile", names);
            Assert.Contains("Shortlists", names);
            Assert.Contains("Scout Desk", names);
            Assert.Contains("Role Lab", names);
            Assert.Contains("Benchmarks", names);
            Assert.Contains("Data Sources", names);
            Assert.Contains("Diagnostics", names);
            Assert.Contains(items, item => item.Name == "Squad" && !item.IsBuilt && item.SafeSubtitle.Contains("No fake", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, item => item.Name == "Settings" && !item.IsBuilt && item.SafeSubtitle.Contains("not built yet", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(items, item => item.SafeSubtitle.Contains("FM26 supported", StringComparison.OrdinalIgnoreCase));
        }
    }
}
