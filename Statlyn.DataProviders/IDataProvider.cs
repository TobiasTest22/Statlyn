using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders
{
    public interface IDataProvider
    {
        string ProviderName { get; }

        ProviderType ProviderType { get; }

        bool IsLive { get; }

        bool IsLicensed { get; }

        ProviderCapabilities Capabilities { get; }

        SnapshotResult<bool> Connect();

        SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayers();

        DiagnosticReport GetDiagnostics();

        void Disconnect();
    }
}
