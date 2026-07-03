using System;

namespace Statlyn.Data.Persistence
{
    public sealed class ImportAuditRecord
    {
        public ImportAuditRecord(
            string sourceName,
            string providerType,
            DateTimeOffset importedAtUtc,
            int rowsRead,
            int rowsAccepted,
            int rowsRejected,
            int fieldsStored,
            int playerStatsStored,
            int physicalMetricsStored,
            int blockedFields,
            int unknownFields,
            string diagnostics)
        {
            SourceName = sourceName ?? string.Empty;
            ProviderType = providerType ?? string.Empty;
            ImportedAtUtc = importedAtUtc;
            RowsRead = rowsRead;
            RowsAccepted = rowsAccepted;
            RowsRejected = rowsRejected;
            FieldsStored = fieldsStored;
            PlayerStatsStored = playerStatsStored;
            PhysicalMetricsStored = physicalMetricsStored;
            BlockedFields = blockedFields;
            UnknownFields = unknownFields;
            Diagnostics = diagnostics ?? string.Empty;
        }

        public string SourceName { get; }

        public string ProviderType { get; }

        public DateTimeOffset ImportedAtUtc { get; }

        public int RowsRead { get; }

        public int RowsAccepted { get; }

        public int RowsRejected { get; }

        public int FieldsStored { get; }

        public int PlayerStatsStored { get; }

        public int PhysicalMetricsStored { get; }

        public int BlockedFields { get; }

        public int UnknownFields { get; }

        public string Diagnostics { get; }
    }
}
