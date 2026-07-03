using System.Collections.Generic;

namespace Statlyn.Data.Workflow
{
    public sealed class FixtureCsvPathResolutionResult
    {
        public FixtureCsvPathResolutionResult(bool success, string filePath, IReadOnlyList<string> candidatePaths, string message)
        {
            Success = success;
            FilePath = filePath ?? string.Empty;
            CandidatePaths = candidatePaths ?? new List<string>();
            Message = message ?? string.Empty;
        }

        public bool Success { get; }

        public string FilePath { get; }

        public IReadOnlyList<string> CandidatePaths { get; }

        public string Message { get; }
    }
}
