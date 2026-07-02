using Statlyn.Core;

namespace Statlyn.DataProviders.Import
{
    public sealed class FieldMapping
    {
        public FieldMapping(string sourceColumn, PlayerFieldKey fieldKey, FieldValueKind valueKind)
        {
            SourceColumn = sourceColumn ?? string.Empty;
            FieldKey = fieldKey;
            ValueKind = valueKind;
        }

        public string SourceColumn { get; }

        public PlayerFieldKey FieldKey { get; }

        public FieldValueKind ValueKind { get; }
    }
}
