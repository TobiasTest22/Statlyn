using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.DataProviders.Fm26.MemoryMaps;

namespace Statlyn.DataProviders.Fm26.Snapshots
{
    public sealed class SafeFm26SnapshotService
    {
        private readonly SafeFm26ConnectorService _connectorService;
        private readonly MemoryMapRegistryLoader _memoryMapLoader;
        private readonly MemoryMapSelector _memoryMapSelector;
        private readonly Fm26SnapshotGateEvaluator _gateEvaluator;

        public SafeFm26SnapshotService(
            SafeFm26ConnectorService connectorService,
            MemoryMapRegistryLoader memoryMapLoader)
            : this(connectorService, memoryMapLoader, new MemoryMapSelector(), new Fm26SnapshotGateEvaluator())
        {
        }

        public SafeFm26SnapshotService(
            SafeFm26ConnectorService connectorService,
            MemoryMapRegistryLoader memoryMapLoader,
            MemoryMapSelector memoryMapSelector,
            Fm26SnapshotGateEvaluator gateEvaluator)
        {
            _connectorService = connectorService ?? throw new ArgumentNullException(nameof(connectorService));
            _memoryMapLoader = memoryMapLoader ?? throw new ArgumentNullException(nameof(memoryMapLoader));
            _memoryMapSelector = memoryMapSelector ?? throw new ArgumentNullException(nameof(memoryMapSelector));
            _gateEvaluator = gateEvaluator ?? throw new ArgumentNullException(nameof(gateEvaluator));
        }

        public Fm26SnapshotResult CreateSnapshot(Fm26SnapshotRequest? request = null)
        {
            var safeRequest = request ?? new Fm26SnapshotRequest();
            var diagnostic = _connectorService.GetDiagnostic();
            var registry = _memoryMapLoader.Load();
            var selection = _memoryMapSelector.Select(registry, diagnostic.Process);
            var evaluation = _gateEvaluator.Evaluate(diagnostic, registry, selection, safeRequest);
            var snapshot = BuildSnapshot(diagnostic, registry, selection, evaluation, safeRequest);

            return new Fm26SnapshotResult
            {
                Success = snapshot.Errors.Count == 0,
                Snapshot = snapshot,
                Errors = snapshot.Errors
            };
        }

        private static Fm26SafeSnapshot BuildSnapshot(
            Fm26ConnectorDiagnostic diagnostic,
            MemoryMapRegistryDiagnostic registry,
            MemoryMapSelectionResult selection,
            Fm26SnapshotEvaluation evaluation,
            Fm26SnapshotRequest request)
        {
            var process = diagnostic.Process ?? new Fm26ProcessDiagnostic();
            var summary = new Fm26SnapshotSourceSummary
            {
                ConnectorAvailability = diagnostic.Availability.ToString(),
                IsNativeConnectorAvailable = diagnostic.IsNativeConnectorAvailable,
                PlatformStatus = diagnostic.IsWindows ? "Windows" : "Unsupported",
                IsWindows = diagnostic.IsWindows,
                ProcessDetected = process.IsDetected,
                ProcessStatus = SafeConnectorText.Sanitize(process.DetectionStatus),
                ProcessName = SafeConnectorText.Sanitize(process.ProcessName),
                ProcessId = request.IncludeSafeProcessMetadata ? process.ProcessId : null,
                ProductVersion = request.IncludeSafeProcessMetadata ? SafeConnectorText.Sanitize(process.ProductVersion) : string.Empty,
                FileVersion = request.IncludeSafeProcessMetadata ? SafeConnectorText.Sanitize(process.FileVersion) : string.Empty,
                Architecture = request.IncludeSafeProcessMetadata ? SafeConnectorText.Sanitize(process.Architecture) : string.Empty,
                ReadOnlyAccessStatus = SafeConnectorText.Sanitize(FirstNonBlank(diagnostic.ReadOnlyAccessStatus, process.ReadOnlyAccessStatus, "Unavailable")),
                MemoryMapRegistryStatus = SafeConnectorText.Sanitize(registry.RegistryStatus),
                MapsFound = registry.MapsFoundCount,
                ValidatedMaps = registry.UsableMapsCount,
                TemplateMaps = registry.TemplateMapsCount,
                InvalidMaps = registry.InvalidMapsCount,
                SelectedMapId = SafeConnectorText.Sanitize(selection.SelectedMapId),
                SelectedMapDisplayName = SafeConnectorText.Sanitize(selection.SelectedMapDisplayName),
                SelectedMapBuild = SafeConnectorText.Sanitize(FirstNonBlank(selection.SelectedMap?.BuildNumber, selection.SelectedMap?.GameVersion, string.Empty)),
                SelectedMapStatus = SafeConnectorText.Sanitize(selection.SupportStatus)
            };

            var warnings = diagnostic.Warnings
                .Concat(new[]
                {
                    registry.SafeMessage,
                    selection.SupportMessage,
                    "Safe FM26 snapshots are diagnostic metadata only. No player data is read."
                })
                .Select(SafeConnectorText.Sanitize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var errors = diagnostic.Errors
                .Select(SafeConnectorText.Sanitize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new Fm26SafeSnapshot
            {
                SnapshotId = "fm26-snapshot-" + Guid.NewGuid().ToString("N"),
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Status = evaluation.Status,
                SafeMessage = MessageFor(evaluation.Status),
                SourceSummary = summary,
                CapabilityReport = new Fm26SnapshotCapabilityReport
                {
                    AllGatesPassed = evaluation.BlockReasons.Count == 0,
                    BlockingGate = evaluation.BlockingGate,
                    IsFm26Supported = false,
                    IsLiveReadingAvailable = false,
                    ReaderStatus = "NotImplemented",
                    FieldPolicyStatus = evaluation.BlockingGate == "fieldPolicy" ? "Blocked" : "NotEvaluated"
                },
                Gates = evaluation.Gates,
                BlockReasons = evaluation.BlockReasons,
                NextActionSafeMessage = SafeConnectorText.Sanitize(evaluation.NextActionSafeMessage),
                Warnings = warnings,
                Errors = errors
            };
        }

        private static string MessageFor(Fm26SnapshotStatus status)
        {
            switch (status)
            {
                case Fm26SnapshotStatus.BlockedConnectorUnavailable:
                    return "FM26 safe snapshot created from metadata checks. Native connector diagnostics are unavailable and no player data is read.";
                case Fm26SnapshotStatus.BlockedUnsupportedPlatform:
                    return "FM26 safe snapshot created from metadata checks. This platform cannot run FM26 connector diagnostics.";
                case Fm26SnapshotStatus.BlockedProcessNotDetected:
                    return "FM26 safe snapshot created from metadata checks. FM.exe is not detected and no player data is read.";
                case Fm26SnapshotStatus.BlockedReadOnlyAccessUnavailable:
                    return "FM26 safe snapshot created from metadata checks. Read-only process diagnostics are unavailable.";
                case Fm26SnapshotStatus.BlockedNoValidatedMap:
                    return "FM26 safe snapshot created from diagnostics metadata. Live reading is blocked because no validated FM26 map is selected.";
                case Fm26SnapshotStatus.BlockedMapMismatch:
                    return "FM26 safe snapshot created from diagnostics metadata. Live reading is blocked because no validated map matches the detected build.";
                case Fm26SnapshotStatus.BlockedReaderNotImplemented:
                    return "FM26 safe snapshot created from diagnostics metadata. Live reading is blocked because the reader is not implemented.";
                case Fm26SnapshotStatus.BlockedFieldPolicy:
                    return "FM26 safe snapshot created from diagnostics metadata. Field policy blocked the requested snapshot.";
                case Fm26SnapshotStatus.MetadataSnapshotCreated:
                    return "FM26 diagnostic metadata snapshot was created. No player data is read.";
                default:
                    return "FM26 safe snapshot is diagnostic metadata only. No player data is read.";
            }
        }

        private static string FirstNonBlank(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
