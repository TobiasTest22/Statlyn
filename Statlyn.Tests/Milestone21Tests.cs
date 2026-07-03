using System;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Core;
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
    public sealed class Milestone21Tests
    {
        [Fact]
        public void RefactoredProfileBuildersStayAlignedWithReport()
        {
            using var factory = CreateImportedDatabase();
            var id = StatlynPlayerIdByName(factory, "Synthetic Wide Player");
            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = id });
            var report = PlayerProfileReportViewModel.From(result);

            var core = PlayerProfileMetricTileBuilder.BuildCore(result);
            var supporting = PlayerProfileMetricTileBuilder.BuildSupporting(result);

            Assert.Equal(report.CoreOutputMetrics.Select(metric => metric.Label), core.Select(metric => metric.Label));
            Assert.Equal(report.SupportingOutputMetrics.Select(metric => metric.Label), supporting.Select(metric => metric.Label));
            Assert.Equal(report.EvidenceCards.Count, PlayerProfileEvidenceBuilder.Build(result).Count);
            Assert.Equal(report.DataQualityCards.Count, PlayerProfileDataQualityBuilder.Build(result).Length);
            Assert.Equal(report.BlockedDataNotice.Count, PlayerProfileBlockedDataBuilder.Build(result).Count);
        }

        [Fact]
        public void VisualAnalyticsBuildsSafeOutputFirstSections()
        {
            using var factory = CreateImportedDatabase();
            var report = BuildReport(factory, "Synthetic Wide Player");
            var visuals = StatlynVisualAnalyticsBuilder.Build(report);

            Assert.Contains(visuals.ScoreCards, card => card.Title == "Role fit");
            Assert.NotNull(visuals.RoleOutput);
            Assert.Contains(visuals.MetricGroups, group => group.Title == "Core Role Output");
            Assert.Contains(visuals.MetricGroups, group => group.Title == "Supporting Output");
            Assert.Contains(visuals.MetricGroups, group => group.Title == "Physical Output");
            Assert.NotEmpty(visuals.DataQuality);
            Assert.NotEmpty(visuals.Evidence);
            Assert.NotEmpty(visuals.ScoutActions);
            var sectionOrder = visuals.SectionOrder.ToList();
            Assert.True(sectionOrder.IndexOf("Core Role Output") < sectionOrder.IndexOf("Attribute Support"));
            Assert.False(visuals.BenchmarkStatus.HasBenchmark);
            Assert.Null(visuals.BenchmarkStatus.Percentile);
            Assert.Equal("No benchmark yet.", visuals.BenchmarkStatus.SafeMessage);
        }

        [Fact]
        public void VisualAnalyticsRejectsRawAndDoesNotLeakHiddenValues()
        {
            using var factory = CreateImportedDatabase();
            var report = BuildReport(factory, "Synthetic Wide Player");
            var visuals = StatlynVisualAnalyticsBuilder.Build(report);
            var text = VisualText(visuals);

            Assert.Throws<InvalidOperationException>(() => StatlynVisualAnalyticsBuilder.Build(TestPlayers.CreateExternalPlayer()));
            Assert.DoesNotContain("CurrentAbility 199", text);
            Assert.DoesNotContain("Professionalism 19", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("IRawFootballEntity", text);
            Assert.DoesNotContain("90th percentile", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Fixture comparison group", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MissingOutputAndGenericMetricStatusBecomeVisualWarningsNotZeros()
        {
            using var factory = CreateImportedDatabase();
            var report = BuildReport(factory, "Synthetic Wide Player");
            var visuals = StatlynVisualAnalyticsBuilder.Build(report);
            var text = VisualText(visuals);

            Assert.Contains(visuals.MissingData, missing => missing.IsMissing && (missing.Label == "SuccessfulDribbles" || missing.Label == "ProgressiveCarries"));
            Assert.DoesNotContain("ProgressiveCarries 0", text);
            Assert.Contains(visuals.MetricGroups.SelectMany(group => group.Metrics), metric => metric.IsGenericImportMetric);
            Assert.Contains(visuals.DataQuality, item => item.Label == "Metric status" && item.IsWarning);
            Assert.Contains(visuals.ScoutActions, action => action.Title.Contains("Collect missing output", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void RecruitmentCentreMiniVisualsBindSafeRowsOnly()
        {
            using var factory = CreateImportedDatabase();
            var row = RecruitmentCentrePlayerRowViewModel.From(new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { SearchText = "Wide" }).Players.Single());
            var visuals = RecruitmentCentreMiniVisualBuilder.Build(row);
            var text = MiniVisualText(visuals);

            Assert.Throws<InvalidOperationException>(() => RecruitmentCentreMiniVisualBuilder.Build(TestPlayers.CreateExternalPlayer()));
            Assert.Equal("Role fit", visuals.RoleFitScore.Title);
            Assert.Equal("Confidence", visuals.ConfidenceBar.Label);
            Assert.Equal("Completeness", visuals.DataCompletenessBar.Label);
            Assert.Contains("No live FM26 data", visuals.NoLiveDataLabel);
            Assert.Contains(visuals.Badges, badge => badge.Contains("Missing data", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(visuals.Badges, badge => badge.Contains("Blocked fields", StringComparison.OrdinalIgnoreCase));
            Assert.NotEmpty(visuals.OutputMiniList);
            Assert.DoesNotContain("CurrentAbility 199", text);
            Assert.DoesNotContain("Professionalism 19", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
        }

        private static PlayerProfileReportViewModel BuildReport(StatlynDbConnectionFactory factory, string displayName)
        {
            var id = StatlynPlayerIdByName(factory, displayName);
            var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = id });
            return PlayerProfileReportViewModel.From(result);
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
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static string VisualText(StatlynVisualAnalyticsViewModel visuals)
        {
            var builder = new StringBuilder();
            builder.Append(string.Join(" ", visuals.SectionOrder)).Append(' ')
                .Append(string.Join(" ", visuals.ScoreCards.Select(card => card.Title + " " + card.Value + " " + card.Caption + " " + card.Status))).Append(' ')
                .Append(visuals.RoleOutput.RoleName).Append(' ')
                .Append(visuals.RoleOutput.OutputFitLabel).Append(' ')
                .Append(visuals.RoleOutput.TacticalFitDisplay).Append(' ')
                .Append(string.Join(" ", visuals.RoleOutput.Bars.Select(bar => bar.Label + " " + bar.Value + " " + bar.Caption))).Append(' ')
                .Append(string.Join(" ", visuals.MetricGroups.Select(group => group.Title + " " + group.Summary + " " + string.Join(" ", group.Metrics.Select(MetricText)) + " " + string.Join(" ", group.MissingMetrics.Select(MissingText))))).Append(' ')
                .Append(string.Join(" ", visuals.DataQuality.Select(item => item.Label + " " + item.Value + " " + item.Caption))).Append(' ')
                .Append(string.Join(" ", visuals.Warnings.Select(item => item.Title + " " + item.Message + " " + item.Severity))).Append(' ')
                .Append(string.Join(" ", visuals.Evidence.Select(EvidenceText))).Append(' ')
                .Append(string.Join(" ", visuals.ScoutActions.Select(EvidenceText))).Append(' ')
                .Append(string.Join(" ", visuals.AttributeSupport.Select(MetricText))).Append(' ')
                .Append(string.Join(" ", visuals.MissingData.Select(MissingText))).Append(' ')
                .Append(visuals.BlockedData.Title).Append(' ')
                .Append(visuals.BlockedData.Message).Append(' ')
                .Append(string.Join(" ", visuals.BlockedData.Rows)).Append(' ')
                .Append(visuals.BenchmarkStatus.SafeMessage).Append(' ')
                .Append(visuals.BenchmarkStatus.HasBenchmark.ToString());
            return builder.ToString();
        }

        private static string MiniVisualText(RecruitmentCentreMiniVisuals visuals)
        {
            return visuals.RoleFitScore.Title + " " + visuals.RoleFitScore.Value + " " +
                   visuals.ConfidenceBar.Label + " " + visuals.ConfidenceBar.Value + " " +
                   visuals.DataCompletenessBar.Label + " " + visuals.DataCompletenessBar.Value + " " +
                   visuals.RiskIndicator.Message + " " +
                   string.Join(" ", visuals.OutputMiniList.Select(MetricText)) + " " +
                   string.Join(" ", visuals.Badges) + " " +
                   visuals.NoLiveDataLabel;
        }

        private static string MetricText(StatlynMetricTileVisual metric)
        {
            return metric.Label + " " + metric.Value + " " + metric.Section + " " + metric.Source + " " + metric.Confidence + " " + metric.Sample + " " + metric.VerificationLabel;
        }

        private static string MissingText(StatlynMissingDataVisual missing)
        {
            return missing.Label + " " + missing.SafeMessage + " " + missing.Caption;
        }

        private static string EvidenceText(StatlynEvidenceVisual evidence)
        {
            return evidence.Category + " " + evidence.Title + " " + evidence.Body + " " + evidence.Source + " " + evidence.Confidence;
        }
    }
}
