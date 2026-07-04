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
    public sealed class Milestone31Tests
    {
        [Fact]
        public void NullConnectorReportsSafeUnavailableStatus()
        {
            var service = new SafeFm26ConnectorService(new NullFm26NativeConnector());

            var diagnostic = service.GetDiagnostic();

            Assert.False(diagnostic.IsNativeConnectorAvailable);
            Assert.False(diagnostic.IsFm26Supported);
            Assert.False(diagnostic.Process.IsDetected);
            Assert.Equal("Unavailable", diagnostic.ReadOnlyAccessStatus);
            Assert.Contains("No live FM26 data", diagnostic.SafeMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NativeConnectorHandlesUnsupportedPlatformWithoutCallingNativeLibrary()
        {
            var interop = new RecordingInterop();
            var connector = new NativeFm26Connector(interop, new Fm26NativeConnectorOptions { ForceIsWindows = false });

            var diagnostic = connector.GetDiagnostic();

            Assert.False(diagnostic.IsNativeConnectorAvailable);
            Assert.Equal(NativeConnectorAvailability.UnsupportedPlatform, diagnostic.Availability);
            Assert.False(diagnostic.IsWindows);
            Assert.False(diagnostic.IsFm26Supported);
            Assert.False(interop.WasCalled);
        }

        [Fact]
        public void NativeConnectorHandlesMissingLibraryAndMissingExportSafely()
        {
            var missingLibraryConnector = new NativeFm26Connector(
                new ThrowingInterop(new DllNotFoundException("library missing")),
                new Fm26NativeConnectorOptions { ForceIsWindows = true });
            var missingLibraryVersion = missingLibraryConnector.GetConnectorVersion();
            var missingLibrary = missingLibraryConnector.GetDiagnostic();
            var missingExport = new NativeFm26Connector(
                new ThrowingInterop(new EntryPointNotFoundException("export missing")),
                new Fm26NativeConnectorOptions { ForceIsWindows = true }).GetDiagnostic();

            Assert.Equal(string.Empty, missingLibraryVersion);
            Assert.Equal(NativeConnectorAvailability.MissingLibrary, missingLibrary.Availability);
            Assert.False(missingLibrary.IsNativeConnectorAvailable);
            Assert.False(missingLibrary.IsFm26Supported);
            Assert.Equal("Native connector library is not available.", missingLibrary.LastErrorSafeMessage);

            Assert.Equal(NativeConnectorAvailability.MissingExport, missingExport.Availability);
            Assert.False(missingExport.IsNativeConnectorAvailable);
            Assert.False(missingExport.IsFm26Supported);
            Assert.Equal("Native connector exports are not aligned with the managed binding.", missingExport.LastErrorSafeMessage);
        }

        [Fact]
        public void NativeConnectorSanitizesSafeDiagnosticsAndKeepsFmUnsupported()
        {
            var connector = new NativeFm26Connector(
                new StatusInterop(
                    statusCode: (int)NativeConnectorStatusCode.AccessDenied,
                    lastError: "Access denied near 0xDEADBEEF."),
                new Fm26NativeConnectorOptions { ForceIsWindows = true });

            var diagnostic = connector.GetDiagnostic();
            var json = JsonSerializer.Serialize(diagnostic);

            Assert.True(diagnostic.IsNativeConnectorAvailable);
            Assert.Equal(NativeConnectorAvailability.Available, diagnostic.Availability);
            Assert.True(diagnostic.Process.IsDetected);
            Assert.False(diagnostic.IsFm26Supported);
            Assert.DoesNotContain("0xDEADBEEF", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MemoryAddress", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Raw", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PublicConnectorModelsDoNotExposeUnsafeFieldsOrReaderMethods()
        {
            foreach (var type in new[]
            {
                typeof(Fm26ConnectorDiagnostic),
                typeof(Fm26ProcessDiagnostic),
                typeof(Fm26ConnectorStatusDto),
                typeof(Fm26ProcessInfo)
            })
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    AssertSafePublicName(type, property.Name);
                    Assert.False(property.PropertyType == typeof(IntPtr), $"{type.Name}.{property.Name} exposes IntPtr.");
                    Assert.False(property.PropertyType == typeof(UIntPtr), $"{type.Name}.{property.Name} exposes UIntPtr.");
                }
            }

            var connectorMethods = typeof(IFm26NativeConnector).GetMethods().Select(method => method.Name).ToList();
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Player", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(connectorMethods, name => name.Equals("Read", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Read", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ApiConnectorEndpointsReturnSafeUnsupportedDtos()
        {
            using var temp = new TemporaryDirectory();
            using var factory = CreateApiFactory(temp);
            using var client = factory.CreateClient();

            foreach (var path in new[] { "/connector/status", "/connector/fm26", "/diagnostics/fm26" })
            {
                var dto = await ReadJson<Fm26ConnectorStatusDto>(client, path);
                var json = await client.GetStringAsync(path);

                Assert.False(dto.IsFm26Supported);
                Assert.False(string.IsNullOrWhiteSpace(dto.SupportStatusMessage));
                Assert.Contains("unsupported", dto.SupportStatusMessage, StringComparison.OrdinalIgnoreCase);
                AssertSafeJson(path, json);
            }
        }

        [Fact]
        public void ReactTauriReadsConnectorStatusThroughApiOnly()
        {
            var root = FindRepositoryRoot();
            var desktop = Path.Combine(root, "Statlyn.Desktop");
            var sourceText = ReadDesktopAndTauriText(desktop);

            Assert.Contains("/connector/status", File.ReadAllText(Path.Combine(desktop, "src", "api.ts")), StringComparison.Ordinal);
            foreach (var forbidden in new[]
            {
                "Statlyn.NativeConnector",
                "NativeFm26Connector",
                "PInvokeNativeConnectorInterop",
                "OpenProcess",
                "PROCESS_VM_READ",
                "ReadProcessMemory",
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
        }

        [Fact]
        public void ConnectorScriptsAndDocsRecordSafeDiagnosticsBoundary()
        {
            var root = FindRepositoryRoot();
            var nativeScan = File.ReadAllText(Path.Combine(root, "tools", "check-native-readonly.ps1"));
            var connectorScript = File.ReadAllText(Path.Combine(root, "tools", "run-connector-diagnostics.ps1"));
            var docs = File.ReadAllText(Path.Combine(root, "docs", "fm26-connector-diagnostics.md"));

            foreach (var forbiddenPattern in new[]
            {
                "WriteProcessMemory",
                "CreateRemoteThread",
                "VirtualAllocEx",
                "PROCESS_ALL_ACCESS",
                "PROCESS_VM_WRITE",
                "PROCESS_VM_OPERATION",
                "DebugActiveProcess",
                "LoadLibraryA",
                "LoadLibraryW"
            })
            {
                Assert.Contains(forbiddenPattern, nativeScan, StringComparison.Ordinal);
            }

            Assert.Contains("/connector/status", connectorScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/health", connectorScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Stop-Process", connectorScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("diagnostics foundation", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("React/Tauri calls the API only", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IsFm26Supported", docs, StringComparison.Ordinal);
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
                "MemoryAddress",
                "PlayerRawSnapshot",
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

        private sealed class RecordingInterop : INativeConnectorInterop
        {
            public bool WasCalled { get; private set; }

            public int GetVersion(StringBuilder buffer, int bufferLength)
            {
                WasCalled = true;
                return 0;
            }

            public int GetBuildInfo(StringBuilder buffer, int bufferLength)
            {
                WasCalled = true;
                return 0;
            }

            public int DetectFmProcess(out NativeFm26ProcessInfo processInfo)
            {
                WasCalled = true;
                processInfo = default;
                return 1;
            }

            public int GetLastError(StringBuilder buffer, int bufferLength)
            {
                WasCalled = true;
                return 0;
            }

            public void ResetLastError()
            {
                WasCalled = true;
            }
        }

        private sealed class ThrowingInterop : INativeConnectorInterop
        {
            private readonly Exception _exception;

            public ThrowingInterop(Exception exception)
            {
                _exception = exception;
            }

            public int GetVersion(StringBuilder buffer, int bufferLength)
            {
                throw _exception;
            }

            public int GetBuildInfo(StringBuilder buffer, int bufferLength)
            {
                throw _exception;
            }

            public int DetectFmProcess(out NativeFm26ProcessInfo processInfo)
            {
                processInfo = default;
                throw _exception;
            }

            public int GetLastError(StringBuilder buffer, int bufferLength)
            {
                throw _exception;
            }

            public void ResetLastError()
            {
                throw _exception;
            }
        }

        private sealed class StatusInterop : INativeConnectorInterop
        {
            private readonly int _statusCode;
            private readonly string _lastError;

            public StatusInterop(int statusCode, string lastError)
            {
                _statusCode = statusCode;
                _lastError = lastError;
            }

            public int GetVersion(StringBuilder buffer, int bufferLength)
            {
                buffer.Append("test-native-0.1.0");
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
                    ExecutablePath = "C:\\Games\\Football Manager 26\\fm.exe",
                    ProductVersion = "26.0.0-test",
                    Architecture = "x64",
                    Detected = 1,
                    ReadOnlyAccess = 0
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
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-milestone31-" + Guid.NewGuid().ToString("N"));
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
