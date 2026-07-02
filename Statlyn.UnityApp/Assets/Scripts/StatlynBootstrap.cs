using System.Collections.Generic;
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

            var subtitle = new Label("No live data source connected");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Scouting firewall active");
            status.AddToClassList("status-pill");
            header.Add(status);

            var dashboard = new VisualElement();
            dashboard.AddToClassList("dashboard-grid");
            main.Add(dashboard);

            dashboard.Add(MakeCard("Active Source", new[] { "Mode: FM26", "Status: Not connected", "Players: 0" }));
            dashboard.Add(MakeCard("Connection", new[] { "FM26 process: Not checked", "Build support: Unsupported until mapped", "Snapshot: Not loaded" }));
            dashboard.Add(MakeCard("Recruitment", new[] { "Squad needs: Awaiting data", "Targets: Awaiting data", "Alerts: 0" }));
            dashboard.Add(MakeCard("Protection", new[] { "Hidden values blocked", "Raw entities blocked from UI", "Low confidence requires scouting" }));

            var diagnostics = MakeDiagnosticsPanel();
            main.Add(diagnostics);
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
