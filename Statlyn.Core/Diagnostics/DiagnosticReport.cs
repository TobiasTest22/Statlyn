using System.Collections.Generic;

namespace Statlyn.Core.Diagnostics
{
    public sealed class DiagnosticReport
    {
        private readonly List<DiagnosticItem> _items = new List<DiagnosticItem>();

        public IReadOnlyList<DiagnosticItem> Items
        {
            get { return _items; }
        }

        public DiagnosticStatus OverallStatus
        {
            get
            {
                var hasPartial = false;
                var hasUnsupported = false;

                foreach (var item in _items)
                {
                    if (item.Status == DiagnosticStatus.Failed)
                    {
                        return DiagnosticStatus.Failed;
                    }

                    if (item.Status == DiagnosticStatus.Partial)
                    {
                        hasPartial = true;
                    }

                    if (item.Status == DiagnosticStatus.Unsupported)
                    {
                        hasUnsupported = true;
                    }
                }

                if (hasUnsupported)
                {
                    return DiagnosticStatus.Unsupported;
                }

                return hasPartial ? DiagnosticStatus.Partial : DiagnosticStatus.Verified;
            }
        }

        public void Add(string key, DiagnosticStatus status, string message, string technicalDetail)
        {
            _items.Add(new DiagnosticItem(key, status, message, technicalDetail));
        }
    }
}
