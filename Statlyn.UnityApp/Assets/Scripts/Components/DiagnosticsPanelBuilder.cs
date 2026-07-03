using Statlyn.UnityApp.Components;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Components
{
    public static class DiagnosticsPanelBuilder
    {
        public static VisualElement BuildAdvancedDiagnostics()
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");

            panel.Add(StatlynUiFactory.MakeSectionTitle("Advanced Diagnostics"));
            panel.Add(StatlynUiFactory.MakeDiagnosticRow("Process", "Not checked"));
            panel.Add(StatlynUiFactory.MakeDiagnosticRow("Read-only handle", "Not opened"));
            panel.Add(StatlynUiFactory.MakeDiagnosticRow("Memory map", "No supported FM26 build registered"));
            panel.Add(StatlynUiFactory.MakeDiagnosticRow("Managed club", "Unavailable until live snapshot"));
            panel.Add(StatlynUiFactory.MakeDiagnosticRow("Player validation", "No player data loaded"));

            return panel;
        }
    }
}
