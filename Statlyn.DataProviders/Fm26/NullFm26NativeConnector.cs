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
                ProcessName = "fm.exe",
                ProcessId = null,
                ReadOnlyAccessStatus = "Unavailable",
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
                SupportStatusMessage = "FM26 unsupported until validated maps exist.",
                LastErrorSafeMessage = LastError,
                SafeMessage = "Native connector unavailable. No live FM26 data is exposed."
            };
        }

        public DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo)
        {
            return DiagnosticStatus.Unsupported;
        }
    }
}
