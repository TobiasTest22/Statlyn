using System;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Statlyn.Data.Persistence
{
    public abstract class SqliteRepository
    {
        protected SqliteRepository(StatlynDbConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        protected StatlynDbConnectionFactory ConnectionFactory { get; }

        protected static void Add(SqliteCommand command, string name, object? value)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        protected static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction)
        {
            var command = connection.CreateCommand();
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            return command;
        }

        protected static int Bool(bool value)
        {
            return value ? 1 : 0;
        }

        protected static bool ReadBool(SqliteDataReader reader, int ordinal)
        {
            return reader.GetInt32(ordinal) != 0;
        }

        protected static string ReadString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        protected static double? ReadNullableDouble(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (double?)null : reader.GetDouble(ordinal);
        }

        protected static int? ReadNullableInt(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        protected static long LastInsertRowId(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT last_insert_rowid();";
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        protected static string JoinValues(System.Collections.Generic.IEnumerable<string> values)
        {
            return string.Join("|", values ?? Array.Empty<string>());
        }

        protected static string[] SplitValues(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
