using Statlyn.UnityApp.Pages;
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
        private readonly DashboardPageBuilder _dashboard = new DashboardPageBuilder();
        private readonly DataSourcesPageBuilder _dataSources = new DataSourcesPageBuilder();
        private readonly RecruitmentCentrePageBuilder _recruitmentCentre = new RecruitmentCentrePageBuilder();

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

            _dashboard.Build(main);
        }

        private void ShowPage(VisualElement main, string pageName)
        {
            if (pageName == "Data Sources")
            {
                _dataSources.Build(main);
                return;
            }

            if (pageName == "Recruitment")
            {
                _recruitmentCentre.Build(main);
                return;
            }

            _dashboard.Build(main);
        }
    }
}
