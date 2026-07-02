using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class NullFm26NativeConnector : IFm26NativeConnector
    {
        public string ConnectorVersion
        {
            get { return "managed-null-0.1.0"; }
        }

        public SnapshotResult<Fm26ProcessInfo> Detect()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("fm26.process", DiagnosticStatus.Failed, "Football Manager 26 is not detected.", "The null connector never reports a live FM26 process.");
            return SnapshotResult<Fm26ProcessInfo>.FromFailure("FM26 is not running or cannot be inspected.", diagnostics);
        }

        public DiagnosticStatus ValidateBuild(Fm26ProcessInfo processInfo)
        {
            return DiagnosticStatus.Unsupported;
        }

        public SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayerSnapshot()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("fm26.snapshot.players", DiagnosticStatus.Unsupported, "No supported FM26 memory map is active.", "Statlyn returned an empty snapshot instead of fixture or placeholder players.");
            return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromFailure("Unsupported FM26 build.", diagnostics);
        }
    }
}
