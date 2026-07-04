using System;
using System.Collections.Generic;

namespace Statlyn.DataProviders.Fm26.Snapshots
{
    public enum Fm26SnapshotStatus
    {
        DiagnosticsOnly,
        BlockedConnectorUnavailable,
        BlockedUnsupportedPlatform,
        BlockedProcessNotDetected,
        BlockedReadOnlyAccessUnavailable,
        BlockedNoValidatedMap,
        BlockedMapMismatch,
        BlockedReaderNotImplemented,
        BlockedFieldPolicy,
        MetadataSnapshotCreated
    }

    public enum Fm26SnapshotGateStatus
    {
        Passed,
        Warning,
        Blocked,
        NotChecked
    }

    public sealed class Fm26SnapshotRequest
    {
        public string RequestedMode { get; set; } = "DiagnosticsOnly";

        public bool IncludeSafeProcessMetadata { get; set; } = true;
    }

    public sealed class Fm26SnapshotGateResult
    {
        public string GateKey { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public Fm26SnapshotGateStatus GateStatus { get; set; } = Fm26SnapshotGateStatus.NotChecked;

        public Fm26SnapshotStatus SnapshotStatus { get; set; } = Fm26SnapshotStatus.DiagnosticsOnly;

        public string SafeMessage { get; set; } = string.Empty;

        public string NextActionSafeMessage { get; set; } = string.Empty;
    }

    public sealed class Fm26SnapshotBlockReason
    {
        public string GateKey { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string SafeMessage { get; set; } = string.Empty;

        public string NextActionSafeMessage { get; set; } = string.Empty;
    }

    public sealed class Fm26SnapshotSourceSummary
    {
        public string ConnectorAvailability { get; set; } = string.Empty;

        public bool IsNativeConnectorAvailable { get; set; }

        public string PlatformStatus { get; set; } = string.Empty;

        public bool IsWindows { get; set; }

        public bool ProcessDetected { get; set; }

        public string ProcessStatus { get; set; } = string.Empty;

        public string ProcessName { get; set; } = string.Empty;

        public int? ProcessId { get; set; }

        public string ProductVersion { get; set; } = string.Empty;

        public string FileVersion { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;

        public string ReadOnlyAccessStatus { get; set; } = string.Empty;

        public string MemoryMapRegistryStatus { get; set; } = string.Empty;

        public int MapsFound { get; set; }

        public int ValidatedMaps { get; set; }

        public int TemplateMaps { get; set; }

        public int InvalidMaps { get; set; }

        public string SelectedMapId { get; set; } = string.Empty;

        public string SelectedMapDisplayName { get; set; } = string.Empty;

        public string SelectedMapBuild { get; set; } = string.Empty;

        public string SelectedMapStatus { get; set; } = string.Empty;
    }

    public sealed class Fm26SnapshotCapabilityReport
    {
        public bool AllGatesPassed { get; set; }

        public string BlockingGate { get; set; } = string.Empty;

        public bool IsFm26Supported { get; set; }

        public bool IsLiveReadingAvailable { get; set; }

        public string ReaderStatus { get; set; } = "NotImplemented";

        public string FieldPolicyStatus { get; set; } = "NotEvaluated";
    }

    public sealed class Fm26SafeSnapshot
    {
        public string SnapshotId { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public Fm26SnapshotStatus Status { get; set; } = Fm26SnapshotStatus.DiagnosticsOnly;

        public string SafeMessage { get; set; } = string.Empty;

        public Fm26SnapshotSourceSummary SourceSummary { get; set; } = new Fm26SnapshotSourceSummary();

        public Fm26SnapshotCapabilityReport CapabilityReport { get; set; } = new Fm26SnapshotCapabilityReport();

        public IReadOnlyList<Fm26SnapshotGateResult> Gates { get; set; } = Array.Empty<Fm26SnapshotGateResult>();

        public IReadOnlyList<Fm26SnapshotBlockReason> BlockReasons { get; set; } = Array.Empty<Fm26SnapshotBlockReason>();

        public string NextActionSafeMessage { get; set; } = string.Empty;

        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }

    public sealed class Fm26SnapshotResult
    {
        public bool Success { get; set; }

        public Fm26SafeSnapshot Snapshot { get; set; } = new Fm26SafeSnapshot();

        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}
