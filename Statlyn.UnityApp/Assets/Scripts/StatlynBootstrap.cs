using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Workflow;
using Statlyn.UI.ProfileFixtures;
using Statlyn.UI.UnityBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp
{
    public sealed class StatlynBootstrap : MonoBehaviour
    {
        private static readonly string[] NavigationItems =
        {
            "Home",
            "Squad",
            "Recruitment",
            "Shortlists",
            "Player Profile",
            "Role Lab",
            "Scout Desk",
            "Alerts",
            "Data Sources",
            "Settings",
            "Diagnostics"
        };

        private UIDocument _document;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeShell()
        {
            if (FindObjectOfType<StatlynBootstrap>() != null)
            {
                return;
            }

            var app = new GameObject("Statlyn App");
            DontDestroyOnLoad(app);
            app.AddComponent<StatlynBootstrap>();
        }

        private void Awake()
        {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Statlyn Runtime Panel";

            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = panelSettings;
        }

        private void Start()
        {
            BuildShell();
        }

        private void BuildShell()
        {
            var root = _document.rootVisualElement;
            root.Clear();
            root.AddToClassList("statlyn-root");

            var style = Resources.Load<StyleSheet>("StatlynTheme");
            if (style != null)
            {
                root.styleSheets.Add(style);
            }

            var sidebar = new VisualElement();
            sidebar.AddToClassList("sidebar");
            root.Add(sidebar);

            var brand = new Label("Statlyn");
            brand.AddToClassList("brand");
            sidebar.Add(brand);

            var main = new VisualElement();
            main.AddToClassList("main");
            root.Add(main);

            foreach (var item in NavigationItems)
            {
                var navItem = item;
                var button = new Button();
                button.text = item;
                button.AddToClassList("nav-button");
                button.clicked += () => ShowPage(main, navItem);
                sidebar.Add(button);
            }

            BuildDashboardPage(main);
        }

        private void ShowPage(VisualElement main, string pageName)
        {
            if (pageName == "Data Sources")
            {
                BuildDataSourcesPage(main);
                return;
            }

            BuildDashboardPage(main);
        }

        private void BuildDashboardPage(VisualElement main)
        {
            main.Clear();
            var header = new VisualElement();
            header.AddToClassList("header");
            main.Add(header);

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            header.Add(titleStack);

            var title = new Label("Recruitment Intelligence");
            title.AddToClassList("screen-title");
            titleStack.Add(title);

            var subtitle = new Label("Fixture mode preview - no live FM26 data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Scouting firewall active");
            status.AddToClassList("status-pill");
            header.Add(status);

            var dashboard = new VisualElement();
            dashboard.AddToClassList("dashboard-grid");
            main.Add(dashboard);

            dashboard.Add(MakeCard("Active Source", new[] { "Mode: Fixture preview", "Status: No live FM26 data", "Players: 1 synthetic preview" }));
            dashboard.Add(MakeCard("Connection", new[] { "FM26 process: Not checked", "Build support: Unsupported until mapped", "Snapshot: Not loaded" }));
            dashboard.Add(MakeCard("Recruitment", new[] { "Squad needs: Awaiting data", "Targets: Awaiting data", "Alerts: 0" }));
            dashboard.Add(MakeCard("Protection", new[] { "Hidden values blocked", "Raw entities blocked from UI", "Low confidence requires scouting" }));

            main.Add(MakePlayerProfileSlice(UnityProfileRenderModel.From(FixtureProfileFactory.CreateDevelopmentPreviewProfile())));

            var diagnostics = MakeDiagnosticsPanel();
            main.Add(diagnostics);
        }

        private void BuildDataSourcesPage(VisualElement main)
        {
            main.Clear();

            var databasePath = Path.Combine(Application.persistentDataPath, "statlyn.db");

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

            var status = new Label("Active database: " + databasePath);
            status.AddToClassList("status-pill");
            header.Add(status);

            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            main.Add(form);

            var sourceType = new TextField("Source type");
            sourceType.value = "CSV";
            sourceType.SetEnabled(false);
            form.Add(sourceType);

            var csvPath = new TextField("CSV path");
            csvPath.value = string.Empty;
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

            var isLicensed = MakeToggle("Is licensed / permitted", true);
            var permitsImages = MakeToggle("Permits player images", false);
            var permitsFlags = MakeToggle("Permits provider flags", false);
            var safeFlags = MakeToggle("Uses bundled safe flag assets", true);
            var permitsBadges = MakeToggle("Permits club badges", false);
            var allowsExport = MakeToggle("Allows export", true);
            toggleGrid.Add(isLicensed);
            toggleGrid.Add(permitsImages);
            toggleGrid.Add(permitsFlags);
            toggleGrid.Add(safeFlags);
            toggleGrid.Add(permitsBadges);
            toggleGrid.Add(allowsExport);

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            form.Add(actions);

            var useFixture = new Button();
            useFixture.text = "Use synthetic fixture CSV";
            useFixture.clicked += () => csvPath.value = ResolveSyntheticFixtureCsvPath();
            actions.Add(useFixture);

            var preview = new Button();
            preview.text = "Preview CSV";
            actions.Add(preview);

            var import = new Button();
            import.text = "Run Safe Import";
            actions.Add(import);

            var clear = new Button();
            clear.text = "Clear";
            actions.Add(clear);

            var results = new VisualElement();
            results.AddToClassList("data-source-results");
            main.Add(results);
            RenderDataSourcePlaceholder(results, databasePath);

            preview.clicked += () =>
            {
                var request = BuildRequest(csvPath, sourceName, licenceStatus, allowedUsage, confidence, isLicensed, permitsImages, permitsFlags, safeFlags, permitsBadges, allowsExport);
                RunPreview(databasePath, request, results, status);
            };

            import.clicked += () =>
            {
                var request = BuildRequest(csvPath, sourceName, licenceStatus, allowedUsage, confidence, isLicensed, permitsImages, permitsFlags, safeFlags, permitsBadges, allowsExport);
                RunImport(databasePath, request, results, status);
            };

            clear.clicked += () =>
            {
                csvPath.value = string.Empty;
                RenderDataSourcePlaceholder(results, databasePath);
            };
        }

        private static Toggle MakeToggle(string label, bool value)
        {
            var toggle = new Toggle(label);
            toggle.value = value;
            return toggle;
        }

        private static DataSourceImportRequest BuildRequest(
            TextField csvPath,
            TextField sourceName,
            TextField licenceStatus,
            TextField allowedUsage,
            TextField confidence,
            Toggle isLicensed,
            Toggle permitsImages,
            Toggle permitsFlags,
            Toggle safeFlags,
            Toggle permitsBadges,
            Toggle allowsExport)
        {
            var sourceConfidence = 80;
            int parsedConfidence;
            if (int.TryParse(confidence.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedConfidence))
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

        private static void RunPreview(string databasePath, DataSourceImportRequest request, VisualElement results, Label status)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var workflow = new DataSourceImportWorkflowService(factory);
                    var result = workflow.Preview(request);
                    status.text = result.DatabaseDiagnostics == null
                        ? "Active database: " + databasePath
                        : "Active database: " + result.DatabaseDiagnostics.DatabasePath;
                    RenderPreviewResult(results, result);
                }
            }
            catch (Exception ex)
            {
                RenderRuntimeError(results, databasePath, ex);
            }
        }

        private static void RunImport(string databasePath, DataSourceImportRequest request, VisualElement results, Label status)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var workflow = new DataSourceImportWorkflowService(factory);
                    var result = workflow.Import(request);
                    status.text = result.DatabaseDiagnostics == null
                        ? "Active database: " + databasePath
                        : "Active database: " + result.DatabaseDiagnostics.DatabasePath;
                    RenderImportResult(results, result);
                }
            }
            catch (Exception ex)
            {
                RenderRuntimeError(results, databasePath, ex);
            }
        }

        private static void RenderDataSourcePlaceholder(VisualElement results, string databasePath)
        {
            results.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            results.Add(cards);
            cards.Add(MakeCard("Active Database", new[] { "Status: local SQLite path selected", "Path: " + databasePath }));
            cards.Add(MakeCard("Import Scope", new[] { "Source type: CSV only", "Network sources: disabled", "FM26 live data: unsupported" }));
            cards.Add(MakeCard("Preview", new[] { "File readable: not checked", "Columns detected: 0", "Rows detected: 0" }));
            cards.Add(MakeCard("Import Result", new[] { "No safe import has been run", "Stored data: masked fields only" }));
        }

        private static void RenderPreviewResult(VisualElement results, DataSourceImportWorkflowResult result)
        {
            results.Clear();
            var preview = result.PreviewViewModel;
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            results.Add(cards);

            cards.Add(MakeCard("File Readable", new[] { result.Preview != null && result.Preview.FileReadable ? "Yes" : "No", preview.FilePath }));
            cards.Add(MakeCard("Columns Detected", new[] { preview.ColumnRows.Count.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Rows Detected", new[] { preview.RowsDetected.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Mapped Fields", new[] { preview.MappedCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Unknown Fields", new[] { preview.UnknownCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Forbidden Fields", new[] { preview.ForbiddenCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Import Result", new[] { "Preview only", "No data stored" }));
            cards.Add(MakeCard("Last Error", preview.Errors.Count == 0 ? new[] { "None" } : ToArray(preview.Errors)));

            results.Add(MakeMessages("Warnings", preview.Warnings));
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

            cards.Add(MakeCard("File Readable", new[] { result.Preview != null && result.Preview.FileReadable ? "Yes" : "No", preview.FilePath }));
            cards.Add(MakeCard("Columns Detected", new[] { preview.ColumnRows.Count.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Rows Detected", new[] { preview.RowsDetected.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Mapped Fields", new[] { preview.MappedCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Unknown Fields", new[] { preview.UnknownCount.ToString(CultureInfo.InvariantCulture) }));
            cards.Add(MakeCard("Forbidden Fields", new[] { preview.ForbiddenCount.ToString(CultureInfo.InvariantCulture) }));

            if (import == null)
            {
                cards.Add(MakeCard("Import Result", new[] { "Not started" }));
                cards.Add(MakeCard("Last Error", ToArray(result.ErrorMessages)));
            }
            else
            {
                cards.Add(MakeCard("Import Result", new[]
                {
                    import.Success ? "Completed" : "Completed with warnings",
                    "Rows accepted: " + import.RowsAccepted.ToString(CultureInfo.InvariantCulture),
                    "Rows rejected: " + import.RowsRejected.ToString(CultureInfo.InvariantCulture),
                    "Players in database: " + import.DatabasePlayersCount.ToString(CultureInfo.InvariantCulture),
                    "Stats in database: " + import.DatabaseStatsCount.ToString(CultureInfo.InvariantCulture)
                }));
                cards.Add(MakeCard("Last Error", import.Errors.Count == 0 ? new[] { "None" } : ToArray(import.Errors)));
            }

            results.Add(MakeMessages("Warnings", result.WarningMessages));
            results.Add(MakeColumnPreviewList(preview.ColumnRows));
        }

        private static VisualElement MakeMessages(string title, IReadOnlyList<string> messages)
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");
            panel.Add(MakeSectionTitle(title));
            if (messages == null || messages.Count == 0)
            {
                panel.Add(new Label("None"));
                return panel;
            }

            foreach (var message in messages)
            {
                panel.Add(new Label(message));
            }

            return panel;
        }

        private static VisualElement MakeColumnPreviewList(IReadOnlyList<ColumnPreviewViewModel> rows)
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");
            panel.Add(MakeSectionTitle("Column Mapping"));

            if (rows == null || rows.Count == 0)
            {
                panel.Add(new Label("No columns previewed."));
                return panel;
            }

            foreach (var row in rows)
            {
                panel.Add(MakeDiagnosticRow(row.SourceColumn, row.Status + " - " + (string.IsNullOrWhiteSpace(row.MappedTo) ? row.Category : row.MappedTo)));
            }

            return panel;
        }

        private static void RenderRuntimeError(VisualElement results, string databasePath, Exception ex)
        {
            results.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            results.Add(cards);
            cards.Add(MakeCard("Active Database", new[] { "Path: " + databasePath }));
            cards.Add(MakeCard("Import Result", new[] { "Not completed" }));
            cards.Add(MakeCard("Last Error", new[] { ex.GetType().Name + ": " + ex.Message }));
        }

        private static string ResolveSyntheticFixtureCsvPath()
        {
            var repoFixture = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Statlyn.Tests", "Fixtures", "players.sample.csv"));
            if (File.Exists(repoFixture))
            {
                return repoFixture;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "Fixtures", "players.sample.csv"));
        }

        private static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return new[] { "None" };
            }

            var rows = new string[values.Count];
            for (var index = 0; index < values.Count; index++)
            {
                rows[index] = values[index];
            }

            return rows;
        }

        private static VisualElement MakePlayerProfileSlice(UnityProfileRenderModel model)
        {
            var profile = new VisualElement();
            profile.AddToClassList("profile-slice");

            var header = new VisualElement();
            header.AddToClassList("profile-header");
            profile.Add(header);

            var avatar = new VisualElement();
            avatar.AddToClassList("profile-avatar");
            var initials = new Label(model.Initials);
            initials.AddToClassList("profile-avatar-text");
            avatar.Add(initials);
            header.Add(avatar);

            var identity = new VisualElement();
            identity.AddToClassList("profile-identity");
            header.Add(identity);

            var titleRow = new VisualElement();
            titleRow.AddToClassList("profile-title-row");
            identity.Add(titleRow);

            var name = new Label(model.PlayerName);
            name.AddToClassList("profile-name");
            titleRow.Add(name);

            var mode = new Label(model.IsFixtureMode ? "Fixture Mode" : model.SourceName);
            mode.AddToClassList("fixture-pill");
            titleRow.Add(mode);

            var detail = new Label(model.DetailLine);
            detail.AddToClassList("profile-detail");
            identity.Add(detail);

            var flag = new Label(model.FlagLine);
            flag.AddToClassList("profile-flag");
            identity.Add(flag);

            var summaryGrid = new VisualElement();
            summaryGrid.AddToClassList("profile-summary-grid");
            profile.Add(summaryGrid);

            summaryGrid.Add(MakeMetricCard("Source Confidence", model.SourceConfidence.ToString(), model.SourceName));
            summaryGrid.Add(MakeMetricCard("Data Completeness", model.DataCompleteness.ToString(), model.DataCompletenessCaption));
            summaryGrid.Add(MakeMetricCard("Role Fit", model.RoleFit, "Role fit visual placeholder"));
            summaryGrid.Add(MakeMetricCard("Confidence", model.Confidence, model.ConfidenceCaption));
            summaryGrid.Add(MakeMetricCard("Risk", model.Risk, model.RiskCaption));

            var visualGrid = new VisualElement();
            visualGrid.AddToClassList("profile-visual-grid");
            profile.Add(visualGrid);

            var radar = new VisualElement();
            radar.AddToClassList("radar-card");
            radar.Add(MakeSectionTitle("Radar Chart"));
            foreach (var metric in model.RadarMetrics)
            {
                radar.Add(new Label(metric.Label + ": " + metric.Value + " / " + metric.MaximumValue + " (" + metric.Confidence + "% confidence)"));
            }

            radar.AddToClassList("placeholder-text");
            visualGrid.Add(radar);

            var bars = new VisualElement();
            bars.AddToClassList("percentile-card");
            bars.Add(MakeSectionTitle("Percentile Bars"));
            foreach (var bar in model.PercentileBars)
            {
                bars.Add(MakePercentileBar(bar.Label + " vs " + bar.ComparisonGroup, bar.Percentile));
            }

            visualGrid.Add(bars);

            var evidence = new VisualElement();
            evidence.AddToClassList("evidence-grid");
            profile.Add(evidence);
            foreach (var card in model.EvidenceCards)
            {
                evidence.Add(MakeEvidenceCard(card.Title, card.Body));
            }

            var warning = new VisualElement();
            warning.AddToClassList("missing-warning");
            warning.Add(new Label("Missing Data"));
            warning.Add(new Label(model.MissingDataMessage));
            warning.Add(new Label(model.BlockedDataMessage));
            profile.Add(warning);

            return profile;
        }

        private static VisualElement MakeMetricCard(string title, string value, string caption)
        {
            var card = new VisualElement();
            card.AddToClassList("metric-card");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("metric-title");
            card.Add(titleLabel);
            var valueLabel = new Label(value);
            valueLabel.AddToClassList("metric-value");
            card.Add(valueLabel);
            var captionLabel = new Label(caption);
            captionLabel.AddToClassList("metric-caption");
            card.Add(captionLabel);
            return card;
        }

        private static Label MakeSectionTitle(string title)
        {
            var label = new Label(title);
            label.AddToClassList("card-title");
            return label;
        }

        private static VisualElement MakePercentileBar(string label, int value)
        {
            var row = new VisualElement();
            row.AddToClassList("percentile-row");
            row.Add(new Label(label));
            var track = new VisualElement();
            track.AddToClassList("percentile-track");
            var fill = new VisualElement();
            fill.AddToClassList("percentile-fill");
            fill.style.width = Length.Percent(value);
            track.Add(fill);
            row.Add(track);
            return row;
        }

        private static VisualElement MakeEvidenceCard(string title, string copy)
        {
            var card = new VisualElement();
            card.AddToClassList("evidence-card");
            card.Add(MakeSectionTitle(title));
            var body = new Label(copy);
            body.AddToClassList("evidence-copy");
            card.Add(body);
            return card;
        }

        private static VisualElement MakeCard(string heading, IEnumerable<string> rows)
        {
            var card = new VisualElement();
            card.AddToClassList("glass-card");

            var label = new Label(heading);
            label.AddToClassList("card-title");
            card.Add(label);

            foreach (var row in rows)
            {
                var rowLabel = new Label(row);
                rowLabel.AddToClassList("card-row");
                card.Add(rowLabel);
            }

            return card;
        }

        private static VisualElement MakeDiagnosticsPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");

            var title = new Label("Advanced Diagnostics");
            title.AddToClassList("card-title");
            panel.Add(title);

            panel.Add(MakeDiagnosticRow("Process", "Not checked"));
            panel.Add(MakeDiagnosticRow("Read-only handle", "Not opened"));
            panel.Add(MakeDiagnosticRow("Memory map", "No supported FM26 build registered"));
            panel.Add(MakeDiagnosticRow("Managed club", "Unavailable until live snapshot"));
            panel.Add(MakeDiagnosticRow("Player validation", "No player data loaded"));

            return panel;
        }

        private static VisualElement MakeDiagnosticRow(string name, string state)
        {
            var row = new VisualElement();
            row.AddToClassList("diagnostic-row");

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("diagnostic-name");
            row.Add(nameLabel);

            var stateLabel = new Label(state);
            stateLabel.AddToClassList("diagnostic-state");
            row.Add(stateLabel);

            return row;
        }

    }
}
