using System.Collections.Generic;

namespace Statlyn.Data.Workflow
{
    public sealed class UnityNavigationItem
    {
        public UnityNavigationItem(string name, bool isBuilt, string safeSubtitle)
        {
            Name = name ?? string.Empty;
            IsBuilt = isBuilt;
            SafeSubtitle = safeSubtitle ?? string.Empty;
        }

        public string Name { get; }

        public bool IsBuilt { get; }

        public string SafeSubtitle { get; }
    }

    public static class UnityNavigationCatalog
    {
        public static IReadOnlyList<UnityNavigationItem> Items
        {
            get
            {
                return new[]
                {
                    new UnityNavigationItem("Home", true, "Local dashboard foundation - no live FM26 data"),
                    new UnityNavigationItem("Squad", false, "This page is not built yet. No fake squad data is shown."),
                    new UnityNavigationItem("Recruitment", true, "Persisted safe recruitment rows"),
                    new UnityNavigationItem("Shortlists", true, "Persisted shortlist workflow"),
                    new UnityNavigationItem("Player Profile", true, "Persisted safe profile report"),
                    new UnityNavigationItem("Role Lab", true, "Generic/import role templates"),
                    new UnityNavigationItem("Benchmarks", true, "Generic/import benchmarks - no fake percentiles"),
                    new UnityNavigationItem("Scout Desk", true, "Qualitative local scouting workflow"),
                    new UnityNavigationItem("Alerts", false, "This page is not built yet. No fake alerts are shown."),
                    new UnityNavigationItem("Data Sources", true, "CSV local import only"),
                    new UnityNavigationItem("Diagnostics", true, "Runtime check and full smoke test"),
                    new UnityNavigationItem("Settings", false, "This page is not built yet. No fake settings are shown.")
                };
            }
        }
    }
}
