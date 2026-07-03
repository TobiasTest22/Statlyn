using System.Collections.Generic;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders.Import;

namespace Statlyn.Data.Workflow
{
    public sealed class ImportResultViewModel
    {
        public ImportResultViewModel(
            bool success,
            int rowsRead,
            int rowsAccepted,
            int rowsRejected,
            int fieldsStored,
            int playerStatsStored,
            int physicalMetricsStored,
            int blockedFields,
            int unknownFields,
            int databasePlayersCount,
            int databaseStatsCount,
            string safeMessage,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors)
        {
            Success = success;
            RowsRead = rowsRead;
            RowsAccepted = rowsAccepted;
            RowsRejected = rowsRejected;
            FieldsStored = fieldsStored;
            PlayerStatsStored = playerStatsStored;
            PhysicalMetricsStored = physicalMetricsStored;
            BlockedFields = blockedFields;
            UnknownFields = unknownFields;
            DatabasePlayersCount = databasePlayersCount;
            DatabaseStatsCount = databaseStatsCount;
            SafeMessage = safeMessage ?? string.Empty;
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public bool Success { get; }

        public int RowsRead { get; }

        public int RowsAccepted { get; }

        public int RowsRejected { get; }

        public int FieldsStored { get; }

        public int PlayerStatsStored { get; }

        public int PhysicalMetricsStored { get; }

        public int BlockedFields { get; }

        public int UnknownFields { get; }

        public int DatabasePlayersCount { get; }

        public int DatabaseStatsCount { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public static ImportResultViewModel From(ImportPipelineResult result, CsvPreviewResult preview, StatlynDatabaseDiagnostics diagnostics, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            var success = result.RowsAccepted > 0 && result.RowsRejected == 0 && errors.Count == 0;
            return new ImportResultViewModel(
                success,
                result.RowsRead,
                result.RowsAccepted,
                result.RowsRejected,
                result.FieldsStored,
                result.PlayerStatsStored,
                result.PhysicalMetricsStored,
                result.BlockedFields,
                result.UnknownFields == 0 ? preview.UnknownCount : result.UnknownFields,
                diagnostics.PlayersCount,
                diagnostics.PlayerStatsCount,
                success ? "Safe CSV import completed. Only masked fields were persisted." : "Safe CSV import completed with warnings or rejected rows.",
                warnings,
                errors);
        }
    }
}
