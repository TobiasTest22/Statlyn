using System;
using Microsoft.Data.Sqlite;

namespace Statlyn.Data
{
    public sealed class StatlynMigrationRunner
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;

        public StatlynMigrationRunner(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public void ApplyMigrations()
        {
            using (var connection = _connectionFactory.OpenConnection())
            {
                var deferredIndexes = new System.Collections.Generic.List<string>();
                foreach (var statement in StatlynDatabaseSchema.CreateStatements)
                {
                    if (IsDeferredIndex(statement))
                    {
                        deferredIndexes.Add(statement);
                        continue;
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = statement;
                        command.ExecuteNonQuery();
                    }
                }

                EnsureColumn(connection, "RoleScore", "Recommendation", "TEXT NOT NULL DEFAULT 'ScoutFurther'");
                EnsureColumn(connection, "RoleScore", "RoleName", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "PlayerStat", "SampleMinutesMissing", "INTEGER NOT NULL DEFAULT 1");
                EnsureColumn(connection, "PlayerStat", "MinutesSource", "TEXT NOT NULL DEFAULT 'missing'");
                EnsureColumn(connection, "Shortlist", "UpdatedAtUtc", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "Shortlist", "IsArchived", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumn(connection, "ShortlistPlayer", "StatlynPlayerId", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ShortlistPlayer", "Priority", "TEXT NOT NULL DEFAULT 'Medium'");
                EnsureColumn(connection, "ShortlistPlayer", "FollowUpAction", "TEXT NOT NULL DEFAULT 'None'");
                EnsureColumn(connection, "ShortlistPlayer", "RoleName", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ShortlistPlayer", "Recommendation", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ShortlistPlayer", "AddedReason", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ShortlistPlayer", "UserNote", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ShortlistPlayer", "UpdatedAtUtc", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "StatlynPlayerId", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "ShortlistPlayerId", "INTEGER NULL");
                EnsureColumn(connection, "ScoutAssignment", "ShortlistId", "INTEGER NULL");
                EnsureColumn(connection, "ScoutAssignment", "AssignmentTitle", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "RoleName", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "PositionGroup", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "Priority", "TEXT NOT NULL DEFAULT 'Medium'");
                EnsureColumn(connection, "ScoutAssignment", "Status", "TEXT NOT NULL DEFAULT 'Open'");
                EnsureColumn(connection, "ScoutAssignment", "AssignedTo", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "DueAtUtc", "TEXT NULL");
                EnsureColumn(connection, "ScoutAssignment", "UpdatedAtUtc", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "ClosedAtUtc", "TEXT NULL");
                EnsureColumn(connection, "ScoutAssignment", "SourceName", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutAssignment", "IsArchived", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumn(connection, "ScoutReport", "AssignmentId", "INTEGER NULL");
                EnsureColumn(connection, "ScoutReport", "StatlynPlayerId", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutReport", "TechnicalRating", "TEXT NOT NULL DEFAULT 'Unknown'");
                EnsureColumn(connection, "ScoutReport", "TacticalRating", "TEXT NOT NULL DEFAULT 'Unknown'");
                EnsureColumn(connection, "ScoutReport", "PhysicalRating", "TEXT NOT NULL DEFAULT 'Unknown'");
                EnsureColumn(connection, "ScoutReport", "MentalRating", "TEXT NOT NULL DEFAULT 'Unknown'");
                EnsureColumn(connection, "ScoutReport", "OverallRecommendation", "TEXT NOT NULL DEFAULT 'ScoutFurther'");
                EnsureColumn(connection, "ScoutReport", "Risks", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutReport", "FinalSummary", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutReport", "CreatedAtUtc", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(connection, "ScoutReport", "UpdatedAtUtc", "TEXT NOT NULL DEFAULT ''");

                foreach (var statement in deferredIndexes)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = statement;
                        command.ExecuteNonQuery();
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM SchemaVersion;";
                    var count = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
                    if (count == 0)
                    {
                        command.CommandText = "INSERT INTO SchemaVersion (SchemaVersion, AppliedAtUtc) VALUES ($version, $appliedAtUtc);";
                        command.Parameters.AddWithValue("$version", StatlynSchemaVersion.Current);
                        command.Parameters.AddWithValue("$appliedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        command.CommandText = "UPDATE SchemaVersion SET SchemaVersion = $version, AppliedAtUtc = $appliedAtUtc;";
                        command.Parameters.AddWithValue("$version", StatlynSchemaVersion.Current);
                        command.Parameters.AddWithValue("$appliedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string declaration)
        {
            if (HasColumn(connection, tableName, columnName))
            {
                return;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + declaration + ";";
                command.ExecuteNonQuery();
            }
        }

        private static bool IsDeferredIndex(string statement)
        {
            return statement.IndexOf("IX_Shortlist", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   statement.IndexOf("UX_ShortlistPlayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   statement.IndexOf("IX_ScoutAssignment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   statement.IndexOf("IX_ScoutReport", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasColumn(SqliteConnection connection, string tableName, string columnName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(" + tableName + ");";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
