using Statlyn.Core.Diagnostics;

namespace Statlyn.Data.Persistence
{
    public sealed class ImportPipelineResult
    {
        public ImportPipelineResult(
            int rowsRead,
            int rowsAccepted,
            int rowsRejected,
            int fieldsStored,
            int playerStatsStored,
            int physicalMetricsStored,
            int blockedFields,
            int unknownFields,
            DiagnosticReport diagnostics)
        {
            RowsRead = rowsRead;
            RowsAccepted = rowsAccepted;
            RowsRejected = rowsRejected;
            FieldsStored = fieldsStored;
            PlayerStatsStored = playerStatsStored;
            PhysicalMetricsStored = physicalMetricsStored;
            BlockedFields = blockedFields;
            UnknownFields = unknownFields;
            Diagnostics = diagnostics;
        }

        public int RowsRead { get; }

        public int RowsAccepted { get; }

        public int RowsRejected { get; }

        public int FieldsStored { get; }

        public int PlayerStatsStored { get; }

        public int PhysicalMetricsStored { get; }

        public int BlockedFields { get; }

        public int UnknownFields { get; }

        public DiagnosticReport Diagnostics { get; }
    }
}
