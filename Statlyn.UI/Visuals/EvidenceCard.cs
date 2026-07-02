namespace Statlyn.UI.Visuals
{
    public sealed class EvidenceCard
    {
        public EvidenceCard(string title, EvidenceCategory category, string body, string sourceName, int confidence, string actionSuggestion)
        {
            Title = title ?? string.Empty;
            Category = category;
            Body = body ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            Confidence = confidence;
            ActionSuggestion = actionSuggestion ?? string.Empty;
        }

        public string Title { get; }

        public EvidenceCategory Category { get; }

        public string Body { get; }

        public string SourceName { get; }

        public int Confidence { get; }

        public string ActionSuggestion { get; }
    }
}
