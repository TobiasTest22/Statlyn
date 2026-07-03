using System;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Dashboard;
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
            Assert.Equal(CommandStatusCategory.Warning, ThemeTokens.ResolveStatusCategory("Insufficient sample"));
            Assert.Equal(CommandStatusCategory.Danger, ThemeTokens.ResolveStatusCategory("Runtime error"));
            Assert.Equal(CommandStatusCategory.Info, ThemeTokens.ResolveStatusCategory("No live FM26 data"));
            Assert.Equal(CommandStatusCategory.Info, ThemeTokens.ResolveStatusCategory("Generic/import metric"));
            Assert.Equal(CommandStatusCategory.Muted, ThemeTokens.ResolveStatusCategory("This page is not built yet."));
            Assert.Equal(CommandStatusCategory.Muted, ThemeTokens.ResolveStatusCategory("No benchmark yet."));
            Assert.Equal(CommandStatusCategory.Success, ThemeTokens.BenchmarkStatus("Available benchmark."));
            Assert.Equal(CommandStatusCategory.Warning, ThemeTokens.BenchmarkStatus("Insufficient sample."));
            Assert.Equal(CommandStatusCategory.Muted, ThemeTokens.BenchmarkStatus("No benchmark yet."));
            Assert.Equal(CommandStatusCategory.Warning, ThemeTokens.Fm26Status(false));
            Assert.NotEqual(CommandStatusCategory.Success, ThemeTokens.Fm26Status(false));
            Assert.Equal("status-warning", ThemeTokens.StatusClassFor(CommandStatusCategory.Warning));
            Assert.Equal("status-info", ThemeTokens.StatusClassFor(CommandStatusCategory.Info));
            Assert.Equal("status-muted", ThemeTokens.StatusClassFor(CommandStatusCategory.Muted));
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

        [Fact]
        public void EmptyDatabaseDashboardShowsAwaitingLocalDataWithoutFakeCounts()
        {
            using var temp = new TemporaryDirectory();
            using var factory = RuntimeDatabaseFactory.CreateFile(Path.Combine(temp.Path, "statlyn.db"));
            var overview = new DashboardStatusService(factory).BuildOverview();

            Assert.Equal(0, overview.ImportedPlayersCount);
            Assert.Equal(0, overview.ShortlistCount);
            Assert.Equal(0, overview.ScoutAssignmentCount);
            Assert.Contains("Awaiting local data", overview.ImportedPlayersStatus, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Unsupported", overview.Fm26Status, StringComparison.OrdinalIgnoreCase);
            Assert.False(overview.HasLiveFm26Data);
            Assert.False(overview.IsFm26Supported);
            Assert.DoesNotContain("fake live", overview.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", overview.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", overview.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ImportedDatabaseDashboardShowsSafeLocalCounts()
        {
            using var temp = new TemporaryDirectory();
            var smoke = new UnitySmokeTestService().Run(new UnitySmokeTestOptions
            {
                TemporaryRoot = temp.Path,
                ApplicationDataPath = AppContext.BaseDirectory,
                StreamingAssetsPath = Path.Combine(AppContext.BaseDirectory, "StreamingAssets"),
                MainDatabasePath = Path.Combine(temp.Path, "statlyn.db"),
                ClearSmokeTestDatabase = true
            });

            Assert.True(smoke.Success, smoke.ToSafeText());
            using var factory = RuntimeDatabaseFactory.CreateFile(smoke.DatabasePath);
            var overview = new DashboardStatusService(factory).BuildOverview();

            Assert.True(overview.ImportedPlayersCount > 0);
            Assert.True(overview.ShortlistCount > 0);
            Assert.True(overview.ScoutAssignmentCount > 0);
            Assert.True(overview.RoleLabTemplateCount > 0);
            Assert.True(overview.BenchmarkDefinitionCount > 0);
            Assert.Contains("Safe local data", overview.RecruitmentCentreStatus, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FM26 supported", overview.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", overview.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FullSmokeTestStillPassesAfterCommandCenterUiMilestone()
        {
            using var temp = new TemporaryDirectory();
            var result = new UnitySmokeTestService().Run(new UnitySmokeTestOptions
            {
                TemporaryRoot = temp.Path,
                ApplicationDataPath = AppContext.BaseDirectory,
                StreamingAssetsPath = Path.Combine(AppContext.BaseDirectory, "StreamingAssets"),
                MainDatabasePath = Path.Combine(temp.Path, "statlyn.db"),
                ClearSmokeTestDatabase = true
            });

            Assert.True(result.Success, result.ToSafeText());
            Assert.Contains("CSV-only", result.SafeSummary);
            Assert.DoesNotContain("fake live", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                    {
                        Directory.Delete(Path, recursive: true);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
