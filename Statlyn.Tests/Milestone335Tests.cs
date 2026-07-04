using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Statlyn.Api;

namespace Statlyn.Tests
{
    public sealed class Milestone335Tests
    {
        [Fact]
        public void ReactTauriUsesFlatAnalystVisualComponentsWithoutDecisionLogic()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var sourceText = ReadDesktopAndTauriText(desktop);

            foreach (var required in new[]
            {
                "MetricCard",
                "StatusMatrix",
                "RiskSignal",
                "DataQualityBar",
                "ModelConfidenceBar",
                "DistributionStrip",
                "SignalBadge",
                "DiagnosticLedger",
                "ComparisonMatrix",
                "VisualTableCell",
                "SectionHeader",
                "EmptyVisualState",
                "Model Control Panel",
                "Diagnostic risk ledger",
                "Trend unavailable"
            })
            {
                Assert.Contains(required, app, StringComparison.Ordinal);
            }

            foreach (var forbidden in new[]
            {
                "RecruitmentDecisionEngine",
                "RoleScoringEngine",
                "BenchmarkEngine",
                "ShortlistDecisionHelper",
                "sqlite",
                "rusqlite",
                "better-sqlite",
                "OpenProcess",
                "ReadProcessMemory",
                "NativeFm26Connector",
                "processMemory",
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ReactTauriAvoidsFakeRowsChartsReferenceDataAndDisplayedProcessInternals()
        {
            var root = FindRepositoryRoot();
            var app = File.ReadAllText(Path.Combine(root, "Statlyn.Desktop", "src", "App.tsx"));
            var styles = File.ReadAllText(Path.Combine(root, "Statlyn.Desktop", "src", "styles.css"));

            foreach (var forbidden in new[]
            {
                "Kenan Yildiz",
                "Joao Neves",
                "Warren Zaire-Emery",
                "Juventus",
                "Market Value",
                "fake radar",
                "fake sparkline",
                "line chart",
                "radar chart",
                "Process ID",
                "memory address",
                "raw offset",
                "handle"
            })
            {
                Assert.DoesNotContain(forbidden, app, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Contains("No demo rows are generated", app, StringComparison.Ordinal);
            Assert.Contains("No live FM26 data", app, StringComparison.Ordinal);
            Assert.DoesNotContain("linear-gradient", styles, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("radial-gradient", styles, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("backdrop-filter", styles, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReactTauriDocsDescribeFlatFinancialAnalystDirectionAndNoFakeVisuals()
        {
            var root = FindRepositoryRoot();
            var docs = string.Join(
                "\n",
                File.ReadAllText(Path.Combine(root, "README.md")),
                File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md")),
                File.ReadAllText(Path.Combine(root, "docs", "command-center-ui.md")),
                File.ReadAllText(Path.Combine(root, "docs", "ui-design.md")));

            Assert.Contains("React/Tauri is the strategic", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("flat financial-analysis-style", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dense readable tables", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No fake visuals", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri remains display/API-only", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FM26 remains unsupported", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("metadata-only", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No player data is read", docs, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ApiDtosRemainSafeForFlatVisualDisplay()
        {
            foreach (var type in new[]
            {
                typeof(AppHealthDto),
                typeof(DashboardOverviewDto),
                typeof(RecruitmentBoardDto),
                typeof(PlayerListItemDto),
                typeof(MemoryMapRegistryDto),
                typeof(MemoryMapDiagnosticDto),
                typeof(Fm26ConnectorStatusDto)
            })
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var name = property.Name;
                    Assert.DoesNotContain("Offset", name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Pointer", name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Address", name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Handle", name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Raw", name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("CurrentAbility", name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("PotentialAbility", name, StringComparison.OrdinalIgnoreCase);
                    Assert.False(string.Equals("CA", name, StringComparison.OrdinalIgnoreCase));
                    Assert.False(string.Equals("PA", name, StringComparison.OrdinalIgnoreCase));
                }
            }
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
