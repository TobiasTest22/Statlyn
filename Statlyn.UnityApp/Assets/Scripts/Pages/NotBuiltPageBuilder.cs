using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class NotBuiltPageBuilder
    {
        public void Build(VisualElement main, string title, string safeMessage)
        {
            main.Clear();
            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                title,
                "This page is not built yet",
                "Not built yet",
                CommandStatusCategory.Muted));

            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            main.Add(grid);
            grid.Add(StatlynUiFactory.MakeCommandEmptyState(title, safeMessage, "This page is not built yet.", "No fake players, alerts or live FM26 data are shown."));
            grid.Add(StatlynUiFactory.MakeSafetyBanner("Persisted safe data only", "No raw provider data", "No hidden values"));
        }
    }
}
