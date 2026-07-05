using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Statlyn.Analytics.PlayerIntelligence;
using Statlyn.Api;
using Statlyn.Data;
using Statlyn.Data.PlayerIntelligence;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.Tests
{
    public sealed class Milestone40Tests
    {
        [Fact]
        public void PlayerIntelligenceModelsAndDtosContainNoHiddenFmFields()
        {
            var types = new[]
            {
                typeof(PlayerIntelligenceProfile),
                typeof(PlayerSkillRadar),
                typeof(PlayerRadarAxis),
                typeof(PlayerPer90Metric),
                typeof(PlayerHeatmapSummary),
                typeof(PlayerHeatmapPoint),
                typeof(PlayerValueEstimate),
                typeof(PlayerFitProjection),
                typeof(PlayerArchetypeResult),
                typeof(PlayerSimilarityResult),
                typeof(SimilarPlayerCandidate),
                typeof(LeagueAverageComparison),
                typeof(RoleSpecificAssessment),
                typeof(RoleParameterDefinition),
                typeof(RoleParameterMetric),
                typeof(PlayerStyleVector),
                typeof(TeamStyleModel),
                typeof(PlayerDataAvailabilityReport),
                typeof(PlayerIntelligenceDto),
                typeof(PlayerValueEstimateDto),
                typeof(PlayerHeatmapDto),
                typeof(PlayerSimilarityDto)
            };

            foreach (var type in types)
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertSafeName(property.Name);
                }
            }
        }

        [Fact]
        public void ServicesReturnSafeUnavailableStatesWhenNoDataExists()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var service = new PlayerIntelligenceService(factory);

            var readiness = service.GetReadiness();
            var intelligence = service.GetIntelligence("missing-player");

            Assert.False(readiness.Available);
            Assert.Contains("awaiting local player data", readiness.SafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(intelligence.Profile.Available);
            Assert.False(intelligence.Heatmap.Available);
            Assert.False(intelligence.ValueEstimate.Available);
            Assert.False(intelligence.SimilarPlayers.Available);
            Assert.False(intelligence.LeagueComparison.Available);
            Assert.Empty(intelligence.Heatmap.Points);
            Assert.Empty(intelligence.SimilarPlayers.Candidates);
            Assert.Null(intelligence.ValueEstimate.FairValueMid);
        }

        [Fact]
        public void FairValueUnavailableWithoutAnchorOrComparableSample()
        {
            var result = new PlayerValueEstimateService().Estimate(new FairValueInput
            {
                Age = 22,
                RoleName = "Take-on Winger",
                Minutes = 1800,
                PerformanceIndex = 70,
                RoleFit = 75,
                DataCompleteness = 85
            });

            Assert.False(result.Available);
            Assert.Null(result.FairValueLow);
            Assert.Null(result.FairValueMid);
            Assert.Contains("valuation anchor", result.MissingInputs, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FairValueReturnsRangeWithSafeAnchorAndPerformanceInputs()
        {
            var result = new PlayerValueEstimateService().Estimate(new FairValueInput
            {
                Currency = "EUR",
                AnchorValue = 5000000,
                Age = 21,
                RoleName = "Take-on Winger",
                ContractMonthsRemaining = 36,
                Minutes = 2100,
                PerformanceIndex = 72,
                RoleFit = 76,
                TacticalFit = 68,
                LeagueStrengthMultiplier = 1.02,
                DataCompleteness = 88
            });

            Assert.True(result.Available);
            Assert.Equal("EUR", result.Currency);
            Assert.NotNull(result.FairValueLow);
            Assert.NotNull(result.FairValueMid);
            Assert.NotNull(result.FairValueHigh);
            Assert.True(result.FairValueLow < result.FairValueMid);
            Assert.True(result.FairValueMid < result.FairValueHigh);
            Assert.Contains("estimate", result.SafeMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MissingContractLowersFairValueConfidence()
        {
            var complete = FairValue(new FairValueInput { ContractMonthsRemaining = 36, Minutes = 1800, DataCompleteness = 90 });
            var missingContract = FairValue(new FairValueInput { ContractMonthsRemaining = null, Minutes = 1800, DataCompleteness = 90 });

            Assert.True(complete.Confidence > missingContract.Confidence);
            Assert.Contains("contract context", missingContract.MissingInputs, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void WeakSampleSizeWidensFairValueRange()
        {
            var strongSample = FairValue(new FairValueInput { ContractMonthsRemaining = 36, Minutes = 1800, DataCompleteness = 90 });
            var weakSample = FairValue(new FairValueInput { ContractMonthsRemaining = 36, Minutes = 300, DataCompleteness = 90 });

            Assert.True(RangeWidth(weakSample) > RangeWidth(strongSample));
            Assert.True(strongSample.Confidence > weakSample.Confidence);
        }

        [Fact]
        public void HighRoleFitPremiumIsCapped()
        {
            var lowFit = FairValue(new FairValueInput
            {
                RoleName = "Ball-carrying Centre-back",
                RoleFit = 55,
                PerformanceIndex = 72,
                ContractMonthsRemaining = 36,
                Minutes = 1800,
                DataCompleteness = 90
            });
            var highFit = FairValue(new FairValueInput
            {
                RoleName = "Ball-carrying Centre-back",
                RoleFit = 95,
                PerformanceIndex = 72,
                ContractMonthsRemaining = 36,
                Minutes = 1800,
                DataCompleteness = 90
            });

            Assert.True(highFit.FairValueMid.HasValue);
            Assert.True(lowFit.FairValueMid.HasValue);
            Assert.True(highFit.FairValueMid.Value / lowFit.FairValueMid.Value <= 1.11);
        }

        [Fact]
        public void RoleParameterDefinitionsCoverMajorFamilies()
        {
            var service = new RoleParameterDefinitionService();
            var definitions = service.GetDefaultDefinitions();

            foreach (var role in new[]
            {
                "Goalkeeper",
                "Centre-back",
                "Ball-carrying Centre-back",
                "Full-back",
                "Wing-back",
                "Defensive Midfielder",
                "Controller Midfielder",
                "Attacking Midfielder",
                "Winger",
                "Take-on Winger",
                "Inside Forward",
                "Pressing Forward",
                "Target Forward",
                "Poacher / Finisher"
            })
            {
                Assert.Contains(definitions, definition => string.Equals(definition.RoleName, role, StringComparison.OrdinalIgnoreCase));
            }

            var carryingCentreBack = service.FindByRole("Ball-carrying Centre-back");
            Assert.NotNull(carryingCentreBack);
            Assert.Contains(carryingCentreBack!.PrimaryMetrics, metric => metric.MetricKey == "progressiveCarries");
            Assert.Contains(carryingCentreBack.PrimaryMetrics, metric => metric.MetricKey == "progressivePasses");
            Assert.Contains(carryingCentreBack.SecondaryMetrics, metric => metric.MetricKey == "defensiveDuelSuccess");

            var shotStoppingGoalkeeper = service.FindByRole("Shot-stopping Goalkeeper");
            Assert.NotNull(shotStoppingGoalkeeper);
            Assert.Contains(shotStoppingGoalkeeper!.PrimaryMetrics, metric => metric.MetricKey == "savePercentage");
            Assert.Contains(shotStoppingGoalkeeper.PrimaryMetrics, metric => metric.MetricKey == "shotsFaced");
        }

        [Fact]
        public async Task ApiEndpointsReturnSafeUnavailableDtos()
        {
            using var temp = new TemporaryDirectory();
            using var factory = CreateApiFactory(temp);
            using var client = factory.CreateClient();

            var readiness = await ReadJson<PlayerIntelligenceReadinessDto>(client, "/analytics/player-intelligence/readiness");
            var intelligence = await ReadJson<PlayerIntelligenceDto>(client, "/players/missing-player/intelligence");
            var heatmap = await ReadJson<PlayerHeatmapDto>(client, "/players/missing-player/heatmap");
            var value = await ReadJson<PlayerValueEstimateDto>(client, "/players/missing-player/value");
            var similar = await ReadJson<PlayerSimilarityDto>(client, "/players/missing-player/similar");
            var league = await ReadJson<LeagueAverageComparisonDto>(client, "/players/missing-player/league-comparison");
            var health = await ReadJson<AppHealthDto>(client, "/health");
            var json = await client.GetStringAsync("/players/missing-player/intelligence");

            Assert.False(readiness.Available);
            Assert.False(intelligence.Profile.Available);
            Assert.False(heatmap.Available);
            Assert.False(value.Available);
            Assert.False(similar.Available);
            Assert.False(league.Available);
            Assert.Empty(heatmap.Points);
            Assert.Empty(similar.Candidates);
            Assert.Null(value.FairValueMid);
            Assert.False(health.IsFm26Supported);
            AssertSafeJson(json);
        }

        [Fact]
        public void ReactTauriPlayerIntelligenceRemainsDisplayOnly()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var api = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));
            var sourceText = ReadDesktopAndTauriText(desktop);

            Assert.Contains("Player Intelligence", app, StringComparison.Ordinal);
            Assert.Contains("Statlyn Fair Value Estimate", app, StringComparison.Ordinal);
            Assert.Contains("Heatmap unavailable", app, StringComparison.Ordinal);
            Assert.Contains("/analytics/player-intelligence/readiness", api, StringComparison.Ordinal);
            Assert.Contains("/intelligence", api, StringComparison.Ordinal);

            foreach (var forbidden in new[]
            {
                "RecruitmentDecisionEngine",
                "RoleScoringEngine",
                "PlayerValueEstimateService",
                "sqlite",
                "better-sqlite",
                "OpenProcess",
                "ReadProcessMemory",
                "NativeFm26Connector",
                "CurrentAbility",
                "PotentialAbility",
                "Kenan Yildiz",
                "Joao Neves",
                "Warren Zaire-Emery",
                "fake radar",
                "fake heatmap"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void DocsDescribePlayerIntelligenceFoundationAndDataBoundaries()
        {
            var root = FindRepositoryRoot();
            var docs = string.Join(
                "\n",
                File.ReadAllText(Path.Combine(root, "docs", "player-intelligence-analytics.md")),
                File.ReadAllText(Path.Combine(root, "docs", "player-intelligence-data-availability.md")),
                File.ReadAllText(Path.Combine(root, "docs", "advanced-recruitment-analysis-roadmap.md")),
                File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md")));

            Assert.Contains("Player Intelligence Analytics Foundation", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no fake data", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri is the strategic", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri remains display/API-only", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FM26 remains unsupported", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Fair value unavailable. Missing valuation anchor, contract context or comparable player sample.", docs, StringComparison.Ordinal);
            Assert.Contains("Heatmap unavailable. No safe event-location data has been imported.", docs, StringComparison.Ordinal);
        }

        private static PlayerValueEstimate FairValue(FairValueInput overrides)
        {
            return new PlayerValueEstimateService().Estimate(new FairValueInput
            {
                Currency = "EUR",
                AnchorValue = 10000000,
                Age = 23,
                RoleName = string.IsNullOrWhiteSpace(overrides.RoleName) ? "Take-on Winger" : overrides.RoleName,
                ContractMonthsRemaining = overrides.ContractMonthsRemaining,
                Minutes = overrides.Minutes,
                PerformanceIndex = overrides.PerformanceIndex ?? 70,
                RoleFit = overrides.RoleFit ?? 75,
                TacticalFit = 65,
                LeagueStrengthMultiplier = 1.0,
                DataCompleteness = overrides.DataCompleteness
            });
        }

        private static double RangeWidth(PlayerValueEstimate estimate)
        {
            Assert.True(estimate.FairValueLow.HasValue);
            Assert.True(estimate.FairValueMid.HasValue);
            Assert.True(estimate.FairValueHigh.HasValue);
            return (estimate.FairValueHigh.Value - estimate.FairValueLow.Value) / estimate.FairValueMid.Value;
        }

        private static WebApplicationFactory<Program> CreateApiFactory(TemporaryDirectory temp)
        {
            var databasePath = Path.Combine(temp.Path, "statlyn-api-test.db");
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Statlyn:DatabasePath"] = databasePath,
                        ["Statlyn:MemoryMapsPath"] = temp.MapsPath
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IFm26NativeConnector>();
                    services.RemoveAll<SafeFm26ConnectorService>();
                    services.AddSingleton<IFm26NativeConnector>(new NullFm26NativeConnector());
                    services.AddSingleton<SafeFm26ConnectorService>();
                });
            });
        }

        private static async Task<T> ReadJson<T>(HttpClient client, string path)
        {
            var value = await client.GetFromJsonAsync<T>(path);
            Assert.NotNull(value);
            return value;
        }

        private static void AssertSafeJson(string json)
        {
            JsonDocument.Parse(json).Dispose();
            foreach (var forbidden in new[]
            {
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "MemoryAddress",
                "BaseAddress",
                "Pointer",
                "Offset",
                "Handle",
                "RawValue",
                "StackTrace",
                "ExceptionDetails",
                "0xDEADBEEF",
                "\"ca\"",
                "\"pa\""
            })
            {
                Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void AssertSafeName(string name)
        {
            foreach (var forbidden in new[]
            {
                "Offset",
                "Pointer",
                "Address",
                "Handle",
                "Raw",
                "CurrentAbility",
                "PotentialAbility"
            })
            {
                Assert.DoesNotContain(forbidden, name, StringComparison.OrdinalIgnoreCase);
            }

            Assert.False(string.Equals("CA", name, StringComparison.OrdinalIgnoreCase));
            Assert.False(string.Equals("PA", name, StringComparison.OrdinalIgnoreCase));
        }

        private static string ReadDesktopAndTauriText(string desktop)
        {
            var files = Directory.GetFiles(Path.Combine(desktop, "src"), "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
                .Concat(Directory.GetFiles(Path.Combine(desktop, "src-tauri", "src"), "*.rs", SearchOption.AllDirectories))
                .Concat(new[]
                {
                    Path.Combine(desktop, "src-tauri", "Cargo.toml"),
                    Path.Combine(desktop, "src-tauri", "tauri.conf.json"),
                    Path.Combine(desktop, "package.json")
                })
                .Where(File.Exists);

            return string.Join("\n", files.Select(File.ReadAllText));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Statlyn.sln")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new InvalidOperationException("Could not find repository root.");
            }

            return directory.FullName;
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-tests-" + Guid.NewGuid().ToString("N"));
                MapsPath = System.IO.Path.Combine(Path, "maps");
                Directory.CreateDirectory(MapsPath);
            }

            public string Path { get; }

            public string MapsPath { get; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }
}
