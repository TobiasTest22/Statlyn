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

        DiagnosticReport ValidateAccess();

        ProviderReadResult<SourceMetadata> ReadSourceMetadata();

        SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayers();

        ProviderReadResult<IReadOnlyList<TeamSnapshot>> ReadTeams();

        ProviderReadResult<IReadOnlyList<MatchSnapshot>> ReadMatches();

        ProviderReadResult<IReadOnlyList<PlayerStatSnapshot>> ReadPlayerStats();

        ProviderReadResult<IReadOnlyList<ScoutingReportSnapshot>> ReadScoutReports();

        ProviderReadResult<IReadOnlyList<PlayerImageReference>> ReadPlayerImages();

        ProviderReadResult<IReadOnlyList<NationalityFlagReference>> ReadNationalityFlags();

        DataCompletenessReport GetDataCompleteness();

        DiagnosticReport GetDiagnostics();

        void Disconnect();
    }
}
