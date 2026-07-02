using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.External
{
    public sealed class ExternalFootballDataProviderTemplate : IDataProvider
    {
        public string ProviderName
        {
            get { return "External football data provider template"; }
        }

        public ProviderType ProviderType
        {
            get { return ProviderType.FutureExternalProvider; }
        }

        public bool IsLive
        {
            get { return false; }
        }

        public bool IsLicensed
        {
            get { return false; }
        }

        public ProviderCapabilities Capabilities
        {
            get { return new ProviderCapabilities(true, true, true, false, false, true, false, false); }
        }

        public SnapshotResult<bool> Connect()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("external.template", DiagnosticStatus.Unsupported, "Template provider only.", "Future providers must validate licensed, exported, user-provided or otherwise permitted data access.");
            return SnapshotResult<bool>.FromFailure("Template provider is not a live source.", diagnostics);
        }

        public DiagnosticReport ValidateAccess()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("external.template.access", DiagnosticStatus.Unsupported, "No external access configured.", "This template must not scrape FotMob or any other unlicensed source.");
            return diagnostics;
        }

        public ProviderReadResult<SourceMetadata> ReadSourceMetadata()
        {
            var diagnostics = ValidateAccess();
            var metadata = new SourceMetadata(ProviderName, ProviderType, false, false, "not configured", "template only", false, false, System.DateTimeOffset.UtcNow, 0);
            return ProviderReadResult<SourceMetadata>.FromSuccess(metadata, diagnostics);
        }

        public SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayers()
        {
            var diagnostics = ValidateAccess();
            return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromSuccess(new List<PlayerRawSnapshot>(), diagnostics);
        }

        public ProviderReadResult<IReadOnlyList<TeamSnapshot>> ReadTeams() { return Empty<TeamSnapshot>(); }

        public ProviderReadResult<IReadOnlyList<MatchSnapshot>> ReadMatches() { return Empty<MatchSnapshot>(); }

        public ProviderReadResult<IReadOnlyList<PlayerStatSnapshot>> ReadPlayerStats() { return Empty<PlayerStatSnapshot>(); }

        public ProviderReadResult<IReadOnlyList<ScoutingReportSnapshot>> ReadScoutReports() { return Empty<ScoutingReportSnapshot>(); }

        public ProviderReadResult<IReadOnlyList<PlayerImageReference>> ReadPlayerImages() { return Empty<PlayerImageReference>(); }

        public ProviderReadResult<IReadOnlyList<NationalityFlagReference>> ReadNationalityFlags() { return Empty<NationalityFlagReference>(); }

        public DataCompletenessReport GetDataCompleteness()
        {
            return new DataCompletenessReport(0, 1, new[] { "configured permitted external source" });
        }

        public DiagnosticReport GetDiagnostics()
        {
            return ValidateAccess();
        }

        public void Disconnect()
        {
        }

        private static ProviderReadResult<IReadOnlyList<T>> Empty<T>()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("external.template.read", DiagnosticStatus.Unsupported, "Template provider returned no data.", "No network calls or scraping are implemented.");
            return ProviderReadResult<IReadOnlyList<T>>.FromSuccess(new List<T>(), diagnostics);
        }
    }
}
