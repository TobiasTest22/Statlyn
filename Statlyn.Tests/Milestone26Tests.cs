using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.Data.Workflow;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone26Tests
    {
        [Fact]
        public void UnitySmokeTestRunsFullCsvOnlyWorkflowAgainstTempDatabase()
        {
            using var temp = new TemporaryDirectory();
            var result = RunSmokeTest(temp.Path);

            Assert.True(result.Success, result.ToSafeText());
            Assert.Equal(UnitySmokeTestService.ExpectedStepNames(), result.Steps.Select(step => step.StepName).ToList());
            Assert.All(result.Steps, step => Assert.Equal(UnitySmokeTestStepStatus.Passed, step.Status));
            Assert.True(File.Exists(result.DatabasePath));
            Assert.Contains("CSV-only", result.SafeSummary);
            Assert.DoesNotContain("live FM26", result.SafeSummary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UnitySmokeTestCreatesImportedPlayersShortlistScoutReportRoleLabAndBenchmarks()
        {
            using var temp = new TemporaryDirectory();
            var result = RunSmokeTest(temp.Path);

            using var factory = RuntimeDatabaseFactory.CreateFile(result.DatabasePath);
            var recruitment = new Statlyn.Data.Recruitment.RecruitmentCentreQueryService(factory).Query(new Statlyn.Data.Recruitment.RecruitmentCentreQuery { Limit = 10 });
            var shortlists = new ShortlistWorkflowService(factory).BuildPageViewModel(includeArchived: false);
            var scoutDesk = new ScoutDeskWorkflowService(factory).BuildPageViewModel(new ScoutDeskQuery());
            var roleLab = new Statlyn.Data.RoleLab.RoleLabRepository(factory).LoadRoles(includeArchived: false);
            var benchmarks = new BenchmarkRepository(factory).LoadDefinitions(includeArchived: false);
            var benchmarkRuns = benchmarks.Select(definition => new BenchmarkRepository(factory).LoadLatestRun(definition.Id)).Where(run => run != null).ToList();

            Assert.NotEmpty(recruitment.Players);
            Assert.NotEmpty(shortlists.SelectedShortlist.Players);
            Assert.NotEmpty(scoutDesk.Assignments);
            Assert.NotNull(scoutDesk.SelectedAssignment);
            Assert.NotEmpty(roleLab);
            Assert.NotEmpty(benchmarks);
            Assert.NotEmpty(benchmarkRuns);
        }

        [Fact]
        public void UnitySmokeTestDoesNotRequireFm26OrPolluteMainDatabase()
        {
            using var temp = new TemporaryDirectory();
            var resolver = new StatlynDatabasePathResolver();
            var mainPath = resolver.ResolvePath(temp.Path, StatlynDatabasePathMode.RuntimeMain);
            using (RuntimeDatabaseFactory.CreateFile(mainPath))
            {
            }

            var result = new UnitySmokeTestService().Run(new UnitySmokeTestOptions
            {
                TemporaryRoot = temp.Path,
                ApplicationDataPath = AppContext.BaseDirectory,
                StreamingAssetsPath = Path.Combine(AppContext.BaseDirectory, "StreamingAssets"),
                MainDatabasePath = mainPath,
                ClearSmokeTestDatabase = true
            });

            Assert.True(result.Success, result.ToSafeText());
            Assert.NotEqual(Path.GetFullPath(mainPath), Path.GetFullPath(result.DatabasePath), StringComparer.OrdinalIgnoreCase);
            using var mainFactory = RuntimeDatabaseFactory.CreateFile(mainPath);
            var mainRows = new Statlyn.Data.Recruitment.RecruitmentCentreQueryService(mainFactory).Query(new Statlyn.Data.Recruitment.RecruitmentCentreQuery { Limit = 10 });
            Assert.Empty(mainRows.Players);
            Assert.Contains(result.Warnings, warning => warning.Contains("FM26 is not required", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("FM26 supported", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("live player", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DatabasePathModesAreSeparatedAndInMemoryStillWorks()
        {
            using var temp = new TemporaryDirectory();
            var resolver = new StatlynDatabasePathResolver();
            var main = resolver.ResolvePath(temp.Path, StatlynDatabasePathMode.RuntimeMain);
            var smoke = resolver.ResolveSmokeTestPath(temp.Path);
            var memory = resolver.ResolvePath(temp.Path, StatlynDatabasePathMode.UnitTestInMemory);

            Assert.EndsWith("statlyn.db", main, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("statlyn-smoke-test.db", smoke, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(Path.GetFullPath(main), Path.GetFullPath(smoke), StringComparer.OrdinalIgnoreCase);
            Assert.Equal(":memory:", memory);

            using var factory = RuntimeDatabaseFactory.CreateForMode(temp.Path, StatlynDatabasePathMode.UnitTestInMemory);
            Assert.StartsWith("in-memory:", factory.DatabasePath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FixtureResolverFindsTestFixtureAndReportsMissingSafely()
        {
            using var temp = new TemporaryDirectory();
            var resolver = new UnityFixtureCsvPathResolver();

            var found = resolver.Resolve(AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "StreamingAssets"));
            var missing = resolver.Resolve(temp.Path, Path.Combine(temp.Path, "StreamingAssets"));

            Assert.True(found.Success);
            Assert.EndsWith("players.sample.csv", found.FilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Synthetic", found.Message);
            Assert.False(missing.Success);
            Assert.Contains("Run tools/copy-managed-to-unity.ps1", missing.Message);
            Assert.Contains(missing.CandidatePaths, path => path.Contains("StreamingAssets", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void UnityNavigationMetadataIncludesBuiltPagesAndSafePlaceholders()
        {
            var items = UnityNavigationCatalog.Items;
            var names = items.Select(item => item.Name).ToList();

            Assert.Contains("Benchmarks", names);
            Assert.Contains("Role Lab", names);
            Assert.Contains("Scout Desk", names);
            Assert.Contains("Diagnostics", names);
            Assert.Contains(items, item => item.Name == "Squad" && !item.IsBuilt && item.SafeSubtitle.Contains("not built yet", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, item => item.Name == "Alerts" && !item.IsBuilt && item.SafeSubtitle.Contains("No fake", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(items, item => item.Name == "Data Sources" && item.IsBuilt && item.SafeSubtitle.Contains("CSV", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(items, item => item.SafeSubtitle.Contains("live FM26 data", StringComparison.OrdinalIgnoreCase) && !item.SafeSubtitle.Contains("no live", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void SmokeTestMissingFixtureFailsCleanlyWithoutFakeData()
        {
            using var temp = new TemporaryDirectory();
            var result = new UnitySmokeTestService().Run(new UnitySmokeTestOptions
            {
                TemporaryRoot = temp.Path,
                ApplicationDataPath = temp.Path,
                StreamingAssetsPath = Path.Combine(temp.Path, "StreamingAssets"),
                MainDatabasePath = new StatlynDatabasePathResolver().ResolvePath(temp.Path, StatlynDatabasePathMode.RuntimeMain),
                ClearSmokeTestDatabase = true
            });

            Assert.False(result.Success);
            Assert.Contains(result.Steps, step => step.StepName == "Locate synthetic fixture CSV" && step.Status == UnitySmokeTestStepStatus.Failed);
            Assert.Contains("Synthetic fixture CSV was not found", result.ToSafeText());
            Assert.DoesNotContain("fake", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", result.ToSafeText(), StringComparison.OrdinalIgnoreCase);
        }

        private static UnitySmokeTestResult RunSmokeTest(string tempRoot)
        {
            return new UnitySmokeTestService().Run(new UnitySmokeTestOptions
            {
                TemporaryRoot = tempRoot,
                ApplicationDataPath = AppContext.BaseDirectory,
                StreamingAssetsPath = Path.Combine(AppContext.BaseDirectory, "StreamingAssets"),
                MainDatabasePath = new StatlynDatabasePathResolver().ResolvePath(tempRoot, StatlynDatabasePathMode.RuntimeMain),
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
