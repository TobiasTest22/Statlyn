using Statlyn.Core.Diagnostics;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.Tests
{
    internal sealed class UnsupportedBuildConnector : IFm26NativeConnector
    {
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
            return "test-unsupported-connector";
        }

        public string GetBuildInfo()
        {
            return "test connector";
        }

        public Fm26ProcessDiagnostic DetectFmProcess()
        {
            return new Fm26ProcessDiagnostic
            {
                IsDetected = true,
                ProcessName = "fm.exe",
                ProcessId = 4242,
                ProcessPath = "C:\\Games\\Football Manager 26\\fm.exe",
                Architecture = "x64",
                ProductVersion = "26.0.0-test",
                HasReadOnlyAccess = true,
                ReadOnlyAccessStatus = "Available",
                SafeMessage = "FM26 detected."
            };
        }

        public Fm26ConnectorDiagnostic GetDiagnostic()
        {
            var process = DetectFmProcess();
            return new Fm26ConnectorDiagnostic
            {
                IsNativeConnectorAvailable = true,
                Availability = NativeConnectorAvailability.Available,
                ConnectorVersion = GetConnectorVersion(),
                ConnectorBuildInfo = GetBuildInfo(),
                IsWindows = true,
                Process = process,
                ReadOnlyAccessStatus = process.ReadOnlyAccessStatus,
                IsFm26Supported = false,
                SupportStatusMessage = "FM26 unsupported until validated maps exist.",
                SafeMessage = "Test connector reports a process but no supported build."
            };
        }

        public DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo)
        {
            return DiagnosticStatus.Unsupported;
        }
    }
}
