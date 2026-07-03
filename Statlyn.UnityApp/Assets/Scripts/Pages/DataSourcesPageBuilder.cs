using System;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Workflow;
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
            var databasePath = Path.Combine(Application.persistentDataPath, "statlyn.db");
            BuildHeader(main, databasePath, out var status);

            var safety = new VisualElement();
            safety.AddToClassList("dashboard-grid");
            main.Add(safety);
            safety.Add(StatlynUiFactory.MakeCard("CSV Local Import Only", new[] { "Manual local path", "No network sources" }));
            safety.Add(StatlynUiFactory.MakeCard("No Live FM26 Data", new[] { "FM26 remains unsupported", "No fake live player rows" }));
            safety.Add(StatlynUiFactory.MakeCard("No Scraping Or APIs", new[] { "No FotMob scraping", "No external API calls" }));
            safety.Add(StatlynUiFactory.MakeCard("Preview Safety", new[] { "Preview does not store data", "No row values shown" }));
            safety.Add(StatlynUiFactory.MakeCard("Import Safety", new[] { "Stores masked fields only", "Forbidden values stay blocked" }));

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
            actions.AddToClassList("action-row");
            form.Add(actions);
            var useFixture = new Button { text = "Use synthetic fixture CSV" };
            var runtimeCheck = new Button { text = "Run Runtime Check" };
            var preview = new Button { text = "Preview CSV" };
            var import = new Button { text = "Run Safe Import" };
            var clear = new Button { text = "Clear" };
            actions.Add(useFixture);
            actions.Add(runtimeCheck);
            actions.Add(preview);
            actions.Add(import);
            actions.Add(clear);

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
            main.Add(header);
            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            header.Add(titleStack);
            var title = new Label("Data Sources");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("CSV local import only - no scraping, APIs or live FM26 data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);
            status = new Label("Active database: " + databasePath);
            status.AddToClassList("status-pill");
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
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("Active Database", new[] { "Status: local SQLite path selected", "Path: " + databasePath }));
            cards.Add(StatlynUiFactory.MakeCard("Import Scope", new[] { "Source type: CSV only", "Network sources: disabled", "FM26 live data: unsupported" }));
            cards.Add(StatlynUiFactory.MakeCard("Preview", new[] { "File readable: not checked", "Columns detected: 0", "Rows detected: 0" }));
            cards.Add(StatlynUiFactory.MakeCard("Import Result", new[] { "No safe import has been run", "Stored data: masked fields only" }));
        }

        private static void RenderRuntimeDiagnosticsPanel(VisualElement diagnostics, string databasePath, UnityRuntimeCheckResult result, string lastPreviewResult, string lastImportResult, string lastRuntimeException)
        {
            diagnostics.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            diagnostics.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("Managed Assemblies", new[] { result == null ? "Not checked" : BoolText(result.AssembliesOk) }));
            cards.Add(StatlynUiFactory.MakeCard("SQLite Managed", new[] { result == null ? "Not checked" : BoolText(result.SqliteManagedOk) }));
            cards.Add(StatlynUiFactory.MakeCard("SQLite Native", new[] { result == null ? "Not checked" : BoolText(result.SqliteNativeOk) }));
            cards.Add(StatlynUiFactory.MakeCard("Database Init", new[] { result == null ? "Not checked" : BoolText(result.DatabaseInitOk) }));
            cards.Add(StatlynUiFactory.MakeCard("Database Path", new[] { databasePath }));
            cards.Add(StatlynUiFactory.MakeCard("Last Self-Check", new[] { result == null ? "Not run" : result.Success ? "Passed" : "Failed", result == null ? string.Empty : result.CheckedAtUtc.ToString("u", CultureInfo.InvariantCulture) }));
            cards.Add(StatlynUiFactory.MakeCard("Last Preview Result", new[] { string.IsNullOrWhiteSpace(lastPreviewResult) ? "Not run" : lastPreviewResult }));
            cards.Add(StatlynUiFactory.MakeCard("Last Import Result", new[] { string.IsNullOrWhiteSpace(lastImportResult) ? "Not run" : lastImportResult }));
            cards.Add(StatlynUiFactory.MakeCard("Last Runtime Exception", new[] { string.IsNullOrWhiteSpace(lastRuntimeException) ? "None" : lastRuntimeException }));

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
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("Active Database", new[] { "Path: " + databasePath }));
            cards.Add(StatlynUiFactory.MakeCard("Import Result", new[] { "Not completed" }));
            cards.Add(StatlynUiFactory.MakeCard("Last Error", new[] { ex.GetType().Name + ": " + ex.Message }));
        }

        private static void RenderFixtureResolutionFailure(VisualElement results, FixtureCsvPathResolutionResult fixture)
        {
            results.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            results.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("Synthetic Fixture CSV", new[] { "Not found", fixture.Message }));
            cards.Add(StatlynUiFactory.MakeCard("Import Scope", new[] { "Fixture remains synthetic only", "No live FM26 data" }));
            results.Add(StatlynUiFactory.MakeMessages("Fixture Paths Checked", fixture.CandidatePaths));
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
