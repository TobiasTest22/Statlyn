using System;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Data;
using Statlyn.Data.Workflow;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone181Tests
    {
        [Fact]
        public void RuntimeCheckModelFormatsSafeOutputAndRedactsHiddenLookingValues()
        {
            var result = new UnityRuntimeCheckResult(
                false,
                DateTimeOffset.UtcNow,
                "runtime-check.db",
                false,
                false,
                false,
                false,
                false,
                new[] { "CurrentAbility: 200" },
                new[] { "Professionalism=19" },
                new[] { "PA 199" });

            var summary = result.ToSafeSummary();

            Assert.Contains("CurrentAbility: [redacted]", summary);
            Assert.Contains("Professionalism= [redacted]", summary);
            Assert.Contains("PA [redacted]", summary);
            Assert.DoesNotContain(" 200", summary);
            Assert.DoesNotContain("=19", summary);
            Assert.DoesNotContain(" 199", summary);
        }

        [Fact]
        public void RuntimeCheckRunsTempDatabaseInitWithoutFm26()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "statlyn-runtime-check-" + Guid.NewGuid().ToString("N"));
            var defaultPath = Path.Combine(tempRoot, "main", "statlyn.db");

            var result = new UnityRuntimeDependencyCheck().Run(tempRoot, defaultPath);
            var summary = result.ToSafeSummary();

            Assert.True(result.Success);
            Assert.True(result.AssembliesOk);
            Assert.True(result.SqliteManagedOk);
            Assert.True(result.SqliteNativeOk);
            Assert.True(result.DatabaseInitOk);
            Assert.True(result.WorkflowServiceOk);
            Assert.False(File.Exists(result.DatabasePath));
            Assert.Contains("FM26 is not required", summary);
            Assert.DoesNotContain("live FM26 data", summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FM26 process: connected", summary, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FixturePathResolverUsesRepoPathThenStreamingAssetsFallback()
        {
            var root = Path.Combine(Path.GetTempPath(), "statlyn-fixture-path-" + Guid.NewGuid().ToString("N"));
            var assets = Path.Combine(root, "Statlyn.UnityApp", "Assets");
            var repoFixture = Path.Combine(root, "Statlyn.Tests", "Fixtures", "players.sample.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(repoFixture)!);
            File.WriteAllText(repoFixture, "SourcePlayerId,DisplayName" + Environment.NewLine + "one,Synthetic One" + Environment.NewLine);

            var resolver = new UnityFixtureCsvPathResolver();
            var repoResult = resolver.Resolve(assets, Path.Combine(assets, "StreamingAssets"));

            Assert.True(repoResult.Success);
            Assert.Equal(Path.GetFullPath(repoFixture), repoResult.FilePath);
            Assert.Contains("not live FM26 data", repoResult.Message);

            File.Delete(repoFixture);
            var streamingFixture = Path.Combine(assets, "StreamingAssets", "Statlyn", "Fixtures", "players.sample.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(streamingFixture)!);
            File.WriteAllText(streamingFixture, "SourcePlayerId,DisplayName" + Environment.NewLine + "one,Synthetic One" + Environment.NewLine);

            var streamingResult = resolver.Resolve(assets, Path.Combine(assets, "StreamingAssets"));

            Assert.True(streamingResult.Success);
            Assert.Equal(Path.GetFullPath(streamingFixture), streamingResult.FilePath);
        }

        [Fact]
        public void FixturePathResolverReturnsClearFallbackMessageWhenMissing()
        {
            var root = Path.Combine(Path.GetTempPath(), "statlyn-fixture-missing-" + Guid.NewGuid().ToString("N"));
            var assets = Path.Combine(root, "Statlyn.UnityApp", "Assets");

            var result = new UnityFixtureCsvPathResolver().Resolve(assets, Path.Combine(assets, "StreamingAssets"));

            Assert.False(result.Success);
            Assert.True(result.CandidatePaths.Count >= 3);
            Assert.Contains("Run tools/copy-managed-to-unity.ps1", result.Message);
            Assert.DoesNotContain("live FM26", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PreviewStillDoesNotStoreDataAndImportStillWorks()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var workflow = new DataSourceImportWorkflowService(factory);

            var preview = workflow.Preview(CreateRequest());

            Assert.True(preview.Success);
            Assert.Equal(0, CountRows(factory, "Player"));
            Assert.Equal(0, CountRows(factory, "PlayerStat"));

            var import = workflow.Import(CreateRequest());

            Assert.True(import.Success);
            Assert.Equal(2, CountRows(factory, "Player"));
            Assert.Equal(2, CountWhere(factory, "PlayerStat", "StatName = 'xG'"));
            Assert.Equal(4, CountRows(factory, "BlockedFieldAudit"));
        }

        [Fact]
        public void RuntimeAndWorkflowDiagnosticsDoNotExposeHiddenValuesOrFakeFm26Status()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var workflowResult = new DataSourceImportWorkflowService(factory).Preview(CreateRequest());
            var runtimeResult = new UnityRuntimeDependencyCheck().Run(Path.GetTempPath(), Path.Combine(Path.GetTempPath(), "statlyn.db"));

            var text = WorkflowText(workflowResult) + " " + runtimeResult.ToSafeSummary();

            Assert.DoesNotContain("CurrentAbility: 200", text);
            Assert.DoesNotContain("Professionalism:20", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("FM26 process: connected", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fake live", text, StringComparison.OrdinalIgnoreCase);
        }

        private static DataSourceImportRequest CreateRequest()
        {
            return new DataSourceImportRequest
            {
                CsvPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv"),
                SourceName = "Synthetic CSV fixture",
                LicenceStatus = "synthetic test fixture",
                AllowedUsage = "development fixture only",
                IsLicensed = true,
                SourceConfidence = 80,
                PermitsPlayerImages = false,
                PermitsProviderFlags = false,
                UsesBundledSafeFlagAssets = true,
                PermitsClubBadges = false,
                AllowsExport = true
            };
        }

        private static string WorkflowText(DataSourceImportWorkflowResult result)
        {
            var builder = new StringBuilder();
            builder.Append(result.SafeMessage).Append(' ');
            foreach (var warning in result.WarningMessages)
            {
                builder.Append(warning).Append(' ');
            }

            foreach (var error in result.ErrorMessages)
            {
                builder.Append(error).Append(' ');
            }

            foreach (var row in result.PreviewViewModel.ColumnRows)
            {
                builder.Append(row.SourceColumn).Append(' ')
                    .Append(row.MappedTo).Append(' ')
                    .Append(row.Category).Append(' ')
                    .Append(row.Status).Append(' ')
                    .Append(row.Reason).Append(' ');
            }

            return builder.ToString();
        }

        private static int CountRows(StatlynDbConnectionFactory factory, string table)
        {
            return CountWhere(factory, table, "1 = 1");
        }

        private static int CountWhere(StatlynDbConnectionFactory factory, string table, string where)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE " + where + ";";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
