using Statlyn.Core;

namespace Statlyn.DataProviders.Import
{
    public sealed class FieldMapping
    {
        public FieldMapping(string sourceColumn, PlayerFieldKey fieldKey, FieldValueKind valueKind)
            : this(sourceColumn, fieldKey, sourceColumn, valueKind)
        {
        }

        public FieldMapping(string sourceColumn, PlayerFieldKey fieldKey, string fieldName, FieldValueKind valueKind)
        {
            SourceColumn = sourceColumn ?? string.Empty;
            FieldKey = fieldKey;
            FieldName = string.IsNullOrWhiteSpace(fieldName) ? SourceColumn : fieldName;
            ValueKind = valueKind;
        }

        public string SourceColumn { get; }

        public PlayerFieldKey FieldKey { get; }

        public string FieldName { get; }

        public FieldValueKind ValueKind { get; }
    }
}
