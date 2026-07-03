using System.Collections.Generic;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders.Import;

namespace Statlyn.Data.Workflow
{
    public sealed class DataSourceImportWorkflowResult
    {
        public DataSourceImportWorkflowResult(
            CsvPreviewResult? preview,
            ImportPipelineResult? importResult,
            StatlynDatabaseDiagnostics? databaseDiagnostics,
            DataSourcePreviewViewModel previewViewModel,
            ImportResultViewModel? importResultViewModel,
            bool success,
            string safeMessage,
            IReadOnlyList<string> warningMessages,
            IReadOnlyList<string> errorMessages)
        {
            Preview = preview;
            ImportResult = importResult;
            DatabaseDiagnostics = databaseDiagnostics;
            PreviewViewModel = previewViewModel;
            ImportResultViewModel = importResultViewModel;
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            WarningMessages = warningMessages ?? new List<string>();
            ErrorMessages = errorMessages ?? new List<string>();
        }

        public CsvPreviewResult? Preview { get; }

        public ImportPipelineResult? ImportResult { get; }

        public StatlynDatabaseDiagnostics? DatabaseDiagnostics { get; }

        public DataSourcePreviewViewModel PreviewViewModel { get; }

        public ImportResultViewModel? ImportResultViewModel { get; }

        public bool Success { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<string> WarningMessages { get; }

        public IReadOnlyList<string> ErrorMessages { get; }
    }
}
