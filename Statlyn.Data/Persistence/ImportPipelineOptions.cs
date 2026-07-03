using Statlyn.Analytics;

namespace Statlyn.Data.Persistence
{
    public sealed class ImportPipelineOptions
    {
        public ImportPipelineOptions(RoleModel previewRole)
        {
            PreviewRole = previewRole;
        }

        public RoleModel PreviewRole { get; }

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
