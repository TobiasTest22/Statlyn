using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.DataProviders.Import;
using Statlyn.Scouting;

namespace Statlyn.Data.Workflow
{
    public sealed class UnityRuntimeDependencyCheck
    {
        private static readonly string[] RequiredAssemblies =
        {
            "Statlyn.Core",
            "Statlyn.DataProviders",
            "Statlyn.Scouting",
            "Statlyn.Analytics",
            "Statlyn.Data",
            "Statlyn.UI"
        };

        private static readonly string[] SqliteManagedAssemblies =
        {
            "Microsoft.Data.Sqlite",
            "SQLitePCLRaw.core",
            "SQLitePCLRaw.batteries_v2",
            "SQLitePCLRaw.provider.e_sqlite3"
        };

        public UnityRuntimeCheckResult Run(string temporaryRoot, string defaultDatabasePath)
        {
            var messages = new List<string>();
            var warnings = new List<string>();
            var errors = new List<string>();
            var checkedAt = DateTimeOffset.UtcNow;
            var databasePath = BuildTemporaryDatabasePath(temporaryRoot);

            messages.Add("CSV Data Sources runtime check started.");
            messages.Add("FM26 is not required for this check.");
            if (!string.IsNullOrWhiteSpace(defaultDatabasePath))
            {
                messages.Add("Default database path resolves to: " + defaultDatabasePath);
            }

            TouchCompileTimeTypes();

            var assembliesOk = CheckAssemblies(RequiredAssemblies, messages, errors);
            var sqliteManagedOk = CheckAssemblies(SqliteManagedAssemblies, messages, errors);
            var sqliteNativeOk = false;
            var databaseInitOk = false;
            var workflowServiceOk = false;

            try
            {
                using (var connection = new SqliteConnection("Data Source=:memory:"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 1;";
                        command.ExecuteScalar();
                    }
                }

                sqliteNativeOk = true;
                messages.Add("SQLite managed/native open test succeeded.");
            }
            catch (Exception ex)
            {
                errors.Add("SQLite open test failed: " + SafeException(ex));
            }

            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var diagnostics = new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics();
                    databaseInitOk = diagnostics.SchemaVersion == StatlynSchemaVersion.Current;
                    if (databaseInitOk)
                    {
                        messages.Add("Temporary SQLite database initialized.");
                    }
                    else
                    {
                        errors.Add("Temporary SQLite database initialized with unexpected schema version.");
                    }

                    var workflow = new DataSourceImportWorkflowService(factory);
                    var preview = new CsvPreviewService();
                    workflowServiceOk = workflow != null && preview != null;
                    if (workflowServiceOk)
                    {
                        messages.Add("Data source workflow and CSV preview services can be constructed.");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add("Temporary database or workflow construction failed: " + SafeException(ex));
            }
            finally
            {
                TryDeleteTemporaryDatabase(databasePath, warnings);
            }

            var success = assembliesOk && sqliteManagedOk && sqliteNativeOk && databaseInitOk && workflowServiceOk && errors.Count == 0;
            if (!success)
            {
                warnings.Add("Runtime check did not pass. Review SQLite dependency and path messages before importing.");
            }

            return new UnityRuntimeCheckResult(
                success,
                checkedAt,
                databasePath,
                assembliesOk,
                sqliteManagedOk,
                sqliteNativeOk,
                databaseInitOk,
                workflowServiceOk,
                messages,
                warnings,
                errors);
        }

        private static void TouchCompileTimeTypes()
        {
            _ = typeof(SourceMetadata);
            _ = typeof(CsvPreviewService);
            _ = typeof(ScoutingKnowledgeFirewall);
            _ = typeof(RoleScoringEngine);
            _ = typeof(RuntimeDatabaseFactory);
        }

        private static bool CheckAssemblies(IEnumerable<string> assemblyNames, IList<string> messages, IList<string> errors)
        {
            var ok = true;
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assemblyName in assemblyNames)
            {
                if (loaded.Any(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)))
                {
                    messages.Add(assemblyName + " resolved.");
                    continue;
                }

                try
                {
                    Assembly.Load(assemblyName);
                    messages.Add(assemblyName + " loaded.");
                }
                catch (Exception ex)
                {
                    ok = false;
                    errors.Add(assemblyName + " could not be loaded: " + SafeException(ex));
                }
            }

            return ok;
        }

        private static string BuildTemporaryDatabasePath(string temporaryRoot)
        {
            var root = string.IsNullOrWhiteSpace(temporaryRoot)
                ? Path.GetTempPath()
                : temporaryRoot;
            var directory = Path.Combine(root, "StatlynRuntimeCheck");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "runtime-check-" + Guid.NewGuid().ToString("N") + ".db");
        }

        private static void TryDeleteTemporaryDatabase(string databasePath, IList<string> warnings)
        {
            try
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }

                DeleteIfExists(databasePath + "-wal");
                DeleteIfExists(databasePath + "-shm");
            }
            catch (Exception ex)
            {
                warnings.Add("Temporary runtime-check database could not be deleted: " + SafeException(ex));
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
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
