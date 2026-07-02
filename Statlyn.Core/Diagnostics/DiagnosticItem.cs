using System;

namespace Statlyn.Core.Diagnostics
{
    public sealed class DiagnosticItem
    {
        public DiagnosticItem(string key, DiagnosticStatus status, string message, string technicalDetail)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Status = status;
            Message = message ?? string.Empty;
            TechnicalDetail = technicalDetail ?? string.Empty;
            TimestampUtc = DateTimeOffset.UtcNow;
        }

        public string Key { get; }

        public DiagnosticStatus Status { get; }

        public string Message { get; }

        public string TechnicalDetail { get; }

        public DateTimeOffset TimestampUtc { get; }
    }
}
