using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26LiveMemoryProvider : IDataProvider
    {
        private readonly IFm26NativeConnector _connector;
        private Fm26ProcessInfo? _processInfo;

        public Fm26LiveMemoryProvider(IFm26NativeConnector connector)
        {
            _connector = connector;
        }

        public string ProviderName
        {
            get { return "FM26 live memory"; }
        }

        public ProviderType ProviderType
        {
            get { return ProviderType.FM26LiveMemory; }
        }

        public bool IsLive
        {
            get { return true; }
        }

        public bool IsLicensed
        {
            get { return true; }
        }

        public ProviderCapabilities Capabilities
        {
            get
            {
                return new ProviderCapabilities(
                    supportsPlayers: true,
                    supportsTeams: true,
                    supportsMatches: false,
                    supportsPhysicalData: false,
                    supportsEventData: false,
                    supportsScoutingReports: true,
                    supportsPlayerImages: false,
                    supportsNationalityFlags: false);
            }
        }

        public SnapshotResult<bool> Connect()
        {
            var detection = _connector.Detect();
            if (!detection.Success || detection.Value == null)
            {
                return SnapshotResult<bool>.FromFailure(detection.Message, detection.Diagnostics);
            }

            _processInfo = detection.Value;
            var status = _connector.ValidateBuild(_processInfo);
            detection.Diagnostics.Add(
                "fm26.build",
                status,
                status == DiagnosticStatus.Verified ? "FM26 build is supported." : "FM26 detected but this build is not supported yet.",
                "A supported build requires a validated memory-map registry entry.");

            return status == DiagnosticStatus.Verified
                ? SnapshotResult<bool>.FromSuccess(true, detection.Diagnostics)
                : SnapshotResult<bool>.FromFailure("FM26 build is unsupported.", detection.Diagnostics);
        }

        public SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayers()
        {
            if (_processInfo == null)
            {
                var diagnostics = new DiagnosticReport();
                diagnostics.Add("fm26.connection", DiagnosticStatus.Failed, "FM26 is not connected.", "ReadPlayers was called before a verified connection.");
                return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromFailure("FM26 is not connected.", diagnostics);
            }

            var buildStatus = _connector.ValidateBuild(_processInfo);
            if (buildStatus != DiagnosticStatus.Verified)
            {
                var diagnostics = new DiagnosticReport();
                diagnostics.Add("fm26.build", DiagnosticStatus.Unsupported, "FM26 detected but this build is not supported yet.", "No player data is returned because Statlyn has no validated memory map for this build.");
                return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromSuccess(new List<PlayerRawSnapshot>(), diagnostics);
            }

            return _connector.ReadPlayerSnapshot();
        }

        public DiagnosticReport GetDiagnostics()
        {
            var report = new DiagnosticReport();
            report.Add("provider.name", DiagnosticStatus.Verified, ProviderName, "Provider is registered.");
            report.Add("provider.connector", DiagnosticStatus.Verified, _connector.ConnectorVersion, "Native connector facade is available.");
            report.Add("provider.process", _processInfo == null ? DiagnosticStatus.NotChecked : DiagnosticStatus.Verified, _processInfo == null ? "No process has been connected." : "FM26 process metadata is available.", string.Empty);
            return report;
        }

        public void Disconnect()
        {
            _processInfo = null;
        }
    }
}
