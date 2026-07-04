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
using Statlyn.DataProviders.Fm26;
using Statlyn.DataProviders.Fm26.MemoryMaps;
using Statlyn.DataProviders.Fm26.Snapshots;

namespace Statlyn.Tests
{
    public sealed class Milestone34Tests
    {
        [Fact]
        public void SnapshotModelsAndDtosExposeNoUnsafeMemoryOrHiddenFieldNames()
        {
            foreach (var type in new[]
            {
                typeof(Fm26SafeSnapshot),
                typeof(Fm26SnapshotSourceSummary),
                typeof(Fm26SnapshotCapabilityReport),
                typeof(Fm26SnapshotGateResult),
                typeof(Fm26SnapshotBlockReason),
                typeof(Fm26SnapshotResult),
                typeof(Fm26SnapshotDto),
                typeof(Fm26SnapshotGateDto),
                typeof(Fm26SnapshotBlockReasonDto),
                typeof(Fm26SelectedMapSummaryDto)
            })
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertSafePropertyName(property.Name);
                    Assert.DoesNotContain("Player", property.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Squad", property.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void MissingConnectorBlocksConnectorUnavailable()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());

            var snapshot = CreateSnapshot(new NullFm26NativeConnector(), temp.MapsPath);

            Assert.Equal(Fm26SnapshotStatus.BlockedConnectorUnavailable, snapshot.Status);
            Assert.Equal("connector", snapshot.CapabilityReport.BlockingGate);
            Assert.False(snapshot.CapabilityReport.IsFm26Supported);
            Assert.False(snapshot.CapabilityReport.IsLiveReadingAvailable);
            Assert.Contains(snapshot.Gates, gate => gate.GateKey == "connector" && gate.GateStatus == Fm26SnapshotGateStatus.Blocked);
        }

        [Fact]
        public void UnsupportedPlatformBlocksBeforeProcessOrMapChecks()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", ValidatedMap("26.0.0-test"));

            var snapshot = CreateSnapshot(TestConnector.NonWindows(), temp.MapsPath);

            Assert.Equal(Fm26SnapshotStatus.BlockedUnsupportedPlatform, snapshot.Status);
            Assert.Equal("platform", snapshot.CapabilityReport.BlockingGate);
            Assert.Contains(snapshot.Gates, gate => gate.GateKey == "process" && gate.GateStatus == Fm26SnapshotGateStatus.NotChecked);
        }

        [Fact]
        public void MissingFmProcessBlocksProcessGate()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", ValidatedMap("26.0.0-test"));

            var snapshot = CreateSnapshot(TestConnector.ProcessMissing(), temp.MapsPath);

            Assert.Equal(Fm26SnapshotStatus.BlockedProcessNotDetected, snapshot.Status);
            Assert.Equal("process", snapshot.CapabilityReport.BlockingGate);
            Assert.False(snapshot.SourceSummary.ProcessDetected);
        }

        [Fact]
        public void TemplateOnlyMapsBlockNoValidatedMap()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());

            var snapshot = CreateSnapshot(TestConnector.ProcessDetected("26.0.0-test"), temp.MapsPath);

            Assert.Equal(Fm26SnapshotStatus.BlockedNoValidatedMap, snapshot.Status);
            Assert.Equal("validatedMap", snapshot.CapabilityReport.BlockingGate);
            Assert.Equal(0, snapshot.SourceSummary.ValidatedMaps);
            Assert.Equal(1, snapshot.SourceSummary.TemplateMaps);
            Assert.False(snapshot.CapabilityReport.IsFm26Supported);
            Assert.False(snapshot.CapabilityReport.IsLiveReadingAvailable);
        }

        [Fact]
        public void ValidatedSyntheticMapStillBlocksReaderNotImplemented()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", ValidatedMap("26.0.0-test"));

            var snapshot = CreateSnapshot(TestConnector.ProcessDetected("26.0.0-test"), temp.MapsPath);

            Assert.Equal(Fm26SnapshotStatus.BlockedReaderNotImplemented, snapshot.Status);
            Assert.Equal("reader", snapshot.CapabilityReport.BlockingGate);
            Assert.Equal("NotImplemented", snapshot.CapabilityReport.ReaderStatus);
            Assert.Equal(1, snapshot.SourceSummary.ValidatedMaps);
            Assert.Contains(snapshot.Gates, gate => gate.GateKey == "selectedMap" && gate.GateStatus == Fm26SnapshotGateStatus.Passed);
            Assert.Contains("No player data is read", JsonSerializer.Serialize(snapshot), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async System.Threading.Tasks.Task SnapshotEndpointWorksWithMissingConnectorAndSafeJson()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());
            using var factory = CreateApiFactory(temp, temp.MapsPath, new NullFm26NativeConnector());
            using var client = factory.CreateClient();

            var dto = await ReadJson<Fm26SnapshotDto>(client, "/diagnostics/fm26/snapshot");
            var connectorDto = await ReadJson<Fm26SnapshotDto>(client, "/connector/fm26/snapshot");
            var readinessDto = await ReadJson<Fm26SnapshotDto>(client, "/diagnostics/fm26/snapshot/readiness");
            var json = await client.GetStringAsync("/diagnostics/fm26/snapshot");
            var health = await ReadJson<AppHealthDto>(client, "/health");

            Assert.Equal("BlockedConnectorUnavailable", dto.SnapshotStatus);
            Assert.Equal(dto.SnapshotStatus, connectorDto.SnapshotStatus);
            Assert.Equal(dto.IsFm26Supported, readinessDto.IsFm26Supported);
            Assert.False(dto.IsFm26Supported);
            Assert.False(dto.IsLiveReadingAvailable);
            Assert.False(health.IsFm26Supported);
            AssertSafeSnapshotJson(json);
        }

        [Fact]
        public void DirectApiFactoryReturnsTemplateOnlySnapshotBlockedAtValidatedMap()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());
            using var db = RuntimeDatabaseFactory.CreateFile(Path.Combine(temp.Path, "statlyn.db"));
            var factory = new StatlynApiDtoFactory(
                db,
                new SafeFm26ConnectorService(TestConnector.ProcessDetected("26.0.0-test")),
                new MemoryMapRegistryLoader(temp.MapsPath));

            var snapshot = factory.GetFm26Snapshot();
            var json = JsonSerializer.Serialize(snapshot);

            Assert.Equal("BlockedNoValidatedMap", snapshot.SnapshotStatus);
            Assert.Equal("validatedMap", snapshot.BlockingGate);
            Assert.Equal(0, snapshot.ValidatedMaps);
            Assert.Equal(1, snapshot.TemplateMaps);
            Assert.False(snapshot.IsFm26Supported);
            Assert.False(snapshot.IsLiveReadingAvailable);
            AssertSafeSnapshotJson(json);
        }

        [Fact]
        public void ReactTauriDisplaysSnapshotThroughApiOnly()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var api = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));
            var types = File.ReadAllText(Path.Combine(desktop, "src", "types.ts"));
            var sourceText = ReadDesktopAndTauriText(desktop);

            Assert.Contains("/diagnostics/fm26/snapshot", api, StringComparison.Ordinal);
            Assert.Contains("Safe FM26 Snapshot", app, StringComparison.Ordinal);
            Assert.Contains("No Player Data", app, StringComparison.Ordinal);
            Assert.Contains("Fm26SnapshotDto", types, StringComparison.Ordinal);

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
                "RawValue"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ManagedConnectorSurfaceStillHasNoSnapshotOrPlayerReaders()
        {
            var connectorMethods = typeof(IFm26NativeConnector).GetMethods().Select(method => method.Name).ToList();
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Player", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Equals("Read", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Read", StringComparison.OrdinalIgnoreCase));
        }

        private static Fm26SafeSnapshot CreateSnapshot(IFm26NativeConnector connector, string mapsPath)
        {
            var service = new SafeFm26SnapshotService(
                new SafeFm26ConnectorService(connector),
                new MemoryMapRegistryLoader(mapsPath));
            return service.CreateSnapshot().Snapshot;
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

        private static string ValidatedMap(string build)
        {
            return @"{
  ""mapId"": ""fm26-test-map"",
  ""displayName"": ""FM26 Test Metadata Map"",
  ""buildTarget"": {
    ""game"": ""Football Manager 26"",
    ""gameVersion"": """ + build + @""",
    ""buildNumber"": """ + build + @""",
    ""platform"": ""Windows"",
    ""architecture"": ""x64""
  },
  ""isTemplate"": false,
  ""isValidated"": true,
  ""allowedUsage"": ""metadataOnly"",
  ""fields"": [
    { ""fieldKey"": ""displayName"", ""fieldName"": ""displayName"", ""category"": ""identity"", ""visibility"": ""visible"", ""dataType"": ""string"", ""isReadOnly"": true, ""canDisplay"": true, ""canStore"": true, ""canScore"": false }
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

        private static void AssertSafeSnapshotJson(string json)
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

        private sealed class TestConnector : IFm26NativeConnector
        {
            private readonly Fm26ConnectorDiagnostic _diagnostic;

            private TestConnector(Fm26ConnectorDiagnostic diagnostic)
            {
                _diagnostic = diagnostic;
            }

            public bool IsAvailable
            {
                get { return _diagnostic.IsNativeConnectorAvailable; }
            }

            public string LastError
            {
                get { return _diagnostic.LastErrorSafeMessage; }
            }

            public static TestConnector NonWindows()
            {
                var diagnostic = BaseDiagnostic("26.0.0-test");
                diagnostic.IsWindows = false;
                diagnostic.BuildSupportStatus = Fm26DiagnosticSupportStatus.UnsupportedPlatform.ToString();
                diagnostic.BuildSupportMessage = "Unsupported platform.";
                return new TestConnector(diagnostic);
            }

            public static TestConnector ProcessMissing()
            {
                var diagnostic = BaseDiagnostic("26.0.0-test");
                diagnostic.Process = new Fm26ProcessDiagnostic
                {
                    IsDetected = false,
                    DetectionStatus = Fm26DiagnosticSupportStatus.NotDetected.ToString(),
                    DetectionStatusMessage = "FM process not detected.",
                    ReadOnlyAccessAttempted = false,
                    HasReadOnlyAccess = false,
                    ReadOnlyAccessStatus = "Unavailable"
                };
                diagnostic.ReadOnlyAccessStatus = "Unavailable";
                return new TestConnector(diagnostic);
            }

            public static TestConnector ProcessDetected(string build)
            {
                return new TestConnector(BaseDiagnostic(build));
            }

            public string GetConnectorVersion()
            {
                return _diagnostic.ConnectorVersion;
            }

            public string GetBuildInfo()
            {
                return _diagnostic.ConnectorBuildInfo;
            }

            public Fm26ProcessDiagnostic DetectFmProcess()
            {
                return _diagnostic.Process;
            }

            public Fm26ConnectorDiagnostic GetDiagnostic()
            {
                return _diagnostic;
            }

            public Statlyn.Core.Diagnostics.DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo)
            {
                return Statlyn.Core.Diagnostics.DiagnosticStatus.Unsupported;
            }

            private static Fm26ConnectorDiagnostic BaseDiagnostic(string build)
            {
                return new Fm26ConnectorDiagnostic
                {
                    IsNativeConnectorAvailable = true,
                    Availability = NativeConnectorAvailability.Available,
                    ConnectorVersion = "test-native-0.4.0",
                    ConnectorBuildInfo = "test read-only diagnostics",
                    IsWindows = true,
                    Process = new Fm26ProcessDiagnostic
                    {
                        IsDetected = true,
                        DetectionStatus = Fm26DiagnosticSupportStatus.DiagnosticsOnly.ToString(),
                        DetectionStatusMessage = "FM process detected with read-only diagnostics.",
                        ProcessName = "fm.exe",
                        ProcessId = 2600,
                        ProductVersion = build,
                        FileVersion = build,
                        Architecture = "x64",
                        ReadOnlyAccessAttempted = true,
                        HasReadOnlyAccess = true,
                        ReadOnlyAccessStatus = "Available",
                        RequiredAccessLevel = "Read-only diagnostic process query; no write or injection.",
                        SafeMessage = "FM process diagnostics are available."
                    },
                    ReadOnlyAccessStatus = "Available",
                    IsFm26Supported = false,
                    BuildSupportStatus = Fm26DiagnosticSupportStatus.DiagnosticsOnly.ToString(),
                    BuildSupportMessage = "Diagnostics only.",
                    SupportStatusMessage = "FM26 unsupported until player reading is implemented.",
                    SafeMessage = "FM process detected. Statlyn reports diagnostics only; no player data is read."
                };
            }
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-milestone34-" + Guid.NewGuid().ToString("N"));
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
