using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Recruitment;
using Statlyn.Data.Workflow;
using Statlyn.DataProviders.Import;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone19Tests
    {
        [Fact]
        public void RecruitmentCentreQueryReturnsImportedPlayersAndSources()
        {
            using var factory = CreateImportedDatabase();

            var result = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery());

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Players.Count);
            Assert.Contains("Synthetic CSV fixture", result.Sources);
            Assert.Contains(result.Diagnostics, item => item.Contains("persisted safe", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void RecruitmentCentreSearchAndFiltersWork()
        {
            using var factory = CreateImportedDatabase();
            var service = new RecruitmentCentreQueryService(factory);

            var search = service.Query(new RecruitmentCentreQuery { SearchText = "Wide" });
            var position = service.Query(new RecruitmentCentreQuery { PrimaryPosition = "RW" });
            var source = service.Query(new RecruitmentCentreQuery { SourceName = "Synthetic CSV fixture" });
            var blocked = service.Query(new RecruitmentCentreQuery { HasBlockedFields = true });

            Assert.Single(search.Players);
            Assert.Equal("Synthetic Wide Player", search.Players[0].DisplayName);
            Assert.Single(position.Players);
            Assert.Equal(2, source.TotalCount);
            Assert.Equal(2, blocked.TotalCount);
        }

        [Fact]
        public void RecruitmentCentreLoadsRoleFitConfidenceBlockedCountsAndUnknownTacticalFit()
        {
            using var factory = CreateImportedDatabase();

            var row = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery()).Players.First();

            Assert.True(row.RoleFit.HasValue);
            Assert.True(row.Confidence.HasValue);
            Assert.True(row.BlockedFieldCount > 0);
            Assert.Equal("Unknown", row.TacticalFitDisplay);
            Assert.NotNull(row.Recommendation);
        }

        [Fact]
        public void RecruitmentCentreRowsHideHiddenValuesAndRawProviderEntities()
        {
            using var factory = CreateImportedDatabase();
            var result = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery());
            var text = RowsText(result);

            Assert.DoesNotContain("CurrentAbility", text);
            Assert.DoesNotContain("Professionalism:20", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("IRawFootballEntity", text);
            Assert.DoesNotContain("fake live", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RecruitmentCentreKeyOutputMetricsIncludeExpectedOutputStats()
        {
            using var factory = CreateImportedDatabase();

            var wide = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { SearchText = "Wide" }).Players.Single();
            var metrics = string.Join(" ", wide.KeyOutputMetrics);

            Assert.Contains("xA", metrics);
            Assert.Contains("xG", metrics);
            Assert.DoesNotContain("Finishing", metrics);
        }

        [Fact]
        public void RecruitmentOutputSummaryIsPositionSpecificAndOutputFirst()
        {
            var service = new RecruitmentOutputSummaryService();
            var wide = service.Build("RW", new[]
            {
                Stat("xA", 0.32),
                Stat("xG", 0.21),
                Stat("ProgressiveCarries", 6)
            }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("WingerWideForward"), null);
            var centreBack = service.Build("CB", new[]
            {
                Stat("AerialDuelsWonPct", 68),
                Stat("Clearances", 7),
                Stat("Blocks", 2)
            }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("CentreBack"), null);
            var goalkeeper = service.Build("GK", new[]
            {
                Stat("Saves", 4),
                Stat("SavePercentage", 78)
            }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("Goalkeeper"), null);

            Assert.Contains(wide.CoreMetrics, metric => metric.StartsWith("xA", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("SuccessfulDribbles", centreBack.MissingCoreMetrics);
            Assert.DoesNotContain("SuccessfulDribbles", goalkeeper.MissingCoreMetrics);
            Assert.DoesNotContain("Finishing", string.Join(" ", wide.CoreMetrics.Concat(wide.SupportingMetrics)));
        }

        [Fact]
        public void MissingCoreOutputMetricsAreMissingNotZeroAndGenericMetricsRemainFm26Unverified()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            new PerformanceMetricDefinitionRepository(factory).SeedGenericDefaults();
            var service = new RecruitmentOutputSummaryService();

            var summary = service.Build("ST", Array.Empty<PlayerStatRecord>(), Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("StrikerForward"), null);
            var definitions = new PerformanceMetricDefinitionRepository(factory).LoadAll();

            Assert.Contains("xG", summary.MissingCoreMetrics);
            Assert.DoesNotContain("xG 0", string.Join(" ", summary.CoreMetrics));
            Assert.All(definitions, definition => Assert.False(definition.IsVerifiedFm26Metric));
        }

        [Fact]
        public void RecruitmentCentreViewModelsAreUiSafe()
        {
            using var factory = CreateImportedDatabase();
            var query = new RecruitmentCentreQuery();
            var result = new RecruitmentCentreQueryService(factory).Query(query);
            var viewModel = RecruitmentCentreViewModel.From(result, query, factory.DatabasePath);
            var text = ViewModelText(viewModel);

            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("CurrentAbility 200", text);
            Assert.DoesNotContain("Professionalism 20", text);
            Assert.Contains("xG", text);
            Assert.Contains(viewModel.Players, row => row.BlockedFieldCount > 0);
            Assert.DoesNotContain(typeof(PlayerRawSnapshot), typeof(RecruitmentCentrePlayerRowViewModel).GetProperties().Select(property => property.PropertyType));
            Assert.DoesNotContain(typeof(Core.Abstractions.IRawFootballEntity), typeof(RecruitmentCentrePlayerRowViewModel).GetProperties().Select(property => property.PropertyType));
        }

        [Fact]
        public void PersistedImportedPlayerCanBuildSafeProfilePreview()
        {
            using var factory = CreateImportedDatabase();
            var row = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery()).Players.First();

            var profile = new RecruitmentCentreProfilePreviewService(factory).LoadProfile(row.StatlynPlayerId)!;
            var text = profile.PlayerName + " " + profile.BlockedDataNotice.SafeMessage + " " + string.Join(" ", profile.BlockedDataNotice.Categories) + " " + profile.RoleFitVisual.TacticalFitLabel;

            Assert.NotNull(profile);
            Assert.Contains(profile.PlayerName, new[] { "Synthetic Forward", "Synthetic Wide Player" });
            Assert.DoesNotContain("CurrentAbility 200", text);
            Assert.DoesNotContain("Professionalism 20", text);
            Assert.Contains("unknown", profile.RoleFitVisual.TacticalFitLabel, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RecruitmentCentreEmptyDatabaseAndDoubleImportStayHonest()
        {
            using (var empty = RuntimeDatabaseFactory.CreateInMemory())
            {
                var emptyResult = new RecruitmentCentreQueryService(empty).Query(new RecruitmentCentreQuery());
                Assert.Empty(emptyResult.Players);
                Assert.Contains("No imported players", emptyResult.SafeMessage);
            }

            using (var factory = RuntimeDatabaseFactory.CreateInMemory())
            {
                ImportFixture(factory);
                ImportFixture(factory);
                var result = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery());

                Assert.Equal(2, result.TotalCount);
                Assert.Equal(2, CountRows(factory, "Player"));
                Assert.Equal(2, CountWhere(factory, "PlayerStat", "StatName = 'xG'"));
            }
        }

        private static StatlynDbConnectionFactory CreateImportedDatabase()
        {
            var factory = RuntimeDatabaseFactory.CreateInMemory();
            ImportFixture(factory);
            return factory;
        }

        private static void ImportFixture(StatlynDbConnectionFactory factory)
        {
            new ImportPipelineService(factory).Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());
        }

        private static CsvImportProvider CreateCsvProvider()
        {
            return new CsvImportProvider(Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv"), CreateFixtureMetadata(), new FieldMappingSet(Array.Empty<FieldMapping>()));
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

        private static PlayerStatRecord Stat(string name, double value)
        {
            return new PlayerStatRecord(1, "PlayerStat:" + name, name, value, 1000, false, "test", "test", 80);
        }

        private static string RowsText(RecruitmentCentreResult result)
        {
            return string.Join(" ", result.Players.Select(row => row.DisplayName + " " + string.Join(" ", row.KeyOutputMetrics) + " " + string.Join(" ", row.KeyWarnings)));
        }

        private static string ViewModelText(RecruitmentCentreViewModel viewModel)
        {
            var builder = new StringBuilder();
            foreach (var row in viewModel.Players)
            {
                builder.Append(row.Name).Append(' ')
                    .Append(row.Recommendation).Append(' ')
                    .Append(string.Join(" ", row.KeyOutputMetrics)).Append(' ')
                    .Append(string.Join(" ", row.Warnings)).Append(' ');
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
