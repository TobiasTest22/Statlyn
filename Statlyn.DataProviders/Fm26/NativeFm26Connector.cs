using System;
using System.Runtime.InteropServices;
using System.Text;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class NativeFm26Connector : IFm26NativeConnector
    {
        private const int TextBufferLength = 2048;
        private const string UnsupportedMessage = "FM26 unsupported until validated maps exist.";
        private const string MapMissingMessage = "No validated FM26 memory map is loaded.";
        private const string NextActionMessage = "A validated FM26 memory map is required before live FM player data can be read.";
        private const string ReadOnlyAccessLevel = "Read-only diagnostic process query; no write or injection.";
        private readonly INativeConnectorInterop _interop;
        private readonly Fm26NativeConnectorOptions _options;
        private NativeConnectorAvailability _availability = NativeConnectorAvailability.Unavailable;
        private string _lastError = string.Empty;

        public NativeFm26Connector()
            : this(new PInvokeNativeConnectorInterop(), new Fm26NativeConnectorOptions())
        {
        }

        internal NativeFm26Connector(INativeConnectorInterop interop, Fm26NativeConnectorOptions options)
        {
            _interop = interop ?? throw new ArgumentNullException(nameof(interop));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool IsAvailable
        {
            get { return GetDiagnostic().IsNativeConnectorAvailable; }
        }

        public string LastError
        {
            get { return _lastError; }
        }

        public string GetConnectorVersion()
        {
            return TryReadConnectorText(_interop.GetVersion);
        }

        public string GetBuildInfo()
        {
            return TryReadConnectorText(_interop.GetBuildInfo);
        }

        public Fm26ProcessDiagnostic DetectFmProcess()
        {
            return GetDiagnostic().Process;
        }

        public Fm26ConnectorDiagnostic GetDiagnostic()
        {
            var isWindows = IsWindows();
            if (!isWindows)
            {
                _availability = NativeConnectorAvailability.UnsupportedPlatform;
                _lastError = "Native FM diagnostics are available only on Windows.";
                return BuildUnavailableDiagnostic(isWindows, _availability, _lastError);
            }

            try
            {
                _interop.ResetLastError();
                var version = TryReadText(_interop.GetVersion, out var connectorVersion) ? connectorVersion : string.Empty;
                var buildInfo = TryReadText(_interop.GetBuildInfo, out var connectorBuildInfo) ? connectorBuildInfo : string.Empty;

                var status = (NativeConnectorStatusCode)_interop.DetectFmProcess(out var nativeProcess);
                var lastError = ReadNativeLastError();
                var process = MapProcessDiagnostic(nativeProcess, status, lastError);
                _availability = NativeConnectorAvailability.Available;
                _lastError = lastError;

                return new Fm26ConnectorDiagnostic
                {
                    IsNativeConnectorAvailable = true,
                    Availability = _availability,
                    ConnectorVersion = version,
                    ConnectorBuildInfo = buildInfo,
                    IsWindows = true,
                    Process = process,
                    ReadOnlyAccessStatus = process.ReadOnlyAccessStatus,
                    IsFm26Supported = false,
                    BuildSupportStatus = ResolveBuildSupportStatus(true, _availability, status, process),
                    BuildSupportMessage = ResolveBuildSupportMessage(status, process),
                    MapSupportStatus = Fm26DiagnosticSupportStatus.MapMissing.ToString(),
                    MapSupportMessage = MapMissingMessage,
                    SupportStatusMessage = UnsupportedMessage,
                    NextActionSafeMessage = NextActionMessage,
                    LastErrorSafeMessage = lastError,
                    GeneratedAtUtc = DateTimeOffset.UtcNow,
                    Warnings = BuildWarnings(process, status),
                    Errors = BuildErrors(_availability, lastError),
                    SafeMessage = process.IsDetected
                        ? "FM process detected. Statlyn reports diagnostics only; no player data is read."
                        : "Native connector is available. FM process not detected."
                };
            }
            catch (DllNotFoundException ex)
            {
                return CaptureUnavailable(NativeConnectorAvailability.MissingLibrary, "Native connector library is not available.", ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                return CaptureUnavailable(NativeConnectorAvailability.MissingExport, "Native connector exports are not aligned with the managed binding.", ex);
            }
            catch (BadImageFormatException ex)
            {
                return CaptureUnavailable(NativeConnectorAvailability.BadImage, "Native connector binary architecture is not compatible.", ex);
            }
            catch (Exception ex)
            {
                return CaptureUnavailable(NativeConnectorAvailability.Error, "Native connector diagnostics could not be read safely.", ex);
            }
        }

        public DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo)
        {
            return DiagnosticStatus.Unsupported;
        }

        private bool IsWindows()
        {
            return _options.ForceIsWindows ?? RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        private string TryReadConnectorText(Func<StringBuilder, int, int> reader)
        {
            if (!IsWindows())
            {
                _availability = NativeConnectorAvailability.UnsupportedPlatform;
                _lastError = "Native FM diagnostics are available only on Windows.";
                return string.Empty;
            }

            try
            {
                return TryReadText(reader, out var value) ? value : string.Empty;
            }
            catch (DllNotFoundException ex)
            {
                CaptureUnavailable(NativeConnectorAvailability.MissingLibrary, "Native connector library is not available.", ex);
                return string.Empty;
            }
            catch (EntryPointNotFoundException ex)
            {
                CaptureUnavailable(NativeConnectorAvailability.MissingExport, "Native connector exports are not aligned with the managed binding.", ex);
                return string.Empty;
            }
            catch (BadImageFormatException ex)
            {
                CaptureUnavailable(NativeConnectorAvailability.BadImage, "Native connector binary architecture is not compatible.", ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                CaptureUnavailable(NativeConnectorAvailability.Error, "Native connector diagnostics could not be read safely.", ex);
                return string.Empty;
            }
        }

        private Fm26ConnectorDiagnostic CaptureUnavailable(NativeConnectorAvailability availability, string message, Exception exception)
        {
            _availability = availability;
            _lastError = SafeConnectorText.Sanitize(message);
            return BuildUnavailableDiagnostic(true, availability, _lastError);
        }

        private static Fm26ConnectorDiagnostic BuildUnavailableDiagnostic(bool isWindows, NativeConnectorAvailability availability, string message)
        {
            var safeMessage = SafeConnectorText.Sanitize(message);
            return new Fm26ConnectorDiagnostic
            {
                IsNativeConnectorAvailable = false,
                Availability = availability,
                ConnectorVersion = string.Empty,
                ConnectorBuildInfo = string.Empty,
                IsWindows = isWindows,
                Process = new Fm26ProcessDiagnostic
                {
                    IsDetected = false,
                    DetectionStatus = isWindows
                        ? Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString()
                        : Fm26DiagnosticSupportStatus.UnsupportedPlatform.ToString(),
                    DetectionStatusMessage = safeMessage,
                    ProcessName = "fm.exe",
                    ProcessId = null,
                    ExecutableFileName = "fm.exe",
                    ReadOnlyAccessStatus = "Unavailable",
                    RequiredAccessLevel = ReadOnlyAccessLevel,
                    SafeMessage = "FM process diagnostics are unavailable."
                },
                ReadOnlyAccessStatus = "Unavailable",
                IsFm26Supported = false,
                BuildSupportStatus = isWindows
                    ? Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString()
                    : Fm26DiagnosticSupportStatus.UnsupportedPlatform.ToString(),
                BuildSupportMessage = safeMessage,
                MapSupportStatus = Fm26DiagnosticSupportStatus.MapMissing.ToString(),
                MapSupportMessage = MapMissingMessage,
                SupportStatusMessage = UnsupportedMessage,
                NextActionSafeMessage = NextActionMessage,
                LastErrorSafeMessage = safeMessage,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Warnings = new[] { UnsupportedMessage, MapMissingMessage },
                Errors = string.IsNullOrWhiteSpace(safeMessage) ? Array.Empty<string>() : new[] { safeMessage },
                SafeMessage = safeMessage
            };
        }

        private static Fm26ProcessDiagnostic MapProcessDiagnostic(NativeFm26ProcessInfo nativeProcess, NativeConnectorStatusCode status, string lastError)
        {
            var detected = nativeProcess.Detected != 0 && status != NativeConnectorStatusCode.NotFound;
            var accessStatus = nativeProcess.ReadOnlyAccess != 0 ? "Available" : status == NativeConnectorStatusCode.AccessDenied ? "Access denied" : "Unavailable";
            var executableFileName = SafeConnectorText.FileNameLabel(nativeProcess.ExecutablePath, "fm.exe");
            var directoryLabel = SafeConnectorText.DirectoryLabel(nativeProcess.ExecutablePath);
            var architecture = SafeConnectorText.Sanitize(nativeProcess.Architecture);

            return new Fm26ProcessDiagnostic
            {
                IsDetected = detected,
                DetectionStatus = ResolveDetectionStatus(status, detected),
                DetectionStatusMessage = ResolveProcessMessage(status, detected, lastError),
                ProcessDetectedAtUtc = detected ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
                ProcessName = "fm.exe",
                ProcessId = detected ? (int?)nativeProcess.ProcessId : null,
                ExecutableFileName = executableFileName,
                ExecutableDirectorySafeLabel = directoryLabel,
                ProcessPath = SafeConnectorText.PathLabel(nativeProcess.ExecutablePath, executableFileName),
                ProductName = string.Empty,
                ProductVersion = SafeConnectorText.Sanitize(nativeProcess.ProductVersion),
                FileVersion = SafeConnectorText.Sanitize(nativeProcess.ProductVersion),
                Architecture = architecture,
                Is64BitProcess = string.IsNullOrWhiteSpace(architecture) ? (bool?)null : architecture.IndexOf("64", StringComparison.OrdinalIgnoreCase) >= 0 || architecture.Equals("x64", StringComparison.OrdinalIgnoreCase),
                ReadOnlyAccessAttempted = detected,
                HasReadOnlyAccess = nativeProcess.ReadOnlyAccess != 0,
                ReadOnlyAccessStatus = accessStatus,
                RequiredAccessLevel = ReadOnlyAccessLevel,
                SafeMessage = ResolveProcessMessage(status, detected, lastError)
            };
        }

        private static string ResolveDetectionStatus(NativeConnectorStatusCode status, bool detected)
        {
            if (!detected || status == NativeConnectorStatusCode.NotFound)
            {
                return Fm26DiagnosticSupportStatus.NotDetected.ToString();
            }

            if (status == NativeConnectorStatusCode.AccessDenied)
            {
                return Fm26DiagnosticSupportStatus.AccessDenied.ToString();
            }

            if (status == NativeConnectorStatusCode.UnsupportedBuild)
            {
                return Fm26DiagnosticSupportStatus.UnsupportedBuild.ToString();
            }

            return Fm26DiagnosticSupportStatus.DiagnosticsOnly.ToString();
        }

        private static string ResolveBuildSupportStatus(bool isWindows, NativeConnectorAvailability availability, NativeConnectorStatusCode status, Fm26ProcessDiagnostic process)
        {
            if (!isWindows)
            {
                return Fm26DiagnosticSupportStatus.UnsupportedPlatform.ToString();
            }

            if (availability != NativeConnectorAvailability.Available)
            {
                return Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString();
            }

            if (!process.IsDetected)
            {
                return Fm26DiagnosticSupportStatus.NotDetected.ToString();
            }

            if (status == NativeConnectorStatusCode.AccessDenied)
            {
                return Fm26DiagnosticSupportStatus.AccessDenied.ToString();
            }

            if (status == NativeConnectorStatusCode.UnsupportedBuild)
            {
                return Fm26DiagnosticSupportStatus.UnsupportedBuild.ToString();
            }

            return Fm26DiagnosticSupportStatus.DiagnosticsOnly.ToString();
        }

        private static string ResolveBuildSupportMessage(NativeConnectorStatusCode status, Fm26ProcessDiagnostic process)
        {
            if (!process.IsDetected)
            {
                return "FM process not detected. Statlyn cannot validate a build.";
            }

            if (status == NativeConnectorStatusCode.AccessDenied)
            {
                return "FM process detected, but Windows denied the read-only diagnostic access required for future map validation.";
            }

            if (status == NativeConnectorStatusCode.UnsupportedBuild)
            {
                return "FM process detected, but this build has no validated Statlyn map.";
            }

            return "FM process metadata is diagnostics-only. Build support cannot become available until a validated memory map is loaded.";
        }

        private static string[] BuildWarnings(Fm26ProcessDiagnostic process, NativeConnectorStatusCode status)
        {
            if (status == NativeConnectorStatusCode.AccessDenied)
            {
                return new[] { "Read-only process diagnostics were denied.", UnsupportedMessage, MapMissingMessage };
            }

            if (!process.IsDetected)
            {
                return new[] { "FM process not detected.", UnsupportedMessage, MapMissingMessage };
            }

            return new[] { UnsupportedMessage, MapMissingMessage, "No player data is read by diagnostics." };
        }

        private static string[] BuildErrors(NativeConnectorAvailability availability, string lastError)
        {
            if (availability == NativeConnectorAvailability.Available || string.IsNullOrWhiteSpace(lastError))
            {
                return Array.Empty<string>();
            }

            return new[] { SafeConnectorText.Sanitize(lastError) };
        }

        private static string ResolveProcessMessage(NativeConnectorStatusCode status, bool detected, string lastError)
        {
            if (!string.IsNullOrWhiteSpace(lastError) && status != NativeConnectorStatusCode.Ok)
            {
                return lastError;
            }

            switch (status)
            {
                case NativeConnectorStatusCode.Ok:
                    return detected
                        ? "FM process detected with read-only diagnostics."
                        : "FM process not detected.";
                case NativeConnectorStatusCode.NotFound:
                    return "FM process not detected.";
                case NativeConnectorStatusCode.AccessDenied:
                    return "FM process detected, but Windows denied read-only diagnostics.";
                case NativeConnectorStatusCode.UnsupportedBuild:
                    return "FM process detected, but this build is unsupported.";
                default:
                    return "FM process diagnostics are unavailable.";
            }
        }

        private string ReadNativeLastError()
        {
            return TryReadText(_interop.GetLastError, out var lastError) ? lastError : _lastError;
        }

        private bool TryReadText(Func<StringBuilder, int, int> reader, out string value)
        {
            var buffer = new StringBuilder(TextBufferLength);
            var status = reader(buffer, buffer.Capacity);
            value = SafeConnectorText.Sanitize(buffer.ToString());
            return status == (int)NativeConnectorStatusCode.Ok;
        }
    }
}
