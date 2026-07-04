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
using Statlyn.Api;
using Statlyn.DataProviders.Fm26;
using Statlyn.DataProviders.Fm26.MemoryMaps;

namespace Statlyn.Tests
{
    public sealed class Milestone33Tests
    {
        [Fact]
        public void LoaderHandlesMissingDirectorySafely()
        {
            var loader = new MemoryMapRegistryLoader(Path.Combine(Path.GetTempPath(), "statlyn-missing-maps-" + Guid.NewGuid().ToString("N")));

            var registry = loader.Load();

            Assert.Equal(MemoryMapSupportStatus.RegistryMissing.ToString(), registry.RegistryStatus);
            Assert.Equal(0, registry.MapsFoundCount);
            Assert.Contains("directory was not found", registry.SafeMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void LoaderHandlesInvalidJsonWithoutThrowing()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.Path, "broken.map.json", "{ not-json ");

            var registry = new MemoryMapRegistryLoader(temp.Path).Load();

            Assert.Equal(1, registry.MapsFoundCount);
            Assert.Equal(1, registry.InvalidMapsCount);
            Assert.Equal(MemoryMapSupportStatus.Invalid.ToString(), registry.Maps[0].SupportStatus);
            Assert.DoesNotContain("StackTrace", JsonSerializer.Serialize(registry), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ExistingTemplateShapeLoadsButIsNotUsable()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.Path, "players.map.json", TemplatePlayersMap());

            var registry = new MemoryMapRegistryLoader(temp.Path).Load();
            var map = registry.Maps.Single();

            Assert.Equal(MemoryMapSupportStatus.TemplateOnly.ToString(), registry.RegistryStatus);
            Assert.Equal(1, registry.TemplateMapsCount);
            Assert.Equal(0, registry.UsableMapsCount);
            Assert.False(map.IsUsable);
            Assert.True(map.IsTemplate);
            Assert.Equal(3, map.FieldCount);
            Assert.Equal(1, map.VisibleFieldCount);
            Assert.Equal(2, map.HiddenFieldCountBlocked);
            Assert.DoesNotContain("currentAbility", JsonSerializer.Serialize(MapToApiDto(map)), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidatorRejectsUnvalidatedAndWriteEnabledMaps()
        {
            var validator = new MemoryMapRegistryValidator();
            var unvalidated = validator.Validate(Manifest(isValidated: false, isTemplate: false));
            var writeEnabled = Manifest(isValidated: true, isTemplate: false);
            writeEnabled.AllowedUsage = "readWrite";

            var writeResult = validator.Validate(writeEnabled);

            Assert.True(unvalidated.IsValid);
            Assert.False(unvalidated.IsUsable);
            Assert.Equal(MemoryMapSupportStatus.Unvalidated.ToString(), unvalidated.SupportStatus);

            Assert.False(writeResult.IsValid);
            Assert.False(writeResult.IsUsable);
            Assert.Contains(writeResult.Errors, error => error.Contains("Write-enabled", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ValidatorBlocksHiddenFieldsAndDeniesUnknownVisibility()
        {
            var validator = new MemoryMapRegistryValidator();
            var safeHidden = Manifest(isValidated: true, isTemplate: false);
            safeHidden.Fields = new[]
            {
                new MemoryMapFieldDefinition
                {
                    FieldKey = "PotentialAbility",
                    FieldName = "PotentialAbility",
                    Category = "blocked",
                    Visibility = "neverVisible",
                    DataType = "int",
                    IsReadOnly = true,
                    CanDisplay = false,
                    CanStore = false,
                    CanScore = false
                }
            };

            var leakingHidden = Manifest(isValidated: true, isTemplate: false);
            leakingHidden.Fields = new[]
            {
                new MemoryMapFieldDefinition
                {
                    FieldKey = "CurrentAbility",
                    FieldName = "CurrentAbility",
                    Category = "blocked",
                    Visibility = "neverVisible",
                    DataType = "int",
                    IsReadOnly = true,
                    CanDisplay = true,
                    CanStore = false,
                    CanScore = false
                }
            };

            var unknown = Manifest(isValidated: true, isTemplate: false);
            unknown.Fields = new[]
            {
                new MemoryMapFieldDefinition
                {
                    FieldKey = "safeVisibleMetric",
                    FieldName = "safeVisibleMetric",
                    Category = "technical",
                    Visibility = "mystery",
                    DataType = "int",
                    IsReadOnly = true,
                    CanDisplay = true
                }
            };

            Assert.True(validator.Validate(safeHidden).IsUsable);
            Assert.False(validator.Validate(leakingHidden).IsValid);
            Assert.False(validator.Validate(unknown).IsValid);
        }

        [Fact]
        public void SelectorReturnsValidatedExactMatchOnly()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.Path, "validated.map.json", ValidatedMap("26.0.0-test"));
            var registry = new MemoryMapRegistryLoader(temp.Path).Load();
            var selector = new MemoryMapSelector();

            var process = new Fm26ProcessDiagnostic
            {
                IsDetected = true,
                ProductVersion = "26.0.0-test",
                FileVersion = "26.0.0-test",
                Architecture = "x64"
            };
            var mismatch = new Fm26ProcessDiagnostic
            {
                IsDetected = true,
                ProductVersion = "26.9.9-other",
                FileVersion = "26.9.9-other",
                Architecture = "x64"
            };

            var selected = selector.Select(registry, process);
            var noMatch = selector.Select(registry, mismatch);

            Assert.True(selected.HasValidatedMap);
            Assert.True(selected.HasSelectedMap);
            Assert.Equal(MemoryMapSupportStatus.MapAvailable.ToString(), selected.SupportStatus);
            Assert.Contains("Player reading is not implemented", selected.SupportMessage, StringComparison.OrdinalIgnoreCase);

            Assert.True(noMatch.HasValidatedMap);
            Assert.False(noMatch.HasSelectedMap);
            Assert.Equal(MemoryMapSupportStatus.BuildMismatch.ToString(), noMatch.SupportStatus);
        }

        [Fact]
        public async System.Threading.Tasks.Task ApiMemoryMapEndpointsReturnSafeTemplateStatus()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());
            using var factory = CreateApiFactory(temp, temp.MapsPath);
            using var client = factory.CreateClient();

            foreach (var path in new[] { "/diagnostics/memory-maps", "/connector/memory-maps" })
            {
                var dto = await ReadJson<MemoryMapRegistryDto>(client, path);
                var json = await client.GetStringAsync(path);

                Assert.Equal(1, dto.MapsFoundCount);
                Assert.Equal(1, dto.TemplateMapsCount);
                Assert.Equal(0, dto.UsableMapsCount);
                Assert.Equal(MemoryMapSupportStatus.TemplateOnly.ToString(), dto.RegistryStatus);
                AssertSafeMemoryMapJson(json);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task ConnectorStatusIncludesMemoryMapSummaryAndKeepsFmUnsupported()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "players.map.json", TemplatePlayersMap());
            using var factory = CreateApiFactory(temp, temp.MapsPath);
            using var client = factory.CreateClient();

            var health = await ReadJson<AppHealthDto>(client, "/health");
            var connector = await ReadJson<Fm26ConnectorStatusDto>(client, "/connector/status");
            var fm26 = await ReadJson<Fm26ConnectorStatusDto>(client, "/diagnostics/fm26");

            Assert.False(health.IsFm26Supported);
            Assert.False(connector.IsFm26Supported);
            Assert.False(fm26.IsFm26Supported);
            Assert.Equal(MemoryMapSupportStatus.TemplateOnly.ToString(), connector.MemoryMapRegistryStatus);
            Assert.Equal(1, connector.MemoryMapCount);
            Assert.Equal(0, connector.UsableMemoryMapCount);
            Assert.Contains("Templates are not usable", connector.MapSupportMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DirectApiFactoryCanReportValidatedMapMetadataWithoutPlayerSupport()
        {
            using var temp = new TemporaryDirectory();
            WriteMap(temp.MapsPath, "validated.map.json", ValidatedMap("26.0.0-test"));
            using var db = Statlyn.Data.RuntimeDatabaseFactory.CreateFile(Path.Combine(temp.Path, "statlyn.db"));
            var factory = new StatlynApiDtoFactory(
                db,
                new SafeFm26ConnectorService(new StaticConnector("26.0.0-test")),
                new MemoryMapRegistryLoader(temp.MapsPath));

            var connector = factory.GetConnectorStatus();
            var registry = factory.GetMemoryMaps();

            Assert.False(connector.IsFm26Supported);
            Assert.True(connector.HasValidatedMap);
            Assert.Equal(MemoryMapSupportStatus.MapAvailable.ToString(), connector.MapSupportStatus);
            Assert.Equal(1, registry.UsableMapsCount);
            Assert.Contains("Player reading is not implemented", connector.MapSupportMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PublicDtosDoNotExposeMapInternalsOrPlayerValues()
        {
            foreach (var type in new[]
            {
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

        [Fact]
        public void ReactTauriShowsMapRegistryThroughApiOnly()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var sourceText = ReadDesktopAndTauriText(desktop);
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var api = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));

            Assert.Contains("/diagnostics/memory-maps", api, StringComparison.Ordinal);
            Assert.Contains("Memory-map Registry", app, StringComparison.Ordinal);
            Assert.Contains("player reading not implemented", app, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in new[]
            {
                "JsonDocument",
                "JSON.parse(local",
                "readFile",
                "fs.",
                "OpenProcess",
                "ReadProcessMemory",
                "NativeFm26Connector",
                "sqlite",
                "memory map",
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "RawValue"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ManagedConnectorSurfaceStillDoesNotAddPlayerReaders()
        {
            var connectorMethods = typeof(IFm26NativeConnector).GetMethods().Select(method => method.Name).ToList();
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Player", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Equals("Read", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Read", StringComparison.OrdinalIgnoreCase));
        }

        private static MemoryMapManifest Manifest(bool isValidated, bool isTemplate)
        {
            return new MemoryMapManifest
            {
                MapId = "synthetic-safe-map",
                DisplayName = "Synthetic safe FM26 map",
                IsTemplate = isTemplate,
                IsValidated = isValidated,
                AllowedUsage = "metadataOnly",
                BuildTarget = new MemoryMapBuildTarget
                {
                    Game = "Football Manager 26",
                    GameVersion = "26.0.0-test",
                    BuildNumber = "26.0.0-test",
                    Platform = "Windows",
                    Architecture = "x64"
                },
                Fields = new[]
                {
                    new MemoryMapFieldDefinition
                    {
                        FieldKey = "displayName",
                        FieldName = "displayName",
                        Category = "identity",
                        Visibility = "alwaysVisible",
                        DataType = "string",
                        IsReadOnly = true,
                        CanDisplay = true,
                        CanStore = true,
                        CanScore = false
                    }
                }
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

        private static MemoryMapDiagnosticDto MapToApiDto(MemoryMapFileDiagnostic map)
        {
            return new MemoryMapDiagnosticDto(
                map.MapId,
                map.DisplayName,
                map.GameVersion,
                map.BuildNumber,
                map.Platform,
                map.Architecture,
                map.IsTemplate,
                map.IsValidated,
                map.IsUsable,
                map.SupportStatus,
                map.FieldCount,
                map.VisibleFieldCount,
                map.HiddenFieldCountBlocked,
                map.SafeMessage,
                map.ValidationWarnings,
                map.ValidationErrors);
        }

        private static void WriteMap(string directory, string name, string json)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), json);
        }

        private static WebApplicationFactory<Program> CreateApiFactory(TemporaryDirectory temp, string memoryMapsPath)
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
            });
        }

        private static async System.Threading.Tasks.Task<T> ReadJson<T>(HttpClient client, string path)
        {
            var value = await client.GetFromJsonAsync<T>(path);
            Assert.NotNull(value);
            return value;
        }

        private static void AssertSafeMemoryMapJson(string json)
        {
            JsonDocument.Parse(json).Dispose();
            foreach (var forbidden in new[]
            {
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "pointerPath",
                "symbolicReference",
                "Offset",
                "Address",
                "Handle",
                "RawValue",
                "StackTrace",
                "ExceptionDetails"
            })
            {
                Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("\"ca\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"pa\"", json, StringComparison.OrdinalIgnoreCase);
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

        private sealed class StaticConnector : IFm26NativeConnector
        {
            private readonly string _build;

            public StaticConnector(string build)
            {
                _build = build;
            }

            public bool IsAvailable
            {
                get { return true; }
            }

            public string LastError
            {
                get { return string.Empty; }
            }

            public string GetConnectorVersion()
            {
                return "test-native-0.3.0";
            }

            public string GetBuildInfo()
            {
                return "test read-only diagnostics";
            }

            public Fm26ProcessDiagnostic DetectFmProcess()
            {
                return GetDiagnostic().Process;
            }

            public Fm26ConnectorDiagnostic GetDiagnostic()
            {
                return new Fm26ConnectorDiagnostic
                {
                    IsNativeConnectorAvailable = true,
                    Availability = NativeConnectorAvailability.Available,
                    ConnectorVersion = GetConnectorVersion(),
                    ConnectorBuildInfo = GetBuildInfo(),
                    IsWindows = true,
                    Process = new Fm26ProcessDiagnostic
                    {
                        IsDetected = true,
                        DetectionStatus = Fm26DiagnosticSupportStatus.DiagnosticsOnly.ToString(),
                        DetectionStatusMessage = "FM process detected with read-only diagnostics.",
                        ProductVersion = _build,
                        FileVersion = _build,
                        Architecture = "x64",
                        ReadOnlyAccessAttempted = true,
                        HasReadOnlyAccess = true,
                        ReadOnlyAccessStatus = "Available",
                        RequiredAccessLevel = "Read-only diagnostic process query; no write or injection."
                    },
                    ReadOnlyAccessStatus = "Available",
                    IsFm26Supported = false,
                    BuildSupportStatus = Fm26DiagnosticSupportStatus.DiagnosticsOnly.ToString(),
                    BuildSupportMessage = "Diagnostics only.",
                    SupportStatusMessage = "FM26 unsupported until player reading is implemented.",
                    SafeMessage = "FM process detected. Statlyn reports diagnostics only; no player data is read."
                };
            }

            public Statlyn.Core.Diagnostics.DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo)
            {
                return Statlyn.Core.Diagnostics.DiagnosticStatus.Unsupported;
            }
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-milestone33-" + Guid.NewGuid().ToString("N"));
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
