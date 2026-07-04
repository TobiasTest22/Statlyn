using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Statlyn.Analytics;
using Statlyn.Api;
using Statlyn.Data;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.Tests
{
    public sealed class ArchitectureMigrationTests
    {
        [Fact]
        public void StrategicCSharpProjectsDoNotReferenceUnity()
        {
            var root = FindRepositoryRoot();
            foreach (var project in new[]
            {
                "Statlyn.Core/Statlyn.Core.csproj",
                "Statlyn.Analytics/Statlyn.Analytics.csproj",
                "Statlyn.Data/Statlyn.Data.csproj",
                "Statlyn.Scouting/Statlyn.Scouting.csproj",
                "Statlyn.DataProviders/Statlyn.DataProviders.csproj",
                "Statlyn.Api/Statlyn.Api.csproj"
            })
            {
                var text = File.ReadAllText(Path.Combine(root, project));
                Assert.DoesNotContain("Unity", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Statlyn.UnityApp", text, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ApiDtosDoNotExposeHiddenOrRawProviderFields()
        {
            var dtoTypes = typeof(AppHealthDto).Assembly.GetTypes()
                .Where(type => type.Namespace == "Statlyn.Api" && type.Name.EndsWith("Dto", StringComparison.Ordinal))
                .ToList();

            Assert.NotEmpty(dtoTypes);
            foreach (var property in dtoTypes.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)))
            {
                var name = property.Name;
                Assert.DoesNotContain("CurrentAbility", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("PotentialAbility", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Professionalism", name, StringComparison.OrdinalIgnoreCase);
                Assert.False(string.Equals("CA", name, StringComparison.OrdinalIgnoreCase));
                Assert.False(string.Equals("PA", name, StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain("Raw", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("MemoryAddress", name, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ApiReturnsSafeEmptyStatesForUnsupportedConnectorAndEmptyDatabase()
        {
            using var temp = new TemporaryDirectory();
            using var factory = RuntimeDatabaseFactory.CreateFile(Path.Combine(temp.Path, "statlyn.db"));
            var api = new StatlynApiDtoFactory(factory);

            var health = api.GetHealth();
            var players = api.GetPlayers();
            var diagnostics = api.GetDiagnostics();

            Assert.False(health.IsFm26Supported);
            Assert.Contains("unsupported", health.ConnectorStatus, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(players);
            Assert.Equal(0, diagnostics.ImportedPlayerCount);
            Assert.Contains("No live FM26 data", diagnostics.Fm26Status, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AnalyticsRejectRawProviderObjectsAndBenchmarkInsufficientSamples()
        {
            Assert.Throws<InvalidOperationException>(() => new RoleScoringEngine().ScorePlayer(new object(), new RoleModel("Wide Forward")));

            var benchmark = new BenchmarkEngine().EvaluateSample("Wide attackers", sampleSize: 3, minimumSampleSize: 10);

            Assert.Equal(DecisionStatus.InsufficientData, benchmark.Status);
            Assert.Null(benchmark.Score);
            Assert.Contains("No fake percentile", benchmark.SafeSummary, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UnsupportedFmProviderReturnsNoFakePlayerData()
        {
            var provider = new Fm26LiveMemoryProvider(new NullFm26NativeConnector());
            var connected = provider.Connect();
            var players = provider.ReadPlayers();

            Assert.False(connected.Success);
            Assert.False(players.Success);
            Assert.Null(players.Value);
            Assert.Contains("not connected", players.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReactDesktopContainsNoFootballDecisionEngines()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop", "src");
            var text = string.Join(
                "\n",
                Directory.GetFiles(desktop, "*.*", SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
                    .Select(File.ReadAllText));

            foreach (var forbidden in new[]
            {
                "RecruitmentDecisionEngine",
                "RoleScoringEngine",
                "BenchmarkEngine",
                "OutperformanceEngine",
                "SquadGapEngine",
                "PlayerComparisonEngine",
                "RedFlagEngine",
                "ScoutingKnowledgeFirewall",
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism"
            })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ReactTauriDesktopDoesNotBypassApiForStorageProvidersOrNativeConnector()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var scannedFiles = Directory.GetFiles(Path.Combine(desktop, "src"), "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
                .Concat(Directory.GetFiles(Path.Combine(desktop, "src-tauri", "src"), "*.rs", SearchOption.AllDirectories))
                .Concat(new[]
                {
                    Path.Combine(desktop, "src-tauri", "Cargo.toml"),
                    Path.Combine(desktop, "package.json")
                })
                .Where(File.Exists)
                .ToList();

            Assert.NotEmpty(scannedFiles);
            var text = string.Join("\n", scannedFiles.Select(File.ReadAllText));

            foreach (var forbidden in new[]
            {
                "sqlite",
                "rusqlite",
                "sqlx",
                "better-sqlite",
                "Microsoft.Data.Sqlite",
                "System.Data.SQLite",
                "Statlyn.Data",
                "Statlyn.Analytics",
                "Statlyn.Scouting",
                "Statlyn.DataProviders",
                "readMemory",
                "memory map",
                "Statlyn.NativeConnector",
                "NativeFm26Connector"
            })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }

            var apiClient = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));
            Assert.Contains("VITE_STATLYN_API_URL", apiClient, StringComparison.Ordinal);
            Assert.Contains("fetch(", apiClient, StringComparison.Ordinal);
            Assert.Contains("/health", apiClient, StringComparison.Ordinal);
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
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-architecture-" + Guid.NewGuid().ToString("N"));
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
