using System.Collections.Generic;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreResult
    {
        public RecruitmentCentreResult(
            IReadOnlyList<RecruitmentCentrePlayerRow> players,
            int totalCount,
            IReadOnlyList<string> sources,
            IReadOnlyList<string> diagnostics,
            string safeMessage)
        {
            Players = players ?? new List<RecruitmentCentrePlayerRow>();
            TotalCount = totalCount;
            Sources = sources ?? new List<string>();
            Diagnostics = diagnostics ?? new List<string>();
            SafeMessage = safeMessage ?? string.Empty;
        }

        public IReadOnlyList<RecruitmentCentrePlayerRow> Players { get; }

        public int TotalCount { get; }

        public IReadOnlyList<string> Sources { get; }

        public IReadOnlyList<string> Diagnostics { get; }

        public string SafeMessage { get; }
    }
}
