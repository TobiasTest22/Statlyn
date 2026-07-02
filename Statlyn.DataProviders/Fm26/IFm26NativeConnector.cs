using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public interface IFm26NativeConnector
    {
        string ConnectorVersion { get; }

        SnapshotResult<Fm26ProcessInfo> Detect();

        DiagnosticStatus ValidateBuild(Fm26ProcessInfo processInfo);

        SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayerSnapshot();
    }
}
