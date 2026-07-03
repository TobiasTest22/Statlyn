using System;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.Data.RoleLab;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.Data.Workflow;

namespace Statlyn.Data.Readiness
{
    public sealed class LocalProductReadinessService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly string _applicationDataPath;
        private readonly string _streamingAssetsPath;

        public LocalProductReadinessService(StatlynDbConnectionFactory connectionFactory, string applicationDataPath, string streamingAssetsPath)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _applicationDataPath = applicationDataPath ?? string.Empty;
            _streamingAssetsPath = streamingAssetsPath ?? string.Empty;
        }

        public LocalProductReadinessResult Run()
        {
            var checks = new System.Collections.Generic.List<LocalProductReadinessCheck>();
            var schemaVersion = 0;
            var fixturePath = string.Empty;
            var importedPlayerCount = 0;
            var shortlistCount = 0;
            var scoutReportCount = 0;
            var roleLabTemplateCount = 0;
            var benchmarkDefinitionCount = 0;
            var hasImportedPlayers = false;
            var hasShortlists = false;
            var hasScoutReports = false;
            var hasRoleLabTemplates = false;
            var hasBenchmarkDefinitions = false;

            try
            {
                new StatlynDatabaseInitializer(_connectionFactory).Initialize();
                var diagnostics = new StatlynDatabaseDiagnosticsService(_connectionFactory).ReadDiagnostics();
                schemaVersion = diagnostics.SchemaVersion;
                importedPlayerCount = diagnostics.PlayersCount;
                hasImportedPlayers = importedPlayerCount > 0;
                checks.Add(Check(
                    "Database",
                    diagnostics.SchemaVersion == StatlynSchemaVersion.Current ? LocalProductReadinessCheckStatus.Passed : LocalProductReadinessCheckStatus.Failed,
                    diagnostics.SchemaVersion == StatlynSchemaVersion.Current ? "Database can initialize with the current schema." : "Database schema version is not current.",
                    "SchemaVersion=" + diagnostics.SchemaVersion.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                checks.Add(Check("Database", LocalProductReadinessCheckStatus.Failed, "Database could not initialize safely.", SafeException(ex)));
            }

            var fixture = new UnityFixtureCsvPathResolver().Resolve(_applicationDataPath, _streamingAssetsPath);
            fixturePath = fixture.FilePath;
            checks.Add(Check(
                "Synthetic Fixture",
                fixture.Success ? LocalProductReadinessCheckStatus.Passed : LocalProductReadinessCheckStatus.Warning,
                fixture.Success ? "Synthetic fixture CSV is available for local checks." : "Synthetic fixture CSV is missing; enter a local CSV manually or rerun the copy script.",
                fixture.Success ? fixture.FilePath : fixture.Message));

            try
            {
                using (var connection = new SqliteConnection("Data Source=:memory:"))
                {
                    connection.Open();
                }

                checks.Add(Check("SQLite Dependencies", LocalProductReadinessCheckStatus.Passed, "SQLite managed runtime can open an in-memory connection.", "No Unity Editor required."));
            }
            catch (Exception ex)
            {
                checks.Add(Check("SQLite Dependencies", LocalProductReadinessCheckStatus.Failed, "SQLite managed runtime is unavailable.", SafeException(ex)));
            }

            TryCheck(checks, "Import Workflow", () =>
            {
                _ = new DataSourceImportWorkflowService(_connectionFactory);
                return Check("Import Workflow", LocalProductReadinessCheckStatus.Passed, "Safe CSV import workflow can be constructed.", "No external APIs required.");
            });

            TryCheck(checks, "Recruitment Centre", () =>
            {
                var result = new RecruitmentCentreQueryService(_connectionFactory).Query(new RecruitmentCentreQuery { Limit = 1 });
                return Check("Recruitment Centre", LocalProductReadinessCheckStatus.Passed, "Recruitment Centre can query persisted safe data.", "Rows=" + result.Players.Count.ToString(CultureInfo.InvariantCulture));
            });

            TryCheck(checks, "Player Profile", () =>
            {
                var recruitment = new RecruitmentCentreQueryService(_connectionFactory).Query(new RecruitmentCentreQuery { Limit = 1 });
                if (recruitment.Players.Count == 0)
                {
                    return Check("Player Profile", LocalProductReadinessCheckStatus.Skipped, "No imported players yet; Player Profile load was skipped.", "No fake player was created.");
                }

                var profile = new PlayerProfileQueryService(_connectionFactory).Query(new PlayerProfileQuery { StatlynPlayerId = recruitment.Players[0].StatlynPlayerId });
                return Check("Player Profile", profile.Success ? LocalProductReadinessCheckStatus.Passed : LocalProductReadinessCheckStatus.Warning, profile.Success ? "Player Profile can load an imported safe player." : profile.SafeMessage, "StatlynPlayerId resolved from Recruitment Centre.");
            });

            TryCheck(checks, "Shortlists", () =>
            {
                var page = new ShortlistWorkflowService(_connectionFactory).BuildPageViewModel(includeArchived: false);
                shortlistCount = page.Shortlists.Count;
                hasShortlists = shortlistCount > 0;
                return Check("Shortlists", LocalProductReadinessCheckStatus.Passed, "Shortlists repository can load.", "Shortlists=" + shortlistCount.ToString(CultureInfo.InvariantCulture));
            });

            TryCheck(checks, "Scout Desk", () =>
            {
                var page = new ScoutDeskWorkflowService(_connectionFactory).BuildPageViewModel(new ScoutDeskQuery());
                scoutReportCount = CountRows("ScoutReport");
                hasScoutReports = scoutReportCount > 0;
                return Check("Scout Desk", LocalProductReadinessCheckStatus.Passed, "Scout Desk repository can load.", "Assignments=" + page.Assignments.Count.ToString(CultureInfo.InvariantCulture));
            });

            TryCheck(checks, "Role Lab", () =>
            {
                var page = new RoleLabWorkflowService(_connectionFactory).BuildPageViewModel(includeArchived: false);
                roleLabTemplateCount = page.Roles.Count;
                hasRoleLabTemplates = roleLabTemplateCount > 0;
                return Check("Role Lab", LocalProductReadinessCheckStatus.Passed, "Role Lab repository can load.", "Roles=" + roleLabTemplateCount.ToString(CultureInfo.InvariantCulture));
            });

            TryCheck(checks, "Benchmarks", () =>
            {
                var page = new BenchmarkWorkflowService(_connectionFactory).BuildPageViewModel();
                benchmarkDefinitionCount = page.Definitions.Count;
                hasBenchmarkDefinitions = benchmarkDefinitionCount > 0;
                return Check("Benchmarks", LocalProductReadinessCheckStatus.Passed, "Benchmark repository can load generic/import definitions.", "Definitions=" + benchmarkDefinitionCount.ToString(CultureInfo.InvariantCulture));
            });

            checks.Add(Check("Smoke Test Service", LocalProductReadinessCheckStatus.Passed, "Full smoke test service is available.", typeof(UnitySmokeTestService).Name));
            checks.Add(Check("FM26 Status", LocalProductReadinessCheckStatus.Warning, "FM26 remains unsupported until validated memory maps exist.", "No live FM26 data."));

            return new LocalProductReadinessResult(
                checks,
                _connectionFactory.DatabasePath,
                fixturePath,
                schemaVersion,
                hasImportedPlayers,
                hasShortlists,
                hasScoutReports,
                hasRoleLabTemplates,
                hasBenchmarkDefinitions,
                importedPlayerCount,
                shortlistCount,
                scoutReportCount,
                roleLabTemplateCount,
                benchmarkDefinitionCount);
        }

        private static LocalProductReadinessCheck Check(string name, LocalProductReadinessCheckStatus status, string safeMessage, string technicalDetail)
        {
            return new LocalProductReadinessCheck(name, status, safeMessage, technicalDetail);
        }

        private static void TryCheck(System.Collections.Generic.ICollection<LocalProductReadinessCheck> checks, string name, Func<LocalProductReadinessCheck> action)
        {
            try
            {
                checks.Add(action());
            }
            catch (Exception ex)
            {
                checks.Add(Check(name, LocalProductReadinessCheckStatus.Failed, name + " could not load safely.", SafeException(ex)));
            }
        }

        private int CountRows(string tableName)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + tableName + ";";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static string SafeException(Exception ex)
        {
            return ex.GetType().Name + ": " + ex.Message;
        }
    }
}
