using System;
using System.Collections.Generic;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Maintenance
{
    public sealed class LocalDatabaseBackupRecord
    {
        public LocalDatabaseBackupRecord(string originalPath, string backupPath, DateTimeOffset createdAtUtc, bool sourceExisted)
        {
            OriginalPath = DiagnosticSanitizer.Sanitize(originalPath ?? string.Empty);
            BackupPath = DiagnosticSanitizer.Sanitize(backupPath ?? string.Empty);
            CreatedAtUtc = createdAtUtc;
            SourceExisted = sourceExisted;
        }

        public string OriginalPath { get; }

        public string BackupPath { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public bool SourceExisted { get; }
    }

    public sealed class LocalDatabaseMaintenanceResult
    {
        public LocalDatabaseMaintenanceResult(bool success, string safeMessage, string databasePath, LocalDatabaseBackupRecord? backup, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            Success = success;
            SafeMessage = DiagnosticSanitizer.Sanitize(safeMessage ?? string.Empty);
            DatabasePath = DiagnosticSanitizer.Sanitize(databasePath ?? string.Empty);
            Backup = backup;
            Warnings = Sanitize(warnings);
            Errors = Sanitize(errors);
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public string DatabasePath { get; }

        public LocalDatabaseBackupRecord? Backup { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public string ToSafeText()
        {
            return DiagnosticSanitizer.Sanitize(SafeMessage + " DatabasePath=" + DatabasePath + " BackupPath=" + (Backup == null ? string.Empty : Backup.BackupPath));
        }

        private static IReadOnlyList<string> Sanitize(IReadOnlyList<string> values)
        {
            var output = new List<string>();
            if (values == null)
            {
                return output;
            }

            foreach (var value in values)
            {
                output.Add(DiagnosticSanitizer.Sanitize(value ?? string.Empty));
            }

            return output;
        }
    }
}
