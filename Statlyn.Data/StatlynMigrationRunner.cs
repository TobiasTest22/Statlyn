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
                foreach (var statement in StatlynDatabaseSchema.CreateStatements)
                {
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
