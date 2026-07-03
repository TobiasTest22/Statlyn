using System;
using System.Collections.Generic;
using System.IO;
using Statlyn.Core;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders.Import;

namespace Statlyn.Data.Workflow
{
    public sealed class DataSourceImportWorkflowService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly FieldMappingSet _mappingSet;
        private readonly CsvPreviewService _previewService;

        public DataSourceImportWorkflowService(StatlynDbConnectionFactory connectionFactory)
            : this(connectionFactory, new FieldMappingSet(Array.Empty<FieldMapping>()))
        {
        }

        public DataSourceImportWorkflowService(StatlynDbConnectionFactory connectionFactory, FieldMappingSet mappingSet)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _mappingSet = mappingSet ?? new FieldMappingSet(Array.Empty<FieldMapping>());
            _previewService = new CsvPreviewService();
            new StatlynDatabaseInitializer(_connectionFactory).Initialize();
        }

        public DataSourceImportWorkflowResult Preview(DataSourceImportRequest request)
        {
            request = request ?? new DataSourceImportRequest();
            var warnings = BuildWarnings(request);
            var errors = ValidateRequest(request, requireReadableFile: false);
            var metadata = BuildMetadata(request);
            CsvPreviewResult? preview = null;

            if (errors.Count == 0)
            {
                preview = _previewService.Preview(request.CsvPath, _mappingSet, metadata);
                if (!preview.Success)
                {
                    errors.Add("CSV preview could not read the selected local file.");
                }
            }

            var databaseDiagnostics = ReadDatabaseDiagnostics();
            var previewViewModel = preview == null
                ? DataSourcePreviewViewModel.Empty(metadata.SourceName, request.CsvPath, warnings, errors)
                : DataSourcePreviewViewModel.From(preview, warnings, errors);

            return new DataSourceImportWorkflowResult(
                preview,
                null,
                databaseDiagnostics,
                previewViewModel,
                null,
                errors.Count == 0 && preview != null && preview.Success,
                errors.Count == 0 ? "CSV preview is ready. No data was stored." : "CSV preview needs attention.",
                warnings,
                errors);
        }

        public DataSourceImportWorkflowResult Import(DataSourceImportRequest request)
        {
            request = request ?? new DataSourceImportRequest();
            var warnings = BuildWarnings(request);
            var errors = ValidateRequest(request, requireReadableFile: true);
            var metadata = BuildMetadata(request);
            CsvPreviewResult? preview = null;

            if (errors.Count == 0)
            {
                preview = _previewService.Preview(request.CsvPath, _mappingSet, metadata);
                if (!preview.Success)
                {
                    errors.Add("CSV preview failed, so import was not started.");
                }
            }

            if (errors.Count > 0 || preview == null)
            {
                var diagnostics = ReadDatabaseDiagnostics();
                var previewViewModel = preview == null
                    ? DataSourcePreviewViewModel.Empty(metadata.SourceName, request.CsvPath, warnings, errors)
                    : DataSourcePreviewViewModel.From(preview, warnings, errors);

                return new DataSourceImportWorkflowResult(
                    preview,
                    null,
                    diagnostics,
                    previewViewModel,
                    null,
                    false,
                    "Safe import was not started.",
                    warnings,
                    errors);
            }

            var provider = new CsvImportProvider(request.CsvPath, metadata, _mappingSet);
            var importResult = new ImportPipelineService(_connectionFactory).Import(provider, ImportPipelineOptions.CreateDefault());
            var databaseDiagnosticsAfterImport = ReadDatabaseDiagnostics();
            var previewModel = DataSourcePreviewViewModel.From(preview, warnings, errors);
            var importModel = ImportResultViewModel.From(importResult, preview, databaseDiagnosticsAfterImport, warnings, errors);

            return new DataSourceImportWorkflowResult(
                preview,
                importResult,
                databaseDiagnosticsAfterImport,
                previewModel,
                importModel,
                importModel.Success,
                importModel.SafeMessage,
                warnings,
                errors);
        }

        private StatlynDatabaseDiagnostics ReadDatabaseDiagnostics()
        {
            return new StatlynDatabaseDiagnosticsService(_connectionFactory).ReadDiagnostics();
        }

        private static SourceMetadata BuildMetadata(DataSourceImportRequest request)
        {
            var sourceName = string.IsNullOrWhiteSpace(request.SourceName)
                ? "Local CSV import"
                : request.SourceName.Trim();
            var licenceStatus = string.IsNullOrWhiteSpace(request.LicenceStatus)
                ? "unspecified"
                : request.LicenceStatus.Trim();
            var allowedUsage = string.IsNullOrWhiteSpace(request.AllowedUsage)
                ? "unspecified local CSV import"
                : request.AllowedUsage.Trim();

            return new SourceMetadata(
                sourceName,
                ProviderType.Csv,
                false,
                request.IsLicensed,
                licenceStatus,
                allowedUsage,
                request.PermitsPlayerImages,
                request.PermitsProviderFlags,
                request.UsesBundledSafeFlagAssets,
                request.PermitsClubBadges,
                request.AllowsExport,
                DateTimeOffset.UtcNow,
                request.SourceConfidence);
        }

        private static List<string> BuildWarnings(DataSourceImportRequest request)
        {
            var warnings = new List<string>();
            if (!request.IsLicensed)
            {
                warnings.Add("Source is not marked licensed or permitted; licensed external fields will remain blocked.");
            }

            if (!request.PermitsPlayerImages)
            {
                warnings.Add("Player image fields are not permitted and will remain blocked.");
            }

            if (!request.PermitsProviderFlags && request.UsesBundledSafeFlagAssets)
            {
                warnings.Add("Provider flags are not permitted; bundled safe flag placeholders may be used instead.");
            }
            else if (!request.PermitsProviderFlags)
            {
                warnings.Add("Provider flag fields are not permitted and will remain blocked.");
            }

            if (!request.PermitsClubBadges)
            {
                warnings.Add("Club badge fields are not permitted and will remain blocked.");
            }

            if (string.IsNullOrWhiteSpace(request.AllowedUsage))
            {
                warnings.Add("Allowed usage is unspecified; enter the permitted usage before relying on imported data.");
            }

            return warnings;
        }

        private static List<string> ValidateRequest(DataSourceImportRequest request, bool requireReadableFile)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.CsvPath))
            {
                errors.Add("Enter a local CSV file path.");
                return errors;
            }

            if (LooksLikeNetworkSource(request.CsvPath))
            {
                errors.Add("Network sources are not supported for this milestone.");
                return errors;
            }

            if (!string.Equals(Path.GetExtension(request.CsvPath), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only local CSV files are supported.");
            }

            if (requireReadableFile && !File.Exists(request.CsvPath))
            {
                errors.Add("The selected CSV file is not readable.");
            }

            return errors;
        }

        private static bool LooksLikeNetworkSource(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
