using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Statlyn.Core.Diagnostics;

namespace Statlyn.Data
{
    public sealed class StatlynDatabaseDiagnostics
    {
        public StatlynDatabaseDiagnostics(
            string databasePath,
            int schemaVersion,
            IReadOnlyList<string> tables,
            int dataSourcesCount,
            int playersCount,
            int visibleFieldsCount,
            int playerStatsCount,
            int physicalMetricsCount,
            int blockedAuditCount,
            int performanceMetricDefinitionsCount,
            int roleOutputProfilesCount,
            string lastImportTime,
            string lastError,
            DiagnosticReport report)
        {
            DatabasePath = databasePath;
            SchemaVersion = schemaVersion;
            Tables = tables;
            DataSourcesCount = dataSourcesCount;
            PlayersCount = playersCount;
            VisibleFieldsCount = visibleFieldsCount;
            PlayerStatsCount = playerStatsCount;
            PhysicalMetricsCount = physicalMetricsCount;
            BlockedAuditCount = blockedAuditCount;
            PerformanceMetricDefinitionsCount = performanceMetricDefinitionsCount;
            RoleOutputProfilesCount = roleOutputProfilesCount;
            LastImportTime = lastImportTime ?? string.Empty;
            LastError = lastError ?? string.Empty;
            Report = report;
        }

        public string DatabasePath { get; }

        public int SchemaVersion { get; }

        public IReadOnlyList<string> Tables { get; }

        public int DataSourcesCount { get; }

        public int PlayersCount { get; }

        public int VisibleFieldsCount { get; }

        public int PlayerStatsCount { get; }

        public int PhysicalMetricsCount { get; }

        public int BlockedAuditCount { get; }

        public int PerformanceMetricDefinitionsCount { get; }

        public int RoleOutputProfilesCount { get; }

        public string LastImportTime { get; }

        public string LastError { get; }

        public DiagnosticReport Report { get; }

        public string ToSafeSummary()
        {
            return "Database=" + DatabasePath +
                   "; SchemaVersion=" + SchemaVersion.ToString(CultureInfo.InvariantCulture) +
                   "; DataSources=" + DataSourcesCount.ToString(CultureInfo.InvariantCulture) +
                   "; Players=" + PlayersCount.ToString(CultureInfo.InvariantCulture) +
                   "; VisibleFields=" + VisibleFieldsCount.ToString(CultureInfo.InvariantCulture) +
                   "; PlayerStats=" + PlayerStatsCount.ToString(CultureInfo.InvariantCulture) +
                   "; PhysicalMetrics=" + PhysicalMetricsCount.ToString(CultureInfo.InvariantCulture) +
                   "; BlockedAudit=" + BlockedAuditCount.ToString(CultureInfo.InvariantCulture) +
                   "; PerformanceMetricDefinitions=" + PerformanceMetricDefinitionsCount.ToString(CultureInfo.InvariantCulture) +
                   "; RoleOutputProfiles=" + RoleOutputProfilesCount.ToString(CultureInfo.InvariantCulture) +
                   "; LastImportTime=" + LastImportTime +
                   "; LastError=" + LastError;
        }
    }

    public sealed class StatlynDatabaseDiagnosticsService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;

        public StatlynDatabaseDiagnosticsService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public StatlynDatabaseDiagnostics ReadDiagnostics()
        {
            using (var connection = _connectionFactory.OpenConnection())
            {
                var tables = ReadTables(connection);
                var report = new DiagnosticReport();
                var schemaVersion = CountRows(connection, "SchemaVersion") == 0 ? 0 : ReadInt(connection, "SELECT SchemaVersion FROM SchemaVersion ORDER BY AppliedAtUtc DESC LIMIT 1;");
                report.Add("database.initialized", schemaVersion > 0 ? DiagnosticStatus.Verified : DiagnosticStatus.Partial, schemaVersion > 0 ? "Database is initialized." : "Database has not been initialized.", _connectionFactory.DatabasePath);
                report.Add("database.tables", DiagnosticStatus.Verified, "Database table count read.", tables.Count.ToString(CultureInfo.InvariantCulture));

                return new StatlynDatabaseDiagnostics(
                    _connectionFactory.DatabasePath,
                    schemaVersion,
                    tables,
                    CountRows(connection, "DataSource"),
                    CountRows(connection, "Player"),
                    CountRows(connection, "VisibleField"),
                    CountRows(connection, "PlayerStat"),
                    CountRows(connection, "PhysicalMetric"),
                    CountRows(connection, "BlockedFieldAudit"),
                    CountRows(connection, "PerformanceMetricDefinition"),
                    CountRows(connection, "RoleOutputExpectationProfile"),
                    ReadString(connection, "SELECT ImportedAtUtc FROM ImportAudit ORDER BY ImportedAtUtc DESC LIMIT 1;"),
                    ReadString(connection, "SELECT Message FROM DiagnosticsLog WHERE Status = 'Failed' ORDER BY CreatedAtUtc DESC LIMIT 1;"),
                    report);
            }
        }

        private static IReadOnlyList<string> ReadTables(SqliteConnection connection)
        {
            var tables = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            return tables;
        }

        private static int CountRows(SqliteConnection connection, string table)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + table + ";";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static int ReadInt(SqliteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static string ReadString(SqliteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }
    }
}
