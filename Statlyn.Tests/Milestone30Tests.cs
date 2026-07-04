using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Statlyn.Api;

namespace Statlyn.Tests
{
    public sealed class Milestone30Tests
    {
        [Fact]
        public async Task ApiEndpointsReturnSafeEmptyStatesForEmptyDatabase()
        {
            using var temp = new TemporaryDirectory();
            using var factory = CreateApiFactory(temp);
            using var client = factory.CreateClient();

            var health = await ReadJson<AppHealthDto>(client, "/health");
            var dashboard = await ReadJson<DashboardOverviewDto>(client, "/dashboard");
            var players = await ReadJson<List<PlayerListItemDto>>(client, "/players");
            var missingPlayer = await ReadJson<PlayerProfileDto>(client, "/players/missing-player");
            var diagnostics = await ReadJson<DiagnosticsDto>(client, "/diagnostics");

            Assert.Equal("ok", health.Status);
            Assert.False(health.IsFm26Supported);
            Assert.Contains("unsupported", health.ConnectorStatus, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No live FM26 data", health.SafeMessage, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, dashboard.ImportedPlayersCount);
            Assert.Contains("unsupported", dashboard.Fm26Status, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(players);

            Assert.False(missingPlayer.Found);
            Assert.Equal("missing-player", missingPlayer.StatlynPlayerId);
            Assert.Empty(missingPlayer.OutputMetrics);

            Assert.False(string.IsNullOrWhiteSpace(diagnostics.SafeSummary));
            Assert.Equal(0, diagnostics.ImportedPlayerCount);
            Assert.Contains("No live FM26 data", diagnostics.Fm26Status, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SerializedApiResponsesDoNotLeakHiddenRawOrDiagnosticImplementationNames()
        {
            using var temp = new TemporaryDirectory();
            using var factory = CreateApiFactory(temp);
            using var client = factory.CreateClient();

            foreach (var path in new[]
            {
                "/health",
                "/dashboard",
                "/players",
                "/players/missing-player",
                "/recruitment-board",
                "/role-lab",
                "/squad-gaps",
                "/comparisons",
                "/scout-reports",
                "/data-sources",
                "/diagnostics"
            })
            {
                var json = await client.GetStringAsync(path);
                AssertSafeJson(path, json);
            }
        }

        [Fact]
        public void DesktopSourceAndTauriConfigStayApiOnly()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var sourceText = ReadAllDesktopSource(desktop);

            foreach (var forbidden in new[]
            {
                "rusqlite",
                "sqlx",
                "sqlite",
                "Statlyn.Data",
                "Statlyn.Analytics",
                "Statlyn.Scouting",
                "Statlyn.NativeConnector",
                "NativeFm26Connector",
                "readMemory",
                "memory map",
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "RawValue"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }

            var apiClient = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));
            Assert.Contains("VITE_STATLYN_API_URL", apiClient, StringComparison.Ordinal);
            Assert.Contains("AbortController", apiClient, StringComparison.Ordinal);
            Assert.Contains("Cannot reach Statlyn.Api", apiClient, StringComparison.Ordinal);

            var tauriConfig = File.ReadAllText(Path.Combine(desktop, "src-tauri", "tauri.conf.json"));
            Assert.Contains("com.statlyn.desktop", tauriConfig, StringComparison.Ordinal);
            Assert.Contains("icons/icon.ico", tauriConfig, StringComparison.Ordinal);
            Assert.Contains("connect-src", tauriConfig, StringComparison.Ordinal);
            Assert.Contains("localhost:5118", tauriConfig, StringComparison.Ordinal);
        }

        [Fact]
        public void DesktopValidationScriptAndDocsRecordMilestone30Boundaries()
        {
            var root = FindRepositoryRoot();
            var validationScript = File.ReadAllText(Path.Combine(root, "tools", "run-desktop-validation.ps1"));
            Assert.Contains("dotnet build", validationScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet test", validationScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("check-native-readonly.ps1", validationScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/health", validationScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("npm run check", validationScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("npm run tauri:build", validationScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Stop-Process", validationScript, StringComparison.OrdinalIgnoreCase);

            var reactDocs = File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md"));
            Assert.Contains("Statlyn.Api", reactDocs, StringComparison.Ordinal);
            Assert.Contains("API must be started separately", reactDocs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sidecar", reactDocs, StringComparison.OrdinalIgnoreCase);

            var testingDocs = File.ReadAllText(Path.Combine(root, "docs", "testing.md"));
            Assert.Contains("Milestone 3.0", testingDocs, StringComparison.Ordinal);
            Assert.Contains("audit findings", testingDocs, StringComparison.OrdinalIgnoreCase);

            var auditDocs = File.ReadAllText(Path.Combine(root, "docs", "npm-audit-notes.md"));
            Assert.Contains("vite", auditDocs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("esbuild", auditDocs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("major", auditDocs, StringComparison.OrdinalIgnoreCase);

            var unityDocs = File.ReadAllText(Path.Combine(root, "docs", "unity-legacy-transition.md"));
            Assert.Contains("legacy/current shell only", unityDocs, StringComparison.OrdinalIgnoreCase);
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
                        ["Statlyn:DatabasePath"] = databasePath
                    });
                });
            });
        }

        private static async Task<T> ReadJson<T>(HttpClient client, string path)
        {
            var value = await client.GetFromJsonAsync<T>(path);
            Assert.NotNull(value);
            return value;
        }

        private static void AssertSafeJson(string path, string json)
        {
            JsonDocument.Parse(json).Dispose();
            foreach (var forbidden in new[]
            {
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "HiddenCurrentAbility",
                "HiddenPotentialAbility",
                "HiddenPersonality",
                "MemoryAddress",
                "RawValue",
                "PlayerRawSnapshot",
                "StackTrace",
                "ExceptionDetails"
            })
            {
                Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("\"ca\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"pa\"", json, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadAllDesktopSource(string desktop)
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
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-milestone30-" + Guid.NewGuid().ToString("N"));
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
