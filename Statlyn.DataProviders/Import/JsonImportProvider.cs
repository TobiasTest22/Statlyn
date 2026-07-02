using System.Collections.Generic;
using System.IO;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Import
{
    public sealed class JsonImportProvider : IDataProvider
    {
        private readonly string _filePath;
        private readonly SourceMetadata _metadata;

        public JsonImportProvider(string filePath, SourceMetadata metadata)
        {
            _filePath = filePath ?? string.Empty;
            _metadata = metadata;
        }

        public string ProviderName { get { return "JSON import"; } }

        public ProviderType ProviderType { get { return ProviderType.Json; } }

        public bool IsLive { get { return false; } }

        public bool IsLicensed { get { return _metadata.IsLicensed; } }

        public ProviderCapabilities Capabilities
        {
            get { return new ProviderCapabilities(true, true, true, false, false, true, _metadata.PermitsImages, _metadata.PermitsFlags); }
        }

        public SnapshotResult<bool> Connect()
        {
            var diagnostics = ValidateAccess();
            return diagnostics.OverallStatus == DiagnosticStatus.Verified
                ? SnapshotResult<bool>.FromSuccess(true, diagnostics)
                : SnapshotResult<bool>.FromFailure("JSON import file is not readable.", diagnostics);
        }

        public DiagnosticReport ValidateAccess()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add(
                "json.file",
                File.Exists(_filePath) ? DiagnosticStatus.Verified : DiagnosticStatus.Failed,
                File.Exists(_filePath) ? "JSON file is readable." : "JSON file was not found.",
                _filePath);
            diagnostics.Add("json.network", DiagnosticStatus.Verified, "No network access is used.", "JSON imports are local-file only.");
            return diagnostics;
        }

        public ProviderReadResult<SourceMetadata> ReadSourceMetadata()
        {
            return ProviderReadResult<SourceMetadata>.FromSuccess(_metadata, ValidateAccess());
        }

        public SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayers()
        {
            var diagnostics = ValidateAccess();
            diagnostics.Add("json.players", DiagnosticStatus.Unsupported, "JSON player mapping is a skeleton only.", "Add explicit mapping before importing JSON player rows.");
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
            return new DataCompletenessReport(0, 1, new[] { "explicit JSON field mapping" });
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
            diagnostics.Add("json.read", DiagnosticStatus.Unsupported, "JSON import skeleton returned no data.", "No placeholder data is returned.");
            return ProviderReadResult<IReadOnlyList<T>>.FromSuccess(new List<T>(), diagnostics);
        }
    }
}
