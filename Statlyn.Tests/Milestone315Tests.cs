using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Statlyn.Api;

namespace Statlyn.Tests
{
    public sealed class Milestone315Tests
    {
        [Fact]
        public void ReactTauriCockpitRemainsApiOnlyAndSafetyBounded()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var text = ReadDesktopAndTauriText(desktop);
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var api = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));

            Assert.Contains("Professional Recruitment Workspace", app, StringComparison.Ordinal);
            Assert.Contains("Search local player, position, source or recommendation", app, StringComparison.Ordinal);
            Assert.Contains("Clear filters", app, StringComparison.Ordinal);
            Assert.Contains("API Offline", app, StringComparison.Ordinal);
            Assert.Contains("No demo rows", app, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No live FM26 data", app, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/connector/status", api, StringComparison.Ordinal);
            Assert.DoesNotContain("Insight Panel", app, StringComparison.Ordinal);
            Assert.DoesNotContain("InsightRail", app, StringComparison.Ordinal);

            foreach (var forbidden in new[]
            {
                "RecruitmentDecisionEngine",
                "RoleScoringEngine",
                "BenchmarkEngine",
                "ScoutingKnowledgeFirewall",
                "sqlite",
                "rusqlite",
                "sqlx",
                "Statlyn.Data",
                "Statlyn.Analytics",
                "Statlyn.Scouting",
                "Statlyn.NativeConnector",
                "NativeFm26Connector",
                "OpenProcess",
                "ReadProcessMemory",
                "readMemory",
                "memory map",
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "RawValue"
            })
            {
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ReactTauriScoutRoomCorrectionAvoidsGameVisualsAndReferenceData()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var styles = File.ReadAllText(Path.Combine(desktop, "src", "styles.css"));
            var desktopText = string.Join("\n", app, styles);

            Assert.DoesNotContain("gradient", styles, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("insight-rail", styles, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("board-stat-grid", styles, StringComparison.Ordinal);
            Assert.Contains("Recruitment Board", app, StringComparison.Ordinal);
            Assert.Contains("Professional Recruitment Workspace", app, StringComparison.Ordinal);

            foreach (var referenceName in new[]
            {
                "Kenan Yildiz",
                "Joao Neves",
                "João Neves",
                "Warren Zaire-Emery",
                "Warren Zaïre-Emery",
                "Geovany Quenda",
                "Pau Cubarsi",
                "Pau Cubarsí",
                "Rayan Cherki",
                "Benjamin Sesko",
                "Benjamin Šeško",
                "Jorrel Hato",
                "Alejandro Garnacho",
                "Mamadou Sarr"
            })
            {
                Assert.DoesNotContain(referenceName, desktopText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ApiDtosRemainSafeForCockpitDisplay()
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
                Assert.DoesNotContain("Hidden", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Raw", name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("MemoryAddress", name, StringComparison.OrdinalIgnoreCase);
                Assert.False(string.Equals("CA", name, StringComparison.OrdinalIgnoreCase));
                Assert.False(string.Equals("PA", name, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public void UiDocsRecordPremiumAnalystCockpitDirection()
        {
            var root = FindRepositoryRoot();
            var reactDocs = File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md"));
            var commandDocs = File.ReadAllText(Path.Combine(root, "docs", "command-center-ui.md"));
            var uiDocs = File.ReadAllText(Path.Combine(root, "docs", "ui-design.md"));
            var readme = File.ReadAllText(Path.Combine(root, "README.md"));
            var combined = string.Join("\n", reactDocs, commandDocs, uiDocs, readme);

            Assert.Contains("professional dark football recruitment analyst cockpit", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri is the strategic", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no persistent insight panel", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("not a game UI", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No fake data", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No live FM26 data", combined, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri remains display/API-only", combined, StringComparison.OrdinalIgnoreCase);
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
    }
}
