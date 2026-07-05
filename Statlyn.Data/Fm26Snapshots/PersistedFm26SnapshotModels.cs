using System;
using System.Collections.Generic;

namespace Statlyn.Data.Fm26Snapshots
{
    public sealed class PersistedFm26SnapshotRecord
    {
        public string SnapshotId { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string SnapshotStatus { get; set; } = string.Empty;

        public string SafeMessage { get; set; } = string.Empty;

        public string ConnectorAvailability { get; set; } = string.Empty;

        public string PlatformStatus { get; set; } = string.Empty;

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

        public bool AllGatesPassed { get; set; }

        public string BlockingGate { get; set; } = string.Empty;

        public bool LiveReadingAllowed { get; set; }

        public string NextActionSafeMessage { get; set; } = string.Empty;

        public int WarningCount { get; set; }

        public int ErrorCount { get; set; }

        public IReadOnlyList<PersistedFm26SnapshotGateRecord> Gates { get; set; } = Array.Empty<PersistedFm26SnapshotGateRecord>();
    }

    public sealed class PersistedFm26SnapshotGateRecord
    {
        public string SnapshotId { get; set; } = string.Empty;

        public string GateKey { get; set; } = string.Empty;

        public string GateName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string SafeMessage { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }

    public sealed class Fm26SnapshotHistoryResult
    {
        public bool Success { get; set; }

        public string SafeMessage { get; set; } = string.Empty;

        public PersistedFm26SnapshotRecord? Snapshot { get; set; }

        public IReadOnlyList<PersistedFm26SnapshotRecord> Snapshots { get; set; } = Array.Empty<PersistedFm26SnapshotRecord>();

        public int TotalCount { get; set; }

        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }
}
