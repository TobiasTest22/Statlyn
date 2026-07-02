using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Import
{
    public sealed class ImportDiagnostic
    {
        public ImportDiagnostic(string message, DiagnosticStatus status, string detail)
        {
            Message = message ?? string.Empty;
            Status = status;
            Detail = detail ?? string.Empty;
        }

        public string Message { get; }

        public DiagnosticStatus Status { get; }

        public string Detail { get; }
    }
}
