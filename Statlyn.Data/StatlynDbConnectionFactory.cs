using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Statlyn.Data
{
    public sealed class StatlynDbConnectionFactory : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection? _keeperConnection;

        private StatlynDbConnectionFactory(string connectionString, string databasePath, bool keepAlive)
        {
            _connectionString = connectionString;
            DatabasePath = databasePath;
            if (keepAlive)
            {
                _keeperConnection = new SqliteConnection(_connectionString);
                _keeperConnection.Open();
            }
        }

        public string DatabasePath { get; }

        public static StatlynDbConnectionFactory CreateInMemory(string? name = null)
        {
            var databaseName = string.IsNullOrWhiteSpace(name) ? "statlyn-" + Guid.NewGuid().ToString("N") : name;
            return new StatlynDbConnectionFactory("Data Source=" + databaseName + ";Mode=Memory;Cache=Shared", "in-memory:" + databaseName, keepAlive: true);
        }

        public static StatlynDbConnectionFactory CreateFile(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("A database path is required.", nameof(databasePath));
            }

            var fullPath = Path.GetFullPath(databasePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return new StatlynDbConnectionFactory("Data Source=" + fullPath, fullPath, keepAlive: false);
        }

        public SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public void Dispose()
        {
            _keeperConnection?.Dispose();
        }
    }
}
