using System;
using System.Collections.Generic;

namespace Statlyn.DataProviders.Fm26.MemoryMaps
{
    public sealed class MemoryMapBuildTarget
    {
        public string Game { get; set; } = string.Empty;

        public string GameVersion { get; set; } = string.Empty;

        public string BuildNumber { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;
    }

    public sealed class MemoryMapFieldDefinition
    {
        public string FieldKey { get; set; } = string.Empty;

        public string FieldName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Visibility { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public string SymbolicReference { get; set; } = string.Empty;

        public bool IsReadOnly { get; set; } = true;

        public bool IsHidden { get; set; }

        public bool CanDisplay { get; set; }

        public bool CanStore { get; set; }

        public bool CanScore { get; set; }

        public string Notes { get; set; } = string.Empty;
    }

    public sealed class MemoryMapManifest
    {
        public string MapId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public MemoryMapBuildTarget BuildTarget { get; set; } = new MemoryMapBuildTarget();

        public DateTimeOffset? CreatedAtUtc { get; set; }

        public DateTimeOffset? UpdatedAtUtc { get; set; }

        public string Source { get; set; } = string.Empty;

        public bool IsTemplate { get; set; }

        public bool IsValidated { get; set; }

        public string ValidationNotes { get; set; } = string.Empty;

        public string AllowedUsage { get; set; } = "metadataOnly";

        public string MinimumConnectorVersion { get; set; } = string.Empty;

        public IReadOnlyList<MemoryMapFieldDefinition> Fields { get; set; } = Array.Empty<MemoryMapFieldDefinition>();

        public int FieldCount
        {
            get { return Fields.Count; }
        }

        public int VisibleFieldCount
        {
            get
            {
                var count = 0;
                foreach (var field in Fields)
                {
                    if (field.CanDisplay && !field.IsHidden)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int HiddenFieldCountBlocked
        {
            get
            {
                var count = 0;
                foreach (var field in Fields)
                {
                    if (field.IsHidden && !field.CanDisplay && !field.CanStore && !field.CanScore)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public string Checksum { get; set; } = string.Empty;

        public string SafeMessage { get; set; } = string.Empty;
    }

    public sealed class MemoryMapValidationResult
    {
        public bool IsValid { get; set; }

        public bool IsUsable { get; set; }

        public string SupportStatus { get; set; } = MemoryMapSupportStatus.Invalid.ToString();

        public string SafeMessage { get; set; } = string.Empty;

        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    }

    public sealed class MemoryMapFileDiagnostic
    {
        public string FileName { get; set; } = string.Empty;

        public string MapId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string GameVersion { get; set; } = string.Empty;

        public string BuildNumber { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;

        public bool IsTemplate { get; set; }

        public bool IsValidated { get; set; }

        public bool IsUsable { get; set; }

        public string SupportStatus { get; set; } = MemoryMapSupportStatus.Invalid.ToString();

        public int FieldCount { get; set; }

        public int VisibleFieldCount { get; set; }

        public int HiddenFieldCountBlocked { get; set; }

        public string Checksum { get; set; } = string.Empty;

        public string SafeMessage { get; set; } = string.Empty;

        public IReadOnlyList<string> ValidationWarnings { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> ValidationErrors { get; set; } = Array.Empty<string>();

        public MemoryMapManifest? Manifest { get; set; }
    }

    public sealed class MemoryMapRegistryDiagnostic
    {
        public string RegistryStatus { get; set; } = MemoryMapSupportStatus.Empty.ToString();

        public int MapsFoundCount { get; set; }

        public int UsableMapsCount { get; set; }

        public int TemplateMapsCount { get; set; }

        public int InvalidMapsCount { get; set; }

        public bool HasValidatedMap { get; set; }

        public string SafeMessage { get; set; } = string.Empty;

        public string NextActionSafeMessage { get; set; } = string.Empty;

        public IReadOnlyList<MemoryMapFileDiagnostic> Maps { get; set; } = Array.Empty<MemoryMapFileDiagnostic>();
    }

    public sealed class MemoryMapSelectionResult
    {
        public bool HasValidatedMap { get; set; }

        public bool HasSelectedMap { get; set; }

        public string SelectedMapId { get; set; } = string.Empty;

        public string SelectedMapDisplayName { get; set; } = string.Empty;

        public string SupportStatus { get; set; } = MemoryMapSupportStatus.MapMissing.ToString();

        public string SupportMessage { get; set; } = "No validated FM26 map metadata is loaded.";

        public string NextActionSafeMessage { get; set; } = "Validated FM26 map metadata is required before any future player snapshot milestone.";

        public MemoryMapFileDiagnostic? SelectedMap { get; set; }
    }
}
