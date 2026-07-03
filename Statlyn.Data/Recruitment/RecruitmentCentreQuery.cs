namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreQuery
    {
        public string SearchText { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;

        public string PositionGroup { get; set; } = string.Empty;

        public string PrimaryPosition { get; set; } = string.Empty;

        public string Nationality { get; set; } = string.Empty;

        public int? MinimumAge { get; set; }

        public int? MaximumAge { get; set; }

        public int? MinimumRoleFit { get; set; }

        public int? MinimumConfidence { get; set; }

        public string Recommendation { get; set; } = string.Empty;

        public bool? HasBlockedFields { get; set; }

        public bool? HasMissingData { get; set; }

        public string SortBy { get; set; } = "DisplayName";

        public string SortDirection { get; set; } = "Ascending";

        public int Limit { get; set; } = 100;
    }
}
