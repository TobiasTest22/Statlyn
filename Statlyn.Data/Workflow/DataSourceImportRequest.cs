namespace Statlyn.Data.Workflow
{
    public sealed class DataSourceImportRequest
    {
        public string CsvPath { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string LicenceStatus { get; set; } = string.Empty;

        public string AllowedUsage { get; set; } = string.Empty;

        public bool IsLicensed { get; set; }

        public int SourceConfidence { get; set; } = 80;

        public bool PermitsPlayerImages { get; set; }

        public bool PermitsProviderFlags { get; set; }

        public bool UsesBundledSafeFlagAssets { get; set; } = true;

        public bool PermitsClubBadges { get; set; }

        public bool AllowsExport { get; set; }
    }
}
