using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Import
{
    public sealed class CsvPreviewService
    {
        private readonly FieldPolicyRegistry _registry;

        public CsvPreviewService()
            : this(new FieldPolicyRegistry())
        {
        }

        public CsvPreviewService(FieldPolicyRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public CsvPreviewResult Preview(string filePath, FieldMappingSet? mappingSet, SourceMetadata metadata)
        {
            metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            mappingSet = mappingSet ?? new FieldMappingSet(Array.Empty<FieldMapping>());
            var diagnostics = new DiagnosticReport();
            var safePath = filePath ?? string.Empty;

            if (LooksLikeNetworkSource(safePath))
            {
                diagnostics.Add("csv.preview.source", DiagnosticStatus.Failed, "Only local CSV paths are supported.", "Network sources are not supported.");
                return Empty(false, safePath, metadata.SourceName, diagnostics);
            }

            if (!File.Exists(safePath))
            {
                diagnostics.Add("csv.preview.file", DiagnosticStatus.Failed, "CSV file was not found.", safePath);
                return Empty(false, safePath, metadata.SourceName, diagnostics);
            }

            string? headerLine = null;
            var rowsDetected = 0;
            foreach (var line in File.ReadLines(safePath))
            {
                if (IsIgnoredLine(line))
                {
                    continue;
                }

                if (headerLine == null)
                {
                    headerLine = line;
                    continue;
                }

                rowsDetected++;
            }

            if (headerLine == null)
            {
                diagnostics.Add("csv.preview.columns", DiagnosticStatus.Failed, "CSV header row was not found.", "No columns detected.");
                return Empty(true, safePath, metadata.SourceName, diagnostics);
            }

            var columns = SplitCsvLine(headerLine);
            var mappings = new List<ColumnMappingPreview>();
            foreach (var column in columns)
            {
                mappings.Add(BuildMappingPreview(column, mappingSet, metadata));
            }

            var forbiddenFields = mappings
                .Where(mapping => mapping.IsForbidden || mapping.IsPermissionBlocked)
                .Select(mapping => mapping.FieldName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var unknownFields = mappings
                .Where(mapping => mapping.IsUnknown)
                .Select(mapping => mapping.SourceColumn)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var safeFields = mappings
                .Where(mapping => mapping.IsMapped)
                .Select(mapping => mapping.FieldName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            diagnostics.Add("csv.preview.file", DiagnosticStatus.Verified, "CSV file is readable.", safePath);
            diagnostics.Add("csv.preview.columns", DiagnosticStatus.Verified, "CSV columns were detected.", columns.Count.ToString(CultureInfo.InvariantCulture) + " column(s).");
            diagnostics.Add("csv.preview.rows", DiagnosticStatus.Verified, "CSV rows were counted.", rowsDetected.ToString(CultureInfo.InvariantCulture) + " data row(s).");
            diagnostics.Add(
                "csv.preview.mapping",
                forbiddenFields.Count > 0 || unknownFields.Count > 0 ? DiagnosticStatus.Partial : DiagnosticStatus.Verified,
                "CSV column mapping preview completed.",
                "Mapped=" + safeFields.Count.ToString(CultureInfo.InvariantCulture) +
                "; Unknown=" + unknownFields.Count.ToString(CultureInfo.InvariantCulture) +
                "; Forbidden=" + forbiddenFields.Count.ToString(CultureInfo.InvariantCulture) + ".");

            return new CsvPreviewResult(
                true,
                true,
                safePath,
                metadata.SourceName,
                rowsDetected,
                columns,
                mappings,
                safeFields.Count,
                unknownFields.Count,
                forbiddenFields.Count,
                forbiddenFields,
                unknownFields,
                safeFields,
                diagnostics);
        }

        private ColumnMappingPreview BuildMappingPreview(string column, FieldMappingSet mappingSet, SourceMetadata metadata)
        {
            var mapping = mappingSet.Resolve(column, _registry);
            var policy = _registry.GetPolicy(mapping.FieldKey);
            var isUnknown = mapping.FieldKey == PlayerFieldKey.Unknown;
            var isForbidden = _registry.IsForbiddenRawName(column) || policy.IsFm26HiddenValue || policy.VisibilityCategory == FieldVisibilityCategory.NeverVisible;
            var isPermissionBlocked = IsPermissionBlocked(mapping.FieldKey, metadata, out var permissionReason);
            var isMapped = !isUnknown && !isForbidden && !isPermissionBlocked;
            var reason = "Column maps to a safe visible field.";

            if (isForbidden)
            {
                reason = string.IsNullOrWhiteSpace(policy.MissingReason)
                    ? "This field is forbidden and will not be imported."
                    : policy.MissingReason;
            }
            else if (isPermissionBlocked)
            {
                reason = permissionReason;
            }
            else if (isUnknown)
            {
                reason = "No safe mapping exists for this column; unknown fields are not usable.";
            }

            return new ColumnMappingPreview(
                column,
                mapping.FieldKey,
                mapping.FieldName,
                mapping.ValueKind,
                isForbidden,
                isUnknown,
                isMapped,
                isPermissionBlocked,
                reason);
        }

        private static bool IsPermissionBlocked(PlayerFieldKey key, SourceMetadata metadata, out string reason)
        {
            reason = string.Empty;
            if ((key == PlayerFieldKey.PlayerStat || key == PlayerFieldKey.PhysicalData || key == PlayerFieldKey.LicensedExternalData) && !metadata.IsLicensed)
            {
                reason = "This field requires a licensed, permitted or user-provided source.";
                return true;
            }

            if (key == PlayerFieldKey.PlayerFaceImage && !metadata.PermitsPlayerImages)
            {
                reason = "The source does not permit player image display.";
                return true;
            }

            if (key == PlayerFieldKey.NationalityFlag && !metadata.PermitsProviderFlags && !metadata.UsesBundledSafeFlagAssets)
            {
                reason = "The source does not permit provider flags and no bundled safe flag asset is enabled.";
                return true;
            }

            if (key == PlayerFieldKey.ClubBadge && !metadata.PermitsClubBadges)
            {
                reason = "The source does not permit club badge display.";
                return true;
            }

            return false;
        }

        private static CsvPreviewResult Empty(bool fileReadable, string filePath, string sourceName, DiagnosticReport diagnostics)
        {
            return new CsvPreviewResult(
                false,
                fileReadable,
                filePath,
                sourceName,
                0,
                new List<string>(),
                new List<ColumnMappingPreview>(),
                0,
                0,
                0,
                new List<string>(),
                new List<string>(),
                new List<string>(),
                diagnostics);
        }

        private static bool LooksLikeNetworkSource(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIgnoredLine(string line)
        {
            return string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal);
        }

        private static List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var current = new List<char>();
            var inQuotes = false;

            foreach (var character in line ?? string.Empty)
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
    }
}
