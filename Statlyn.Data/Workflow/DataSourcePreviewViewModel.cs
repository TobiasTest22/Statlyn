using System.Collections.Generic;
using System.Globalization;
using Statlyn.DataProviders.Import;

namespace Statlyn.Data.Workflow
{
    public sealed class DataSourcePreviewViewModel
    {
        public DataSourcePreviewViewModel(
            string sourceName,
            string filePath,
            int rowsDetected,
            int mappedCount,
            int unknownCount,
            int forbiddenCount,
            IReadOnlyList<ColumnPreviewViewModel> columnRows,
            string safeMessage,
            IReadOnlyList<string> hardeningMessages,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors)
        {
            SourceName = sourceName ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            RowsDetected = rowsDetected;
            MappedCount = mappedCount;
            UnknownCount = unknownCount;
            ForbiddenCount = forbiddenCount;
            ColumnRows = columnRows ?? new List<ColumnPreviewViewModel>();
            SafeMessage = safeMessage ?? string.Empty;
            HardeningMessages = hardeningMessages ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public string SourceName { get; }

        public string FilePath { get; }

        public int RowsDetected { get; }

        public int MappedCount { get; }

        public int UnknownCount { get; }

        public int ForbiddenCount { get; }

        public IReadOnlyList<ColumnPreviewViewModel> ColumnRows { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<string> HardeningMessages { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public static DataSourcePreviewViewModel From(CsvPreviewResult preview, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            var rows = new List<ColumnPreviewViewModel>();
            foreach (var column in preview.ColumnMappings)
            {
                rows.Add(ColumnPreviewViewModel.From(column));
            }

            var message = preview.Success
                ? "CSV preview completed without storing data."
                : "CSV preview could not be completed.";

            return new DataSourcePreviewViewModel(
                preview.SourceName,
                preview.FilePath,
                preview.RowsDetected,
                preview.MappedCount,
                preview.UnknownCount,
                preview.ForbiddenCount,
                rows,
                message,
                BuildHardeningMessages(preview.RowsDetected, preview.UnknownCount, preview.ForbiddenCount),
                warnings,
                errors);
        }

        public static DataSourcePreviewViewModel Empty(string sourceName, string filePath, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            return new DataSourcePreviewViewModel(
                sourceName,
                filePath,
                0,
                0,
                0,
                0,
                new List<ColumnPreviewViewModel>(),
                "No CSV preview has been run.",
                BuildHardeningMessages(0, 0, 0),
                warnings,
                errors);
        }

        private static IReadOnlyList<string> BuildHardeningMessages(int rowsDetected, int unknownCount, int forbiddenCount)
        {
            return new[]
            {
                "Rows detected: " + rowsDetected.ToString(CultureInfo.InvariantCulture) + ".",
                "Unknown columns detected: " + unknownCount.ToString(CultureInfo.InvariantCulture) + ".",
                "Forbidden columns detected: " + forbiddenCount.ToString(CultureInfo.InvariantCulture) + ".",
                "Unknown columns are not stored unless mapped safely.",
                "Forbidden/hidden-looking fields are blocked.",
                "Missing metrics are not treated as zero.",
                "Re-import replaces current safe snapshot, not duplicate rows."
            };
        }
    }
}
