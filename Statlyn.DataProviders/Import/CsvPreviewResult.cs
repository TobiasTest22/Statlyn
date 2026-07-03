using System.Collections.Generic;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Import
{
    public sealed class CsvPreviewResult
    {
        public CsvPreviewResult(
            bool success,
            bool fileReadable,
            string filePath,
            string sourceName,
            int rowsDetected,
            IReadOnlyList<string> columns,
            IReadOnlyList<ColumnMappingPreview> columnMappings,
            int mappedCount,
            int unknownCount,
            int forbiddenCount,
            IReadOnlyList<string> forbiddenFields,
            IReadOnlyList<string> unknownFields,
            IReadOnlyList<string> safeFields,
            DiagnosticReport diagnostics)
        {
            Success = success;
            FileReadable = fileReadable;
            FilePath = filePath ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            RowsDetected = rowsDetected;
            Columns = columns ?? new List<string>();
            ColumnMappings = columnMappings ?? new List<ColumnMappingPreview>();
            MappedCount = mappedCount;
            UnknownCount = unknownCount;
            ForbiddenCount = forbiddenCount;
            ForbiddenFields = forbiddenFields ?? new List<string>();
            UnknownFields = unknownFields ?? new List<string>();
            SafeFields = safeFields ?? new List<string>();
            Diagnostics = diagnostics;
        }

        public bool Success { get; }

        public bool FileReadable { get; }

        public string FilePath { get; }

        public string SourceName { get; }

        public int RowsDetected { get; }

        public IReadOnlyList<string> Columns { get; }

        public IReadOnlyList<ColumnMappingPreview> ColumnMappings { get; }

        public int MappedCount { get; }

        public int UnknownCount { get; }

        public int ForbiddenCount { get; }

        public IReadOnlyList<string> ForbiddenFields { get; }

        public IReadOnlyList<string> UnknownFields { get; }

        public IReadOnlyList<string> SafeFields { get; }

        public DiagnosticReport Diagnostics { get; }
    }
}
