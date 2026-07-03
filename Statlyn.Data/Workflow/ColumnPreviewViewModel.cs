using Statlyn.DataProviders.Import;

namespace Statlyn.Data.Workflow
{
    public sealed class ColumnPreviewViewModel
    {
        public ColumnPreviewViewModel(string sourceColumn, string mappedTo, string category, string status, string reason)
        {
            SourceColumn = sourceColumn ?? string.Empty;
            MappedTo = mappedTo ?? string.Empty;
            Category = category ?? string.Empty;
            Status = status ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string SourceColumn { get; }

        public string MappedTo { get; }

        public string Category { get; }

        public string Status { get; }

        public string Reason { get; }

        public static ColumnPreviewViewModel From(ColumnMappingPreview preview)
        {
            var status = "Safe";
            if (preview.IsForbidden)
            {
                status = "Forbidden";
            }
            else if (preview.IsPermissionBlocked)
            {
                status = "PermissionBlocked";
            }
            else if (preview.IsUnknown)
            {
                status = "Unknown";
            }

            return new ColumnPreviewViewModel(
                preview.SourceColumn,
                preview.IsUnknown ? string.Empty : preview.FieldName,
                preview.FieldKey.ToString(),
                status,
                preview.Reason);
        }
    }
}
