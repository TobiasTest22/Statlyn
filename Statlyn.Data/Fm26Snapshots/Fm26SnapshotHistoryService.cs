using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders.Fm26.Snapshots;

namespace Statlyn.Data.Fm26Snapshots
{
    public sealed class Fm26SnapshotHistoryService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly Fm26SnapshotRepository _repository;

        public Fm26SnapshotHistoryService(StatlynDbConnectionFactory connectionFactory)
            : this(connectionFactory, new Fm26SnapshotRepository(connectionFactory))
        {
        }

        public Fm26SnapshotHistoryService(StatlynDbConnectionFactory connectionFactory, Fm26SnapshotRepository repository)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Fm26SnapshotHistoryResult SaveSnapshot(Fm26SafeSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return Failure("No safe FM26 snapshot was available to persist.");
            }

            try
            {
                EnsureDatabase();
                var record = Map(snapshot);
                _repository.SaveSnapshot(record);
                return new Fm26SnapshotHistoryResult
                {
                    Success = true,
                    SafeMessage = "Safe FM26 diagnostic snapshot persisted. No player data was stored.",
                    Snapshot = _repository.GetSnapshotById(record.SnapshotId),
                    Snapshots = _repository.ListSnapshots(20),
                    TotalCount = _repository.CountSnapshots()
                };
            }
            catch
            {
                return Failure("Safe FM26 snapshot history could not be updated. No player data was read or stored.");
            }
        }

        public Fm26SnapshotHistoryResult GetLatestSnapshot()
        {
            try
            {
                EnsureDatabase();
                var snapshot = _repository.GetLatestSnapshot();
                return new Fm26SnapshotHistoryResult
                {
                    Success = snapshot != null,
                    SafeMessage = snapshot == null
                        ? "No persisted FM26 snapshots yet."
                        : "Latest persisted FM26 diagnostic snapshot loaded.",
                    Snapshot = snapshot,
                    TotalCount = _repository.CountSnapshots()
                };
            }
            catch
            {
                return Failure("Safe FM26 snapshot history is unavailable.");
            }
        }

        public Fm26SnapshotHistoryResult ListSnapshots(int limit)
        {
            try
            {
                EnsureDatabase();
                var snapshots = _repository.ListSnapshots(limit);
                return new Fm26SnapshotHistoryResult
                {
                    Success = true,
                    SafeMessage = snapshots.Count == 0
                        ? "No persisted FM26 snapshots yet."
                        : "Persisted FM26 diagnostic snapshot history loaded.",
                    Snapshot = snapshots.FirstOrDefault(),
                    Snapshots = snapshots,
                    TotalCount = _repository.CountSnapshots()
                };
            }
            catch
            {
                return Failure("Safe FM26 snapshot history is unavailable.");
            }
        }

        public Fm26SnapshotHistoryResult GetSnapshotById(string snapshotId)
        {
            if (string.IsNullOrWhiteSpace(snapshotId))
            {
                return Failure("Snapshot id is required.");
            }

            try
            {
                EnsureDatabase();
                var snapshot = _repository.GetSnapshotById(DiagnosticSanitizer.Sanitize(snapshotId).Trim());
                return new Fm26SnapshotHistoryResult
                {
                    Success = snapshot != null,
                    SafeMessage = snapshot == null
                        ? "Persisted FM26 snapshot was not found."
                        : "Persisted FM26 diagnostic snapshot loaded.",
                    Snapshot = snapshot,
                    TotalCount = _repository.CountSnapshots()
                };
            }
            catch
            {
                return Failure("Safe FM26 snapshot lookup is unavailable.");
            }
        }

        public int CountSnapshots()
        {
            EnsureDatabase();
            return _repository.CountSnapshots();
        }

        public int DeleteOldSnapshots(int keepLatest)
        {
            EnsureDatabase();
            return _repository.DeleteOldSnapshots(keepLatest);
        }

        private void EnsureDatabase()
        {
            new StatlynDatabaseInitializer(_connectionFactory).Initialize();
        }

        private static Fm26SnapshotHistoryResult Failure(string safeMessage)
        {
            return new Fm26SnapshotHistoryResult
            {
                Success = false,
                SafeMessage = safeMessage,
                Errors = new[] { safeMessage }
            };
        }

        private static PersistedFm26SnapshotRecord Map(Fm26SafeSnapshot snapshot)
        {
            var summary = snapshot.SourceSummary ?? new Fm26SnapshotSourceSummary();
            var capability = snapshot.CapabilityReport ?? new Fm26SnapshotCapabilityReport();

            return new PersistedFm26SnapshotRecord
            {
                SnapshotId = Safe(snapshot.SnapshotId),
                GeneratedAtUtc = snapshot.GeneratedAtUtc,
                SnapshotStatus = snapshot.Status.ToString(),
                SafeMessage = Safe(snapshot.SafeMessage),
                ConnectorAvailability = Safe(summary.ConnectorAvailability),
                PlatformStatus = Safe(summary.PlatformStatus),
                ProcessDetected = summary.ProcessDetected,
                ProcessStatus = Safe(summary.ProcessStatus),
                ProcessName = Safe(summary.ProcessName),
                ProcessId = summary.ProcessId,
                ProductVersion = Safe(summary.ProductVersion),
                FileVersion = Safe(summary.FileVersion),
                Architecture = Safe(summary.Architecture),
                ReadOnlyAccessStatus = Safe(summary.ReadOnlyAccessStatus),
                MemoryMapRegistryStatus = Safe(summary.MemoryMapRegistryStatus),
                MapsFound = Math.Max(0, summary.MapsFound),
                ValidatedMaps = Math.Max(0, summary.ValidatedMaps),
                TemplateMaps = Math.Max(0, summary.TemplateMaps),
                InvalidMaps = Math.Max(0, summary.InvalidMaps),
                SelectedMapId = Safe(summary.SelectedMapId),
                SelectedMapDisplayName = Safe(summary.SelectedMapDisplayName),
                SelectedMapBuild = Safe(summary.SelectedMapBuild),
                AllGatesPassed = capability.AllGatesPassed,
                BlockingGate = Safe(capability.BlockingGate),
                LiveReadingAllowed = capability.IsLiveReadingAvailable,
                NextActionSafeMessage = Safe(snapshot.NextActionSafeMessage),
                WarningCount = snapshot.Warnings == null ? 0 : snapshot.Warnings.Count,
                ErrorCount = snapshot.Errors == null ? 0 : snapshot.Errors.Count,
                Gates = MapGates(snapshot)
            };
        }

        private static IReadOnlyList<PersistedFm26SnapshotGateRecord> MapGates(Fm26SafeSnapshot snapshot)
        {
            var gates = snapshot.Gates ?? Array.Empty<Fm26SnapshotGateResult>();
            var records = new List<PersistedFm26SnapshotGateRecord>();
            var index = 0;
            foreach (var gate in gates)
            {
                records.Add(new PersistedFm26SnapshotGateRecord
                {
                    SnapshotId = Safe(snapshot.SnapshotId),
                    GateKey = Safe(gate.GateKey),
                    GateName = Safe(gate.Label),
                    Status = gate.GateStatus.ToString(),
                    SafeMessage = Safe(gate.SafeMessage),
                    SortOrder = index
                });
                index++;
            }

            return records;
        }

        private static string Safe(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : DiagnosticSanitizer.Sanitize(value).Trim();
        }
    }
}
