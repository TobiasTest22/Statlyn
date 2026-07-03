using System;
using System.Collections.Generic;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Workflow
{
    public sealed class UnityRuntimeCheckResult
    {
        public UnityRuntimeCheckResult(
            bool success,
            DateTimeOffset checkedAtUtc,
            string databasePath,
            bool assembliesOk,
            bool sqliteManagedOk,
            bool sqliteNativeOk,
            bool databaseInitOk,
            bool workflowServiceOk,
            IReadOnlyList<string> messages,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors)
        {
            Success = success;
            CheckedAtUtc = checkedAtUtc;
            DatabasePath = databasePath ?? string.Empty;
            AssembliesOk = assembliesOk;
            SqliteManagedOk = sqliteManagedOk;
            SqliteNativeOk = sqliteNativeOk;
            DatabaseInitOk = databaseInitOk;
            WorkflowServiceOk = workflowServiceOk;
            Messages = Sanitize(messages);
            Warnings = Sanitize(warnings);
            Errors = Sanitize(errors);
        }

        public bool Success { get; }

        public DateTimeOffset CheckedAtUtc { get; }

        public string DatabasePath { get; }

        public bool AssembliesOk { get; }

        public bool SqliteManagedOk { get; }

        public bool SqliteNativeOk { get; }

        public bool DatabaseInitOk { get; }

        public bool WorkflowServiceOk { get; }

        public IReadOnlyList<string> Messages { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public string ToSafeSummary()
        {
            return DiagnosticSanitizer.Sanitize(
                "Success=" + Success +
                "; DatabasePath=" + DatabasePath +
                "; AssembliesOk=" + AssembliesOk +
                "; SqliteManagedOk=" + SqliteManagedOk +
                "; SqliteNativeOk=" + SqliteNativeOk +
                "; DatabaseInitOk=" + DatabaseInitOk +
                "; WorkflowServiceOk=" + WorkflowServiceOk +
                "; Messages=" + string.Join(" | ", Messages) +
                "; Warnings=" + string.Join(" | ", Warnings) +
                "; Errors=" + string.Join(" | ", Errors));
        }

        private static IReadOnlyList<string> Sanitize(IReadOnlyList<string> values)
        {
            var sanitized = new List<string>();
            if (values == null)
            {
                return sanitized;
            }

            foreach (var value in values)
            {
                sanitized.Add(DiagnosticSanitizer.Sanitize(value ?? string.Empty));
            }

            return sanitized;
        }
    }
}
