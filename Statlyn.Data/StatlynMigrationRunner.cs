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
    }
}
