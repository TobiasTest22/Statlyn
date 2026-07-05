using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Statlyn.Api;
using Statlyn.Data;
using Statlyn.Data.Fm26Snapshots;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.Tests
{
    public sealed class Milestone35Tests
    {
        [Fact]
        public void SnapshotPersistenceSchemaCreatesSafeAuditTables()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            new StatlynDatabaseInitializer(factory).Initialize();
            var snapshotSchema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements.Where(statement => statement.Contains("fm26_snapshot", StringComparison.OrdinalIgnoreCase)));

            Assert.Equal(6, StatlynSchemaVersion.Current);
            Assert.True(TableExists(factory, "fm26_snapshot_runs"));
            Assert.True(TableExists(factory, "fm26_snapshot_gate_results"));
            Assert.Contains("snapshot_id TEXT PRIMARY KEY", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("live_reading_allowed INTEGER NOT NULL", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("address", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("offset", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("handle", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("currentAbility", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("potentialAbility", snapshotSchema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("player_data", snapshotSchema, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RepositoryPersistsSnapshotsAndGateRowsInStableOrder()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var repository = new Fm26SnapshotRepository(factory);
            var older = Record("fm26-snapshot-old", DateTimeOffset.UtcNow.AddMinutes(-5), "BlockedConnectorUnavailable", "connector");
            var latest = Record("fm26-snapshot-new", DateTimeOffset.UtcNow, "BlockedNoValidatedMap", "validatedMap");
            latest.Gates = new[]
            {
                Gate(latest.SnapshotId, "validatedMap", "Validated Map", "Blocked", 1),
                Gate(latest.SnapshotId, "connector", "Connector", "Passed", 0)
            };

            repository.SaveSnapshot(older);
            repository.SaveSnapshot(latest);

            var loadedLatest = repository.GetLatestSnapshot();
            var list = repository.ListSnapshots(1);
            var loadedById = repository.GetSnapshotById(latest.SnapshotId);

            Assert.NotNull(loadedLatest);
            Assert.Equal(latest.SnapshotId, loadedLatest!.SnapshotId);
            Assert.Single(list);
            Assert.Equal(latest.SnapshotId, list[0].SnapshotId);
            Assert.NotNull(loadedById);
            Assert.Equal(new[] { "connector", "validatedMap" }, loadedById!.Gates.Select(gate => gate.GateKey).ToArray());
            Assert.False(loadedById.LiveReadingAllowed);
            Assert.Equal(2, repository.CountSnapshots());
        }

        [Fact]
        public void HistoryServiceHandlesMissingAndInvalidSnapshotLookupSafely()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var service = new Fm26SnapshotHistoryService(factory);

            var latest = service.GetLatestSnapshot();
            var blank = service.GetSnapshotById(" ");
            var missing = service.GetSnapshotById("missing-id");

            Assert.False(latest.Success);
            Assert.Contains("No persisted", latest.SafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(blank.Success);
            Assert.Contains("Snapshot id is required", blank.SafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.False(missing.Success);
            Assert.Contains("not found", missing.SafeMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async System.Threading.Tasks.Task PreviewEndpointDoesNotPersistHistory()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());
            using var factory = CreateApiFactory(temp, temp.MapsPath, new NullFm26NativeConnector());
            using var client = factory.CreateClient();

            var preview = await ReadJson<Fm26SnapshotDto>(client, "/diagnostics/fm26/snapshot");
            var history = await ReadJson<Fm26SnapshotHistoryDto>(client, "/diagnostics/fm26/snapshots");

            Assert.Equal("BlockedConnectorUnavailable", preview.SnapshotStatus);
            Assert.Equal(0, history.TotalCount);
            Assert.Empty(history.Snapshots);
            Assert.Null(history.LatestSnapshot);
        }

        [Fact]
        public async System.Threading.Tasks.Task PersistedSnapshotEndpointsCreateLatestHistoryAndDetailSafely()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());
            using var factory = CreateApiFactory(temp, temp.MapsPath, new NullFm26NativeConnector());
            using var client = factory.CreateClient();

            var create = await PostJson<Fm26SnapshotCreateResultDto>(client, "/diagnostics/fm26/snapshots");
            var latest = await ReadJson<Fm26SnapshotLookupDto>(client, "/diagnostics/fm26/snapshots/latest");
            var history = await ReadJson<Fm26SnapshotHistoryDto>(client, "/diagnostics/fm26/snapshots");
            var detail = await ReadJson<Fm26SnapshotLookupDto>(client, "/diagnostics/fm26/snapshots/" + create.Snapshot!.SnapshotId);
            var health = await ReadJson<AppHealthDto>(client, "/health");
            var json = await client.GetStringAsync("/diagnostics/fm26/snapshots");

            Assert.True(create.Success);
            Assert.NotNull(create.Snapshot);
            Assert.False(create.Snapshot!.LiveReadingAllowed);
            Assert.Equal("connector", create.Snapshot.BlockingGate);
            Assert.True(latest.Found);
            Assert.Equal(create.Snapshot.SnapshotId, latest.Snapshot!.SnapshotId);
            Assert.Equal(1, history.TotalCount);
            Assert.Single(history.Snapshots);
            Assert.True(detail.Found);
            Assert.False(health.IsFm26Supported);
            AssertSafeSnapshotAuditJson(json);
        }

        [Fact]
        public void PersistedModelsAndDtosExposeNoUnsafeMemoryOrPlayerFields()
        {
            foreach (var type in new[]
            {
                typeof(PersistedFm26SnapshotRecord),
                typeof(PersistedFm26SnapshotGateRecord),
                typeof(Fm26SnapshotHistoryResult),
                typeof(Fm26SnapshotSummaryDto),
                typeof(Fm26PersistedSnapshotDto),
                typeof(Fm26SnapshotHistoryDto),
                typeof(Fm26SnapshotCreateResultDto),
                typeof(Fm26SnapshotLookupDto)
            })
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertSafePropertyName(property.Name);
                    Assert.DoesNotContain("Player", property.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Squad", property.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Attribute", property.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void ReactTauriSnapshotAuditRemainsApiOnlyAndHasSafeEmptyState()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var api = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));
            var types = File.ReadAllText(Path.Combine(desktop, "src", "types.ts"));
            var sourceText = ReadDesktopAndTauriText(desktop);

            Assert.Contains("/diagnostics/fm26/snapshots", api, StringComparison.Ordinal);
            Assert.Contains("createPersistedFm26Snapshot", api, StringComparison.Ordinal);
            Assert.Contains("No persisted snapshots yet", app, StringComparison.Ordinal);
            Assert.Contains("Persisted Audit Trail", app, StringComparison.Ordinal);
            Assert.Contains("Fm26SnapshotHistoryDto", types, StringComparison.Ordinal);

            foreach (var forbidden in new[]
            {
                "OpenProcess",
                "ReadProcessMemory",
                "NativeFm26Connector",
                "rusqlite",
                "better-sqlite",
                "JSON.parse(local",
                "readFile",
                "fs.",
                "CurrentAbility",
                "PotentialAbility",
                "RawValue",
                "MemoryAddress",
                "BaseAddress",
                "Offset",
                "Handle"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void DocsDescribeSnapshotAuditTrailAndReactBoundary()
        {
            var root = FindRepositoryRoot();
            var docs = string.Join(
                Environment.NewLine,
                File.ReadAllText(Path.Combine(root, "docs", "fm26-safe-snapshot.md")),
                File.Exists(Path.Combine(root, "docs", "fm26-snapshot-audit-trail.md")) ? File.ReadAllText(Path.Combine(root, "docs", "fm26-snapshot-audit-trail.md")) : string.Empty,
                File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md")),
                File.ReadAllText(Path.Combine(root, "docs", "data-source-boundaries.md")));

            Assert.Contains("persisted snapshots are safe diagnostic metadata only", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No player data is stored", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("POST /diagnostics/fm26/snapshots", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Statlyn.Api", docs, StringComparison.OrdinalIgnoreCase);
        }

        private static PersistedFm26SnapshotRecord Record(string id, DateTimeOffset generatedAtUtc, string status, string blockingGate)
        {
            return new PersistedFm26SnapshotRecord
            {
                SnapshotId = id,
                GeneratedAtUtc = generatedAtUtc,
                SnapshotStatus = status,
                SafeMessage = "Safe FM26 diagnostic snapshot persisted. No player data was stored.",
                ConnectorAvailability = "Available",
                PlatformStatus = "Windows",
                ProcessDetected = false,
                ProcessStatus = "NotDetected",
                ProcessName = "fm.exe",
                ProcessId = 2600,
                ProductVersion = "26.0.0-test",
                FileVersion = "26.0.0-test",
                Architecture = "x64",
                ReadOnlyAccessStatus = "Unavailable",
                MemoryMapRegistryStatus = "TemplateOnly",
                MapsFound = 1,
                ValidatedMaps = 0,
                TemplateMaps = 1,
                InvalidMaps = 0,
                AllGatesPassed = false,
                BlockingGate = blockingGate,
                LiveReadingAllowed = false,
                NextActionSafeMessage = "Validate an FM26 memory map before any future live-reading milestone.",
                WarningCount = 1,
                ErrorCount = 0,
                Gates = new[] { Gate(id, blockingGate, "Blocking Gate", "Blocked", 0) }
            };
        }

        private static PersistedFm26SnapshotGateRecord Gate(string snapshotId, string key, string name, string status, int order)
        {
            return new PersistedFm26SnapshotGateRecord
            {
                SnapshotId = snapshotId,
                GateKey = key,
                GateName = name,
                Status = status,
                SafeMessage = "Gate result persisted as safe diagnostics metadata only.",
                SortOrder = order
            };
        }

        private static string TemplatePlayersMap()
        {
            return @"{
  ""game"": ""Football Manager 26"",
  ""build"": ""template"",
  ""entity"": ""players"",
  ""isTemplate"": true,
  ""supported"": false,
  ""fields"": [
    { ""fieldName"": ""displayName"", ""dataType"": ""string"", ""visibilityCategory"": ""alwaysVisible"", ""canDisplay"": true, ""canScore"": false, ""canStore"": true },
    { ""fieldName"": ""currentAbility"", ""dataType"": ""int"", ""visibilityCategory"": ""neverVisible"", ""canDisplay"": false, ""canScore"": false, ""canStore"": false },
    { ""fieldName"": ""potentialAbility"", ""dataType"": ""int"", ""visibilityCategory"": ""neverVisible"", ""canDisplay"": false, ""canScore"": false, ""canStore"": false }
  ]
}";
        }

        private static void WriteMap(string directory, string name, string json)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), json);
        }

        private static WebApplicationFactory<Program> CreateApiFactory(TemporaryDirectory temp, string memoryMapsPath, IFm26NativeConnector connector)
        {
            var databasePath = Path.Combine(temp.Path, "statlyn-api-test.db");
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Statlyn:DatabasePath"] = databasePath,
                        ["Statlyn:MemoryMapsPath"] = memoryMapsPath
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IFm26NativeConnector>();
                    services.RemoveAll<SafeFm26ConnectorService>();
                    services.AddSingleton<IFm26NativeConnector>(connector);
                    services.AddSingleton<SafeFm26ConnectorService>();
                });
            });
        }

        private static async System.Threading.Tasks.Task<T> ReadJson<T>(HttpClient client, string path)
        {
            var value = await client.GetFromJsonAsync<T>(path);
            Assert.NotNull(value);
            return value;
        }

        private static async System.Threading.Tasks.Task<T> PostJson<T>(HttpClient client, string path)
        {
            using var response = await client.PostAsync(path, null);
            response.EnsureSuccessStatusCode();
            var value = await response.Content.ReadFromJsonAsync<T>();
            Assert.NotNull(value);
            return value;
        }

        private static bool TableExists(StatlynDbConnectionFactory factory, string tableName)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", tableName);
            return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
        }

        private static void AssertSafeSnapshotAuditJson(string json)
        {
            JsonDocument.Parse(json).Dispose();
            foreach (var forbidden in new[]
            {
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "PlayerRawSnapshot",
                "SquadData",
                "MemoryAddress",
                "BaseAddress",
                "ModuleAddress",
                "Pointer",
                "Offset",
                "Handle",
                "RawValue",
                "ProcessPath",
                "StackTrace",
                "ExceptionDetails",
                "0xDEADBEEF"
            })
            {
                Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("\"ca\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"pa\"", json, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertSafePropertyName(string name)
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
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-milestone35-" + Guid.NewGuid().ToString("N"));
                MapsPath = System.IO.Path.Combine(Path, "memory-maps");
                Directory.CreateDirectory(Path);
                Directory.CreateDirectory(MapsPath);
            }

            public string Path { get; }

            public string MapsPath { get; }

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
