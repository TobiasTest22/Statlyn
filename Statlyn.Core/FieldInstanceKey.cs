using System;

namespace Statlyn.Core
{
    public sealed class FieldInstanceKey : IEquatable<FieldInstanceKey>
    {
        public FieldInstanceKey(PlayerFieldKey key, string fieldName, string sourceFieldName)
        {
            Key = key;
            FieldName = NormalizeName(fieldName);
            SourceFieldName = string.IsNullOrWhiteSpace(sourceFieldName) ? FieldName : sourceFieldName.Trim();
        }

        public PlayerFieldKey Key { get; }

        public string FieldName { get; }

        public string SourceFieldName { get; }

        public string StableId
        {
            get { return Key + ":" + FieldName; }
        }

        public static FieldInstanceKey From(PlayerFieldKey key, string fieldName, string sourceFieldName)
        {
            return new FieldInstanceKey(key, fieldName, sourceFieldName);
        }

        public bool Equals(FieldInstanceKey? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Key == other.Key && string.Equals(FieldName, other.FieldName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as FieldInstanceKey);
        }

        public override int GetHashCode()
        {
            return ((int)Key * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(FieldName);
        }

        public override string ToString()
        {
            return StableId;
        }

        private static string NormalizeName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "Value" : name.Trim();
        }
    }
}
