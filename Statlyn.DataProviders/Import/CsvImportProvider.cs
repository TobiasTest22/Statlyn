using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Import
{
    public sealed class CsvImportProvider : IDataProvider
    {
        private readonly string _filePath;
        private readonly SourceMetadata _metadata;
        private readonly FieldMappingSet _mappingSet;
        private readonly FieldPolicyRegistry _registry = new FieldPolicyRegistry();

        public CsvImportProvider(string filePath, SourceMetadata metadata, FieldMappingSet mappingSet)
        {
            _filePath = filePath ?? string.Empty;
            _metadata = metadata;
            _mappingSet = mappingSet ?? new FieldMappingSet(Array.Empty<FieldMapping>());
        }

        public string ProviderName
        {
            get { return "CSV import"; }
        }

        public ProviderType ProviderType
        {
            get { return ProviderType.Csv; }
        }

        public bool IsLive
        {
            get { return false; }
        }

        public bool IsLicensed
        {
            get { return _metadata.IsLicensed; }
        }

        public ProviderCapabilities Capabilities
        {
            get { return new ProviderCapabilities(true, false, false, false, false, false, _metadata.PermitsImages, _metadata.PermitsFlags); }
        }

        public SnapshotResult<bool> Connect()
        {
            var diagnostics = ValidateAccess();
            return diagnostics.OverallStatus == DiagnosticStatus.Verified
                ? SnapshotResult<bool>.FromSuccess(true, diagnostics)
                : SnapshotResult<bool>.FromFailure("CSV import file is not readable.", diagnostics);
        }

        public DiagnosticReport ValidateAccess()
        {
            var diagnostics = new DiagnosticReport();
            if (File.Exists(_filePath))
            {
                diagnostics.Add("csv.file", DiagnosticStatus.Verified, "CSV file is readable.", _filePath);
            }
            else
            {
                diagnostics.Add("csv.file", DiagnosticStatus.Failed, "CSV file was not found.", _filePath);
            }

            diagnostics.Add(
                "csv.licence",
                _metadata.IsLicensed ? DiagnosticStatus.Verified : DiagnosticStatus.Partial,
                _metadata.IsLicensed ? "Source metadata says this import is permitted." : "Source metadata is unlicensed or incomplete.",
                _metadata.AllowedUsage);

            return diagnostics;
        }

        public ProviderReadResult<SourceMetadata> ReadSourceMetadata()
        {
            return ProviderReadResult<SourceMetadata>.FromSuccess(_metadata, ValidateAccess());
        }

        public SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayers()
        {
            var diagnostics = ValidateAccess();
            if (diagnostics.OverallStatus == DiagnosticStatus.Failed)
            {
                return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromFailure("CSV import file is not readable.", diagnostics);
            }

            var lines = ReadDataLines(_filePath);
            if (lines.Length == 0)
            {
                diagnostics.Add("csv.rows", DiagnosticStatus.Partial, "CSV file has no rows.", "No players imported.");
                return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromSuccess(new List<PlayerRawSnapshot>(), diagnostics);
            }

            var headers = SplitCsvLine(lines[0]);
            var players = new List<PlayerRawSnapshot>();
            var fieldsMapped = 0;
            var unknownFields = 0;
            var forbiddenFields = 0;

            for (var index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                var values = SplitCsvLine(lines[index]);
                var sourcePlayerId = GetValue(headers, values, "SourcePlayerId");
                if (string.IsNullOrWhiteSpace(sourcePlayerId))
                {
                    sourcePlayerId = "csv-row-" + index.ToString(CultureInfo.InvariantCulture);
                }

                var player = new PlayerRawSnapshot(sourcePlayerId, ProviderName, ProviderType)
                {
                    SourceContext = _metadata.ToSourceContext(ProviderName),
                    ScoutContext = new ScoutContext(false, 0, false),
                    DisplayName = GetValue(headers, values, "DisplayName")
                };

                for (var column = 0; column < headers.Count && column < values.Count; column++)
                {
                    var mapping = _mappingSet.Resolve(headers[column], _registry);
                    var value = Coerce(values[column], mapping.ValueKind);
                    if (mapping.FieldKey == PlayerFieldKey.Unknown)
                    {
                        unknownFields++;
                    }

                    if (_registry.IsForbiddenRawName(headers[column]))
                    {
                        forbiddenFields++;
                    }

                    fieldsMapped++;
                    player.AddField(new RawFieldValue(
                        mapping.FieldKey,
                        mapping.FieldName,
                        headers[column],
                        value,
                        mapping.ValueKind,
                        _metadata.SourceConfidence,
                        isKnown: !string.IsNullOrWhiteSpace(values[column])));
                }

                players.Add(player);
            }

            diagnostics.Add("csv.rows", DiagnosticStatus.Verified, "CSV rows were read.", (lines.Length - 1) + " data row(s).");
            diagnostics.Add("csv.players", DiagnosticStatus.Verified, "CSV players were read.", players.Count + " player rows.");
            diagnostics.Add("csv.fields.mapped", DiagnosticStatus.Verified, "CSV fields were mapped.", fieldsMapped + " field instance(s).");
            diagnostics.Add("csv.fields.blocked", forbiddenFields > 0 || unknownFields > 0 ? DiagnosticStatus.Partial : DiagnosticStatus.Verified, "CSV blocked-field candidates were counted.", "Forbidden fields: " + forbiddenFields + "; unknown fields: " + unknownFields + ".");
            diagnostics.Add("csv.images", _metadata.PermitsPlayerImages ? DiagnosticStatus.Verified : DiagnosticStatus.Partial, _metadata.PermitsPlayerImages ? "Player image display is permitted by source metadata." : "Player image display is not permitted by source metadata.", "No image bytes or URLs are imported by this skeleton.");
            diagnostics.Add("csv.flags", _metadata.PermitsProviderFlags || _metadata.UsesBundledSafeFlagAssets ? DiagnosticStatus.Verified : DiagnosticStatus.Partial, _metadata.PermitsProviderFlags ? "Provider flags are permitted." : _metadata.UsesBundledSafeFlagAssets ? "Bundled safe flags are enabled." : "Flags are not permitted.", "No unlicensed provider flags are assumed.");
            diagnostics.Add("csv.completeness", DiagnosticStatus.Verified, "CSV data completeness calculated.", GetDataCompleteness().CompletenessPercentage + "%.");
            return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromSuccess(players, diagnostics);
        }

        public ProviderReadResult<IReadOnlyList<TeamSnapshot>> ReadTeams() { return Empty<TeamSnapshot>("csv.teams", "CSV team import is not implemented in this skeleton."); }

        public ProviderReadResult<IReadOnlyList<MatchSnapshot>> ReadMatches() { return Empty<MatchSnapshot>("csv.matches", "CSV match import is not implemented in this skeleton."); }

        public ProviderReadResult<IReadOnlyList<PlayerStatSnapshot>> ReadPlayerStats() { return Empty<PlayerStatSnapshot>("csv.stats", "CSV player-stat import requires explicit field mappings."); }

        public ProviderReadResult<IReadOnlyList<ScoutingReportSnapshot>> ReadScoutReports() { return Empty<ScoutingReportSnapshot>("csv.scoutReports", "CSV scout-report import requires explicit field mappings."); }

        public ProviderReadResult<IReadOnlyList<PlayerImageReference>> ReadPlayerImages() { return Empty<PlayerImageReference>("csv.images", "CSV image references require permitted source metadata."); }

        public ProviderReadResult<IReadOnlyList<NationalityFlagReference>> ReadNationalityFlags() { return Empty<NationalityFlagReference>("csv.flags", "CSV flag references require permitted source metadata or bundled safe assets."); }

        public DataCompletenessReport GetDataCompleteness()
        {
            if (!File.Exists(_filePath))
            {
                return new DataCompletenessReport(0, 1, new[] { "CSV file" });
            }

            var lines = ReadDataLines(_filePath);
            if (lines.Length <= 1)
            {
                return new DataCompletenessReport(0, 1, new[] { "player rows" });
            }

            var headers = SplitCsvLine(lines[0]);
            var knownHeaders = 0;
            var missing = new List<string>();
            foreach (var header in headers)
            {
                var mapping = _mappingSet.Resolve(header, _registry);
                if (mapping.FieldKey == PlayerFieldKey.Unknown)
                {
                    missing.Add(header);
                }
                else
                {
                    knownHeaders++;
                }
            }

            return new DataCompletenessReport(knownHeaders, headers.Count, missing);
        }

        public DiagnosticReport GetDiagnostics()
        {
            return ValidateAccess();
        }

        public void Disconnect()
        {
        }

        private static ProviderReadResult<IReadOnlyList<T>> Empty<T>(string key, string message)
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add(key, DiagnosticStatus.Unsupported, message, "No placeholder data is returned.");
            return ProviderReadResult<IReadOnlyList<T>>.FromSuccess(new List<T>(), diagnostics);
        }

        private static List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var current = new List<char>();
            var inQuotes = false;

            foreach (var character in line)
            {
                if (character == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (character == ',' && !inQuotes)
                {
                    values.Add(new string(current.ToArray()).Trim());
                    current.Clear();
                }
                else
                {
                    current.Add(character);
                }
            }

            values.Add(new string(current.ToArray()).Trim());
            return values;
        }

        private static string[] ReadDataLines(string filePath)
        {
            var lines = new List<string>();
            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                lines.Add(line);
            }

            return lines.ToArray();
        }

        private static string GetValue(IReadOnlyList<string> headers, IReadOnlyList<string> values, string header)
        {
            for (var index = 0; index < headers.Count && index < values.Count; index++)
            {
                if (string.Equals(headers[index], header, StringComparison.OrdinalIgnoreCase))
                {
                    return values[index];
                }
            }

            return string.Empty;
        }

        private static object? Coerce(string value, FieldValueKind valueKind)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (valueKind == FieldValueKind.Number && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numeric))
            {
                return numeric;
            }

            return value;
        }
    }
}
