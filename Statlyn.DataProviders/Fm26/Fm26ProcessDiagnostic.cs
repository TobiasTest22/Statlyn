namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26ProcessDiagnostic
    {
        public bool IsDetected { get; set; }

        public string DetectionStatus { get; set; } = Fm26DiagnosticSupportStatus.NotDetected.ToString();

        public string DetectionStatusMessage { get; set; } = "FM process not detected.";

        public System.DateTimeOffset? ProcessDetectedAtUtc { get; set; }

        public string ProcessName { get; set; } = "fm.exe";

        public int? ProcessId { get; set; }

        public string ExecutableFileName { get; set; } = "fm.exe";

        public string ExecutableDirectorySafeLabel { get; set; } = string.Empty;

        public string ProcessPath { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string ProductVersion { get; set; } = string.Empty;

        public string FileVersion { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;

        public bool? Is64BitProcess { get; set; }

        public bool ReadOnlyAccessAttempted { get; set; }

        public bool HasReadOnlyAccess { get; set; }

        public string ReadOnlyAccessStatus { get; set; } = "Unavailable";

        public string RequiredAccessLevel { get; set; } = "Read-only diagnostic process query; no write or injection.";

        public string SafeMessage { get; set; } = string.Empty;
    }
}
