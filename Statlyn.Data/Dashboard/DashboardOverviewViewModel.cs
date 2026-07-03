using System.Globalization;

namespace Statlyn.Data.Dashboard
{
    public sealed class DashboardOverviewViewModel
    {
        public DashboardOverviewViewModel(
            string databasePath,
            int dataSourceCount,
            int importedPlayersCount,
            int shortlistCount,
            int scoutAssignmentCount,
            int roleLabTemplateCount,
            int benchmarkDefinitionCount)
        {
            DatabasePath = databasePath ?? string.Empty;
            DataSourceCount = dataSourceCount;
            ImportedPlayersCount = importedPlayersCount;
            ShortlistCount = shortlistCount;
            ScoutAssignmentCount = scoutAssignmentCount;
            RoleLabTemplateCount = roleLabTemplateCount;
            BenchmarkDefinitionCount = benchmarkDefinitionCount;
        }

        public string DatabasePath { get; }

        public int DataSourceCount { get; }

        public int ImportedPlayersCount { get; }

        public int ShortlistCount { get; }

        public int ScoutAssignmentCount { get; }

        public int RoleLabTemplateCount { get; }

        public int BenchmarkDefinitionCount { get; }

        public string DataSourceStatus
        {
            get { return DataSourceCount == 0 ? "Awaiting local data." : DataSourceCount.ToString(CultureInfo.InvariantCulture) + " local source(s)."; }
        }

        public string ImportedPlayersStatus
        {
            get { return ImportedPlayersCount == 0 ? "Awaiting local data." : ImportedPlayersCount.ToString(CultureInfo.InvariantCulture) + " imported player(s)."; }
        }

        public string RecruitmentCentreStatus
        {
            get { return ImportedPlayersCount == 0 ? "Awaiting local data." : "Safe local data available."; }
        }

        public string ShortlistsStatus
        {
            get { return ShortlistCount == 0 ? "Awaiting local data." : ShortlistCount.ToString(CultureInfo.InvariantCulture) + " active shortlist(s)."; }
        }

        public string ScoutAssignmentsStatus
        {
            get { return ScoutAssignmentCount == 0 ? "Awaiting local data." : ScoutAssignmentCount.ToString(CultureInfo.InvariantCulture) + " active assignment(s)."; }
        }

        public string RoleLabStatus
        {
            get { return RoleLabTemplateCount == 0 ? "Awaiting local data." : RoleLabTemplateCount.ToString(CultureInfo.InvariantCulture) + " role template(s)."; }
        }

        public string BenchmarkStatus
        {
            get { return BenchmarkDefinitionCount == 0 ? "Awaiting local data." : BenchmarkDefinitionCount.ToString(CultureInfo.InvariantCulture) + " benchmark definition(s)."; }
        }

        public string Fm26Status
        {
            get { return "Unsupported until validated memory maps exist."; }
        }

        public string SmokeTestStatus
        {
            get { return "Not run in this session."; }
        }

        public bool HasLiveFm26Data
        {
            get { return false; }
        }

        public bool IsFm26Supported
        {
            get { return false; }
        }

        public string ToSafeText()
        {
            return string.Join(
                " | ",
                new[]
                {
                    "Database: " + DatabasePath,
                    "Data sources: " + DataSourceStatus,
                    "Imported players: " + ImportedPlayersStatus,
                    "Recruitment Centre: " + RecruitmentCentreStatus,
                    "Shortlists: " + ShortlistsStatus,
                    "Scout assignments: " + ScoutAssignmentsStatus,
                    "Role Lab: " + RoleLabStatus,
                    "Benchmarks: " + BenchmarkStatus,
                    "FM26: " + Fm26Status,
                    "Smoke test: " + SmokeTestStatus,
                    "No live FM26 data"
                });
        }
    }
}
