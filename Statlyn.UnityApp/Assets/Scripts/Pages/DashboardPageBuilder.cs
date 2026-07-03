using System.Globalization;
using Statlyn.Data;
using Statlyn.Data.Dashboard;
using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class DashboardPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            var overview = LoadOverview(databasePath);

            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                "Recruitment Intelligence",
                "Local command-center overview for masked recruitment analysis",
                "No live FM26 data",
                CommandStatusCategory.Info));

            var dashboard = new VisualElement();
            dashboard.AddToClassList("dashboard-grid");
            dashboard.AddToClassList("command-kpi-row");
            main.Add(dashboard);

            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Data Source", CountValue(overview.DataSourceCount), overview.DataSourceStatus, overview.DataSourceCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Info));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Runtime", "Local SQLite", overview.DatabasePath, CommandStatusCategory.Info));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Imported Players", CountValue(overview.ImportedPlayersCount), overview.ImportedPlayersStatus, overview.ImportedPlayersCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Success));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Recruitment Centre", overview.ImportedPlayersCount == 0 ? "Awaiting" : "Ready", overview.RecruitmentCentreStatus, overview.ImportedPlayersCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Success));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Shortlists", CountValue(overview.ShortlistCount), overview.ShortlistsStatus, overview.ShortlistCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Info));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Scout Assignments", CountValue(overview.ScoutAssignmentCount), overview.ScoutAssignmentsStatus, overview.ScoutAssignmentCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Info));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Role Lab", CountValue(overview.RoleLabTemplateCount), overview.RoleLabStatus, overview.RoleLabTemplateCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Info));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Benchmarks", CountValue(overview.BenchmarkDefinitionCount), overview.BenchmarkStatus, overview.BenchmarkDefinitionCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Info));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Local Readiness", "Not checked", overview.LocalReadinessStatus, CommandStatusCategory.Muted));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("FM26 Status", "Unsupported", overview.Fm26Status, CommandStatusCategory.Warning));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Smoke Test", "Not run", overview.SmokeTestStatus, CommandStatusCategory.Muted));

            main.Add(StatlynUiFactory.MakeCommandWarningBanner("Safety Baseline", new[]
            {
                "Dashboard counts are read from the safe local SQLite database.",
                "No live FM26 data, invented KPI values or external API sources are available.",
                "Open Data Sources for local CSV import or Diagnostics for runtime checks."
            }));

            main.Add(StatlynUiFactory.MakeCommandPanel("Workflow Context", new[]
            {
                "CSV local import remains the only user-facing source workflow.",
                "Benchmarks are generic/import definitions until a real comparison group exists.",
                "Diagnostics owns Runtime Check and Full Smoke Test."
            }));
        }

        private static DashboardOverviewViewModel LoadOverview(string databasePath)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    return new DashboardStatusService(factory).BuildOverview();
                }
            }
            catch
            {
                return new DashboardOverviewViewModel(databasePath, 0, 0, 0, 0, 0, 0);
            }
        }

        private static string CountValue(int count)
        {
            return count.ToString(CultureInfo.InvariantCulture);
        }

    }
}
