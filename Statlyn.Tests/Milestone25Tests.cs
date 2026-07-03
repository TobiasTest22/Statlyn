using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone25Tests
    {
        [Fact]
        public void BenchmarkDomainModelsRepresentHonestStatusesAndSafeDefaults()
        {
            var noBenchmark = Result("xG", BenchmarkStatus.NoBenchmark, null, false);
            var insufficient = Result("xG", BenchmarkStatus.InsufficientSample, 88, false);
            var available = Result("xG", BenchmarkStatus.Available, 88, false);
            var generic = Result("xA", BenchmarkStatus.Available, 75, true);
            var allText = string.Join(" ", Enum.GetNames(typeof(BenchmarkScope))
                .Concat(Enum.GetNames(typeof(BenchmarkMetricType)))
                .Concat(Enum.GetNames(typeof(BenchmarkStatus))));

            Assert.Null(noBenchmark.Percentile);
            Assert.Null(insufficient.Percentile);
            Assert.Equal(88, available.Percentile);
            Assert.True(generic.IsGenericImportMetric);
            Assert.False(generic.IsVerifiedFm26Metric);
            Assert.Contains("RoleOutputProfile", allText);
            Assert.Contains("TacticalRolePair", allText);
            Assert.Contains("AttributeSupport", allText);
            Assert.DoesNotContain("CurrentAbility", allText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", allText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", allText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BenchmarkSchemaCreatesSafeTablesIndexesAndIsIdempotent()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            new StatlynDatabaseInitializer(factory).Initialize();
            new StatlynDatabaseInitializer(factory).Initialize();
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);

            Assert.Contains("CREATE TABLE IF NOT EXISTS BenchmarkDefinition", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS BenchmarkRun", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS BenchmarkMetricSnapshot", schema);
            Assert.Contains("IX_BenchmarkDefinition_BenchmarkName", schema);
            Assert.Contains("IX_BenchmarkDefinition_Scope", schema);
            Assert.Contains("IX_BenchmarkDefinition_PositionGroup", schema);
            Assert.Contains("IX_BenchmarkRun_BenchmarkDefinitionId", schema);
            Assert.Contains("IX_BenchmarkMetricSnapshot_BenchmarkRunId", schema);
            Assert.Contains("IX_BenchmarkMetricSnapshot_MetricKey", schema);
            Assert.Contains("MedianValue REAL NULL", schema);
            Assert.Contains("AverageValue REAL NULL", schema);
            Assert.DoesNotContain("Percentile", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PlayerValue", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", schema, StringComparison.OrdinalIgnoreCase);
            Assert.True(IndexExists(factory, "IX_BenchmarkMetricSnapshot_MetricKey"));
            Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics().SchemaVersion);
        }

        [Fact]
        public void BenchmarkRepositoryPersistsDefinitionsRunsSnapshotsAndSanitizesText()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var repository = new BenchmarkRepository(factory);

            var definition = repository.SaveDefinition(Definition("CurrentAbility: 200 Wide Benchmark", BenchmarkScope.PositionGroup, "WingerWideForward", new[] { "xG", "xA" }, minimumSampleSize: 2));
            var run = repository.SaveBenchmarkRun(new BenchmarkRunRecord(0, definition.Id, DateTimeOffset.UtcNow, 2, 1, "Professionalism: 20 aggregate run"));
            var snapshot = repository.SaveMetricSnapshot(new BenchmarkMetricSnapshot(0, run.Id, "xG", "xG", BenchmarkMetricType.PlayerStat, 2, 0.33, 0.33, 0.21, 0.44, "Synthetic CSV fixture", "CA 155 group", true, true));
            var stored = StoredBenchmarkText(factory);

            Assert.NotEqual(0, definition.Id);
            Assert.Equal(definition.Id, repository.LoadDefinition(definition.Id)!.Id);
            Assert.Single(repository.LoadDefinitions(includeArchived: false));
            Assert.Equal(run.Id, repository.LoadLatestRun(definition.Id)!.Id);
            Assert.Single(repository.LoadSnapshotsForRun(run.Id));
            Assert.Equal(snapshot.Id, repository.LoadLatestSnapshots(definition.Id)[0].Id);
            Assert.False(repository.LoadLatestSnapshots(definition.Id)[0].IsVerifiedFm26Metric);
            Assert.DoesNotContain("CurrentAbility: 200", stored);
            Assert.DoesNotContain("Professionalism: 20", stored);
            Assert.DoesNotContain("CA 155", stored);

            repository.ArchiveDefinition(definition.Id);
            Assert.Empty(repository.LoadDefinitions(includeArchived: false));
            Assert.Single(repository.LoadDefinitions(includeArchived: true));
            Assert.Throws<InvalidOperationException>(() => repository.SaveDefinition(TestPlayers.CreateExternalPlayer()));
        }

        [Fact]
        public void BenchmarkCalculationComputesAggregatesAndPercentilesFromImportedPlayers()
        {
            using var factory = CreateImportedDatabase();
            var definition = Definition("Global xG Benchmark", BenchmarkScope.GlobalDataset, string.Empty, new[] { "xG" }, minimumSampleSize: 2);
            var forwardId = StatlynPlayerIdByName(factory, "Synthetic Forward");
            var summary = new BenchmarkCalculationService(factory).Calculate(definition, forwardId).Summary;
            var metric = summary.Results.Single();

            Assert.Equal(BenchmarkStatus.Available, summary.OverallStatus);
            Assert.Equal(BenchmarkStatus.Available, metric.Status);
            Assert.Equal(2, metric.SampleSize);
            Assert.Equal(0.325, metric.BenchmarkMedian!.Value, 3);
            Assert.Equal(0.325, metric.BenchmarkAverage!.Value, 3);
            Assert.Equal(0.21, metric.BenchmarkMin!.Value, 3);
            Assert.Equal(0.44, metric.BenchmarkMax!.Value, 3);
            Assert.Equal(100, metric.Percentile);
            Assert.Contains("All imported sources", metric.ComparisonGroup);
            Assert.True(metric.IsGenericImportMetric);
            Assert.False(metric.IsVerifiedFm26Metric);
        }

        [Fact]
        public void BenchmarkCalculationHandlesInsufficientMissingNoBenchmarkFiltersAndMinutes()
        {
            using var factory = CreateImportedDatabase();
            var calculator = new BenchmarkCalculationService(factory);
            var wideId = StatlynPlayerIdByName(factory, "Synthetic Wide Player");

            var insufficient = calculator.Calculate(Definition("xG min sample", BenchmarkScope.GlobalDataset, string.Empty, new[] { "xG" }, minimumSampleSize: 3), wideId).Summary;
            var missing = calculator.Calculate(Definition("Shots missing", BenchmarkScope.GlobalDataset, string.Empty, new[] { "Shots" }, minimumSampleSize: 1), wideId).Summary;
            var empty = calculator.Calculate(Definition("Missing source", BenchmarkScope.Source, string.Empty, new[] { "xG" }, minimumSampleSize: 1, sourceName: "Missing Source")).Summary;
            var winger = calculator.Calculate(Definition("Wide xG", BenchmarkScope.PositionGroup, "WingerWideForward", new[] { "xG" }, minimumSampleSize: 1), wideId).Summary;
            var minutes = calculator.Calculate(Definition("xG minutes", BenchmarkScope.GlobalDataset, string.Empty, new[] { "xG" }, minimumSampleSize: 2, minimumMinutes: 900), wideId).Summary;
            var hidden = calculator.Calculate(Definition("Hidden current ability", BenchmarkScope.GlobalDataset, string.Empty, new[] { "CurrentAbility" }, minimumSampleSize: 1), wideId).Summary;

            Assert.Equal(BenchmarkStatus.InsufficientSample, insufficient.OverallStatus);
            Assert.Null(insufficient.Results[0].Percentile);
            Assert.Equal(BenchmarkStatus.MissingMetric, missing.OverallStatus);
            Assert.Null(missing.Results[0].BenchmarkMedian);
            Assert.Equal(BenchmarkStatus.NoBenchmark, empty.OverallStatus);
            Assert.Equal(BenchmarkStatus.Available, winger.OverallStatus);
            Assert.Equal(1, winger.Results[0].SampleSize);
            Assert.Equal(BenchmarkStatus.InsufficientSample, minutes.OverallStatus);
            Assert.Equal(1, minutes.Results[0].SampleSize);
            Assert.Equal(BenchmarkStatus.MissingMetric, hidden.OverallStatus);
            Assert.Null(hidden.Results[0].PlayerValue);
            Assert.DoesNotContain("CurrentAbility", string.Join(" ", hidden.Warnings), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AttributeSupportIsExcludedUnlessExplicitlyRequested()
        {
            using var factory = CreateImportedDatabase();
            var calculator = new BenchmarkCalculationService(factory);
            var forwardId = StatlynPlayerIdByName(factory, "Synthetic Forward");

            var plain = calculator.Calculate(Definition("Plain finishing", BenchmarkScope.GlobalDataset, string.Empty, new[] { "Finishing" }, minimumSampleSize: 1), forwardId).Summary;
            var explicitAttribute = calculator.Calculate(Definition("Attribute finishing", BenchmarkScope.GlobalDataset, string.Empty, new[] { "Attribute:Finishing" }, minimumSampleSize: 1), forwardId).Summary;

            Assert.Equal(BenchmarkStatus.MissingMetric, plain.OverallStatus);
            Assert.Equal(BenchmarkMetricType.PlayerStat, plain.Results[0].MetricType);
            Assert.Equal(BenchmarkStatus.Available, explicitAttribute.OverallStatus);
            Assert.Equal(BenchmarkMetricType.AttributeSupport, explicitAttribute.Results[0].MetricType);
            Assert.NotNull(explicitAttribute.Results[0].Percentile);
        }

        [Fact]
        public void BenchmarkSeedCreatesPositionSpecificGenericImportDefinitionsOnly()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var service = new BenchmarkSeedService(factory);
            var repository = new BenchmarkRepository(factory);

            var first = service.SeedDefaultDefinitions();
            var second = service.SeedDefaultDefinitions();
            var definitions = repository.LoadDefinitions(includeArchived: false);
            var goalkeeper = definitions.Single(definition => definition.BenchmarkName == "Goalkeeper Output Benchmark");
            var wide = definitions.Single(definition => definition.BenchmarkName == "Wide Attacker Output Benchmark");
            var stored = StoredBenchmarkText(factory);

            Assert.Equal(6, first.SeededDefinitionCount);
            Assert.Equal(first.ActiveSeedDefinitionCount, second.ActiveSeedDefinitionCount);
            Assert.Equal(6, definitions.Count);
            Assert.Equal("Goalkeeper", goalkeeper.PositionGroup);
            Assert.DoesNotContain("xA", goalkeeper.MetricKeys);
            Assert.DoesNotContain("Saves", wide.MetricKeys);
            Assert.All(definitions, definition => Assert.True(definition.IncludeFixtureData));
            Assert.DoesNotContain("FM26 verified", stored, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("official FM26", stored, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BenchmarkWorkflowRunsDefinitionsAndBuildsSafeUnityFacingViewModels()
        {
            using var factory = CreateImportedDatabase();
            var workflow = new BenchmarkWorkflowService(factory);
            workflow.SeedDefaultDefinitions();

            var batch = workflow.RunAllActiveDefinitions();
            var page = workflow.BuildPageViewModel();
            var summary = workflow.BuildPlayerBenchmarkSummaryViewModel(StatlynPlayerIdByName(factory, "Synthetic Forward"));
            var pageText = page.SafeMessage + " " + string.Join(" ", page.Definitions.Select(definition =>
                definition.BenchmarkName + " " + definition.Scope + " " + definition.PositionGroup + " " + definition.VerificationLabel + " " +
                string.Join(" ", definition.MetricKeys) + " " +
                string.Join(" ", definition.Snapshots.Select(snapshot => snapshot.MetricKey + " " + snapshot.Median + " " + snapshot.Average + " " + snapshot.ComparisonGroup + " " + snapshot.VerificationLabel))));

            Assert.Equal(6, batch.DefinitionsRun);
            Assert.Equal(6, page.Definitions.Count);
            Assert.NotEmpty(page.Definitions.SelectMany(definition => definition.Snapshots));
            Assert.Contains(page.Definitions, definition => definition.LatestRun != null && definition.LatestRun.SafeMessage.Contains("Insufficient", StringComparison.OrdinalIgnoreCase));
            Assert.NotEqual("NoBenchmark", summary.Status);
            Assert.DoesNotContain("CurrentAbility", pageText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", pageText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Raw", pageText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FM26 verified", pageText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PlayerProfileVisualsShowNoBenchmarkInsufficientAndAvailableHonestly()
        {
            using var noBenchmarkFactory = CreateImportedDatabase();
            var noBenchmarkId = StatlynPlayerIdByName(noBenchmarkFactory, "Synthetic Forward");
            var noBenchmarkReport = PlayerProfileReportViewModel.From(new PlayerProfileQueryService(noBenchmarkFactory).Query(new PlayerProfileQuery { StatlynPlayerId = noBenchmarkId }));
            var noBenchmarkVisual = StatlynVisualAnalyticsBuilder.Build(noBenchmarkReport).BenchmarkStatus;

            using var insufficientFactory = CreateImportedDatabase();
            var insufficientRepository = new BenchmarkRepository(insufficientFactory);
            insufficientRepository.SaveDefinition(Definition("Strict xG", BenchmarkScope.GlobalDataset, string.Empty, new[] { "xG" }, minimumSampleSize: 3));
            var insufficientId = StatlynPlayerIdByName(insufficientFactory, "Synthetic Forward");
            var insufficientReport = PlayerProfileReportViewModel.From(new PlayerProfileQueryService(insufficientFactory).Query(new PlayerProfileQuery { StatlynPlayerId = insufficientId }));
            var insufficientVisual = StatlynVisualAnalyticsBuilder.Build(insufficientReport).BenchmarkStatus;

            using var availableFactory = CreateImportedDatabase();
            var availableRepository = new BenchmarkRepository(availableFactory);
            availableRepository.SaveDefinition(Definition("Available xG", BenchmarkScope.GlobalDataset, string.Empty, new[] { "xG" }, minimumSampleSize: 2));
            var availableId = StatlynPlayerIdByName(availableFactory, "Synthetic Forward");
            var availableReport = PlayerProfileReportViewModel.From(new PlayerProfileQueryService(availableFactory).Query(new PlayerProfileQuery { StatlynPlayerId = availableId }));
            var availableVisual = StatlynVisualAnalyticsBuilder.Build(availableReport).BenchmarkStatus;

            Assert.False(noBenchmarkVisual.HasBenchmark);
            Assert.Equal("No benchmark yet.", noBenchmarkVisual.SafeMessage);
            Assert.False(insufficientVisual.HasBenchmark);
            Assert.Contains("Insufficient sample", insufficientVisual.SafeMessage);
            Assert.Null(insufficientVisual.Percentile);
            Assert.True(availableVisual.HasBenchmark);
            Assert.Equal(100, availableVisual.Percentile);
            Assert.Equal(2, availableVisual.SampleSize);
            Assert.Contains("All imported sources", availableVisual.ComparisonGroup);
            Assert.DoesNotContain("CurrentAbility", availableVisual.SafeMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RecruitmentCentreRowsExposeBenchmarkIndicatorWithoutFakePercentiles()
        {
            using var emptyFactory = CreateImportedDatabase();
            var emptyRows = new RecruitmentCentreQueryService(emptyFactory).Query(new RecruitmentCentreQuery { Limit = 10 }).Players.Select(RecruitmentCentrePlayerRowViewModel.From).ToList();

            using var availableFactory = CreateImportedDatabase();
            new BenchmarkRepository(availableFactory).SaveDefinition(Definition("Available xG", BenchmarkScope.GlobalDataset, string.Empty, new[] { "xG" }, minimumSampleSize: 2));
            var availableRows = new RecruitmentCentreQueryService(availableFactory).Query(new RecruitmentCentreQuery { Limit = 10 }).Players.Select(RecruitmentCentrePlayerRowViewModel.From).ToList();
            var available = availableRows.Single(row => row.Name == "Synthetic Forward");
            var empty = emptyRows.Single(row => row.Name == "Synthetic Forward");
            var visual = RecruitmentCentreMiniVisualBuilder.Build(available);

            Assert.Equal("NoBenchmark", empty.BenchmarkIndicator.Status);
            Assert.True(string.IsNullOrWhiteSpace(empty.BenchmarkIndicator.Percentile));
            Assert.Equal("Available", available.BenchmarkIndicator.Status);
            Assert.Equal("xG", available.BenchmarkIndicator.KeyMetric);
            Assert.Equal(2, available.BenchmarkIndicator.SampleSize);
            Assert.Equal(100, visual.BenchmarkStatus.Percentile);
            Assert.DoesNotContain("CurrentAbility", available.BenchmarkIndicator.SafeMessage, StringComparison.OrdinalIgnoreCase);
        }

        private static BenchmarkMetricResult Result(string metricKey, BenchmarkStatus status, double? percentile, bool generic)
        {
            return new BenchmarkMetricResult(
                metricKey,
                metricKey,
                BenchmarkMetricType.PlayerStat,
                1,
                1,
                1,
                1,
                1,
                percentile,
                1,
                status,
                status.ToString(),
                "Synthetic CSV fixture",
                "All imported sources",
                generic,
                false);
        }

        private static BenchmarkDefinition Definition(
            string name,
            BenchmarkScope scope,
            string positionGroup,
            IReadOnlyList<string> metrics,
            int minimumSampleSize,
            int minimumMinutes = 0,
            string sourceName = "")
        {
            var now = DateTimeOffset.UtcNow;
            return new BenchmarkDefinition(
                0,
                name,
                scope,
                sourceName,
                positionGroup,
                string.Empty,
                string.Empty,
                string.Empty,
                metrics,
                minimumSampleSize,
                minimumMinutes,
                includeFixtureData: true,
                now,
                now,
                false);
        }

        private static StatlynDbConnectionFactory CreateImportedDatabase()
        {
            var factory = RuntimeDatabaseFactory.CreateInMemory();
            new ImportPipelineService(factory).Import(CreateCsvProvider(FixturePath()), ImportPipelineOptions.CreateDefault());
            return factory;
        }

        private static CsvImportProvider CreateCsvProvider(string path)
        {
            return new CsvImportProvider(path, CreateFixtureMetadata(), new FieldMappingSet(Array.Empty<FieldMapping>()));
        }

        private static SourceMetadata CreateFixtureMetadata()
        {
            return new SourceMetadata(
                "Synthetic CSV fixture",
                ProviderType.Csv,
                false,
                true,
                "synthetic test fixture",
                "development fixture only",
                false,
                false,
                true,
                false,
                true,
                DateTimeOffset.UtcNow,
                80);
        }

        private static string FixturePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
        }

        private static string StatlynPlayerIdByName(StatlynDbConnectionFactory factory, string name)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT StatlynPlayerId FROM Player WHERE DisplayName = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool IndexExists(StatlynDbConnectionFactory factory, string indexName)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = $name;";
            command.Parameters.AddWithValue("$name", indexName);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
        }

        private static string StoredBenchmarkText(StatlynDbConnectionFactory factory)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                @"SELECT
                    COALESCE((SELECT group_concat(BenchmarkName || ' ' || Scope || ' ' || SourceName || ' ' || PositionGroup || ' ' || RoleProfileName || ' ' || TacticalRoleName || ' ' || TacticalRolePairName || ' ' || MetricKeys, ' ') FROM BenchmarkDefinition), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(SafeMessage, ' ') FROM BenchmarkRun), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(MetricKey || ' ' || FieldName || ' ' || MetricType || ' ' || MedianValue || ' ' || AverageValue || ' ' || MinimumValue || ' ' || MaximumValue || ' ' || SourceName || ' ' || ComparisonGroup || ' ' || IsGenericImportMetric || ' ' || IsVerifiedFm26Metric, ' ') FROM BenchmarkMetricSnapshot), '');";
            return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
