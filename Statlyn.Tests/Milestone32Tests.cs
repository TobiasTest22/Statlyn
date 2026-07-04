using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Statlyn.Api;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.Tests
{
    public sealed class Milestone32Tests
    {
        [Fact]
        public void DiagnosticModelsStaySafeAndNeverExposeSupportedStatus()
        {
            foreach (var type in new[]
            {
                typeof(Fm26ConnectorDiagnostic),
                typeof(Fm26ProcessDiagnostic),
                typeof(Fm26ConnectorStatusDto)
            })
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertSafePublicName(type, property.Name);
                    Assert.False(property.PropertyType == typeof(IntPtr), $"{type.Name}.{property.Name} exposes IntPtr.");
                    Assert.False(property.PropertyType == typeof(UIntPtr), $"{type.Name}.{property.Name} exposes UIntPtr.");
                }
            }

            Assert.DoesNotContain(Enum.GetNames(typeof(Fm26DiagnosticSupportStatus)), name => string.Equals(name, "Supported", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void NullConnectorReportsMapMissingAndValidatedMapNextAction()
        {
            var diagnostic = new NullFm26NativeConnector().GetDiagnostic();

            Assert.False(diagnostic.IsFm26Supported);
            Assert.Equal(Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString(), diagnostic.BuildSupportStatus);
            Assert.Equal(Fm26DiagnosticSupportStatus.MapMissing.ToString(), diagnostic.MapSupportStatus);
            Assert.Contains("validated FM26 memory map", diagnostic.NextActionSafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("unsupported until validated maps", diagnostic.SupportStatusMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NativeConnectorReportsNotDetectedAndAccessDeniedWithoutSupport()
        {
            var notDetected = new NativeFm26Connector(
                new StatusInterop((int)NativeConnectorStatusCode.NotFound, "Football Manager 26 process fm.exe was not found.", detected: false, readOnlyAccess: false),
                new Fm26NativeConnectorOptions { ForceIsWindows = true }).GetDiagnostic();
            var accessDenied = new NativeFm26Connector(
                new StatusInterop((int)NativeConnectorStatusCode.AccessDenied, "Access denied near 0xDEADBEEF.", detected: true, readOnlyAccess: false),
                new Fm26NativeConnectorOptions { ForceIsWindows = true }).GetDiagnostic();

            Assert.False(notDetected.IsFm26Supported);
            Assert.Equal(Fm26DiagnosticSupportStatus.NotDetected.ToString(), notDetected.Process.DetectionStatus);
            Assert.Equal(Fm26DiagnosticSupportStatus.NotDetected.ToString(), notDetected.BuildSupportStatus);
            Assert.Equal(Fm26DiagnosticSupportStatus.MapMissing.ToString(), notDetected.MapSupportStatus);

            Assert.False(accessDenied.IsFm26Supported);
            Assert.True(accessDenied.Process.ReadOnlyAccessAttempted);
            Assert.False(accessDenied.Process.HasReadOnlyAccess);
            Assert.Equal(Fm26DiagnosticSupportStatus.AccessDenied.ToString(), accessDenied.Process.DetectionStatus);
            Assert.Equal(Fm26DiagnosticSupportStatus.AccessDenied.ToString(), accessDenied.BuildSupportStatus);
            Assert.Equal(Fm26DiagnosticSupportStatus.MapMissing.ToString(), accessDenied.MapSupportStatus);
            Assert.Contains("validated FM26 memory map", accessDenied.NextActionSafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("0xDEADBEEF", JsonSerializer.Serialize(accessDenied), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProcessPathIsReducedToSafeExecutableLabel()
        {
            var diagnostic = new NativeFm26Connector(
                new StatusInterop((int)NativeConnectorStatusCode.Ok, string.Empty, detected: true, readOnlyAccess: true),
                new Fm26NativeConnectorOptions { ForceIsWindows = true }).GetDiagnostic();

            Assert.Equal("fm.exe", diagnostic.Process.ExecutableFileName);
            Assert.Equal("FM26", diagnostic.Process.ExecutableDirectorySafeLabel);
            Assert.Equal(Path.Combine("FM26", "fm.exe"), diagnostic.Process.ProcessPath);
            Assert.DoesNotContain("C:\\Users\\tobia", diagnostic.Process.ProcessPath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AppData", diagnostic.Process.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async System.Threading.Tasks.Task ApiFm26DiagnosticsRemainUnsupportedAndSafe()
        {
            using var temp = new TemporaryDirectory();
            using var factory = CreateApiFactory(temp);
            using var client = factory.CreateClient();

            var health = await ReadJson<AppHealthDto>(client, "/health");
            Assert.False(health.IsFm26Supported);
            Assert.Contains("No live FM26 data", health.SafeMessage, StringComparison.OrdinalIgnoreCase);

            foreach (var path in new[] { "/connector/status", "/connector/fm26", "/diagnostics/fm26", "/diagnostics/fm26/summary" })
            {
                var dto = await ReadJson<Fm26ConnectorStatusDto>(client, path);
                var json = await client.GetStringAsync(path);

                Assert.False(dto.IsFm26Supported);
                Assert.Equal(Fm26DiagnosticSupportStatus.MapMissing.ToString(), dto.MapSupportStatus);
                Assert.Contains("validated FM26 memory map", dto.NextActionSafeMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("unsupported until validated maps", dto.SupportStatusMessage, StringComparison.OrdinalIgnoreCase);
                AssertSafeJson(path, json);
            }
        }

        [Fact]
        public void ReactTauriDisplaysDiagnosticsThroughApiOnly()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var sourceText = ReadDesktopAndTauriText(desktop);
            var app = File.ReadAllText(Path.Combine(desktop, "src", "App.tsx"));
            var api = File.ReadAllText(Path.Combine(desktop, "src", "api.ts"));

            Assert.Contains("/connector/status", api, StringComparison.Ordinal);
            Assert.Contains("FM26 Diagnostics", app, StringComparison.Ordinal);
            Assert.Contains("Validated FM26 maps", app, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in new[]
            {
                "Statlyn.NativeConnector",
                "NativeFm26Connector",
                "PInvokeNativeConnectorInterop",
                "child_process",
                "std::process",
                "Command::new",
                "OpenProcess",
                "ReadProcessMemory",
                "PROCESS_VM_READ",
                "sqlite",
                "rusqlite",
                "sqlx",
                "CurrentAbility",
                "PotentialAbility",
                "RawValue"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ConnectorScriptAndDocsDescribeDiagnosticsOnlyMilestone()
        {
            var root = FindRepositoryRoot();
            var script = File.ReadAllText(Path.Combine(root, "tools", "run-connector-diagnostics.ps1"));
            var docs = string.Join(
                "\n",
                File.ReadAllText(Path.Combine(root, "README.md")),
                File.ReadAllText(Path.Combine(root, "docs", "fm26-connector-diagnostics.md")),
                File.ReadAllText(Path.Combine(root, "docs", "native-connector-boundary.md")),
                File.ReadAllText(Path.Combine(root, "docs", "memory-connector.md")),
                File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md")),
                File.ReadAllText(Path.Combine(root, "docs", "testing.md")),
                File.ReadAllText(Path.Combine(root, "docs", "data-source-boundaries.md")));

            Assert.Contains("/diagnostics/fm26", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("nextActionSafeMessage", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FM process detection is diagnostics only", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No player data is read in 3.2", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FM26 remains unsupported without validated memory maps", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri never calls native connector directly", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("First memory-map work is later", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("First safe player snapshot is later", docs, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ManagedConnectorSurfaceStillHasNoPlayerReadingMethods()
        {
            var connectorMethods = typeof(IFm26NativeConnector).GetMethods().Select(method => method.Name).ToList();

            Assert.DoesNotContain(connectorMethods, name => name.Contains("Player", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Equals("Read", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Read", StringComparison.OrdinalIgnoreCase));
        }

        private static void AssertSafePublicName(Type type, string name)
        {
            foreach (var forbidden in new[]
            {
                "CurrentAbility",
                "PotentialAbility",
                "Professionalism",
                "Hidden",
                "Raw",
                "MemoryAddress",
                "BaseAddress",
                "Address",
                "Handle"
            })
            {
                Assert.DoesNotContain(forbidden, name, StringComparison.OrdinalIgnoreCase);
            }

            Assert.False(string.Equals("CA", name, StringComparison.OrdinalIgnoreCase), $"{type.Name}.{name} exposes CA.");
            Assert.False(string.Equals("PA", name, StringComparison.OrdinalIgnoreCase), $"{type.Name}.{name} exposes PA.");
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
                "RawValue",
                "MemoryAddress",
                "BaseAddress",
                "Handle",
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

        private static async System.Threading.Tasks.Task<T> ReadJson<T>(HttpClient client, string path)
        {
            var value = await client.GetFromJsonAsync<T>(path);
            Assert.NotNull(value);
            return value;
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

        private sealed class StatusInterop : INativeConnectorInterop
        {
            private readonly int _statusCode;
            private readonly string _lastError;
            private readonly bool _detected;
            private readonly bool _readOnlyAccess;

            public StatusInterop(int statusCode, string lastError, bool detected, bool readOnlyAccess)
            {
                _statusCode = statusCode;
                _lastError = lastError;
                _detected = detected;
                _readOnlyAccess = readOnlyAccess;
            }

            public int GetVersion(StringBuilder buffer, int bufferLength)
            {
                buffer.Append("test-native-0.2.0");
                return 0;
            }

            public int GetBuildInfo(StringBuilder buffer, int bufferLength)
            {
                buffer.Append("test read-only diagnostics");
                return 0;
            }

            public int DetectFmProcess(out NativeFm26ProcessInfo processInfo)
            {
                processInfo = new NativeFm26ProcessInfo
                {
                    ProcessId = 4242,
                    ExecutablePath = _detected ? "C:\\Users\\tobia\\AppData\\Local\\FM26\\fm.exe" : string.Empty,
                    ProductVersion = _detected ? "26.0.0-test" : string.Empty,
                    Architecture = _detected ? "x64" : string.Empty,
                    Detected = _detected ? 1 : 0,
                    ReadOnlyAccess = _readOnlyAccess ? 1 : 0
                };
                return _statusCode;
            }

            public int GetLastError(StringBuilder buffer, int bufferLength)
            {
                buffer.Append(_lastError);
                return 0;
            }

            public void ResetLastError()
            {
            }
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-milestone32-" + Guid.NewGuid().ToString("N"));
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
