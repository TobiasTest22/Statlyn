using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Statlyn.Scouting;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone17Tests
    {
        [Fact]
        public void SQLiteDatabaseInitializesInMemoryIdempotentlyAndTracksVersion()
        {
            using var factory = CreateInitializedDatabase();
            var initializer = new StatlynDatabaseInitializer(factory);

            initializer.Initialize();

            var diagnostics = new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics();
            Assert.Equal(StatlynSchemaVersion.Current, diagnostics.SchemaVersion);
            Assert.Contains("Player", diagnostics.Tables);
            Assert.Contains("VisibleField", diagnostics.Tables);
            Assert.Contains("PlayerStat", diagnostics.Tables);
            Assert.Contains("PhysicalMetric", diagnostics.Tables);
            Assert.Contains("BlockedFieldAudit", diagnostics.Tables);
            Assert.Contains("PerformanceMetricDefinition", diagnostics.Tables);
            Assert.Contains("RoleOutputExpectationProfile", diagnostics.Tables);
            Assert.Equal(1, CountRows(factory, "SchemaVersion"));
            Assert.Equal("database.initialized", diagnostics.Report.Items.First().Key);
        }

        [Fact]
        public void SQLiteDatabaseInitializesFromFilePath()
        {
            var path = Path.Combine(Path.GetTempPath(), "statlyn-" + Guid.NewGuid().ToString("N"), "statlyn.db");
            using var factory = StatlynDbConnectionFactory.CreateFile(path);

            new StatlynDatabaseInitializer(factory).Initialize();

            Assert.True(File.Exists(path));
            Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics().SchemaVersion);
        }

        [Fact]
        public void SchemaStaysSafeForHiddenAndBlockedValues()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);

            foreach (var forbidden in new[] { "CurrentAbility", "PotentialAbility", "Professionalism", "InjuryProneness", "Consistency", "ImportantMatches", "Pressure", "Ambition", "Loyalty", "Adaptability", "Temperament", "RawValue" })
            {
                Assert.DoesNotContain(forbidden, schema, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("RawValue", TableSql("BlockedFieldAudit", schema), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FieldsStored INTEGER", TableSql("ImportAudit", schema), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PlayerStatsStored INTEGER", TableSql("ImportAudit", schema), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PhysicalMetricsStored INTEGER", TableSql("ImportAudit", schema), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RepositoriesRejectRawAndPersistOnlySafeMaskedData()
        {
            using var factory = CreateInitializedDatabase();
            var metadata = CreateFixtureMetadata();
            var completeness = new DataCompletenessReport(1, 1, Array.Empty<string>());
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsFlags: true);
            raw.AddField(new RawFieldValue(PlayerFieldKey.CurrentAbility, "CurrentAbility", 200, FieldValueKind.Number, 90));
            raw.AddField(new RawFieldValue(PlayerFieldKey.Professionalism, "Professionalism", 20, FieldValueKind.Number, 90));
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Finishing", 14));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);

            var players = new PlayerRepository(factory);
            Assert.Throws<InvalidOperationException>(() => players.Save(raw, metadata, completeness));
            Assert.Throws<InvalidOperationException>(() => new VisibleFieldRepository(factory).SaveFields(1, raw));

            var dataSourceId = new DataSourceRepository(factory).Save(metadata, completeness);
            var playerId = players.Save(masked, metadata, completeness);
            var fieldsStored = new VisibleFieldRepository(factory).SaveFields(playerId, masked);
            var blockedRows = new BlockedFieldAuditRepository(factory).SaveBlockedFields(masked.StatlynPlayerId, masked);

            Assert.True(dataSourceId > 0);
            Assert.True(playerId > 0);
            Assert.True(fieldsStored > 0);
            Assert.Equal(2, blockedRows);
            Assert.Equal(0, CountWhere(factory, "VisibleField", "FieldKey IN ('CurrentAbility','Professionalism')"));
            Assert.Equal(2, CountRows(factory, "BlockedFieldAudit"));
            Assert.DoesNotContain("200", ReadAllText(factory, "BlockedFieldAudit"));
            Assert.DoesNotContain("Professionalism 20", ReadAllText(factory, "BlockedFieldAudit"));
        }

        [Fact]
        public void RepositoriesSkipNonStorableUnknownFieldsAndPreserveRepeatedMetrics()
        {
            using var factory = CreateInitializedDatabase();
            var metadata = CreateFixtureMetadata();
            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true, permitsFlags: true);
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Finishing", 14));
            raw.AddField(Number(PlayerFieldKey.TechnicalAttribute, "Pace", 13));
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xG", 0.44));
            raw.AddField(Number(PlayerFieldKey.PlayerStat, "xA", 0.18));
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "TopSpeed", 33.2));
            raw.AddField(Number(PlayerFieldKey.PhysicalData, "SprintDistance", 810));
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var playerId = new PlayerRepository(factory).Save(masked, metadata, new DataCompletenessReport(1, 1, Array.Empty<string>()));

            var noStoreField = new VisiblePlayerField(PlayerFieldKey.PlayerStat, "NoStoreMetric", "NoStoreMetric", "1", 1, FieldValueKind.Number, true, false, true, true, false, 90, "fixture", string.Empty);
            var unknownField = new VisiblePlayerField(PlayerFieldKey.PlayerStat, "UnknownMetric", "UnknownMetric", string.Empty, null, FieldValueKind.Number, false, false, false, false, false, 0, "fixture", "Missing");
            var noStoreMasked = new MaskedPlayer("fixture:no-store", "No Store", "fixture", ProviderType.Csv, 0, 90, new System.Collections.Generic.Dictionary<FieldInstanceKey, VisiblePlayerField> { [noStoreField.InstanceKey] = noStoreField, [unknownField.InstanceKey] = unknownField }, Array.Empty<BlockedFieldNotice>(), new System.Collections.Generic.Dictionary<string, VisibleField<int>>(), new System.Collections.Generic.Dictionary<string, VisibleField<string>>());
            Assert.Equal(0, new VisibleFieldRepository(factory).SaveFields(playerId, noStoreMasked));

            Assert.True(new VisibleFieldRepository(factory).SaveFields(playerId, masked) >= 6);
            Assert.Equal(2, new PlayerStatRepository(factory).SaveFromFields(playerId, masked));
            Assert.Equal(2, new PhysicalMetricRepository(factory).SaveFromFields(playerId, masked));
            Assert.Contains("PlayerStat:xG", ReadAllText(factory, "PlayerStat"));
            Assert.Contains("PlayerStat:xA", ReadAllText(factory, "PlayerStat"));
            Assert.Contains("PhysicalData:TopSpeed", ReadAllText(factory, "PhysicalMetric"));
            Assert.Contains("PhysicalData:SprintDistance", ReadAllText(factory, "PhysicalMetric"));
        }

        [Fact]
        public void SourcePermissionsAndNullTacticalFitPersist()
        {
            using var factory = CreateInitializedDatabase();
            var metadata = CreateFixtureMetadata();
            new DataSourceRepository(factory).Save(metadata, new DataCompletenessReport(1, 1, Array.Empty<string>()));
            var loaded = new DataSourceRepository(factory).LoadLatest(metadata.SourceName)!;

            Assert.False(loaded.PermitsPlayerImages);
            Assert.False(loaded.PermitsProviderFlags);
            Assert.True(loaded.UsesBundledSafeFlagAssets);
            Assert.False(loaded.PermitsClubBadges);
            Assert.True(loaded.AllowsExport);

            var raw = TestPlayers.CreateExternalPlayer(isLicensed: true);
            var masked = new ScoutingKnowledgeFirewall().Mask(raw);
            var playerId = new PlayerRepository(factory).Save(masked, metadata, new DataCompletenessReport(1, 1, Array.Empty<string>()));
            var score = new RoleScoringEngine().ScorePlayer(masked, new RoleModel("Preview").RequireAttribute("Finishing", 1));
            new RoleScoreRepository(factory).Save(playerId, score);
            var reloaded = new RoleScoreRepository(factory).LoadLatest(playerId, "Preview")!;

            Assert.Null(score.TacticalFit);
            Assert.Null(reloaded.TacticalFit);
            Assert.Contains("TacticalFit INTEGER NULL", string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ImportPipelineStoresSyntheticCsvSafely()
        {
            using var factory = CreateInitializedDatabase();
            var provider = CreateCsvProvider();
            var result = new ImportPipelineService(factory).Import(provider, ImportPipelineOptions.CreateDefault());
            var audit = new ImportAuditRepository(factory).LoadLatest()!;

            Assert.Equal(2, result.RowsRead);
            Assert.Equal(2, result.RowsAccepted);
            Assert.Equal(0, result.RowsRejected);
            Assert.True(result.FieldsStored > 0);
            Assert.True(result.PlayerStatsStored >= 6);
            Assert.True(result.PhysicalMetricsStored >= 4);
            Assert.Equal(4, result.BlockedFields);
            Assert.Equal(2, CountRows(factory, "Player"));
            Assert.Equal(2, CountWhere(factory, "PlayerStat", "StatName = 'xG'"));
            Assert.Equal(2, CountWhere(factory, "PlayerStat", "StatName = 'xA'"));
            Assert.Equal(2, CountWhere(factory, "PhysicalMetric", "MetricName = 'TopSpeed'"));
            Assert.Equal(2, CountWhere(factory, "PhysicalMetric", "MetricName = 'SprintDistance'"));
            Assert.Equal(2, CountWhere(factory, "VisibleField", "FieldName = 'Finishing'"));
            Assert.Equal(2, CountWhere(factory, "VisibleField", "FieldName = 'Pace'"));
            Assert.Equal(2, CountWhere(factory, "VisibleField", "FieldName = 'Acceleration'"));
            Assert.Equal(0, CountWhere(factory, "VisibleField", "FieldKey IN ('CurrentAbility','Professionalism')"));
            Assert.Equal(2, CountWhere(factory, "DataSource", "IsLive = 0"));
            Assert.Contains("CSV rows were read", audit.Diagnostics);
            Assert.DoesNotContain("200", audit.Diagnostics);
            Assert.DoesNotContain("199", audit.Diagnostics);
            Assert.DoesNotContain("Professionalism:20", audit.Diagnostics);
            Assert.DoesNotContain("Professionalism:19", audit.Diagnostics);
        }

        [Fact]
        public void GenericPerformanceMetricDefinitionsSeedPersistAndStayFm26Unverified()
        {
            using var factory = CreateInitializedDatabase();
            var repository = new PerformanceMetricDefinitionRepository(factory);

            repository.SeedGenericDefaults();
            repository.SaveAlias(new PerformanceMetricAlias("xG", ProviderType.Csv, "expected_goals", false, "CSV alias for import/testing only."));

            var definitions = repository.LoadAll();
            Assert.True(definitions.Count >= 29);
            Assert.All(definitions, definition => Assert.False(definition.IsVerifiedFm26Metric));
            Assert.True(repository.FindByFieldInstanceKey(new FieldInstanceKey(PlayerFieldKey.PlayerStat, "xG", "xG"))!.IsPer90Capable);
            Assert.Contains(definitions, definition => definition.MetricKey == "Saves" && definition.PositionGroups.Contains("Goalkeeper"));
            Assert.DoesNotContain(definitions.Where(definition => definition.PositionGroups.Contains("Goalkeeper")), definition => definition.PositionGroups.Contains("WingerWideForward") && definition.MetricKey == "SuccessfulDribbles");
            Assert.Equal(1, CountWhere(factory, "PerformanceMetricAlias", "AliasName = 'expected_goals' AND IsVerifiedFm26Alias = 0"));
            Assert.Equal(definitions.Count, new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics().PerformanceMetricDefinitionsCount);
        }

        [Fact]
        public void GenericRoleOutputExpectationsArePositionSpecificAndPersist()
        {
            using var factory = CreateInitializedDatabase();
            var repository = new RoleOutputExpectationRepository(factory);

            repository.SeedGenericDefaults();

            var goalkeeper = repository.FindByName("Generic Goalkeeper Output")!;
            var wide = repository.FindByName("Generic Wide Attacker Output")!;
            var centreBack = repository.FindByName("Generic Centre-Back Output")!;
            var striker = repository.FindByName("Generic Striker Output")!;

            Assert.DoesNotContain(goalkeeper.MetricExpectations, expectation => expectation.Importance == "Core" && expectation.MetricKey == "SuccessfulDribbles");
            Assert.Contains(wide.MetricExpectations, expectation => expectation.MetricKey == "xA");
            Assert.Contains(wide.MetricExpectations, expectation => expectation.MetricKey == "ProgressiveCarries");
            Assert.Contains(centreBack.MetricExpectations, expectation => expectation.MetricKey == "AerialDuelsWonPct");
            Assert.Contains(centreBack.MetricExpectations, expectation => expectation.MetricKey == "Clearances");
            Assert.Contains(striker.MetricExpectations, expectation => expectation.MetricKey == "xG");
            Assert.Contains(striker.MetricExpectations, expectation => expectation.MetricKey == "Shots");
            Assert.All(repository.LoadAll(), profile => Assert.False(profile.IsFm26Specific));
            Assert.All(repository.LoadAll().SelectMany(profile => profile.MetricExpectations), expectation => Assert.Contains("Missing performance output", expectation.MissingDataImpact));
            Assert.All(repository.LoadAll(), profile => Assert.Contains("supporting", profile.AttributeSupportWeights, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ImportedPlayerReloadsSafelyAndCanBuildMaskedProfile()
        {
            using var factory = CreateInitializedDatabase();
            new ImportPipelineService(factory).Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());
            new PerformanceMetricDefinitionRepository(factory).SeedGenericDefaults();
            new RoleOutputExpectationRepository(factory).SeedGenericDefaults();
            var loader = new PersistedMaskedPlayerLoader(factory);
            var players = loader.LoadPlayersBySource("Synthetic CSV fixture");

            var loaded = loader.LoadByStatlynPlayerId(players.First().StatlynPlayerId, "Generic performance preview")!;
            var profile = MaskedPlayerProfileViewModel.From(loaded.MaskedPlayer, loaded.LatestRoleScore!, loaded.SourceMetadata, loaded.Completeness);

            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "Finishing");
            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "Pace");
            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "Acceleration");
            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "xG");
            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "xA");
            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "TopSpeed");
            Assert.Contains(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "SprintDistance");
            Assert.DoesNotContain(loaded.MaskedPlayer.Fields.Values, field => field.FieldName == "CurrentAbility");
            Assert.DoesNotContain("CurrentAbility 200", ReadAllText(factory, "BlockedFieldAudit"));
            Assert.DoesNotContain("Professionalism 20", ReadAllText(factory, "BlockedFieldAudit"));
            Assert.Null(loaded.LatestRoleScore!.TacticalFit);
            Assert.True(profile.IsFixtureMode);
            Assert.False(profile.IsLiveFm26Data);
            Assert.Equal(2, loaded.PlayerStats.Count(stat => stat.StatName == "xG" || stat.StatName == "xA"));
            Assert.Equal(2, loaded.PhysicalMetrics.Count(metric => metric.MetricName == "TopSpeed" || metric.MetricName == "SprintDistance"));
            Assert.False(new PerformanceMetricDefinitionRepository(factory).FindByMetricKey("xG")!.IsVerifiedFm26Metric);
            Assert.Equal("CentreBack", new RoleOutputExpectationRepository(factory).FindByName("Generic Centre-Back Output")!.PositionGroup);
        }

        [Fact]
        public void DatabaseDiagnosticsReportSafeCountsAfterImportAndSeeds()
        {
            using var factory = CreateInitializedDatabase();
            new ImportPipelineService(factory).Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());
            new PerformanceMetricDefinitionRepository(factory).SeedGenericDefaults();
            new RoleOutputExpectationRepository(factory).SeedGenericDefaults();

            var diagnostics = new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics();
            var summary = diagnostics.ToSafeSummary();

            Assert.True(diagnostics.PlayersCount >= 2);
            Assert.True(diagnostics.VisibleFieldsCount > 0);
            Assert.True(diagnostics.PlayerStatsCount >= 6);
            Assert.True(diagnostics.PhysicalMetricsCount >= 4);
            Assert.Equal(4, diagnostics.BlockedAuditCount);
            Assert.True(diagnostics.PerformanceMetricDefinitionsCount >= 29);
            Assert.True(diagnostics.RoleOutputProfilesCount >= 5);
            Assert.False(string.IsNullOrWhiteSpace(diagnostics.LastImportTime));
            Assert.DoesNotContain("200", summary);
            Assert.DoesNotContain("199", summary);
            Assert.DoesNotContain("Professionalism:20", summary);
            Assert.DoesNotContain("Professionalism:19", summary);
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

        private static RawFieldValue Number(PlayerFieldKey key, string fieldName, double value)
        {
            return new RawFieldValue(key, fieldName, fieldName, value, FieldValueKind.Number, 90);
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

        private static string ReadAllText(StatlynDbConnectionFactory factory, string table)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM " + table + ";";
            using var reader = command.ExecuteReader();
            var text = string.Empty;
            while (reader.Read())
            {
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    text += " " + Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return text;
        }

        private static string TableSql(string tableName, string schema)
        {
            var marker = "CREATE TABLE IF NOT EXISTS " + tableName;
            var start = schema.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return string.Empty;
            }

            var end = schema.IndexOf(");", start, StringComparison.OrdinalIgnoreCase);
            return end < 0 ? schema.Substring(start) : schema.Substring(start, end - start + 2);
        }
    }
}
