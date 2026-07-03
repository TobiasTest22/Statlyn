using System.Globalization;
using System.Linq;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Readiness;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;

namespace Statlyn.Data.Export
{
    public sealed class SafeReportSnapshotService
    {
        public string BuildReadinessReport(LocalProductReadinessResult result)
        {
            if (result == null)
            {
                return "## Local Readiness\nNo readiness result is available.\nNo live FM26 data.";
            }

            return Sanitize(
                "## Local Readiness\n" +
                result.SafeSummary + "\n" +
                "Schema version: " + result.SchemaVersion.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Imported players: " + Bool(result.HasImportedPlayers) + "\n" +
                "Shortlists: " + Bool(result.HasShortlists) + "\n" +
                "Scout reports: " + Bool(result.HasScoutReports) + "\n" +
                "Role Lab templates: " + Bool(result.HasRoleLabTemplates) + "\n" +
                "Benchmark definitions: " + Bool(result.HasBenchmarkDefinitions) + "\n" +
                "FM26 unsupported. No live FM26 data.\n" +
                string.Join("\n", result.Checks.Select(check => "- " + check.Name + ": " + check.Status + " - " + check.SafeMessage)));
        }

        public string BuildPlayerProfileSummary(PlayerProfileResult result)
        {
            if (result == null || !result.Success || result.Player == null)
            {
                return "## Player Profile\nNo persisted safe player profile is available.\nNo live FM26 data.";
            }

            var benchmark = result.BenchmarkSummary == null
                ? "No benchmark yet."
                : result.BenchmarkSummary.SafeMessage;
            return Sanitize(
                "## Player Profile\n" +
                result.Player.DisplayName + "\n" +
                "Source: " + (result.SourceMetadata == null ? "Unknown" : result.SourceMetadata.SourceName) + "\n" +
                "Role: " + (result.LatestRoleScore == null ? "Not scored" : result.LatestRoleScore.RoleName) + "\n" +
                "Benchmark: " + benchmark + "\n" +
                "Metrics are generic/import, not FM26-verified.\n" +
                "No live FM26 data.");
        }

        public string BuildShortlistSummary(ShortlistDetailViewModel detail)
        {
            if (detail == null || detail.ShortlistId == 0)
            {
                return "## Shortlist\nNo shortlist selected.\nNo fake data.";
            }

            return Sanitize(
                "## Shortlist\n" +
                detail.Name + "\n" +
                "Players: " + detail.Players.Count.ToString(CultureInfo.InvariantCulture) + "\n" +
                string.Join("\n", detail.Players.Select(player => "- " + player.PlayerName + " | " + player.Status + " | " + player.Priority + " | No live FM26 data")));
        }

        public string BuildScoutReportSummary(ScoutLatestReportSummaryViewModel summary)
        {
            if (summary == null || !summary.HasReport)
            {
                return "## Scout Report\nNo scout report yet.\nNo live FM26 data.";
            }

            return Sanitize(
                "## Scout Report\n" +
                "Recommendation: " + summary.Recommendation + "\n" +
                "Confidence: " + summary.Confidence + "\n" +
                "Summary: " + ScoutTextSanitizer.Sanitize(summary.Summary) + "\n" +
                "No live FM26 data.");
        }

        public string BuildBenchmarkSummary(BenchmarksPageViewModel page)
        {
            if (page == null || page.Definitions.Count == 0)
            {
                return "## Benchmarks\nNo benchmark definitions yet.\nNo fake percentiles.";
            }

            return Sanitize(
                "## Benchmarks\n" +
                page.SafeMessage + "\n" +
                "Generic/import, not FM26-verified.\n" +
                string.Join("\n", page.Definitions.Select(definition => "- " + definition.BenchmarkName + " | " + definition.VerificationLabel + " | " + (definition.LatestRun == null ? "No run" : definition.LatestRun.SafeMessage))));
        }

        private static string Bool(bool value)
        {
            return value ? "yes" : "no";
        }

        private static string Sanitize(string value)
        {
            return DiagnosticSanitizer.Sanitize(ScoutTextSanitizer.Sanitize(value ?? string.Empty));
        }
    }
}
