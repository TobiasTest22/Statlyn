namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26ProcessDiagnostic
    {
        public bool IsDetected { get; set; }

        public string ProcessName { get; set; } = "fm.exe";

        public int? ProcessId { get; set; }

        public string ProcessPath { get; set; } = string.Empty;

        public string ProductVersion { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;

        public bool HasReadOnlyAccess { get; set; }

        public string ReadOnlyAccessStatus { get; set; } = "Unavailable";

        public string SafeMessage { get; set; } = string.Empty;
    }
}
