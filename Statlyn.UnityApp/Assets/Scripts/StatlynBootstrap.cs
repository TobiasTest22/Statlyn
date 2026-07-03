using System.Linq;
using Statlyn.Data.Workflow;
using Statlyn.UnityApp.Pages;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp
{
    public sealed class StatlynBootstrap : MonoBehaviour
    {
        private static readonly string[] NavigationItems =
            UnityNavigationCatalog.Items.Select(item => item.Name).ToArray();

        private UIDocument _document;
        private readonly DashboardPageBuilder _dashboard = new DashboardPageBuilder();
        private readonly DataSourcesPageBuilder _dataSources = new DataSourcesPageBuilder();
        private readonly RecruitmentCentrePageBuilder _recruitmentCentre = new RecruitmentCentrePageBuilder();
        private readonly PlayerProfilePageBuilder _playerProfile = new PlayerProfilePageBuilder();
        private readonly ShortlistsPageBuilder _shortlists = new ShortlistsPageBuilder();
        private readonly ScoutDeskPageBuilder _scoutDesk = new ScoutDeskPageBuilder();
        private readonly RoleLabPageBuilder _roleLab = new RoleLabPageBuilder();
        private readonly BenchmarksPageBuilder _benchmarks = new BenchmarksPageBuilder();
        private readonly DiagnosticsPageBuilder _diagnostics = new DiagnosticsPageBuilder();
        private readonly NotBuiltPageBuilder _notBuilt = new NotBuiltPageBuilder();

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

            sidebar.Add(StatlynUiFactory.MakeBrandLockup());

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
            if (pageName == "Home")
            {
                _dashboard.Build(main);
                return;
            }

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

            if (pageName == "Player Profile")
            {
                _playerProfile.Build(main);
                return;
            }

            if (pageName == "Shortlists")
            {
                _shortlists.Build(main);
                return;
            }

            if (pageName == "Scout Desk")
            {
                _scoutDesk.Build(main);
                return;
            }

            if (pageName == "Role Lab")
            {
                _roleLab.Build(main);
                return;
            }

            if (pageName == "Benchmarks")
            {
                _benchmarks.Build(main);
                return;
            }

            if (pageName == "Diagnostics")
            {
                _diagnostics.Build(main);
                return;
            }

            var navItem = UnityNavigationCatalog.Items.FirstOrDefault(item => item.Name == pageName);
            _notBuilt.Build(main, string.IsNullOrWhiteSpace(pageName) ? "Not Built" : pageName, navItem == null ? "This page is not built yet." : navItem.SafeSubtitle);
        }
    }
}
