using System.Collections.Generic;
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

            foreach (var item in NavigationItems)
            {
                var button = new Button();
                button.text = item;
                button.AddToClassList("nav-button");
                sidebar.Add(button);
            }

            var main = new VisualElement();
            main.AddToClassList("main");
            root.Add(main);

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
