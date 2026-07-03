using System;
using System.Globalization;
using Statlyn.Data;
using Statlyn.Data.Maintenance;
using Statlyn.Data.Readiness;
using Statlyn.Data.Workflow;
using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class DiagnosticsPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var resolver = new StatlynDatabasePathResolver();
            var mainDatabasePath = resolver.ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            var smokeDatabasePath = resolver.ResolveSmokeTestPath(Application.temporaryCachePath);
            var fixture = new UnityFixtureCsvPathResolver().Resolve(Application.dataPath, Application.streamingAssetsPath);

            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                "Diagnostics",
                "Runtime check and full local smoke test - no FM26 required",
                "CSV-only validation",
                CommandStatusCategory.Info));

            var actions = new VisualElement();
            var runtimeCheckButton = new Button { text = "Run Runtime Check" };
            var smokeTestButton = new Button { text = "Run Full Smoke Test" };
            var readinessButton = new Button { text = "Run Product Readiness Check" };
            var backupButton = new Button { text = "Backup Main Database" };
            var resetSmokeButton = new Button { text = "Reset Smoke-Test Database" };
            actions = StatlynUiFactory.MakeCommandActionButtonRow(runtimeCheckButton, smokeTestButton, readinessButton, backupButton, resetSmokeButton);
            main.Add(actions);

            var summary = new VisualElement();
            summary.AddToClassList("data-source-results");
            main.Add(summary);

            var runtimePanel = new VisualElement();
            runtimePanel.AddToClassList("data-source-results");
            main.Add(runtimePanel);

            var smokePanel = new VisualElement();
            smokePanel.AddToClassList("data-source-results");
            main.Add(smokePanel);
            var readinessPanel = new VisualElement();
            readinessPanel.AddToClassList("data-source-results");
            main.Add(readinessPanel);
            var maintenancePanel = new VisualElement();
            maintenancePanel.AddToClassList("data-source-results");
            main.Add(maintenancePanel);

            UnityRuntimeCheckResult runtimeResult = null;
            UnitySmokeTestResult smokeResult = null;
            LocalProductReadinessResult readinessResult = null;
            LocalDatabaseMaintenanceResult maintenanceResult = null;
            RenderSummary(summary, mainDatabasePath, smokeDatabasePath, fixture);
            RenderRuntime(runtimePanel, runtimeResult);
            RenderSmoke(smokePanel, smokeResult);
            RenderReadiness(readinessPanel, readinessResult);
            RenderMaintenance(maintenancePanel, maintenanceResult);

            runtimeCheckButton.clicked += () =>
            {
                try
                {
                    runtimeResult = new UnityRuntimeDependencyCheck().Run(Application.temporaryCachePath, mainDatabasePath);
                }
                catch (Exception ex)
                {
                    runtimeResult = new UnityRuntimeCheckResult(false, DateTimeOffset.UtcNow, mainDatabasePath, false, false, false, false, false, new[] { "Runtime check failed before results could be collected." }, new[] { "Review copied Unity dependencies." }, new[] { ex.GetType().Name + ": " + ex.Message });
                }

                RenderRuntime(runtimePanel, runtimeResult);
            };

            smokeTestButton.clicked += () =>
            {
                try
                {
                    smokeResult = new UnitySmokeTestService().Run(new UnitySmokeTestOptions
                    {
                        TemporaryRoot = Application.temporaryCachePath,
                        ApplicationDataPath = Application.dataPath,
                        StreamingAssetsPath = Application.streamingAssetsPath,
                        MainDatabasePath = mainDatabasePath,
                        ClearSmokeTestDatabase = true
                    });
                }
                catch (Exception ex)
                {
                    smokeResult = new UnitySmokeTestResult(
                        false,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow,
                        smokeDatabasePath,
                        fixture.FilePath,
                        new[]
                        {
                            new UnitySmokeTestStep("Full smoke test", UnitySmokeTestStepStatus.Failed, "Full smoke test failed safely.", ex.GetType().Name + ": " + ex.Message, 0)
                        },
                        new[] { "Review Unity SQLite dependencies and synthetic fixture copy." },
                        new[] { ex.GetType().Name + ": " + ex.Message },
                        "Unity smoke test needs attention.");
                }

                RenderSmoke(smokePanel, smokeResult);
            };

            readinessButton.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(mainDatabasePath))
                    {
                        readinessResult = new LocalProductReadinessService(factory, Application.dataPath, Application.streamingAssetsPath).Run();
                    }
                }
                catch (Exception ex)
                {
                    readinessResult = new LocalProductReadinessResult(
                        new[] { new LocalProductReadinessCheck("Local Product Readiness", LocalProductReadinessCheckStatus.Failed, "Readiness check could not run safely.", ex.GetType().Name + ": " + ex.Message) },
                        mainDatabasePath,
                        fixture.FilePath,
                        0,
                        false,
                        false,
                        false,
                        false,
                        false);
                }

                RenderReadiness(readinessPanel, readinessResult);
            };

            backupButton.clicked += () =>
            {
                maintenanceResult = new LocalDatabaseMaintenanceService().CreateTimestampedBackupCopy(mainDatabasePath, System.IO.Path.Combine(Application.persistentDataPath, "StatlynBackups"));
                RenderMaintenance(maintenancePanel, maintenanceResult);
            };

            resetSmokeButton.clicked += () =>
            {
                maintenanceResult = new LocalDatabaseMaintenanceService().ResetSmokeTestDatabase(Application.temporaryCachePath);
                RenderMaintenance(maintenancePanel, maintenanceResult);
                RenderSummary(summary, mainDatabasePath, smokeDatabasePath, new UnityFixtureCsvPathResolver().Resolve(Application.dataPath, Application.streamingAssetsPath));
            };
        }

        private static void RenderSummary(VisualElement target, string mainDatabasePath, string smokeDatabasePath, FixtureCsvPathResolutionResult fixture)
        {
            target.Clear();
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            grid.AddToClassList("command-kpi-row");
            target.Add(grid);
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Main Database", "RuntimeMain", mainDatabasePath, CommandStatusCategory.Info));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Smoke Test Database", "RuntimeSmokeTest", smokeDatabasePath, CommandStatusCategory.Info));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Synthetic Fixture", fixture.Success ? "Found" : "Missing", fixture.Success ? fixture.FilePath : fixture.Message, fixture.Success ? CommandStatusCategory.Success : CommandStatusCategory.Warning));
            grid.Add(StatlynUiFactory.MakeSafetyBanner("No FM26 required", "No network or scraping", "No live FM26 data", "Synthetic fixture only"));
        }

        private static void RenderRuntime(VisualElement target, UnityRuntimeCheckResult result)
        {
            target.Clear();
            target.Add(StatlynUiFactory.MakeSectionTitle("Runtime Check"));
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            grid.AddToClassList("command-kpi-row");
            target.Add(grid);
            grid.Add(StatlynUiFactory.MakeRuntimeStatusCard("Managed Assemblies", result == null ? (bool?)null : result.AssembliesOk, result == null ? "Not checked" : result.CheckedAtUtc.ToString("u", CultureInfo.InvariantCulture)));
            grid.Add(StatlynUiFactory.MakeRuntimeStatusCard("SQLite Managed", result == null ? (bool?)null : result.SqliteManagedOk, result == null ? "Not checked" : result.DatabasePath));
            grid.Add(StatlynUiFactory.MakeRuntimeStatusCard("SQLite Native", result == null ? (bool?)null : result.SqliteNativeOk, "SQLite temp open test"));
            grid.Add(StatlynUiFactory.MakeRuntimeStatusCard("Database Init", result == null ? (bool?)null : result.DatabaseInitOk, "Schema check"));
            grid.Add(StatlynUiFactory.MakeRuntimeStatusCard("Workflow Services", result == null ? (bool?)null : result.WorkflowServiceOk, "Data Sources workflow construction"));

            if (result != null)
            {
                target.Add(StatlynUiFactory.MakeMessages("Runtime Messages", result.Messages));
                target.Add(StatlynUiFactory.MakeMessages("Runtime Warnings", result.Warnings));
                target.Add(StatlynUiFactory.MakeMessages("Runtime Errors", result.Errors));
            }
        }

        private static void RenderSmoke(VisualElement target, UnitySmokeTestResult result)
        {
            target.Clear();
            target.Add(StatlynUiFactory.MakeSectionTitle("Full Smoke Test"));
            if (result == null)
            {
                target.Add(StatlynUiFactory.MakeEmptyState("Smoke Test", "Not run yet.", "Runs against a separate smoke-test database.", "No FM26 required."));
                return;
            }

            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            grid.AddToClassList("command-kpi-row");
            target.Add(grid);
            grid.Add(StatlynUiFactory.MakeRuntimeStatusCard("Smoke Test Result", result.Success, result.SafeSummary));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Smoke Test Database", "RuntimeSmokeTest", result.DatabasePath, CommandStatusCategory.Info));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Synthetic Fixture", string.IsNullOrWhiteSpace(result.FixturePath) ? "Unavailable" : "Resolved", string.IsNullOrWhiteSpace(result.FixturePath) ? "Fixture path unavailable" : result.FixturePath, string.IsNullOrWhiteSpace(result.FixturePath) ? CommandStatusCategory.Warning : CommandStatusCategory.Success));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Completed", result.CompletedAtUtc.ToString("u", CultureInfo.InvariantCulture), "Local smoke-test database only", CommandStatusCategory.Info));

            foreach (var step in result.Steps)
            {
                grid.Add(StatlynUiFactory.MakeCommandDataQualityPanel(step.StepName, new[]
                {
                    "Status: " + step.Status,
                    step.SafeMessage,
                    string.IsNullOrWhiteSpace(step.TechnicalDetail) ? "No technical detail." : step.TechnicalDetail,
                    "Duration: " + step.DurationMs.ToString(CultureInfo.InvariantCulture) + " ms"
                }, StepStatusCategory(step.Status)));
            }

            target.Add(StatlynUiFactory.MakeMessages("Smoke Test Warnings", result.Warnings));
            target.Add(StatlynUiFactory.MakeMessages("Smoke Test Errors", result.Errors));
        }

        private static void RenderReadiness(VisualElement target, LocalProductReadinessResult result)
        {
            target.Clear();
            target.Add(StatlynUiFactory.MakeSectionTitle("Local Product Readiness"));
            if (result == null)
            {
                target.Add(StatlynUiFactory.MakeEmptyState("Product Readiness", "Not checked.", "Run Product Readiness Check before a local demo.", "No FM26 required."));
                return;
            }

            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            grid.AddToClassList("command-kpi-row");
            target.Add(grid);
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Readiness", result.Success ? "Passed" : "Needs attention", result.SafeSummary, result.Success ? CommandStatusCategory.Success : CommandStatusCategory.Danger));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Schema", result.SchemaVersion.ToString(CultureInfo.InvariantCulture), "Current schema target: " + StatlynSchemaVersion.Current.ToString(CultureInfo.InvariantCulture), result.SchemaVersion == StatlynSchemaVersion.Current ? CommandStatusCategory.Success : CommandStatusCategory.Warning));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Fixture", string.IsNullOrWhiteSpace(result.FixturePath) ? "Missing" : "Found", string.IsNullOrWhiteSpace(result.FixturePath) ? "Run copy-managed-to-unity or enter CSV manually" : result.FixturePath, string.IsNullOrWhiteSpace(result.FixturePath) ? CommandStatusCategory.Warning : CommandStatusCategory.Success));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Imported Players", result.ImportedPlayerCount.ToString(CultureInfo.InvariantCulture), result.HasImportedPlayers ? "Safe local players exist" : "Awaiting local data.", result.HasImportedPlayers ? CommandStatusCategory.Success : CommandStatusCategory.Muted));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Shortlists", result.ShortlistCount.ToString(CultureInfo.InvariantCulture), result.HasShortlists ? "Local shortlists available" : "No shortlist selected.", result.HasShortlists ? CommandStatusCategory.Info : CommandStatusCategory.Muted));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Scout Reports", result.ScoutReportCount.ToString(CultureInfo.InvariantCulture), result.HasScoutReports ? "Local scout reports available" : "No scout assignments.", result.HasScoutReports ? CommandStatusCategory.Info : CommandStatusCategory.Muted));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Role Lab Templates", result.RoleLabTemplateCount.ToString(CultureInfo.InvariantCulture), result.HasRoleLabTemplates ? "Local Role Lab templates available" : "No Role Lab templates.", result.HasRoleLabTemplates ? CommandStatusCategory.Info : CommandStatusCategory.Muted));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Benchmark Definitions", result.BenchmarkDefinitionCount.ToString(CultureInfo.InvariantCulture), result.HasBenchmarkDefinitions ? "Generic/import definitions available" : "No benchmark definitions.", result.HasBenchmarkDefinitions ? CommandStatusCategory.Info : CommandStatusCategory.Muted));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("FM26", "Unsupported", "No live FM26 data", CommandStatusCategory.Warning));

            foreach (var check in result.Checks)
            {
                grid.Add(StatlynUiFactory.MakeCommandDataQualityPanel(check.Name, new[] { "Status: " + check.Status, check.SafeMessage, check.TechnicalDetail }, ReadinessStatusCategory(check.Status)));
            }
        }

        private static void RenderMaintenance(VisualElement target, LocalDatabaseMaintenanceResult result)
        {
            target.Clear();
            target.Add(StatlynUiFactory.MakeSectionTitle("Database Maintenance"));
            if (result == null)
            {
                target.Add(StatlynUiFactory.MakeEmptyState("Database Maintenance", "No maintenance action has run.", "Backup Main Database copies the SQLite file if it exists.", "Reset Smoke-Test Database does not touch the main database."));
                return;
            }

            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            grid.AddToClassList("command-kpi-row");
            target.Add(grid);
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Maintenance", result.Success ? "Completed" : "Needs attention", result.SafeMessage, result.Success ? CommandStatusCategory.Success : CommandStatusCategory.Warning));
            grid.Add(StatlynUiFactory.MakeCommandKpiCard("Database Path", "Local", result.DatabasePath, CommandStatusCategory.Info));
            if (result.Backup != null)
            {
                grid.Add(StatlynUiFactory.MakeCommandKpiCard("Backup", result.Backup.SourceExisted ? "Created" : "Not created", result.Backup.BackupPath, result.Backup.SourceExisted ? CommandStatusCategory.Success : CommandStatusCategory.Warning));
            }

            target.Add(StatlynUiFactory.MakeMessages("Maintenance Warnings", result.Warnings));
            target.Add(StatlynUiFactory.MakeMessages("Maintenance Errors", result.Errors));
        }

        private static CommandStatusCategory StepStatusCategory(UnitySmokeTestStepStatus status)
        {
            switch (status)
            {
                case UnitySmokeTestStepStatus.Passed:
                    return CommandStatusCategory.Success;
                case UnitySmokeTestStepStatus.Failed:
                    return CommandStatusCategory.Danger;
                default:
                    return CommandStatusCategory.Muted;
            }
        }

        private static CommandStatusCategory ReadinessStatusCategory(LocalProductReadinessCheckStatus status)
        {
            switch (status)
            {
                case LocalProductReadinessCheckStatus.Passed:
                    return CommandStatusCategory.Success;
                case LocalProductReadinessCheckStatus.Failed:
                    return CommandStatusCategory.Danger;
                case LocalProductReadinessCheckStatus.Warning:
                    return CommandStatusCategory.Warning;
                default:
                    return CommandStatusCategory.Muted;
            }
        }
    }
}
