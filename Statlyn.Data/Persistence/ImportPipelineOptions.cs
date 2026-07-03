using Statlyn.Analytics;

namespace Statlyn.Data.Persistence
{
    public sealed class ImportPipelineOptions
    {
        public ImportPipelineOptions(RoleModel previewRole, int fatalFailureAfterAcceptedRows = -1)
        {
            PreviewRole = previewRole;
            FatalFailureAfterAcceptedRows = fatalFailureAfterAcceptedRows;
        }

        public RoleModel PreviewRole { get; }

        public int FatalFailureAfterAcceptedRows { get; }

        public ImportPipelineOptions WithFatalFailureAfterAcceptedRows(int rows)
        {
            return new ImportPipelineOptions(PreviewRole, rows);
        }

        public static ImportPipelineOptions CreateDefault()
        {
            return new ImportPipelineOptions(
                new RoleModel("Generic performance preview")
                    .RequireAttribute("Finishing", 0.75)
                    .RequireAttribute("Pace", 0.5)
                    .RequireAttribute("Acceleration", 0.5)
                    .RequireStat("xG", 2)
                    .RequireStat("xA", 1)
                    .RequirePhysicalMetric("TopSpeed", 0.5)
                    .RequirePhysicalMetric("SprintDistance", 0.5));
        }
    }
}
