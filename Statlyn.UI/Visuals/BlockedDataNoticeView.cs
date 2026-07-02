using System.Collections.Generic;

namespace Statlyn.UI.Visuals
{
    public sealed class BlockedDataNoticeView
    {
        public BlockedDataNoticeView(int count, IReadOnlyList<string> categories, string safeMessage)
        {
            Count = count;
            Categories = categories ?? new List<string>();
            SafeMessage = safeMessage ?? string.Empty;
        }

        public int Count { get; }

        public IReadOnlyList<string> Categories { get; }

        public string SafeMessage { get; }
    }
}
