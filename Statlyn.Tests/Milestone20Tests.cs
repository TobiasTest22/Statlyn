using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Core.Abstractions;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone20Tests
    {
        [Fact]
        public void PlayerProfileQueryLoadsImportedSyntheticPlayerSafely()
        {
            using var factory = CreateImportedDatabase();
            var statlynPlayerId = StatlynPlayerIdByName(factory, "Synthetic Wide Player");

            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = statlynPlayerId });
            var text = ResultText(result);

            Assert.True(result.Success);
            Assert.Equal(statlynPlayerId, result.Player!.StatlynPlayerId);
            Assert.NotNull(result.MaskedPlayer);
            Assert.NotNull(result.LatestRoleScore);
            Assert.NotNull(result.RoleOutputSummary);
            Assert.Contains("xA", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("IRawFootballEntity", text);
            Assert.DoesNotContain("CurrentAbility 199", text);
            Assert.DoesNotContain("Professionalism 19", text);
            Assert.DoesNotContain(typeof(PlayerRawSnapshot), PublicPropertyTypes(typeof(PlayerProfileResult)));
            Assert.DoesNotContain(typeof(IRawFootballEntity), PublicPropertyTypes(typeof(PlayerProfileResult)));
        }

        [Fact]
        public void MissingPlayerReturnsSafeNotFoundResult()
        {
            using var factory = CreateImportedDatabase();

            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = "missing-player" });

            Assert.False(result.Success);
            Assert.Contains("not found", result.SafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.MaskedPlayer);
            Assert.Contains("persisted safe", string.Join(" ", result.Diagnostics), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MissingRoleScoreAndNullTacticalFitStayHonest()
        {
            using var factory = CreateImportedDatabase();
            var statlynPlayerId = StatlynPlayerIdByName(factory, "Synthetic Forward");
            DeleteRoleScores(factory, PlayerIdByName(factory, "Synthetic Forward"));

            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = statlynPlayerId });
            var report = PlayerProfileReportViewModel.From(result);

            Assert.True(result.Success);
            Assert.Equal("Not scored", result.LatestRoleScore!.RoleName);
            Assert.Equal("Not scored", report.RoleName);
            Assert.Equal("Unknown", result.TacticalFitDisplay);
            Assert.Equal("Unknown", report.TacticalFitDisplay);
            Assert.DoesNotContain("Tactical fit: 0", ReportText(report), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SourceAndBlockedAuditsAreSafeForImportedCsv()
        {
            using var factory = CreateImportedDatabase();
            var statlynPlayerId = StatlynPlayerIdByName(factory, "Synthetic Wide Player");

            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = statlynPlayerId });
            var report = PlayerProfileReportViewModel.From(result);
            var text = ReportText(report);

            Assert.True(result.IsFixtureMode);
            Assert.False(result.IsLiveFm26Data);
            Assert.False(report.IsLiveFm26Data);
            Assert.Contains("No live FM26 data", text);
            Assert.True(report.BlockedDataNotice.Count > 0);
            Assert.Contains(report.BlockedDataNotice.Categories, category => category.Contains("CurrentAbility", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("Raw values are not shown", report.BlockedDataNotice.SafeMessage);
            Assert.DoesNotContain("199", report.BlockedDataNotice.SafeMessage);
            Assert.DoesNotContain("Professionalism 19", text);
        }

        [Fact]
        public void PlayerProfileReportBuildsOnlyFromSafeResult()
        {
            using var factory = CreateImportedDatabase();
            var statlynPlayerId = StatlynPlayerIdByName(factory, "Synthetic Forward");
            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = statlynPlayerId });

            var report = PlayerProfileReportViewModel.From(result);

            Assert.Equal(statlynPlayerId, report.StatlynPlayerId);
            Assert.Throws<InvalidOperationException>(() => PlayerProfileReportViewModel.From(TestPlayers.CreateExternalPlayer()));
            Assert.DoesNotContain(typeof(PlayerRawSnapshot), PublicPropertyTypes(typeof(PlayerProfileReportViewModel)));
            Assert.DoesNotContain(typeof(IRawFootballEntity), PublicPropertyTypes(typeof(PlayerProfileReportViewModel)));
        }

        [Fact]
        public void ReportIsOutputFirstAndAttributesAreSupportOnly()
        {
            using var factory = CreateImportedDatabase();
            var wide = BuildReport(factory, "Synthetic Wide Player");
            var striker = BuildReport(factory, "Synthetic Forward");

            Assert.Contains(wide.CoreOutputMetrics, metric => metric.Label == "xA");
            Assert.Contains(striker.CoreOutputMetrics, metric => metric.Label == "xG");
            Assert.Contains(striker.CoreOutputMetrics, metric => metric.Label == "Goals");
            Assert.DoesNotContain(wide.CoreOutputMetrics, metric => metric.Label == "Finishing");
            Assert.DoesNotContain(striker.CoreOutputMetrics, metric => metric.Label == "Finishing");
            Assert.NotEmpty(wide.AttributeSupportCards);
            Assert.All(wide.AttributeSupportCards, card => Assert.Contains("Supporting", card.Caption, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PositionSpecificSummariesDoNotRequireWrongCoreMetrics()
        {
            var service = new RecruitmentOutputSummaryService();
            var centreBack = service.Build("CB", new[]
            {
                Stat("AerialDuelsWonPct", 71),
                Stat("Clearances", 8),
                Stat("Blocks", 2)
            }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("CentreBack"), null);
            var goalkeeper = service.Build("GK", new[]
            {
                Stat("Saves", 5),
                Stat("SavePercentage", 82)
            }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("Goalkeeper"), null);

            Assert.DoesNotContain("SuccessfulDribbles", centreBack.MissingCoreMetrics);
            Assert.DoesNotContain("xG", goalkeeper.MissingCoreMetrics);
            Assert.DoesNotContain("Shots", goalkeeper.MissingCoreMetrics);
        }

        [Fact]
        public void MissingOutputMetricsAndGenericStatusCreateWarningsNotZeros()
        {
            using var factory = CreateImportedDatabase();
            var report = BuildReport(factory, "Synthetic Wide Player");
            var text = ReportText(report);

            Assert.Contains(report.MissingOutputMetrics, metric => metric == "SuccessfulDribbles" || metric == "ProgressiveCarries");
            Assert.DoesNotContain("ProgressiveCarries 0", text);
            Assert.Contains("not FM26-verified", text);
            Assert.Contains(report.ScoutActionCards, action => action.Title.Contains("Collect missing output", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(report.ScoutActionCards, action => action.Title.Contains("Treat metrics", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MinutesAndMissingMinutesAreRepresentedInDataQuality()
        {
            using (var factory = CreateImportedDatabase())
            {
                var report = BuildReport(factory, "Synthetic Forward");

                Assert.Contains(report.DataQualityCards, card => card.Label == "Sample minutes" && card.Value == "1040");
                Assert.Contains(report.CoreOutputMetrics, metric => metric.Sample.Contains("1040"));
            }

            using (var factory = CreateImportedDatabaseWithoutMinutes())
            {
                var report = BuildReport(factory, "Synthetic No Minutes");

                Assert.Contains(report.DataQualityCards, card => card.Label == "Sample minutes" && card.Value == "Missing");
                Assert.Contains(report.KeyWarnings, warning => warning.Contains("minutes are missing", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(report.CoreOutputMetrics, metric => metric.Sample.Contains("Minutes missing", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public void VisualSectionsAvoidFakePercentilesAndShowNoBenchmark()
        {
            using var factory = CreateImportedDatabase();
            var report = BuildReport(factory, "Synthetic Wide Player");
            var text = ReportText(report);

            Assert.Contains(report.VisualSections, section => section.Title == "Benchmark" && section.Summary.Contains("No benchmark yet", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("90th percentile", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Fixture comparison group", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(report.VisualSections, section => section.Title == "Blocked Data" && !string.Join(" ", section.Rows).Contains("Professionalism 19", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void LowConfidenceProducesScoutFurtherAction()
        {
            using var factory = CreateImportedDatabase(sourceConfidence: 50);
            var report = BuildReport(factory, "Synthetic Wide Player");

            Assert.Contains(report.ScoutActionCards, action => action.Title == "Scout further");
            Assert.Contains(report.DataQualityCards, card => card.Label == "Metric status" && card.Value.Contains("Generic/import", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(report.ScoutActionCards, action => action.Reason.Contains("No benchmark", StringComparison.OrdinalIgnoreCase) || action.Title.Contains("Benchmark", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void RecruitmentCentreRowBuildsSameSafeProfileReport()
        {
            using var factory = CreateImportedDatabase();
            var row = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { SearchText = "Wide" }).Players.Single();

            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = row.StatlynPlayerId });
            var report = PlayerProfileReportViewModel.From(result);

            Assert.Equal(row.StatlynPlayerId, report.StatlynPlayerId);
            Assert.True(report.BlockedDataNotice.Count > 0);
            Assert.False(report.IsLiveFm26Data);
            Assert.DoesNotContain("CurrentAbility 199", ReportText(report));
            Assert.DoesNotContain("Professionalism 19", ReportText(report));
        }

        private static PlayerProfileReportViewModel BuildReport(StatlynDbConnectionFactory factory, string displayName)
        {
            var id = StatlynPlayerIdByName(factory, displayName);
            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = id });
            return PlayerProfileReportViewModel.From(result);
        }

        private static StatlynDbConnectionFactory CreateImportedDatabase(int sourceConfidence = 80)
        {
            var factory = RuntimeDatabaseFactory.CreateInMemory();
            new ImportPipelineService(factory).Import(CreateCsvProvider(FixturePath(), sourceConfidence), ImportPipelineOptions.CreateDefault());
            return factory;
        }

        private static StatlynDbConnectionFactory CreateImportedDatabaseWithoutMinutes()
        {
            var path = CreateCsv(
                "SourcePlayerId,DisplayName,Age,Nationality,PrimaryPosition,Finishing,Pace,Acceleration,xG,xA,Goals,TopSpeed,CurrentAbility,Professionalism",
                "no-minutes,Synthetic No Minutes,23,Romania,ST,13,12,14,0.37,0.11,6,32.4,188,18");
            var factory = RuntimeDatabaseFactory.CreateInMemory();
            new ImportPipelineService(factory).Import(CreateCsvProvider(path, 80), ImportPipelineOptions.CreateDefault());
            return factory;
        }

        private static CsvImportProvider CreateCsvProvider(string path, int sourceConfidence)
        {
            return new CsvImportProvider(path, CreateFixtureMetadata(sourceConfidence), new FieldMappingSet(Array.Empty<FieldMapping>()));
        }

        private static SourceMetadata CreateFixtureMetadata(int sourceConfidence)
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
                sourceConfidence);
        }

        private static string FixturePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
        }

        private static string CreateCsv(string header, string row)
        {
            var directory = Path.Combine(Path.GetTempPath(), "statlyn-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "players.csv");
            File.WriteAllText(path, header + Environment.NewLine + row + Environment.NewLine);
            return path;
        }

        private static PlayerStatRecord Stat(string name, double value)
        {
            return new PlayerStatRecord(1, "PlayerStat:" + name, name, value, 1000, false, "test", "test", 80);
        }

        private static long PlayerIdByName(StatlynDbConnectionFactory factory, string name)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM Player WHERE DisplayName = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static string StatlynPlayerIdByName(StatlynDbConnectionFactory factory, string name)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT StatlynPlayerId FROM Player WHERE DisplayName = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static void DeleteRoleScores(StatlynDbConnectionFactory factory, long playerId)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RoleScore WHERE PlayerId = $playerId;";
            command.Parameters.AddWithValue("$playerId", playerId);
            command.ExecuteNonQuery();
        }

        private static string ResultText(PlayerProfileResult result)
        {
            var builder = new StringBuilder();
            builder.Append(result.SafeMessage).Append(' ')
                .Append(result.Player == null ? string.Empty : result.Player.DisplayName).Append(' ')
                .Append(result.SourceMetadata == null ? string.Empty : result.SourceMetadata.SourceName).Append(' ')
                .Append(result.LatestRoleScore == null ? string.Empty : result.LatestRoleScore.RoleName).Append(' ')
                .Append(result.RoleOutputSummary == null ? string.Empty : string.Join(" ", result.RoleOutputSummary.CoreMetrics.Concat(result.RoleOutputSummary.SupportingMetrics).Concat(result.RoleOutputSummary.MissingCoreMetrics))).Append(' ')
                .Append(string.Join(" ", result.BlockedFields.Select(field => field.Key + " " + field.FieldName + " " + field.Reason))).Append(' ')
                .Append(string.Join(" ", result.Diagnostics)).Append(' ')
                .Append(string.Join(" ", result.Warnings)).Append(' ')
                .Append(string.Join(" ", result.Errors));
            return builder.ToString();
        }

        private static string ReportText(PlayerProfileReportViewModel report)
        {
            var builder = new StringBuilder();
            builder.Append(report.PlayerName).Append(' ')
                .Append(report.StatlynPlayerId).Append(' ')
                .Append(report.SourceName).Append(' ')
                .Append(report.RoleName).Append(' ')
                .Append(report.TacticalFitDisplay).Append(' ')
                .Append(report.OutputFitLabel).Append(' ')
                .Append(string.Join(" ", report.CoreOutputMetrics.Select(MetricText))).Append(' ')
                .Append(string.Join(" ", report.SupportingOutputMetrics.Select(MetricText))).Append(' ')
                .Append(string.Join(" ", report.PhysicalOutputMetrics.Select(MetricText))).Append(' ')
                .Append(string.Join(" ", report.MissingOutputMetrics)).Append(' ')
                .Append(string.Join(" ", report.KeyWarnings)).Append(' ')
                .Append(string.Join(" ", report.DataQualityCards.Select(card => card.Label + " " + card.Value + " " + card.Caption))).Append(' ')
                .Append(string.Join(" ", report.AttributeSupportCards.Select(card => card.Label + " " + card.Value + " " + card.Caption))).Append(' ')
                .Append(string.Join(" ", report.EvidenceCards.Select(card => card.Category + " " + card.Title + " " + card.Body))).Append(' ')
                .Append(string.Join(" ", report.ScoutActionCards.Select(card => card.Title + " " + card.Reason + " " + card.Action))).Append(' ')
                .Append(report.BlockedDataNotice.SafeMessage).Append(' ')
                .Append(string.Join(" ", report.BlockedDataNotice.Categories)).Append(' ')
                .Append(string.Join(" ", report.BlockedDataNotice.Reasons)).Append(' ')
                .Append(string.Join(" ", report.VisualSections.Select(section => section.Title + " " + section.Summary + " " + string.Join(" ", section.Rows))));
            return builder.ToString();
        }

        private static string MetricText(PlayerProfileMetricTileViewModel metric)
        {
            return metric.Label + " " + metric.Value + " " + metric.Section + " " + metric.Source + " " + metric.Confidence + " " + metric.Sample + " " + metric.VerificationLabel;
        }

        private static Type[] PublicPropertyTypes(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.PropertyType).ToArray();
        }
    }
}
