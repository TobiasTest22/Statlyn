using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.DataProviders.Fm26.MemoryMaps;

namespace Statlyn.DataProviders.Fm26.Snapshots
{
    public sealed class Fm26SnapshotGateEvaluator
    {
        private static readonly string[] GateOrder =
        {
            "connector",
            "platform",
            "process",
            "readOnly",
            "mapRegistry",
            "validatedMap",
            "selectedMap",
            "reader",
            "fieldPolicy"
        };

        public Fm26SnapshotEvaluation Evaluate(
            Fm26ConnectorDiagnostic diagnostic,
            MemoryMapRegistryDiagnostic registry,
            MemoryMapSelectionResult selection,
            Fm26SnapshotRequest request)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            var gates = new List<Fm26SnapshotGateResult>();

            if (!diagnostic.IsNativeConnectorAvailable)
            {
                AddBlocked(gates, "connector", "Connector", Fm26SnapshotStatus.BlockedConnectorUnavailable, "Native connector diagnostics are unavailable.", "Install or build the read-only native connector before checking FM26.");
                return Finish(gates);
            }

            AddPassed(gates, "connector", "Connector", "Native connector diagnostics boundary is available.");

            if (!diagnostic.IsWindows)
            {
                AddBlocked(gates, "platform", "Platform", Fm26SnapshotStatus.BlockedUnsupportedPlatform, "FM26 connector diagnostics are supported only on Windows.", "Run Statlyn on Windows before checking FM26.");
                return Finish(gates);
            }

            AddPassed(gates, "platform", "Platform", "Windows platform diagnostics are available.");

            var process = diagnostic.Process ?? new Fm26ProcessDiagnostic();
            if (!process.IsDetected)
            {
                AddBlocked(gates, "process", "FM Process", Fm26SnapshotStatus.BlockedProcessNotDetected, process.DetectionStatusMessage, "Start FM26, then run the safe snapshot check again.");
                return Finish(gates);
            }

            AddPassed(gates, "process", "FM Process", "FM process metadata was detected through diagnostics only.");

            if (!process.HasReadOnlyAccess || IsUnavailable(diagnostic.ReadOnlyAccessStatus) || IsUnavailable(process.ReadOnlyAccessStatus))
            {
                AddBlocked(gates, "readOnly", "Read-only Access", Fm26SnapshotStatus.BlockedReadOnlyAccessUnavailable, "Read-only diagnostics are not available for the detected process.", "Allow read-only process query diagnostics; write or injection access is never required.");
                return Finish(gates);
            }

            AddPassed(gates, "readOnly", "Read-only Access", "Read-only process diagnostics are available.");

            if (registry.MapsFoundCount == 0 || IsRegistryMissing(registry.RegistryStatus) || registry.RegistryStatus == MemoryMapSupportStatus.Invalid.ToString())
            {
                AddBlocked(gates, "mapRegistry", "Memory-map Registry", Fm26SnapshotStatus.BlockedNoValidatedMap, registry.SafeMessage, "Add validated FM26 map metadata before any future live-reading milestone.");
                return Finish(gates);
            }

            AddPassed(gates, "mapRegistry", "Memory-map Registry", "Memory-map metadata registry loaded.");

            if (!registry.HasValidatedMap || registry.UsableMapsCount == 0)
            {
                AddBlocked(gates, "validatedMap", "Validated Map", Fm26SnapshotStatus.BlockedNoValidatedMap, selection.SupportMessage, "Validate an FM26 memory map before any future live-reading milestone.");
                return Finish(gates);
            }

            AddPassed(gates, "validatedMap", "Validated Map", "At least one validated map metadata file is available.");

            if (selection.SupportStatus == MemoryMapSupportStatus.BuildMismatch.ToString() || !selection.HasSelectedMap)
            {
                AddBlocked(gates, "selectedMap", "Selected Map", Fm26SnapshotStatus.BlockedMapMismatch, selection.SupportMessage, selection.NextActionSafeMessage);
                return Finish(gates);
            }

            AddPassed(gates, "selectedMap", "Selected Map", "A validated map metadata file matches the diagnostic process metadata.");

            AddBlocked(gates, "reader", "Live Reader", Fm26SnapshotStatus.BlockedReaderNotImplemented, "The live reader is not implemented. No player data is read.", "Implement and validate a future safe reader milestone before requesting live fields.");
            return Finish(gates);
        }

        private static bool IsUnavailable(string value)
        {
            var normalized = value ?? string.Empty;
            return normalized.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("denied", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsRegistryMissing(string value)
        {
            return value == MemoryMapSupportStatus.RegistryMissing.ToString()
                || value == MemoryMapSupportStatus.Empty.ToString();
        }

        private static void AddPassed(List<Fm26SnapshotGateResult> gates, string key, string label, string message)
        {
            gates.Add(new Fm26SnapshotGateResult
            {
                GateKey = key,
                Label = label,
                GateStatus = Fm26SnapshotGateStatus.Passed,
                SnapshotStatus = Fm26SnapshotStatus.DiagnosticsOnly,
                SafeMessage = SafeConnectorText.Sanitize(message),
                NextActionSafeMessage = string.Empty
            });
        }

        private static void AddBlocked(List<Fm26SnapshotGateResult> gates, string key, string label, Fm26SnapshotStatus status, string message, string nextAction)
        {
            gates.Add(new Fm26SnapshotGateResult
            {
                GateKey = key,
                Label = label,
                GateStatus = Fm26SnapshotGateStatus.Blocked,
                SnapshotStatus = status,
                SafeMessage = SafeConnectorText.Sanitize(message),
                NextActionSafeMessage = SafeConnectorText.Sanitize(nextAction)
            });
        }

        private static Fm26SnapshotEvaluation Finish(List<Fm26SnapshotGateResult> gates)
        {
            var checkedKeys = new HashSet<string>(gates.Select(gate => gate.GateKey), StringComparer.OrdinalIgnoreCase);
            foreach (var key in GateOrder)
            {
                if (checkedKeys.Contains(key))
                {
                    continue;
                }

                gates.Add(new Fm26SnapshotGateResult
                {
                    GateKey = key,
                    Label = LabelFor(key),
                    GateStatus = Fm26SnapshotGateStatus.NotChecked,
                    SnapshotStatus = Fm26SnapshotStatus.DiagnosticsOnly,
                    SafeMessage = "Gate not checked because an earlier safety gate blocked the snapshot.",
                    NextActionSafeMessage = string.Empty
                });
            }

            var blockingGate = gates.FirstOrDefault(gate => gate.GateStatus == Fm26SnapshotGateStatus.Blocked);
            return new Fm26SnapshotEvaluation
            {
                Gates = gates,
                Status = blockingGate == null ? Fm26SnapshotStatus.MetadataSnapshotCreated : blockingGate.SnapshotStatus,
                BlockingGate = blockingGate == null ? string.Empty : blockingGate.GateKey,
                BlockReasons = blockingGate == null
                    ? Array.Empty<Fm26SnapshotBlockReason>()
                    : new[]
                    {
                        new Fm26SnapshotBlockReason
                        {
                            GateKey = blockingGate.GateKey,
                            Reason = blockingGate.SnapshotStatus.ToString(),
                            SafeMessage = blockingGate.SafeMessage,
                            NextActionSafeMessage = blockingGate.NextActionSafeMessage
                        }
                    },
                NextActionSafeMessage = blockingGate == null ? "No blocking diagnostic gate was found." : blockingGate.NextActionSafeMessage
            };
        }

        private static string LabelFor(string key)
        {
            switch (key)
            {
                case "connector":
                    return "Connector";
                case "platform":
                    return "Platform";
                case "process":
                    return "FM Process";
                case "readOnly":
                    return "Read-only Access";
                case "mapRegistry":
                    return "Memory-map Registry";
                case "validatedMap":
                    return "Validated Map";
                case "selectedMap":
                    return "Selected Map";
                case "reader":
                    return "Live Reader";
                case "fieldPolicy":
                    return "Field Policy";
                default:
                    return key;
            }
        }
    }

    public sealed class Fm26SnapshotEvaluation
    {
        public Fm26SnapshotStatus Status { get; set; } = Fm26SnapshotStatus.DiagnosticsOnly;

        public string BlockingGate { get; set; } = string.Empty;

        public IReadOnlyList<Fm26SnapshotGateResult> Gates { get; set; } = Array.Empty<Fm26SnapshotGateResult>();

        public IReadOnlyList<Fm26SnapshotBlockReason> BlockReasons { get; set; } = Array.Empty<Fm26SnapshotBlockReason>();

        public string NextActionSafeMessage { get; set; } = string.Empty;
    }
}
