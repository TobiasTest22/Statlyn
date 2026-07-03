using System.Collections.Generic;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreColumnLayout
    {
        public RecruitmentCentreColumnLayout(string name, IReadOnlyList<string> visibleColumns, string sortBy, string sortDirection)
        {
            Name = name ?? string.Empty;
            VisibleColumns = visibleColumns ?? new List<string>();
            SortBy = sortBy ?? "DisplayName";
            SortDirection = sortDirection ?? "Ascending";
        }

        public string Name { get; }

        public IReadOnlyList<string> VisibleColumns { get; }

        public string SortBy { get; }

        public string SortDirection { get; }

        public static RecruitmentCentreColumnLayout OutputView()
        {
            return new RecruitmentCentreColumnLayout(
                "Output View",
                new[] { "Name", "Position", "Source", "RoleFit", "Confidence", "Recommendation", "KeyOutputMetrics", "BlockedFields", "MissingData" },
                "RoleFit",
                "Descending");
        }
    }
}
