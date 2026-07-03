using System.Collections.Generic;
using System.Linq;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreViewModel
    {
        public RecruitmentCentreViewModel(
            IReadOnlyList<RecruitmentCentrePlayerRowViewModel> players,
            int totalCount,
            IReadOnlyList<string> sources,
            RecruitmentCentreFilterViewModel filters,
            RecruitmentCentreDiagnosticViewModel diagnostics,
            string safeMessage)
        {
            Players = players ?? new List<RecruitmentCentrePlayerRowViewModel>();
            TotalCount = totalCount;
            Sources = sources ?? new List<string>();
            Filters = filters;
            Diagnostics = diagnostics;
            SafeMessage = safeMessage ?? string.Empty;
        }

        public IReadOnlyList<RecruitmentCentrePlayerRowViewModel> Players { get; }

        public int TotalCount { get; }

        public IReadOnlyList<string> Sources { get; }

        public RecruitmentCentreFilterViewModel Filters { get; }

        public RecruitmentCentreDiagnosticViewModel Diagnostics { get; }

        public string SafeMessage { get; }

        public static RecruitmentCentreViewModel From(RecruitmentCentreResult result, RecruitmentCentreQuery query, string databasePath)
        {
            return new RecruitmentCentreViewModel(
                result.Players.Select(RecruitmentCentrePlayerRowViewModel.From).ToList(),
                result.TotalCount,
                result.Sources,
                new RecruitmentCentreFilterViewModel(query.SearchText, query.SourceName, query.PositionGroup, query.MinimumConfidence, query.MinimumRoleFit),
                new RecruitmentCentreDiagnosticViewModel(databasePath, result.Diagnostics, new List<string>(), new List<string>()),
                result.SafeMessage);
        }
    }
}
