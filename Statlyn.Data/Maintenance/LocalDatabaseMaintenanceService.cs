using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Statlyn.Data.Maintenance
{
    public sealed class LocalDatabaseMaintenanceService
    {
        public LocalDatabaseMaintenanceResult GetDatabaseStatus(string databasePath)
        {
            var exists = !string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath);
            return new LocalDatabaseMaintenanceResult(
                true,
                exists ? "Local database file exists." : "Local database file does not exist yet.",
                databasePath,
                null,
                exists ? Array.Empty<string>() : new[] { "Database will be created on demand by safe runtime initialization." },
                Array.Empty<string>());
        }

        public LocalDatabaseMaintenanceResult CreateTimestampedBackupCopy(string databasePath, string backupDirectory)
        {
            if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
            {
                return new LocalDatabaseMaintenanceResult(
                    false,
                    "Main database backup was not created because the source database file does not exist.",
                    databasePath,
                    new LocalDatabaseBackupRecord(databasePath, string.Empty, DateTimeOffset.UtcNow, false),
                    new[] { "No data was deleted or modified." },
                    Array.Empty<string>());
            }

            try
            {
                SqliteConnection.ClearAllPools();
                var directory = string.IsNullOrWhiteSpace(backupDirectory)
                    ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(databasePath)) ?? Directory.GetCurrentDirectory(), "Backups")
                    : backupDirectory;
                Directory.CreateDirectory(directory);
                var fileName = Path.GetFileNameWithoutExtension(databasePath) + "-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".db";
                var backupPath = Path.Combine(directory, fileName);
                File.Copy(databasePath, backupPath, overwrite: false);
                var backup = new LocalDatabaseBackupRecord(databasePath, backupPath, DateTimeOffset.UtcNow, true);
                return new LocalDatabaseMaintenanceResult(true, "Main database backup copy created safely.", databasePath, backup, Array.Empty<string>(), Array.Empty<string>());
            }
            catch (Exception ex)
            {
                return new LocalDatabaseMaintenanceResult(false, "Main database backup could not be created safely.", databasePath, null, Array.Empty<string>(), new[] { SafeException(ex) });
            }
        }

        public LocalDatabaseMaintenanceResult ResetSmokeTestDatabase(string temporaryRoot)
        {
            var path = new StatlynDatabasePathResolver().ResolveSmokeTestPath(temporaryRoot);
            try
            {
                DeleteSqliteFiles(path);
                using (RuntimeDatabaseFactory.CreateFile(path))
                {
                }

                return new LocalDatabaseMaintenanceResult(true, "Smoke-test database was reset safely. Main runtime database was not touched.", path, null, Array.Empty<string>(), Array.Empty<string>());
            }
            catch (Exception ex)
            {
                return new LocalDatabaseMaintenanceResult(false, "Smoke-test database could not be reset safely.", path, null, Array.Empty<string>(), new[] { SafeException(ex) });
            }
        }

        public LocalDatabaseMaintenanceResult ClearSmokeTestDatabase(string temporaryRoot)
        {
            var path = new StatlynDatabasePathResolver().ResolveSmokeTestPath(temporaryRoot);
            try
            {
                DeleteSqliteFiles(path);
                return new LocalDatabaseMaintenanceResult(true, "Smoke-test database was cleared safely. Main runtime database was not touched.", path, null, Array.Empty<string>(), Array.Empty<string>());
            }
            catch (Exception ex)
            {
                return new LocalDatabaseMaintenanceResult(false, "Smoke-test database could not be cleared safely.", path, null, Array.Empty<string>(), new[] { SafeException(ex) });
            }
        }

        public LocalDatabaseMaintenanceResult ExplicitlyClearMainRuntimeDatabase(string databasePath)
        {
            try
            {
                DeleteSqliteFiles(databasePath);
                using (RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                }

                return new LocalDatabaseMaintenanceResult(true, "Main runtime database was explicitly cleared and reinitialized.", databasePath, null, new[] { "This method is intentionally explicit and is not called by smoke tests." }, Array.Empty<string>());
            }
            catch (Exception ex)
            {
                return new LocalDatabaseMaintenanceResult(false, "Main runtime database could not be explicitly cleared safely.", databasePath, null, Array.Empty<string>(), new[] { SafeException(ex) });
            }
        }

        private static void DeleteSqliteFiles(string databasePath)
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(databasePath);
            DeleteIfExists(databasePath + "-wal");
            DeleteIfExists(databasePath + "-shm");
        }

        private static void DeleteIfExists(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string SafeException(Exception ex)
        {
            return ex.GetType().Name + ": " + ex.Message;
        }
    }
}
