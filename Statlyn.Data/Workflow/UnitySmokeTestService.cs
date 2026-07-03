using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.Data.RoleLab;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;

namespace Statlyn.Data.Workflow
{
    public sealed class UnitySmokeTestService
    {
        private static readonly string[] ExpectedSteps =
        {
            "Managed Statlyn assemblies",
            "SQLite dependencies",
            "SQLite temp database",
            "Resolve database path",
            "Initialize smoke-test database",
            "Locate synthetic fixture CSV",
            "Preview synthetic CSV",
            "Import synthetic CSV",
            "Query Recruitment Centre",
            "Load first Player Profile",
            "Add first player to shortlist",
            "Create scout assignment",
            "Submit sanitized scout report",
            "Seed Role Lab roles",
            "Seed and run benchmark definitions",
            "Smoke-test report"
        };

        public UnitySmokeTestResult Run(UnitySmokeTestOptions? options)
        {
            options = options ?? new UnitySmokeTestOptions();
            var started = DateTimeOffset.UtcNow;
            var steps = new List<UnitySmokeTestStep>();
            var warnings = new List<string>
            {
                "Smoke test uses synthetic CSV fixture data only.",
                "FM26 is not required and no live FM26 data is claimed.",
                "Smoke test database is separate from the main runtime database."
            };
            var errors = new List<string>();
            var resolver = new StatlynDatabasePathResolver();
            var databasePath = string.Empty;
            var fixturePath = string.Empty;
            StatlynDbConnectionFactory? factory = null;
            RecruitmentCentreResult? recruitment = null;
            PlayerProfileResult? profile = null;
            ShortlistWorkflowResult? shortlistResult = null;
            ScoutDeskWorkflowResult? assignmentResult = null;

            AddStep(steps, "Managed Statlyn assemblies", () =>
            {
                var loaded = CheckAssemblies(new[]
                {
                    "Statlyn.Core",
                    "Statlyn.DataProviders",
                    "Statlyn.Scouting",
                    "Statlyn.Analytics",
                    "Statlyn.Data",
                    "Statlyn.UI"
                });
                return loaded
                    ? StepPassed("Managed Statlyn assemblies are available.", "No FM26 process is required.")
                    : StepFailed("Managed Statlyn assemblies are missing.", "Run tools/copy-managed-to-unity.ps1 before opening Unity.");
            });

            AddStep(steps, "SQLite dependencies", () =>
            {
                var loaded = CheckAssemblies(new[]
                {
                    "Microsoft.Data.Sqlite",
                    "SQLitePCLRaw.core",
                    "SQLitePCLRaw.batteries_v2",
                    "SQLitePCLRaw.provider.e_sqlite3"
                });
                return loaded
                    ? StepPassed("SQLite managed dependencies are available.", "Native dependency is checked by opening a temp database.")
                    : StepFailed("SQLite managed dependencies are missing.", "Copy managed dependencies into Unity.");
            });

            AddStep(steps, "SQLite temp database", () =>
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

                return StepPassed("SQLite temp database initialized.", "e_sqlite3 can be used by Microsoft.Data.Sqlite.");
            });

            AddStep(steps, "Resolve database path", () =>
            {
                databasePath = resolver.ResolveSmokeTestPath(options.TemporaryRoot);
                if (string.IsNullOrWhiteSpace(databasePath))
                {
                    return StepFailed("Smoke-test database path could not be resolved.", string.Empty);
                }

                if (!string.IsNullOrWhiteSpace(options.MainDatabasePath) &&
                    string.Equals(Path.GetFullPath(databasePath), Path.GetFullPath(options.MainDatabasePath), StringComparison.OrdinalIgnoreCase))
                {
                    return StepFailed("Smoke-test database path must differ from the main database path.", databasePath);
                }

                return StepPassed("Smoke-test database path resolved.", databasePath);
            });

            AddStep(steps, "Initialize smoke-test database", () =>
            {
                if (string.IsNullOrWhiteSpace(databasePath))
                {
                    return StepSkipped("Database path was unavailable.", string.Empty);
                }

                if (options.ClearSmokeTestDatabase)
                {
                    DeleteDatabaseFiles(databasePath);
                }

                factory = RuntimeDatabaseFactory.CreateFile(databasePath);
                var diagnostics = new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics();
                return diagnostics.SchemaVersion == StatlynSchemaVersion.Current
                    ? StepPassed("Smoke-test database initialized.", "Schema version " + diagnostics.SchemaVersion.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Smoke-test database schema was not current.", "Schema version " + diagnostics.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            });

            AddStep(steps, "Locate synthetic fixture CSV", () =>
            {
                var fixture = new UnityFixtureCsvPathResolver().Resolve(options.ApplicationDataPath, options.StreamingAssetsPath);
                fixturePath = fixture.FilePath;
                return fixture.Success
                    ? StepPassed("Synthetic fixture CSV found.", fixture.FilePath)
                    : StepFailed("Synthetic fixture CSV was not found.", fixture.Message + " Checked: " + string.Join(" | ", fixture.CandidatePaths));
            });

            AddStep(steps, "Preview synthetic CSV", () =>
            {
                if (factory == null || string.IsNullOrWhiteSpace(fixturePath))
                {
                    return StepSkipped("Smoke-test database or fixture path was unavailable.", string.Empty);
                }

                var result = new DataSourceImportWorkflowService(factory).Preview(BuildImportRequest(fixturePath));
                return result.Success
                    ? StepPassed("Synthetic CSV preview succeeded.", "Rows detected: " + (result.Preview == null ? 0 : result.Preview.RowsDetected).ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Synthetic CSV preview failed.", string.Join(" | ", result.ErrorMessages));
            });

            AddStep(steps, "Import synthetic CSV", () =>
            {
                if (factory == null || string.IsNullOrWhiteSpace(fixturePath))
                {
                    return StepSkipped("Smoke-test database or fixture path was unavailable.", string.Empty);
                }

                var result = new DataSourceImportWorkflowService(factory).Import(BuildImportRequest(fixturePath));
                return result.Success && result.ImportResultViewModel != null && result.ImportResultViewModel.RowsAccepted > 0
                    ? StepPassed("Synthetic CSV safe import succeeded.", "Rows accepted: " + result.ImportResultViewModel.RowsAccepted.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Synthetic CSV safe import failed.", string.Join(" | ", result.ErrorMessages));
            });

            AddStep(steps, "Query Recruitment Centre", () =>
            {
                if (factory == null)
                {
                    return StepSkipped("Smoke-test database was unavailable.", string.Empty);
                }

                recruitment = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { Limit = 10, SortBy = "DisplayName", SortDirection = "Ascending" });
                return recruitment.Players.Count > 0
                    ? StepPassed("Recruitment Centre returned imported players.", "Players: " + recruitment.Players.Count.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Recruitment Centre returned no imported players.", recruitment.SafeMessage);
            });

            AddStep(steps, "Load first Player Profile", () =>
            {
                if (factory == null || recruitment == null || recruitment.Players.Count == 0)
                {
                    return StepSkipped("No imported Recruitment Centre player was available.", string.Empty);
                }

                profile = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = recruitment.Players[0].StatlynPlayerId, IncludeBlockedAudit = true });
                return profile.Success
                    ? StepPassed("First Player Profile loaded from persisted safe data.", "No live FM26 data: " + (!profile.IsLiveFm26Data).ToString(CultureInfo.InvariantCulture))
                    : StepFailed("First Player Profile could not be loaded.", string.Join(" | ", profile.Errors));
            });

            AddStep(steps, "Add first player to shortlist", () =>
            {
                if (factory == null || profile == null || !profile.Success || profile.Player == null)
                {
                    return StepSkipped("No safe player profile was available.", string.Empty);
                }

                shortlistResult = new ShortlistWorkflowService(factory).AddToShortlist(new ShortlistAddPlayerRequest
                {
                    ShortlistName = ShortlistWorkflowService.DefaultShortlistName,
                    CreateShortlistIfMissing = true,
                    StatlynPlayerId = profile.Player.StatlynPlayerId,
                    AddedReason = "Added by Unity smoke test using synthetic fixture data."
                });
                return shortlistResult.Success && shortlistResult.PlayerSummary != null
                    ? StepPassed("First player added to shortlist.", "Shortlist: " + (shortlistResult.Shortlist == null ? ShortlistWorkflowService.DefaultShortlistName : shortlistResult.Shortlist.Name))
                    : StepFailed("First player could not be added to shortlist.", string.Join(" | ", shortlistResult.Errors));
            });

            AddStep(steps, "Create scout assignment", () =>
            {
                if (factory == null || shortlistResult == null || shortlistResult.PlayerSummary == null)
                {
                    return StepSkipped("No shortlist player was available.", string.Empty);
                }

                assignmentResult = new ScoutDeskWorkflowService(factory).CreateAssignmentFromShortlistPlayer(
                    shortlistResult.PlayerSummary.ShortlistPlayer.Id,
                    "Smoke Test Scout",
                    DateTimeOffset.UtcNow.AddDays(7));
                return assignmentResult.Success && assignmentResult.Assignment != null
                    ? StepPassed("Scout assignment created.", "Assignment id: " + assignmentResult.Assignment.Id.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Scout assignment could not be created.", string.Join(" | ", assignmentResult.Errors));
            });

            AddStep(steps, "Submit sanitized scout report", () =>
            {
                if (factory == null || assignmentResult == null || assignmentResult.Assignment == null)
                {
                    return StepSkipped("No scout assignment was available.", string.Empty);
                }

                var reportResult = new ScoutDeskWorkflowService(factory).SubmitReport(new SubmitScoutReportRequest
                {
                    AssignmentId = assignmentResult.Assignment.Id,
                    StatlynPlayerId = assignmentResult.Assignment.StatlynPlayerId,
                    RoleAssessed = assignmentResult.Assignment.RoleName,
                    TechnicalRating = ScoutObservationRating.Average,
                    TacticalRating = ScoutObservationRating.Average,
                    PhysicalRating = ScoutObservationRating.Average,
                    MentalRating = ScoutObservationRating.Unknown,
                    OverallRecommendation = ScoutReportRecommendation.ScoutFurther,
                    Confidence = 55,
                    Strengths = "Synthetic smoke-test note: visible output sample can be reviewed.",
                    Weaknesses = "Synthetic smoke-test note: sample context still needs review.",
                    Risks = "Synthetic smoke-test note: no hidden values used.",
                    FollowUpAction = ScoutFollowUpAction.WatchMore,
                    FinalSummary = "Synthetic smoke-test scout report stored as qualitative local notes.",
                    UpdateShortlistFromReport = true
                });
                return reportResult.Success && reportResult.Report != null
                    ? StepPassed("Sanitized scout report submitted.", "Report id: " + reportResult.Report.Id.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Sanitized scout report could not be submitted.", string.Join(" | ", reportResult.Errors));
            });

            AddStep(steps, "Seed Role Lab roles", () =>
            {
                if (factory == null)
                {
                    return StepSkipped("Smoke-test database was unavailable.", string.Empty);
                }

                var result = new RoleLabSeedService(factory).SeedBuiltInRoles();
                return result.TotalRoles > 0
                    ? StepPassed("Role Lab roles seeded.", "Roles: " + result.TotalRoles.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Role Lab roles were not seeded.", result.SafeMessage);
            });

            AddStep(steps, "Seed and run benchmark definitions", () =>
            {
                if (factory == null)
                {
                    return StepSkipped("Smoke-test database was unavailable.", string.Empty);
                }

                var workflow = new BenchmarkWorkflowService(factory);
                var seed = workflow.SeedDefaultDefinitions();
                var run = workflow.RunAllActiveDefinitions();
                return seed.ActiveSeedDefinitionCount > 0 && run.DefinitionsRun > 0
                    ? StepPassed("Benchmark definitions seeded and run.", "Definitions: " + seed.ActiveSeedDefinitionCount.ToString(CultureInfo.InvariantCulture) + "; runs: " + run.DefinitionsRun.ToString(CultureInfo.InvariantCulture))
                    : StepFailed("Benchmark definitions were not run.", seed.SafeMessage + " " + run.SafeMessage);
            });

            AddStep(steps, "Smoke-test report", () =>
            {
                var failed = steps.Count(step => step.Status == UnitySmokeTestStepStatus.Failed);
                return failed == 0
                    ? StepPassed("Full local smoke test passed without FM26.", "CSV-only workflow completed against smoke-test database.")
                    : StepFailed("Full local smoke test found issues.", "Failed steps: " + failed.ToString(CultureInfo.InvariantCulture));
            });

            if (factory != null)
            {
                factory.Dispose();
            }

            foreach (var step in steps)
            {
                if (step.Status == UnitySmokeTestStepStatus.Failed)
                {
                    errors.Add(step.StepName + ": " + step.SafeMessage);
                }
            }

            var completed = DateTimeOffset.UtcNow;
            var success = errors.Count == 0 && steps.All(step => step.Status == UnitySmokeTestStepStatus.Passed);
            return new UnitySmokeTestResult(
                success,
                started,
                completed,
                databasePath,
                fixturePath,
                steps,
                warnings,
                errors,
                success
                    ? "Unity smoke test passed for the CSV-only local workflow."
                    : "Unity smoke test needs attention. Review failed steps.");
        }

        public static IReadOnlyList<string> ExpectedStepNames()
        {
            return ExpectedSteps;
        }

        private static DataSourceImportRequest BuildImportRequest(string fixturePath)
        {
            return new DataSourceImportRequest
            {
                CsvPath = fixturePath,
                SourceName = "Synthetic CSV smoke-test fixture",
                LicenceStatus = "synthetic test fixture",
                AllowedUsage = "development smoke-test fixture only",
                IsLicensed = true,
                SourceConfidence = 80,
                PermitsPlayerImages = false,
                PermitsProviderFlags = false,
                UsesBundledSafeFlagAssets = true,
                PermitsClubBadges = false,
                AllowsExport = true
            };
        }

        private static bool CheckAssemblies(IReadOnlyList<string> assemblyNames)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assemblyName in assemblyNames)
            {
                if (loaded.Any(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    Assembly.Load(assemblyName);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddStep(IList<UnitySmokeTestStep> steps, string stepName, Func<UnitySmokeTestStepDraft> run)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var draft = run();
                watch.Stop();
                steps.Add(new UnitySmokeTestStep(stepName, draft.Status, draft.SafeMessage, draft.TechnicalDetail, watch.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                watch.Stop();
                steps.Add(new UnitySmokeTestStep(
                    stepName,
                    UnitySmokeTestStepStatus.Failed,
                    "Smoke-test step failed safely.",
                    ex.GetType().Name + ": " + ex.Message,
                    watch.ElapsedMilliseconds));
            }
        }

        private static UnitySmokeTestStepDraft StepPassed(string safeMessage, string technicalDetail)
        {
            return new UnitySmokeTestStepDraft(UnitySmokeTestStepStatus.Passed, safeMessage, technicalDetail);
        }

        private static UnitySmokeTestStepDraft StepFailed(string safeMessage, string technicalDetail)
        {
            return new UnitySmokeTestStepDraft(UnitySmokeTestStepStatus.Failed, safeMessage, technicalDetail);
        }

        private static UnitySmokeTestStepDraft StepSkipped(string safeMessage, string technicalDetail)
        {
            return new UnitySmokeTestStepDraft(UnitySmokeTestStepStatus.Skipped, safeMessage, technicalDetail);
        }

        private static void DeleteDatabaseFiles(string databasePath)
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

        private sealed class UnitySmokeTestStepDraft
        {
            public UnitySmokeTestStepDraft(UnitySmokeTestStepStatus status, string safeMessage, string technicalDetail)
            {
                Status = status;
                SafeMessage = DiagnosticSanitizer.Sanitize(safeMessage ?? string.Empty);
                TechnicalDetail = DiagnosticSanitizer.Sanitize(technicalDetail ?? string.Empty);
            }

            public UnitySmokeTestStepStatus Status { get; }

            public string SafeMessage { get; }

            public string TechnicalDetail { get; }
        }
    }
}
