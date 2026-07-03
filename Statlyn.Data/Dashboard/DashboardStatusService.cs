using System;
using System.Globalization;

namespace Statlyn.Data.Dashboard
{
    public sealed class DashboardStatusService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;

        public DashboardStatusService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public DashboardOverviewViewModel BuildOverview()
        {
            return new DashboardOverviewViewModel(
                _connectionFactory.DatabasePath,
                CountRows("DataSource", string.Empty),
                CountRows("Player", string.Empty),
                CountRows("Shortlist", "IsArchived = 0"),
                CountRows("ScoutAssignment", "IsArchived = 0"),
                CountRows("TacticalRole", "IsArchived = 0"),
                CountRows("BenchmarkDefinition", "IsArchived = 0"));
        }

        private int CountRows(string tableName, string whereClause)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + tableName + (string.IsNullOrWhiteSpace(whereClause) ? string.Empty : " WHERE " + whereClause) + ";";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }
    }
}
