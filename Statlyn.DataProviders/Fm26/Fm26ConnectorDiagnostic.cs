using System;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26ConnectorDiagnostic
    {
        public bool IsNativeConnectorAvailable { get; set; }

        public NativeConnectorAvailability Availability { get; set; } = NativeConnectorAvailability.Unavailable;

        public string ConnectorVersion { get; set; } = string.Empty;

        public string ConnectorBuildInfo { get; set; } = string.Empty;

        public bool IsWindows { get; set; }

        public Fm26ProcessDiagnostic Process { get; set; } = new Fm26ProcessDiagnostic();

        public string ReadOnlyAccessStatus { get; set; } = "Unavailable";

        public bool IsFm26Supported { get; set; }

        public string SupportStatusMessage { get; set; } = "FM26 unsupported until validated maps exist.";

        public string LastErrorSafeMessage { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string SafeMessage { get; set; } = string.Empty;
    }
}
