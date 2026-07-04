using System;
using System.Collections.Generic;

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

        public string BuildSupportStatus { get; set; } = Fm26DiagnosticSupportStatus.ConnectorUnavailable.ToString();

        public string BuildSupportMessage { get; set; } = "FM26 support is not available in diagnostics-only mode.";

        public string MapSupportStatus { get; set; } = Fm26DiagnosticSupportStatus.MapMissing.ToString();

        public string MapSupportMessage { get; set; } = "No validated FM26 memory map is loaded.";

        public string SupportStatusMessage { get; set; } = "FM26 unsupported until validated maps exist.";

        public string NextActionSafeMessage { get; set; } = "A validated FM26 memory map is required before live FM player data can be read.";

        public string LastErrorSafeMessage { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string SafeMessage { get; set; } = string.Empty;

        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}
