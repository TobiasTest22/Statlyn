using System;
using System.IO;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Statlyn.Scouting;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone171Tests
    {
        [Fact]
        public void ImportTransactionCommitsSuccessfulImportAndRejectsRawPersistenceInTransactionPath()
        {
            using var factory = CreateInitializedDatabase();
            var result = new ImportPipelineService(factory).Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());

            Assert.Equal(2, result.RowsAccepted);
            Assert.Equal(2, CountRows(factory, "Player"));
            Assert.True(CountRows(factory, "VisibleField") > 0);
            Assert.True(CountRows(factory, "PlayerStat") > 0);
            Assert.True(CountRows(factory, "PhysicalMetric") > 0);

            using var connection = factory.OpenConnection();
            using var transaction = connection.BeginTransaction();
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            Assert.Throws<InvalidOperationException>(() => new PlayerRepository(factory).Save(raw, CreateFixtureMetadata(), new DataCompletenessReport(1, 1, Array.Empty<string>()), connection, transaction));
        }

        [Fact]
        public void FatalImportFailureRollsBackPersistedRowsAndKeepsAuditSanitized()
        {
            using var factory = CreateInitializedDatabase();
            var options = ImportPipelineOptions.CreateDefault().WithFatalFailureAfterAcceptedRows(1);

            var result = new ImportPipelineService(factory).Import(CreateCsvProvider(), options);
            var audit = new ImportAuditRepository(factory).LoadLatest()!;

            Assert.Equal(0, result.RowsAccepted);
            Assert.Equal(0, CountRows(factory, "DataSource"));
            Assert.Equal(0, CountRows(factory, "Player"));
            Assert.Equal(0, CountRows(factory, "VisibleField"));
            Assert.Equal(0, CountRows(factory, "PlayerStat"));
            Assert.Equal(0, CountRows(factory, "PhysicalMetric"));
            Assert.Equal(0, CountRows(factory, "BlockedFieldAudit"));
            Assert.Equal(1, CountRows(factory, "ImportAudit"));
            Assert.DoesNotContain("200", audit.Diagnostics);
            Assert.DoesNotContain("199", audit.Diagnostics);
            Assert.DoesNotContain("Professionalism=19", audit.Diagnostics);
        }

        [Fact]
        public void ReimportSameCsvIsIdempotentForPlayerSnapshots()
        {
            using var factory = CreateInitializedDatabase();
            var service = new ImportPipelineService(factory);

            service.Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());
            service.Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());

            Assert.Equal(2, CountRows(factory, "Player"));
            Assert.Equal(2, CountWhere(factory, "VisibleField", "FieldName = 'Finishing'"));
            Assert.Equal(2, CountWhere(factory, "PlayerStat", "StatName = 'xG'"));
            Assert.Equal(2, CountWhere(factory, "PhysicalMetric", "MetricName = 'TopSpeed'"));
            Assert.Equal(4, CountRows(factory, "BlockedFieldAudit"));
            Assert.Equal(2, CountRows(factory, "RoleScore"));

            var loader = new PersistedMaskedPlayerLoader(factory);
            var player = loader.LoadPlayersBySource("Synthetic CSV fixture").First();
            var loaded = loader.LoadByStatlynPlayerId(player.StatlynPlayerId, "Generic performance preview")!;
            Assert.Equal(1, loaded.MaskedPlayer.Fields.Values.Count(field => field.FieldName == "xG"));
            Assert.NotNull(loaded.LatestRoleScore);
            Assert.Equal(loaded.LatestRoleScore!.Recommendation, new RoleScoreRepository(factory).LoadLatest(player.Id, "Generic performance preview")!.Recommendation);
        }

        [Theory]
        [InlineData("CurrentAbility: 200", "CurrentAbility: [redacted]")]
        [InlineData("Professionalism=19", "Professionalism= [redacted]")]
        [InlineData("PA 199", "PA [redacted]")]
        public void DiagnosticSanitizerRedactsHiddenValuesAndPreservesSafeContext(string raw, string expected)
        {
            var sanitized = DiagnosticSanitizer.Sanitize("csv.fields.blocked Forbidden fields: 2 Source=Synthetic CSV fixture " + raw);

            Assert.Contains(expected, sanitized);
            Assert.Contains("Forbidden fields: 2", sanitized);
            Assert.Contains("Synthetic CSV fixture", sanitized);
            Assert.Contains(raw.Split(new[] { ':', '=', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0], sanitized);
            Assert.DoesNotContain(" 200", sanitized);
            Assert.DoesNotContain("=19", sanitized);
            Assert.DoesNotContain(" 199", sanitized);
        }

        [Fact]
        public void ImportAuditStoresSanitizedDiagnostics()
        {
            using var factory = CreateInitializedDatabase();
            new ImportAuditRepository(factory).Save(new ImportAuditRecord(
                "Synthetic CSV fixture",
                ProviderType.Csv.ToString(),
                DateTimeOffset.UtcNow,
                1,
                0,
                1,
                0,
                0,
                0,
                1,
                0,
                "CurrentAbility: 200; Professionalism=19; PA 199; Forbidden fields: 2; Source=Synthetic CSV fixture"));

            var audit = new ImportAuditRepository(factory).LoadLatest()!;

            Assert.Contains("CurrentAbility: [redacted]", audit.Diagnostics);
            Assert.Contains("Professionalism= [redacted]", audit.Diagnostics);
            Assert.Contains("PA [redacted]", audit.Diagnostics);
            Assert.Contains("Forbidden fields: 2", audit.Diagnostics);
            Assert.Contains("Synthetic CSV fixture", audit.Diagnostics);
        }

        [Fact]
        public void RoleScoreRecommendationPersistsWithoutRecompute()
        {
            using var factory = CreateInitializedDatabase();
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var metadata = CreateFixtureMetadata();
            var playerId = new PlayerRepository(factory).Save(masked, metadata, new DataCompletenessReport(1, 1, Array.Empty<string>()));
            var score = new RoleScore(
                "Manual low confidence",
                95,
                95,
                95,
                95,
                null,
                20,
                20,
                RecruitmentRecommendation.Avoid,
                Array.Empty<EvidenceItem>(),
                Array.Empty<EvidenceItem>(),
                Array.Empty<string>(),
                string.Empty);

            new RoleScoreRepository(factory).Save(playerId, score);
            var reloaded = new RoleScoreRepository(factory).LoadLatest(playerId, "Manual low confidence")!;

            Assert.Equal(RecruitmentRecommendation.Avoid, reloaded.Recommendation);
            Assert.NotEqual(RecruitmentRecommendation.ScoutFurther, reloaded.Recommendation);
            Assert.Contains("Recommendation TEXT NOT NULL", string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements));
        }

        [Fact]
        public void PlayerStatMinutesPersistWhenAvailableAndRemainMissingWhenUnavailable()
        {
            using var factory = CreateInitializedDatabase();
            new ImportPipelineService(factory).Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());
            var imported = new PersistedMaskedPlayerLoader(factory)
                .LoadByStatlynPlayerId("CSV import:fixture-001", "Generic performance preview")!;

            var xg = imported.PlayerStats.Single(stat => stat.StatName == "xG");
            Assert.Equal(1040, xg.Minutes);
            Assert.False(xg.SampleMinutesMissing);
            Assert.Equal("PlayerStat:Minutes", xg.MinutesSource);

            var noMinutesRaw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            noMinutesRaw.AddField(new RawFieldValue(PlayerFieldKey.PlayerStat, "xG", "xG", 0.33, FieldValueKind.Number, 90));
            var masked = new ScoutingKnowledgeFirewall().Mask(noMinutesRaw);
            var metadata = CreateFixtureMetadata();
            var playerId = new PlayerRepository(factory).Save(masked, metadata, new DataCompletenessReport(1, 1, Array.Empty<string>()));
            new PlayerStatRepository(factory).SaveFromFields(playerId, masked);
            var missingSample = new PlayerStatRepository(factory).LoadForPlayer(playerId).Single(stat => stat.StatName == "xG");
            var definition = new PerformanceMetricDefinitionRepository(factory);
            definition.SeedGenericDefaults();

            Assert.Equal(0, missingSample.Minutes);
            Assert.True(missingSample.SampleMinutesMissing);
            Assert.Equal("missing", missingSample.MinutesSource);
            Assert.True(definition.FindByMetricKey("xG")!.RequiresMinutes);
        }

        [Fact]
        public void SchemaContainsDuplicatePreventionIndexes()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);

            Assert.Contains("UX_VisibleField_Player_FieldInstance", schema);
            Assert.Contains("UX_PlayerStat_Player_FieldInstance", schema);
            Assert.Contains("UX_PhysicalMetric_Player_FieldInstance", schema);
            Assert.Contains("UX_BlockedFieldAudit_Entity_Field", schema);
        }

        private static StatlynDbConnectionFactory CreateInitializedDatabase()
        {
            var factory = StatlynDbConnectionFactory.CreateInMemory();
            new StatlynDatabaseInitializer(factory).Initialize();
            return factory;
        }

        private static CsvImportProvider CreateCsvProvider()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
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
