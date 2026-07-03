using System;
using System.Collections.Generic;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Workflow
{
    public enum UnitySmokeTestStepStatus
    {
        Passed = 0,
        Failed = 1,
        Skipped = 2
    }

    public sealed class UnitySmokeTestOptions
    {
        public string TemporaryRoot { get; set; } = string.Empty;

        public string ApplicationDataPath { get; set; } = string.Empty;

        public string StreamingAssetsPath { get; set; } = string.Empty;

        public string MainDatabasePath { get; set; } = string.Empty;

        public bool ClearSmokeTestDatabase { get; set; } = true;
    }

    public sealed class UnitySmokeTestResult
    {
        public UnitySmokeTestResult(
            bool success,
            DateTimeOffset startedAtUtc,
            DateTimeOffset completedAtUtc,
            string databasePath,
            string fixturePath,
            IReadOnlyList<UnitySmokeTestStep> steps,
            IReadOnlyList<string> warnings,
            IReadOnlyList<string> errors,
            string safeSummary)
        {
            Success = success;
            StartedAtUtc = startedAtUtc;
            CompletedAtUtc = completedAtUtc;
            DatabasePath = DiagnosticSanitizer.Sanitize(databasePath ?? string.Empty);
            FixturePath = DiagnosticSanitizer.Sanitize(fixturePath ?? string.Empty);
            Steps = SanitizeSteps(steps);
            Warnings = Sanitize(warnings);
            Errors = Sanitize(errors);
            SafeSummary = DiagnosticSanitizer.Sanitize(safeSummary ?? string.Empty);
        }

        public bool Success { get; }

        public DateTimeOffset StartedAtUtc { get; }

        public DateTimeOffset CompletedAtUtc { get; }

        public string DatabasePath { get; }

        public string FixturePath { get; }

        public IReadOnlyList<UnitySmokeTestStep> Steps { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public string SafeSummary { get; }

        public string ToSafeText()
        {
            return DiagnosticSanitizer.Sanitize(
                SafeSummary +
                " DatabasePath=" + DatabasePath +
                " FixturePath=" + FixturePath +
                " Steps=" + string.Join(" | ", Steps));
        }

        private static IReadOnlyList<UnitySmokeTestStep> SanitizeSteps(IReadOnlyList<UnitySmokeTestStep> steps)
        {
            var sanitized = new List<UnitySmokeTestStep>();
            if (steps == null)
            {
                return sanitized;
            }

            foreach (var step in steps)
            {
                sanitized.Add(step);
            }

            return sanitized;
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

    public sealed class UnitySmokeTestStep
    {
        public UnitySmokeTestStep(string stepName, UnitySmokeTestStepStatus status, string safeMessage, string technicalDetail, long durationMs)
        {
            StepName = DiagnosticSanitizer.Sanitize(stepName ?? string.Empty);
            Status = status;
            SafeMessage = DiagnosticSanitizer.Sanitize(safeMessage ?? string.Empty);
            TechnicalDetail = DiagnosticSanitizer.Sanitize(technicalDetail ?? string.Empty);
            DurationMs = durationMs < 0 ? 0 : durationMs;
        }

        public string StepName { get; }

        public UnitySmokeTestStepStatus Status { get; }

        public string SafeMessage { get; }

        public string TechnicalDetail { get; }

        public long DurationMs { get; }

        public override string ToString()
        {
            return StepName + ": " + Status + " - " + SafeMessage;
        }
    }
}
