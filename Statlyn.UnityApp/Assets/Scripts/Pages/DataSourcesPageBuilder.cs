using System;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Workflow;
using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class DataSourcesPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            BuildHeader(main, databasePath, out var status);

            var safety = new VisualElement();
            safety.AddToClassList("dashboard-grid");
            safety.AddToClassList("command-kpi-row");
            main.Add(safety);
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("CSV Local Import", "Manual path", "Network sources disabled", CommandStatusCategory.Info));
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("Live FM26", "Unsupported", "No live player rows", CommandStatusCategory.Warning));
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("External Sources", "Off", "No scraping or API calls", CommandStatusCategory.Success));
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("Preview", "Read-only", "No row values stored", CommandStatusCategory.Success));
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("Import", "Masked fields", "Forbidden values stay blocked", CommandStatusCategory.Success));

            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            main.Add(form);

            var sourceType = new TextField("Source type");
            sourceType.value = "CSV";
            sourceType.SetEnabled(false);
            form.Add(sourceType);

            var csvPath = new TextField("CSV path");
            form.Add(csvPath);
            var sourceName = new TextField("Source name");
            sourceName.value = "Synthetic CSV fixture";
            form.Add(sourceName);
            var licenceStatus = new TextField("Licence status");
            licenceStatus.value = "synthetic test fixture";
            form.Add(licenceStatus);
            var allowedUsage = new TextField("Allowed usage");
            allowedUsage.value = "development fixture only";
            form.Add(allowedUsage);
            var confidence = new TextField("Source confidence");
            confidence.value = "80";
            form.Add(confidence);

            var toggleGrid = new VisualElement();
            toggleGrid.AddToClassList("permission-grid");
            form.Add(toggleGrid);
            var isLicensed = StatlynUiFactory.MakeToggle("Is licensed / permitted", true);
            var permitsImages = StatlynUiFactory.MakeToggle("Permits player images", false);
            var permitsFlags = StatlynUiFactory.MakeToggle("Permits provider flags", false);
            var safeFlags = StatlynUiFactory.MakeToggle("Uses bundled safe flag assets", true);
            var permitsBadges = StatlynUiFactory.MakeToggle("Permits club badges", false);
            var allowsExport = StatlynUiFactory.MakeToggle("Allows export", true);
            toggleGrid.Add(isLicensed);
            toggleGrid.Add(permitsImages);
            toggleGrid.Add(permitsFlags);
            toggleGrid.Add(safeFlags);
            toggleGrid.Add(permitsBadges);
            toggleGrid.Add(allowsExport);

            var actions = new VisualElement();
            var useFixture = new Button { text = "Use synthetic fixture CSV" };
            var runtimeCheck = new Button { text = "Run Runtime Check" };
            var preview = new Button { text = "Preview CSV" };
            var import = new Button { text = "Run Safe Import" };
            var clear = new Button { text = "Clear" };
            actions = StatlynUiFactory.MakeCommandActionButtonRow(useFixture, runtimeCheck, preview, import, clear);
            form.Add(actions);

            var results = new VisualElement();
            results.AddToClassList("data-source-results");
            main.Add(results);
            var runtimeDiagnostics = new VisualElement();
            runtimeDiagnostics.AddToClassList("data-source-results");
            main.Add(runtimeDiagnostics);

            UnityRuntimeCheckResult lastRuntimeCheck = null;
            var lastPreviewResult = "Not run";
            var lastImportResult = "Not run";
            var lastRuntimeException = "None";
            RenderDataSourcePlaceholder(results, databasePath);
            RenderRuntimeDiagnosticsPanel(runtimeDiagnostics, databasePath, lastRuntimeCheck, lastPreviewResult, lastImportResult, lastRuntimeException);

            useFixture.clicked += () =>
            {
                var fixture = ResolveSyntheticFixtureCsvPath();
                if (fixture.Success)
                {
                    csvPath.value = fixture.FilePath;
                    lastRuntimeException = "None";
                    lastPreviewResult = "Synthetic fixture path selected.";
                }
                else
                {
                    lastRuntimeException = fixture.Message;
                    lastPreviewResult = "Synthetic fixture path was not found.";
                    RenderFixtureResolutionFailure(results, fixture);
                }

                RenderRuntimeDiagnosticsPanel(runtimeDiagnostics, databasePath, lastRuntimeCheck, lastPreviewResult, lastImportResult, lastRuntimeException);
            };

            runtimeCheck.clicked += () =>
            {
                lastRuntimeCheck = RunRuntimeCheck(databasePath);
                lastRuntimeException = lastRuntimeCheck.Errors.Count == 0 ? "None" : string.Join(" | ", lastRuntimeCheck.Errors);
                RenderRuntimeDiagnosticsPanel(runtimeDiagnostics, databasePath, lastRuntimeCheck, lastPreviewResult, lastImportResult, lastRuntimeException);
            };

            preview.clicked += () =>
            {
                var request = BuildRequest(csvPath, sourceName, licenceStatus, allowedUsage, confidence, isLicensed, permitsImages, permitsFlags, safeFlags, permitsBadges, allowsExport);
                var result = RunPreview(databasePath, request, results, status, out var runtimeException);
                lastPreviewResult = result == null ? "Preview failed before result was created." : result.SafeMessage;
                lastRuntimeException = runtimeException;
                RenderRuntimeDiagnosticsPanel(runtimeDiagnostics, databasePath, lastRuntimeCheck, lastPreviewResult, lastImportResult, lastRuntimeException);
            };

            import.clicked += () =>
            {
                var request = BuildRequest(csvPath, sourceName, licenceStatus, allowedUsage, confidence, isLicensed, permitsImages, permitsFlags, safeFlags, permitsBadges, allowsExport);
                var result = RunImport(databasePath, request, results, status, out var runtimeException);
                lastImportResult = result == null ? "Import failed before result was created." : result.SafeMessage;
                lastRuntimeException = runtimeException;
                RenderRuntimeDiagnosticsPanel(runtimeDiagnostics, databasePath, lastRuntimeCheck, lastPreviewResult, lastImportResult, lastRuntimeException);
            };

            clear.clicked += () =>
            {
                csvPath.value = string.Empty;
                lastPreviewResult = "Not run";
                lastImportResult = "Not run";
                lastRuntimeException = "None";
                RenderDataSourcePlaceholder(results, databasePath);
                RenderRuntimeDiagnosticsPanel(runtimeDiagnostics, databasePath, lastRuntimeCheck, lastPreviewResult, lastImportResult, lastRuntimeException);
            };
        }

        private static void BuildHeader(VisualElement main, string databasePath, out Label status)
        {
            var header = new VisualElement();
            header.AddToClassList("header");
            header.AddToClassList("command-page-header");
            main.Add(header);

            var headerBrand = new VisualElement();
            headerBrand.AddToClassList("header-brand");
            header.Add(headerBrand);

            var logo = StatlynUiFactory.MakeLogoImage(StatlynUiFactory.DarkLogoResourceKey, "header-logo");
            if (logo != null)
            {
                headerBrand.Add(logo);
            }

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            headerBrand.Add(titleStack);
            var title = new Label("Data Sources");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("CSV local import only - no scraping, APIs or live FM26 data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            status = StatlynUiFactory.MakeCommandStatusPill("Active database: " + databasePath, CommandStatusCategory.Info);
            header.Add(status);
        }

        private static DataSourceImportRequest BuildRequest(TextField csvPath, TextField sourceName, TextField licenceStatus, TextField allowedUsage, TextField confidence, Toggle isLicensed, Toggle permitsImages, Toggle permitsFlags, Toggle safeFlags, Toggle permitsBadges, Toggle allowsExport)
        {
            var sourceConfidence = 80;
            if (int.TryParse(confidence.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedConfidence))
            {
                sourceConfidence = parsedConfidence;
            }

            return new DataSourceImportRequest
            {
                CsvPath = csvPath.value,
                SourceName = sourceName.value,
                LicenceStatus = licenceStatus.value,
                AllowedUsage = allowedUsage.value,
                IsLicensed = isLicensed.value,
                SourceConfidence = sourceConfidence,
                PermitsPlayerImages = permitsImages.value,
                PermitsProviderFlags = permitsFlags.value,
                UsesBundledSafeFlagAssets = safeFlags.value,
                PermitsClubBadges = permitsBadges.value,
                AllowsExport = allowsExport.value
            };
        }

        private static DataSourceImportWorkflowResult RunPreview(string databasePath, DataSourceImportRequest request, VisualElement results, Label status, out string runtimeException)
        {
            runtimeException = "None";
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var workflow = new DataSourceImportWorkflowService(factory);
                    var result = workflow.Preview(request);
                    status.text = result.DatabaseDiagnostics == null ? "Active database: " + databasePath : "Active database: " + result.DatabaseDiagnostics.DatabasePath;
                    RenderPreviewResult(results, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                runtimeException = ex.GetType().Name + ": " + ex.Message;
                RenderRuntimeError(results, databasePath, ex);
                return null;
            }
        }

        private static DataSourceImportWorkflowResult RunImport(string databasePath, DataSourceImportRequest request, VisualElement results, Label status, out string runtimeException)
        {
            runtimeException = "None";
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var workflow = new DataSourceImportWorkflowService(factory);
                    var result = workflow.Import(request);
                    status.text = result.DatabaseDiagnostics == null ? "Active database: " + databasePath : "Active database: " + result.DatabaseDiagnostics.DatabasePath;
                    RenderImportResult(results, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                runtimeException = ex.GetType().Name + ": " + ex.Message;
                RenderRuntimeError(results, databasePath, ex);
                return null;
            }
        }

        private static UnityRuntimeCheckResult RunRuntimeCheck(string databasePath)
        {
            try
            {
                return new UnityRuntimeDependencyCheck().Run(Application.temporaryCachePath, databasePath);
            }
            catch (Exception ex)
            {
                return new UnityRuntimeCheckResult(false, DateTimeOffset.UtcNow, databasePath, false, false, false, false, false, new[] { "Runtime check failed before dependency results could be collected." }, new[] { "Review copied Unity dependencies and SQLite runtime files." }, new[] { ex.GetType().Name + ": " + ex.Message });
            }
        }

        private static void RenderDataSourcePlaceholder(VisualElement results, string databasePath)
        {
            results.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            cards.AddToClassList("command-kpi-row");
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Active Database", "Local SQLite", databasePath, CommandStatusCategory.Info));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Import Scope", "CSV only", "Network sources disabled; FM26 live data unsupported", CommandStatusCategory.Info));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Preview", "Not checked", "No preview values have been stored", CommandStatusCategory.Warning));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Import Result", "Not run", "Stored data will be masked fields only", CommandStatusCategory.Warning));
        }

        private static void RenderRuntimeDiagnosticsPanel(VisualElement diagnostics, string databasePath, UnityRuntimeCheckResult result, string lastPreviewResult, string lastImportResult, string lastRuntimeException)
        {
            diagnostics.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            cards.AddToClassList("command-kpi-row");
            diagnostics.Add(cards);
            cards.Add(MakeRuntimeKpiCard("Managed Assemblies", result == null ? (bool?)null : result.AssembliesOk));
            cards.Add(MakeRuntimeKpiCard("SQLite Managed", result == null ? (bool?)null : result.SqliteManagedOk));
            cards.Add(MakeRuntimeKpiCard("SQLite Native", result == null ? (bool?)null : result.SqliteNativeOk));
            cards.Add(MakeRuntimeKpiCard("Database Init", result == null ? (bool?)null : result.DatabaseInitOk));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Database Path", "Runtime main", databasePath, CommandStatusCategory.Info));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Last Self-Check", result == null ? "Not run" : result.Success ? "Passed" : "Failed", result == null ? "Runtime check has not been run" : result.CheckedAtUtc.ToString("u", CultureInfo.InvariantCulture), result == null ? CommandStatusCategory.Warning : result.Success ? CommandStatusCategory.Success : CommandStatusCategory.Danger));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Last Preview Result", string.IsNullOrWhiteSpace(lastPreviewResult) ? "Not run" : lastPreviewResult, "Preview is read-only", ThemeTokens.ResolveStatusCategory(lastPreviewResult)));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Last Import Result", string.IsNullOrWhiteSpace(lastImportResult) ? "Not run" : lastImportResult, "Safe import stores masked fields", ThemeTokens.ResolveStatusCategory(lastImportResult)));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Last Runtime Exception", string.IsNullOrWhiteSpace(lastRuntimeException) ? "None" : lastRuntimeException, "Shown safely for troubleshooting", ThemeTokens.ResolveStatusCategory(lastRuntimeException)));

            if (result != null)
            {
                diagnostics.Add(StatlynUiFactory.MakeMessages("Runtime Check Messages", result.Messages));
                diagnostics.Add(StatlynUiFactory.MakeMessages("Runtime Check Warnings", result.Warnings));
                diagnostics.Add(StatlynUiFactory.MakeMessages("Runtime Check Errors", result.Errors));
            }
        }

        private static void RenderPreviewResult(VisualElement results, DataSourceImportWorkflowResult result)
        {
            results.Clear();
            var preview = result.PreviewViewModel;
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("File Readable", new[] { result.Preview != null && result.Preview.FileReadable ? "Yes" : "No", preview.FilePath }));
            cards.Add(StatlynUiFactory.MakeCard("Columns Detected", new[] { preview.ColumnRows.Count.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Rows Detected", new[] { preview.RowsDetected.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Mapped Fields", new[] { preview.MappedCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Unknown Fields", new[] { preview.UnknownCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Forbidden Fields", new[] { preview.ForbiddenCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Import Result", new[] { "Preview only", "No data stored" }));
            cards.Add(StatlynUiFactory.MakeCard("Last Error", preview.Errors.Count == 0 ? new[] { "None" } : StatlynUiFactory.ToArray(preview.Errors)));
            results.Add(StatlynUiFactory.MakeMessages("Warnings", preview.Warnings));
            results.Add(MakeColumnPreviewList(preview.ColumnRows));
        }

        private static void RenderImportResult(VisualElement results, DataSourceImportWorkflowResult result)
        {
            results.Clear();
            var preview = result.PreviewViewModel;
            var import = result.ImportResultViewModel;
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("File Readable", new[] { result.Preview != null && result.Preview.FileReadable ? "Yes" : "No", preview.FilePath }));
            cards.Add(StatlynUiFactory.MakeCard("Mapped Fields", new[] { preview.MappedCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Unknown Fields", new[] { preview.UnknownCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Forbidden Fields", new[] { preview.ForbiddenCount.ToString(CultureInfo.InvariantCulture) }));
            if (import == null)
            {
                cards.Add(StatlynUiFactory.MakeCard("Import Result", new[] { "Not started" }));
                cards.Add(StatlynUiFactory.MakeCard("Last Error", StatlynUiFactory.ToArray(result.ErrorMessages)));
            }
            else
            {
                cards.Add(StatlynUiFactory.MakeCard("Import Result", new[] { import.Success ? "Completed" : "Completed with warnings", "Rows accepted: " + import.RowsAccepted.ToString(CultureInfo.InvariantCulture), "Rows rejected: " + import.RowsRejected.ToString(CultureInfo.InvariantCulture), "Fields stored: " + import.FieldsStored.ToString(CultureInfo.InvariantCulture), "Player stats stored: " + import.PlayerStatsStored.ToString(CultureInfo.InvariantCulture), "Physical metrics stored: " + import.PhysicalMetricsStored.ToString(CultureInfo.InvariantCulture), "Blocked fields: " + import.BlockedFields.ToString(CultureInfo.InvariantCulture), "Unknown fields: " + import.UnknownFields.ToString(CultureInfo.InvariantCulture), "Players in database: " + import.DatabasePlayersCount.ToString(CultureInfo.InvariantCulture), "Stats in database: " + import.DatabaseStatsCount.ToString(CultureInfo.InvariantCulture) }));
                cards.Add(StatlynUiFactory.MakeCard("Last Error", import.Errors.Count == 0 ? new[] { "None" } : StatlynUiFactory.ToArray(import.Errors)));
            }

            results.Add(StatlynUiFactory.MakeMessages("Warnings", result.WarningMessages));
            results.Add(MakeColumnPreviewList(preview.ColumnRows));
        }

        private static VisualElement MakeColumnPreviewList(System.Collections.Generic.IReadOnlyList<ColumnPreviewViewModel> rows)
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Column Mapping"));
            if (rows == null || rows.Count == 0)
            {
                panel.Add(new Label("No columns previewed."));
                return panel;
            }

            foreach (var row in rows)
            {
                panel.Add(StatlynUiFactory.MakeDiagnosticRow(row.SourceColumn, row.Status + " - " + (string.IsNullOrWhiteSpace(row.MappedTo) ? row.Category : row.MappedTo) + " - " + row.Reason));
            }

            return panel;
        }

        private static void RenderRuntimeError(VisualElement results, string databasePath, Exception ex)
        {
            results.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            cards.AddToClassList("command-kpi-row");
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Active Database", "Runtime main", databasePath, CommandStatusCategory.Info));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Import Result", "Not completed", "No unsafe fallback was shown", CommandStatusCategory.Danger));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Last Error", ex.GetType().Name, ex.Message, CommandStatusCategory.Danger));
        }

        private static void RenderFixtureResolutionFailure(VisualElement results, FixtureCsvPathResolutionResult fixture)
        {
            results.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            cards.AddToClassList("command-kpi-row");
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Synthetic Fixture CSV", "Not found", fixture.Message, CommandStatusCategory.Warning));
            cards.Add(StatlynUiFactory.MakeCommandKpiCard("Import Scope", "Fixture only", "No live FM26 data", CommandStatusCategory.Info));
            results.Add(StatlynUiFactory.MakeMessages("Fixture Paths Checked", fixture.CandidatePaths));
        }

        private static VisualElement MakeRuntimeKpiCard(string title, bool? value)
        {
            if (!value.HasValue)
            {
                return StatlynUiFactory.MakeCommandKpiCard(title, "Not checked", "Run Runtime Check to verify", CommandStatusCategory.Warning);
            }

            return StatlynUiFactory.MakeCommandKpiCard(title, BoolText(value.Value), value.Value ? "Dependency check passed" : "Review runtime dependency setup", value.Value ? CommandStatusCategory.Success : CommandStatusCategory.Danger);
        }

        private static FixtureCsvPathResolutionResult ResolveSyntheticFixtureCsvPath()
        {
            return new UnityFixtureCsvPathResolver().Resolve(Application.dataPath, Application.streamingAssetsPath);
        }

        private static string BoolText(bool value)
        {
            return value ? "OK" : "Failed";
        }
    }
}
