namespace Statlyn.UI.Visuals
{
    public sealed class MissingDataWarning
    {
        public MissingDataWarning(string fieldName, string reason, string impact, string suggestedAction)
        {
            FieldName = fieldName ?? string.Empty;
            Reason = reason ?? string.Empty;
            Impact = impact ?? string.Empty;
            SuggestedAction = suggestedAction ?? string.Empty;
        }

        public string FieldName { get; }

        public string Reason { get; }

        public string Impact { get; }

        public string SuggestedAction { get; }
    }
}
