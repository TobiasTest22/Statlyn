using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Statlyn.Data;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Export;
using Statlyn.Data.Maintenance;
using Statlyn.Data.Profile;
using Statlyn.Data.Readiness;
using Statlyn.Data.Recruitment;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.Data.Workflow;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone28Tests
    {
        [Fact]
        public void EmptyDatabaseReadinessIsSafeAndHonest()
        {
            using var temp = new TemporaryDirectory();
            using var factory = RuntimeDatabaseFactory.CreateFile(Path.Combine(temp.Path, "statlyn.db"));

            var result = new LocalProductReadinessService(factory, temp.Path, Path.Combine(temp.Path, "StreamingAssets")).Run();

            Assert.True(result.Success, result.ToSafeText());
            Assert.False(result.HasImportedPlayers);
            Assert.False(result.HasShortlists);
            Assert.False(result.HasScoutReports);
            Assert.Equal(0, result.ImportedPlayerCount);
            Assert.Equal(0, result.ShortlistCount);
            Assert.Equal(0, result.ScoutReportCount);
            Assert.Contains(result.Checks, check => check.Name == "Synthetic Fixture" && check.Status == LocalProductReadinessCheckStatus.Warning);
            Assert.Contains("No live FM26 data", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ImportedDatabaseReadinessDetectsLocalProductState()
        {
            using var temp = new TemporaryDirectory();
            var smoke = RunSmokeTest(temp.Path);
            Assert.True(smoke.Success, smoke.ToSafeText());

            using var factory = RuntimeDatabaseFactory.CreateFile(smoke.DatabasePath);
            var result = new LocalProductReadinessService(factory, AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "StreamingAssets")).Run();

            Assert.True(result.Success, result.ToSafeText());
            Assert.True(result.HasImportedPlayers);
            Assert.True(result.HasShortlists);
            Assert.True(result.HasScoutReports);
            Assert.True(result.HasRoleLabTemplates);
            Assert.True(result.HasBenchmarkDefinitions);
            Assert.True(result.ImportedPlayerCount > 0);
            Assert.True(result.ShortlistCount > 0);
            Assert.True(result.ScoutReportCount > 0);
            Assert.True(result.RoleLabTemplateCount > 0);
            Assert.True(result.BenchmarkDefinitionCount > 0);
            Assert.Contains(result.Checks, check => check.Name == "FM26 Status" && check.Status == LocalProductReadinessCheckStatus.Warning);
            Assert.DoesNotContain("FM26 supported", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CsvPreviewAndImportExposeHardeningMessagesAndRemainIdempotent()
        {
            using var temp = new TemporaryDirectory();
            var fixture = ResolveFixture();
            using var factory = RuntimeDatabaseFactory.CreateFile(Path.Combine(temp.Path, "statlyn.db"));
            var workflow = new DataSourceImportWorkflowService(factory);
            var request = BuildImportRequest(fixture.FilePath);

            var preview = workflow.Preview(request);
            Assert.True(preview.Success, string.Join(" | ", preview.ErrorMessages));
            Assert.True(preview.PreviewViewModel.UnknownCount >= 0);
            Assert.True(preview.PreviewViewModel.ForbiddenCount > 0);
            Assert.Contains("Unknown columns are not stored unless mapped safely.", preview.PreviewViewModel.HardeningMessages);
            Assert.Contains("Forbidden/hidden-looking fields are blocked.", preview.PreviewViewModel.HardeningMessages);
            Assert.Contains("Missing metrics are not treated as zero.", preview.PreviewViewModel.HardeningMessages);

            var first = workflow.Import(request);
            var second = workflow.Import(request);
            Assert.True(first.ImportResultViewModel != null);
            Assert.True(second.ImportResultViewModel != null);
            Assert.True(first.ImportResultViewModel!.RowsAccepted > 0);
            Assert.Equal(first.ImportResultViewModel.DatabasePlayersCount, second.ImportResultViewModel!.DatabasePlayersCount);
            Assert.Contains("Re-import replaces current safe snapshot, not duplicate rows.", second.ImportResultViewModel.AuditDisplayRows);
            Assert.DoesNotContain("CurrentAbility: 200", string.Join(" ", second.ImportResultViewModel.AuditDisplayRows), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DatabaseMaintenanceBackupsAndSmokeResetAreSafe()
        {
            using var temp = new TemporaryDirectory();
            var maintenance = new LocalDatabaseMaintenanceService();
            var missing = maintenance.CreateTimestampedBackupCopy(Path.Combine(temp.Path, "missing.db"), Path.Combine(temp.Path, "Backups"));
            Assert.False(missing.Success);
            Assert.Contains("does not exist", missing.SafeMessage, StringComparison.OrdinalIgnoreCase);

            var mainPath = Path.Combine(temp.Path, "statlyn.db");
            using (RuntimeDatabaseFactory.CreateFile(mainPath))
            {
            }

            var backup = maintenance.CreateTimestampedBackupCopy(mainPath, Path.Combine(temp.Path, "Backups"));
            Assert.True(backup.Success, backup.ToSafeText());
            Assert.True(File.Exists(backup.Backup!.BackupPath));

            var smokePath = new StatlynDatabasePathResolver().ResolveSmokeTestPath(temp.Path);
            Assert.NotEqual(Path.GetFullPath(mainPath), Path.GetFullPath(smokePath), StringComparer.OrdinalIgnoreCase);
            var reset = maintenance.ResetSmokeTestDatabase(temp.Path);
            Assert.True(reset.Success, reset.ToSafeText());
            Assert.True(File.Exists(mainPath));
            Assert.True(File.Exists(smokePath));
            Assert.Contains(typeof(LocalDatabaseMaintenanceService).GetMethods(BindingFlags.Public | BindingFlags.Instance), method => method.Name == "ExplicitlyClearMainRuntimeDatabase");
            Assert.DoesNotContain("CurrentAbility", reset.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SafeReportSnapshotsDoNotExposeHiddenValuesOrFakeBenchmarkClaims()
        {
            using var temp = new TemporaryDirectory();
            var smoke = RunSmokeTest(temp.Path);
            Assert.True(smoke.Success, smoke.ToSafeText());
            using var factory = RuntimeDatabaseFactory.CreateFile(smoke.DatabasePath);

            var first = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { Limit = 1 }).Players[0];
            var profile = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = first.StatlynPlayerId });
            var shortlists = new ShortlistWorkflowService(factory).BuildPageViewModel(includeArchived: false);
            var scout = new ScoutLatestReportSummaryViewModel(new ScoutReportRecord(1, null, 1, first.StatlynPlayerId, DateTimeOffset.UtcNow, "Role", ScoutObservationRating.Unknown, ScoutObservationRating.Unknown, ScoutObservationRating.Unknown, ScoutObservationRating.Unknown, ScoutReportRecommendation.ScoutFurther, 50, string.Empty, string.Empty, string.Empty, ScoutFollowUpAction.None, "Professionalism: 20 but composed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            var benchmarks = new BenchmarkWorkflowService(factory).BuildPageViewModel();
            var readiness = new LocalProductReadinessService(factory, AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "StreamingAssets")).Run();
            var snapshots = new SafeReportSnapshotService();

            var text = string.Join("\n", new[]
            {
                snapshots.BuildPlayerProfileSummary(profile),
                snapshots.BuildShortlistSummary(shortlists.SelectedShortlist),
                snapshots.BuildScoutReportSummary(scout),
                snapshots.BuildBenchmarkSummary(benchmarks),
                snapshots.BuildReadinessReport(readiness)
            });

            Assert.Contains("generic/import, not FM26-verified", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No live FM26 data", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[redacted hidden scouting value]", text);
            Assert.DoesNotContain("Professionalism: 20", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fake percentile", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NavigationAndSmokeTestRemainReleaseCandidateSafe()
        {
            var items = UnityNavigationCatalog.Items;
            var names = items.Select(item => item.Name).ToArray();

            foreach (var expected in new[] { "Home", "Recruitment", "Player Profile", "Shortlists", "Scout Desk", "Role Lab", "Benchmarks", "Data Sources", "Diagnostics", "Squad", "Alerts", "Settings" })
            {
                Assert.Contains(expected, names);
            }

            Assert.All(items.Where(item => item.IsBuilt), item => Assert.DoesNotContain("not built yet", item.SafeSubtitle, StringComparison.OrdinalIgnoreCase));
            Assert.All(items.Where(item => !item.IsBuilt), item => Assert.Contains("not built yet", item.SafeSubtitle, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(items, item => item.SafeSubtitle.Contains("CurrentAbility", StringComparison.OrdinalIgnoreCase));

            using var temp = new TemporaryDirectory();
            var smoke = RunSmokeTest(temp.Path);
            Assert.True(smoke.Success, smoke.ToSafeText());
            Assert.DoesNotContain("fake live", smoke.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        private static DataSourceImportRequest BuildImportRequest(string fixturePath)
        {
            return new DataSourceImportRequest
            {
                CsvPath = fixturePath,
                SourceName = "Synthetic CSV fixture",
                LicenceStatus = "synthetic test fixture",
                AllowedUsage = "development fixture only",
                IsLicensed = true,
                SourceConfidence = 80,
                UsesBundledSafeFlagAssets = true,
                AllowsExport = true
            };
        }

        private static FixtureCsvPathResolutionResult ResolveFixture()
        {
            var fixture = new UnityFixtureCsvPathResolver().Resolve(AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "StreamingAssets"));
            Assert.True(fixture.Success, fixture.Message);
            return fixture;
        }

        private static UnitySmokeTestResult RunSmokeTest(string tempRoot)
        {
            return new UnitySmokeTestService().Run(new UnitySmokeTestOptions
            {
                TemporaryRoot = tempRoot,
                ApplicationDataPath = AppContext.BaseDirectory,
                StreamingAssetsPath = Path.Combine(AppContext.BaseDirectory, "StreamingAssets"),
                MainDatabasePath = Path.Combine(tempRoot, "statlyn.db"),
                ClearSmokeTestDatabase = true
            });
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statlyn-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                    {
                        Directory.Delete(Path, recursive: true);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
