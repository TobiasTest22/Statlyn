namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26ProcessInfo
    {
        public int ProcessId { get; set; }

        public string ExecutablePath { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;

        public string ProductVersion { get; set; } = string.Empty;

        public bool IsDetected { get; set; }

        public bool HasReadOnlyAccess { get; set; }
    }
}
