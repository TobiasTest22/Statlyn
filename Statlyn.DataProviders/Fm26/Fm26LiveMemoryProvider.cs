using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26LiveMemoryProvider : IDataProvider
    {
        private readonly IFm26NativeConnector _connector;
        private Fm26ProcessDiagnostic? _processInfo;

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
            var process = _connector.DetectFmProcess();
            var diagnostics = new DiagnosticReport();
            diagnostics.Add(
                "fm26.process",
                process.IsDetected ? DiagnosticStatus.Verified : DiagnosticStatus.Failed,
                process.SafeMessage,
                "Safe process diagnostics contain no player data.");

            if (!process.IsDetected)
            {
                return SnapshotResult<bool>.FromFailure("FM26 is not running or cannot be inspected.", diagnostics);
            }

            _processInfo = process;
            var status = _connector.ValidateBuild(_processInfo);
            diagnostics.Add(
                "fm26.build",
                status,
                status == DiagnosticStatus.Verified ? "FM26 build is supported." : "FM26 detected but this build is not supported yet.",
                "A supported build requires a validated map registry entry.");

            return status == DiagnosticStatus.Verified
                ? SnapshotResult<bool>.FromSuccess(true, diagnostics)
                : SnapshotResult<bool>.FromFailure("FM26 build is unsupported.", diagnostics);
        }

        public DiagnosticReport ValidateAccess()
        {
            var report = new DiagnosticReport();
            if (_processInfo == null)
            {
                report.Add("fm26.access", DiagnosticStatus.NotChecked, "FM26 access has not been checked.", "Connect before reading FM26 snapshots.");
                return report;
            }

            report.Add(
                "fm26.access",
                _processInfo.HasReadOnlyAccess ? DiagnosticStatus.Verified : DiagnosticStatus.Failed,
                _processInfo.HasReadOnlyAccess ? "Read-only access is available." : "Read-only access is unavailable.",
                "Connector uses query/read permissions only.");
            return report;
        }

        public ProviderReadResult<SourceMetadata> ReadSourceMetadata()
        {
            var report = GetDiagnostics();
            var metadata = new SourceMetadata(
                ProviderName,
                ProviderType,
                isLive: true,
                isLicensed: true,
                licenceStatus: "local user process, no external data licence",
                allowedUsage: "read-only local FM26 observation",
                permitsImages: false,
                permitsFlags: false,
                importedAtUtc: System.DateTimeOffset.UtcNow,
                sourceConfidence: 100);
            return ProviderReadResult<SourceMetadata>.FromSuccess(metadata, report);
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
                diagnostics.Add("fm26.build", DiagnosticStatus.Unsupported, "FM26 detected but this build is not supported yet.", "No player data is returned because Statlyn has no validated map for this build.");
                return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromSuccess(new List<PlayerRawSnapshot>(), diagnostics);
            }

            var report = new DiagnosticReport();
            report.Add("fm26.snapshot.players", DiagnosticStatus.Unsupported, "FM26 player reading is not implemented in this milestone.", "Connector diagnostics are available, but no player memory is read.");
            return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromSuccess(new List<PlayerRawSnapshot>(), report);
        }

        public ProviderReadResult<IReadOnlyList<TeamSnapshot>> ReadTeams()
        {
            return EmptyProviderResult<TeamSnapshot>("fm26.teams", "FM26 team snapshots are not mapped yet.");
        }

        public ProviderReadResult<IReadOnlyList<MatchSnapshot>> ReadMatches()
        {
            return EmptyProviderResult<MatchSnapshot>("fm26.matches", "FM26 match snapshots are not mapped yet.");
        }

        public ProviderReadResult<IReadOnlyList<PlayerStatSnapshot>> ReadPlayerStats()
        {
            return EmptyProviderResult<PlayerStatSnapshot>("fm26.playerStats", "FM26 player stat snapshots are not mapped yet.");
        }

        public ProviderReadResult<IReadOnlyList<ScoutingReportSnapshot>> ReadScoutReports()
        {
            return EmptyProviderResult<ScoutingReportSnapshot>("fm26.scoutReports", "FM26 scout report snapshots are not mapped yet.");
        }

        public ProviderReadResult<IReadOnlyList<PlayerImageReference>> ReadPlayerImages()
        {
            return EmptyProviderResult<PlayerImageReference>("fm26.playerImages", "FM26 player images are not exposed by the first connector skeleton.");
        }

        public ProviderReadResult<IReadOnlyList<NationalityFlagReference>> ReadNationalityFlags()
        {
            return EmptyProviderResult<NationalityFlagReference>("fm26.flags", "FM26 flag assets are not exposed by the first connector skeleton.");
        }

        public DataCompletenessReport GetDataCompleteness()
        {
            return new DataCompletenessReport(0, 1, new[] { "validated FM26 memory map" });
        }

        public DiagnosticReport GetDiagnostics()
        {
            var report = new DiagnosticReport();
            var connectorDiagnostic = _connector.GetDiagnostic();
            report.Add("provider.name", DiagnosticStatus.Verified, ProviderName, "Provider is registered.");
            report.Add("provider.connector", connectorDiagnostic.IsNativeConnectorAvailable ? DiagnosticStatus.Verified : DiagnosticStatus.Unsupported, connectorDiagnostic.ConnectorVersion, connectorDiagnostic.SafeMessage);
            report.Add("provider.process", _processInfo == null ? DiagnosticStatus.NotChecked : DiagnosticStatus.Verified, _processInfo == null ? "No process has been connected." : "FM26 process metadata is available.", string.Empty);
            return report;
        }

        public void Disconnect()
        {
            _processInfo = null;
        }

        private static ProviderReadResult<IReadOnlyList<T>> EmptyProviderResult<T>(string key, string message)
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add(key, DiagnosticStatus.Unsupported, message, "No fake provider data is returned.");
            return ProviderReadResult<IReadOnlyList<T>>.FromSuccess(new List<T>(), diagnostics);
        }
    }
}
