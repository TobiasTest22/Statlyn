namespace Statlyn.Analytics
{
    public sealed class EvidenceItem
    {
        public EvidenceItem(string fieldName, string message, bool isPositive)
        {
            FieldName = fieldName ?? string.Empty;
            Message = message ?? string.Empty;
            IsPositive = isPositive;
        }

        public string FieldName { get; }

        public string Message { get; }

        public bool IsPositive { get; }
    }
}
