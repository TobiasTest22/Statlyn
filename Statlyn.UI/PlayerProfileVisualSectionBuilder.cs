using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Core;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileVisualSectionBuilder
    {
        public static IReadOnlyList<PlayerProfileVisualSectionViewModel> Build(
            PlayerProfileResult result,
            IReadOnlyList<PlayerProfileMetricTileViewModel> core,
            IReadOnlyList<PlayerProfileMetricTileViewModel> supporting)
        {
            var sections = new List<PlayerProfileVisualSectionViewModel>
            {
                new PlayerProfileVisualSectionViewModel("Score cards", "Role fit " + result.LatestRoleScore!.RoleFit.ToString(CultureInfo.InvariantCulture) + " | Confidence " + result.LatestRoleScore.Confidence.ToString(CultureInfo.InvariantCulture), new[] { "Source: " + result.SourceMetadata!.SourceName, "Tactical fit: " + result.TacticalFitDisplay }),
                new PlayerProfileVisualSectionViewModel("Core Role Output", core.Count == 0 ? "Output metrics missing" : "Output metrics available", core.Select(metric => metric.Label + " " + metric.Value).ToList()),
                new PlayerProfileVisualSectionViewModel("Supporting Output", supporting.Count == 0 ? "No supporting output available yet." : "Supporting output available", supporting.Select(metric => metric.Label + " " + metric.Value).ToList()),
                new PlayerProfileVisualSectionViewModel("Missing Data", result.RoleOutputSummary!.MissingCoreMetrics.Count == 0 ? "No core output missing." : "Missing output lowers confidence.", result.RoleOutputSummary.MissingCoreMetrics),
                new PlayerProfileVisualSectionViewModel("Blocked Data", result.BlockedFields.Count == 0 ? "No blocked fields were present." : "Blocked values excluded safely.", result.BlockedFields.Select(field => field.Key.ToString()).Distinct().ToList()),
                new PlayerProfileVisualSectionViewModel("Benchmark", "No benchmark yet.", new[] { "No fake percentile or comparison group is shown." })
            };

            if (result.VisibleFields.Any(field => field.Key == PlayerFieldKey.TechnicalAttribute))
            {
                sections.Add(new PlayerProfileVisualSectionViewModel("Attribute Support", "Attributes are supporting evidence only.", result.VisibleFields.Where(field => field.Key == PlayerFieldKey.TechnicalAttribute).Take(6).Select(field => field.FieldName).ToList()));
            }

            return sections;
        }
    }
}
