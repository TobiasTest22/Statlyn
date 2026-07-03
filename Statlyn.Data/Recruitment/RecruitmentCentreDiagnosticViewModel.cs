using System.Collections.Generic;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreDiagnosticViewModel
    {
        public RecruitmentCentreDiagnosticViewModel(string databasePath, IReadOnlyList<string> messages, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            DatabasePath = databasePath ?? string.Empty;
            Messages = messages ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public string DatabasePath { get; }

        public IReadOnlyList<string> Messages { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }
    }
}
