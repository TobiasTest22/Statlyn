namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreFilterViewModel
    {
        public RecruitmentCentreFilterViewModel(string searchText, string sourceName, string positionGroup, int? minimumConfidence, int? minimumRoleFit)
        {
            SearchText = searchText ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            MinimumConfidence = minimumConfidence;
            MinimumRoleFit = minimumRoleFit;
        }

        public string SearchText { get; }

        public string SourceName { get; }

        public string PositionGroup { get; }

        public int? MinimumConfidence { get; }

        public int? MinimumRoleFit { get; }
    }
}
