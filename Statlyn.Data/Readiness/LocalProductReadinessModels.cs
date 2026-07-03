using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Readiness
{
    public enum LocalProductReadinessCheckStatus
    {
        Passed,
        Warning,
        Failed,
        Skipped
    }

    public sealed class LocalProductReadinessCheck
    {
        public LocalProductReadinessCheck(string name, LocalProductReadinessCheckStatus status, string safeMessage, string technicalDetail)
        {
            Name = DiagnosticSanitizer.Sanitize(name ?? string.Empty);
            Status = status;
            SafeMessage = DiagnosticSanitizer.Sanitize(safeMessage ?? string.Empty);
            TechnicalDetail = DiagnosticSanitizer.Sanitize(technicalDetail ?? string.Empty);
        }

        public string Name { get; }

        public LocalProductReadinessCheckStatus Status { get; }

        public string SafeMessage { get; }

        public string TechnicalDetail { get; }

        public override string ToString()
        {
            return Name + ": " + Status + " - " + SafeMessage;
        }
    }

    public sealed class LocalProductReadinessResult
    {
        public LocalProductReadinessResult(
            IReadOnlyList<LocalProductReadinessCheck> checks,
            string databasePath,
            string fixturePath,
            int schemaVersion,
            bool hasImportedPlayers,
            bool hasShortlists,
            bool hasScoutReports,
            bool hasRoleLabTemplates,
            bool hasBenchmarkDefinitions,
            int importedPlayerCount = 0,
            int shortlistCount = 0,
            int scoutReportCount = 0,
            int roleLabTemplateCount = 0,
            int benchmarkDefinitionCount = 0)
        {
            Checks = checks ?? new List<LocalProductReadinessCheck>();
            DatabasePath = DiagnosticSanitizer.Sanitize(databasePath ?? string.Empty);
            FixturePath = DiagnosticSanitizer.Sanitize(fixturePath ?? string.Empty);
            SchemaVersion = schemaVersion;
            HasImportedPlayers = hasImportedPlayers;
            HasShortlists = hasShortlists;
            HasScoutReports = hasScoutReports;
            HasRoleLabTemplates = hasRoleLabTemplates;
            HasBenchmarkDefinitions = hasBenchmarkDefinitions;
            ImportedPlayerCount = Math.Max(0, importedPlayerCount);
            ShortlistCount = Math.Max(0, shortlistCount);
            ScoutReportCount = Math.Max(0, scoutReportCount);
            RoleLabTemplateCount = Math.Max(0, roleLabTemplateCount);
            BenchmarkDefinitionCount = Math.Max(0, benchmarkDefinitionCount);
        }

        public bool Success
        {
            get { return Checks.All(check => check.Status != LocalProductReadinessCheckStatus.Failed); }
        }

        public string SafeSummary
        {
            get
            {
                return Success
                    ? "Local CSV readiness check completed without failed checks. No live FM26 data is required."
                    : "Local CSV readiness check needs attention. No fake data was generated.";
            }
        }

        public IReadOnlyList<LocalProductReadinessCheck> Checks { get; }

        public IReadOnlyList<string> Warnings
        {
            get
            {
                return Checks
                    .Where(check => check.Status == LocalProductReadinessCheckStatus.Warning || check.Status == LocalProductReadinessCheckStatus.Skipped)
                    .Select(check => check.SafeMessage)
                    .ToList();
            }
        }

        public IReadOnlyList<string> Errors
        {
            get
            {
                return Checks
                    .Where(check => check.Status == LocalProductReadinessCheckStatus.Failed)
                    .Select(check => check.SafeMessage)
                    .ToList();
            }
        }

        public string DatabasePath { get; }

        public string FixturePath { get; }

        public int SchemaVersion { get; }

        public bool HasImportedPlayers { get; }

        public bool HasShortlists { get; }

        public bool HasScoutReports { get; }

        public bool HasRoleLabTemplates { get; }

        public bool HasBenchmarkDefinitions { get; }

        public int ImportedPlayerCount { get; }

        public int ShortlistCount { get; }

        public int ScoutReportCount { get; }

        public int RoleLabTemplateCount { get; }

        public int BenchmarkDefinitionCount { get; }

        public string ToSafeText()
        {
            return DiagnosticSanitizer.Sanitize(
                SafeSummary +
                " DatabasePath=" + DatabasePath +
                " FixturePath=" + FixturePath +
                " SchemaVersion=" + SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " ImportedPlayers=" + HasImportedPlayers +
                " ImportedPlayerCount=" + ImportedPlayerCount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " Shortlists=" + HasShortlists +
                " ShortlistCount=" + ShortlistCount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " ScoutReports=" + HasScoutReports +
                " ScoutReportCount=" + ScoutReportCount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " RoleLabTemplates=" + HasRoleLabTemplates +
                " RoleLabTemplateCount=" + RoleLabTemplateCount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " BenchmarkDefinitions=" + HasBenchmarkDefinitions +
                " BenchmarkDefinitionCount=" + BenchmarkDefinitionCount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " Checks=" + string.Join(" | ", Checks.Select(check => check.ToString())) +
                " FM26 unsupported. No live FM26 data.");
        }
    }
}
