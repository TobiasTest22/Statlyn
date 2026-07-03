using System;
using System.Collections.Generic;
using System.Globalization;

namespace Statlyn.Data.Persistence
{
    public sealed class ImportAuditRepository : SqliteRepository
    {
        public ImportAuditRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public long Save(ImportAuditRecord audit)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                return Save(audit, connection, null);
            }
        }

        public long Save(ImportAuditRecord audit, Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction? transaction)
        {
            if (audit == null)
            {
                throw new ArgumentNullException(nameof(audit));
            }

            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText =
                    @"INSERT INTO ImportAudit (
                        SourceName, ProviderType, ImportedAtUtc, RowsRead, RowsAccepted, RowsRejected,
                        FieldsStored, PlayerStatsStored, PhysicalMetricsStored, BlockedFields, UnknownFields, Diagnostics)
                      VALUES (
                        $sourceName, $providerType, $importedAtUtc, $rowsRead, $rowsAccepted, $rowsRejected,
                        $fieldsStored, $playerStatsStored, $physicalMetricsStored, $blockedFields, $unknownFields, $diagnostics);";
                Add(command, "$sourceName", audit.SourceName);
                Add(command, "$providerType", audit.ProviderType);
                Add(command, "$importedAtUtc", audit.ImportedAtUtc.ToString("O"));
                Add(command, "$rowsRead", audit.RowsRead);
                Add(command, "$rowsAccepted", audit.RowsAccepted);
                Add(command, "$rowsRejected", audit.RowsRejected);
                Add(command, "$fieldsStored", audit.FieldsStored);
                Add(command, "$playerStatsStored", audit.PlayerStatsStored);
                Add(command, "$physicalMetricsStored", audit.PhysicalMetricsStored);
                Add(command, "$blockedFields", audit.BlockedFields);
                Add(command, "$unknownFields", audit.UnknownFields);
                Add(command, "$diagnostics", DiagnosticSanitizer.Sanitize(audit.Diagnostics));
                command.ExecuteNonQuery();
                return LastInsertRowId(connection);
            }
        }

        public ImportAuditRecord? LoadLatest()
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT SourceName, ProviderType, ImportedAtUtc, RowsRead, RowsAccepted, RowsRejected,
                             FieldsStored, PlayerStatsStored, PhysicalMetricsStored, BlockedFields, UnknownFields, Diagnostics
                      FROM ImportAudit
                      ORDER BY ImportedAtUtc DESC, Id DESC
                      LIMIT 1;";
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var imported = DateTimeOffset.TryParse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                        ? parsed
                        : DateTimeOffset.UtcNow;

                    return new ImportAuditRecord(
                        reader.GetString(0),
                        reader.GetString(1),
                        imported,
                        reader.GetInt32(3),
                        reader.GetInt32(4),
                        reader.GetInt32(5),
                        reader.GetInt32(6),
                        reader.GetInt32(7),
                        reader.GetInt32(8),
                        reader.GetInt32(9),
                        reader.GetInt32(10),
                        reader.GetString(11));
                }
            }
        }

        public IReadOnlyList<ImportAuditRecord> LoadAll()
        {
            var audits = new List<ImportAuditRecord>();
            var latest = LoadLatest();
            if (latest != null)
            {
                audits.Add(latest);
            }

            return audits;
        }
    }
}
