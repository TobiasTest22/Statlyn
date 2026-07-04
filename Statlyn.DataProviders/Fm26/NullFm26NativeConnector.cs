using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class NullFm26NativeConnector : IFm26NativeConnector
    {
        public bool IsAvailable
        {
            get { return false; }
        }

        public string LastError
        {
            get { return "Native connector unavailable."; }
        }

        public string GetConnectorVersion()
        {
            return "managed-null-0.1.0";
        }

        public string GetBuildInfo()
        {
            return "Managed null connector";
        }

        public Fm26ProcessDiagnostic DetectFmProcess()
        {
            return new Fm26ProcessDiagnostic
            {
                IsDetected = false,
                DetectionStatus = Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString(),
                DetectionStatusMessage = "Native connector unavailable.",
                ProcessName = "fm.exe",
                ProcessId = null,
                ExecutableFileName = "fm.exe",
                ReadOnlyAccessAttempted = false,
                ReadOnlyAccessStatus = "Unavailable",
                RequiredAccessLevel = "Read-only diagnostic process query; no write or injection.",
                SafeMessage = "FM process diagnostics are unavailable."
            };
        }

        public Fm26ConnectorDiagnostic GetDiagnostic()
        {
            return new Fm26ConnectorDiagnostic
            {
                IsNativeConnectorAvailable = false,
                Availability = NativeConnectorAvailability.Unavailable,
                ConnectorVersion = GetConnectorVersion(),
                ConnectorBuildInfo = GetBuildInfo(),
                Process = DetectFmProcess(),
                ReadOnlyAccessStatus = "Unavailable",
                IsFm26Supported = false,
                BuildSupportStatus = Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString(),
                BuildSupportMessage = "Native connector diagnostics are unavailable.",
                MapSupportStatus = Fm26DiagnosticSupportStatus.MapMissing.ToString(),
                MapSupportMessage = "No validated FM26 memory map is loaded.",
                SupportStatusMessage = "FM26 unsupported until validated maps exist.",
                NextActionSafeMessage = "A validated FM26 memory map is required before live FM player data can be read.",
                LastErrorSafeMessage = LastError,
                Warnings = new[]
                {
                    "Native connector diagnostics are unavailable.",
                    "FM26 unsupported until validated maps exist.",
                    "No validated FM26 memory map is loaded."
                },
                SafeMessage = "Native connector unavailable. No live FM26 data is exposed."
            };
        }

        public DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo)
        {
            return DiagnosticStatus.Unsupported;
        }
    }
}
