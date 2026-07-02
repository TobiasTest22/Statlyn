namespace Statlyn.Core
{
    public sealed class BlockedFieldNotice
    {
        public BlockedFieldNotice(PlayerFieldKey key, string fieldName, string reason, string sourceProvider)
        {
            Key = key;
            FieldName = fieldName ?? string.Empty;
            Reason = reason ?? string.Empty;
            SourceProvider = sourceProvider ?? string.Empty;
        }

        public PlayerFieldKey Key { get; }

        public string FieldName { get; }

        public string Reason { get; }

        public string SourceProvider { get; }
    }
}
