using System.Collections.Generic;
using Statlyn.Core.Diagnostics;

namespace Statlyn.UI
{
    public sealed class DiagnosticsPanelViewModel
    {
        public DiagnosticsPanelViewModel(DiagnosticReport report)
        {
            Items = report == null ? new List<DiagnosticItem>() : report.Items;
        }

        public IReadOnlyList<DiagnosticItem> Items { get; }
    }
}
