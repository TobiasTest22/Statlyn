using Statlyn.Core;

namespace Statlyn.DataProviders.Import
{
    public sealed class ColumnMappingPreview
    {
        public ColumnMappingPreview(
            string sourceColumn,
            PlayerFieldKey fieldKey,
            string fieldName,
            FieldValueKind fieldValueKind,
            bool isForbidden,
            bool isUnknown,
            bool isMapped,
            bool isPermissionBlocked,
            string reason)
        {
            SourceColumn = sourceColumn ?? string.Empty;
            FieldKey = fieldKey;
            FieldName = fieldName ?? string.Empty;
            FieldValueKind = fieldValueKind;
            IsForbidden = isForbidden;
            IsUnknown = isUnknown;
            IsMapped = isMapped;
            IsPermissionBlocked = isPermissionBlocked;
            Reason = reason ?? string.Empty;
        }

        public string SourceColumn { get; }

        public PlayerFieldKey FieldKey { get; }

        public string FieldName { get; }

        public FieldValueKind FieldValueKind { get; }

        public bool IsForbidden { get; }

        public bool IsUnknown { get; }

        public bool IsMapped { get; }

        public bool IsPermissionBlocked { get; }

        public string Reason { get; }
    }
}
