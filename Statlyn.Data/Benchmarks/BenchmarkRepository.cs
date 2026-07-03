using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Persistence;
using Statlyn.Data.Scouting;

namespace Statlyn.Data.Benchmarks
{
    public sealed class BenchmarkRepository : SqliteRepository
    {
        public BenchmarkRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public BenchmarkDefinition SaveDefinition(BenchmarkDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var safe = SanitizeDefinition(definition);
            using (var connection = ConnectionFactory.OpenConnection())
            {
                var now = DateTimeOffset.UtcNow;
                var id = safe.Id > 0 ? safe.Id : FindDefinitionId(connection, safe.BenchmarkName);
                if (id.HasValue)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"UPDATE BenchmarkDefinition SET
                                BenchmarkName = $benchmarkName,
                                Scope = $scope,
                                SourceName = $sourceName,
                                PositionGroup = $positionGroup,
                                RoleProfileName = $roleProfileName,
                                TacticalRoleName = $tacticalRoleName,
                                TacticalRolePairName = $tacticalRolePairName,
                                MetricKeys = $metricKeys,
                                MinimumSampleSize = $minimumSampleSize,
                                MinimumMinutes = $minimumMinutes,
                                IncludeFixtureData = $includeFixtureData,
                                UpdatedAtUtc = $updatedAtUtc,
                                IsArchived = $isArchived
                              WHERE Id = $id;";
                        AddDefinitionParameters(command, safe, now);
                        Add(command, "$id", id.Value);
                        command.ExecuteNonQuery();
                    }

                    return LoadDefinition(id.Value)!;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO BenchmarkDefinition (
                            BenchmarkName, Scope, SourceName, PositionGroup, RoleProfileName, TacticalRoleName,
                            TacticalRolePairName, MetricKeys, MinimumSampleSize, MinimumMinutes, IncludeFixtureData,
                            CreatedAtUtc, UpdatedAtUtc, IsArchived)
                          VALUES (
                            $benchmarkName, $scope, $sourceName, $positionGroup, $roleProfileName, $tacticalRoleName,
                            $tacticalRolePairName, $metricKeys, $minimumSampleSize, $minimumMinutes, $includeFixtureData,
                            $createdAtUtc, $updatedAtUtc, $isArchived);";
                    AddDefinitionParameters(command, safe, now);
                    Add(command, "$createdAtUtc", now.ToString("O"));
                    command.ExecuteNonQuery();
                }

                return LoadDefinition(LastInsertRowId(connection))!;
            }
        }

        public BenchmarkDefinition SaveDefinition(object definition)
        {
            SafePersistenceGuard.RejectRaw(definition, "Save benchmark definition");
            if (!(definition is BenchmarkDefinition model))
            {
                throw new InvalidOperationException("Benchmark definitions must use safe BenchmarkDefinition data.");
            }

            return SaveDefinition(model);
        }

        public BenchmarkDefinition? LoadDefinition(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = DefinitionSelectSql() + " WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadDefinition(reader) : null;
                }
            }
        }

        public BenchmarkDefinition? LoadDefinitionByName(string benchmarkName)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = DefinitionSelectSql() +
                    @" WHERE lower(BenchmarkName) = lower($benchmarkName)
                       ORDER BY Id ASC
                       LIMIT 1;";
                Add(command, "$benchmarkName", benchmarkName ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadDefinition(reader) : null;
                }
            }
        }

        public IReadOnlyList<BenchmarkDefinition> LoadDefinitions(bool includeArchived)
        {
            var definitions = new List<BenchmarkDefinition>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = DefinitionSelectSql() +
                    @" WHERE $includeArchived = 1 OR IsArchived = 0
                       ORDER BY Scope ASC, PositionGroup ASC, BenchmarkName ASC;";
                Add(command, "$includeArchived", Bool(includeArchived));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        definitions.Add(ReadDefinition(reader));
                    }
                }
            }

            return definitions;
        }

        public void ArchiveDefinition(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE BenchmarkDefinition SET IsArchived = 1, UpdatedAtUtc = $updatedAtUtc WHERE Id = $id;";
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }
        }

        public BenchmarkRunRecord SaveBenchmarkRun(BenchmarkRunRecord run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO BenchmarkRun (BenchmarkDefinitionId, RanAtUtc, PlayerCount, MetricCount, SafeMessage)
                      VALUES ($benchmarkDefinitionId, $ranAtUtc, $playerCount, $metricCount, $safeMessage);";
                Add(command, "$benchmarkDefinitionId", run.BenchmarkDefinitionId);
                Add(command, "$ranAtUtc", run.RanAtUtc.ToString("O"));
                Add(command, "$playerCount", Math.Max(0, run.PlayerCount));
                Add(command, "$metricCount", Math.Max(0, run.MetricCount));
                Add(command, "$safeMessage", SafeText(run.SafeMessage, "Benchmark run stored aggregate-only results."));
                command.ExecuteNonQuery();
                return LoadRun(LastInsertRowId(connection))!;
            }
        }

        public BenchmarkRunRecord SaveBenchmarkRun(object run)
        {
            SafePersistenceGuard.RejectRaw(run, "Save benchmark run");
            if (!(run is BenchmarkRunRecord model))
            {
                throw new InvalidOperationException("Benchmark runs must use safe BenchmarkRunRecord data.");
            }

            return SaveBenchmarkRun(model);
        }

        public BenchmarkMetricSnapshot SaveMetricSnapshot(BenchmarkMetricSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var safe = SanitizeSnapshot(snapshot);
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO BenchmarkMetricSnapshot (
                        BenchmarkRunId, MetricKey, FieldName, MetricType, SampleSize, MedianValue,
                        AverageValue, MinimumValue, MaximumValue, SourceName, ComparisonGroup,
                        IsGenericImportMetric, IsVerifiedFm26Metric)
                      VALUES (
                        $benchmarkRunId, $metricKey, $fieldName, $metricType, $sampleSize, $medianValue,
                        $averageValue, $minimumValue, $maximumValue, $sourceName, $comparisonGroup,
                        $isGenericImportMetric, $isVerifiedFm26Metric);";
                Add(command, "$benchmarkRunId", safe.BenchmarkRunId);
                Add(command, "$metricKey", safe.MetricKey);
                Add(command, "$fieldName", safe.FieldName);
                Add(command, "$metricType", safe.MetricType.ToString());
                Add(command, "$sampleSize", safe.SampleSize);
                Add(command, "$medianValue", safe.MedianValue);
                Add(command, "$averageValue", safe.AverageValue);
                Add(command, "$minimumValue", safe.MinimumValue);
                Add(command, "$maximumValue", safe.MaximumValue);
                Add(command, "$sourceName", safe.SourceName);
                Add(command, "$comparisonGroup", safe.ComparisonGroup);
                Add(command, "$isGenericImportMetric", Bool(safe.IsGenericImportMetric));
                Add(command, "$isVerifiedFm26Metric", Bool(false));
                command.ExecuteNonQuery();
                return LoadSnapshot(LastInsertRowId(connection))!;
            }
        }

        public BenchmarkMetricSnapshot SaveMetricSnapshot(object snapshot)
        {
            SafePersistenceGuard.RejectRaw(snapshot, "Save benchmark metric snapshot");
            if (!(snapshot is BenchmarkMetricSnapshot model))
            {
                throw new InvalidOperationException("Benchmark snapshots must use safe aggregate BenchmarkMetricSnapshot data.");
            }

            return SaveMetricSnapshot(model);
        }

        public BenchmarkRunRecord? LoadLatestRun(long definitionId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, BenchmarkDefinitionId, RanAtUtc, PlayerCount, MetricCount, SafeMessage
                      FROM BenchmarkRun
                      WHERE BenchmarkDefinitionId = $definitionId
                      ORDER BY RanAtUtc DESC, Id DESC
                      LIMIT 1;";
                Add(command, "$definitionId", definitionId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRun(reader) : null;
                }
            }
        }

        public IReadOnlyList<BenchmarkMetricSnapshot> LoadSnapshotsForRun(long runId)
        {
            var snapshots = new List<BenchmarkMetricSnapshot>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SnapshotSelectSql() + " WHERE BenchmarkRunId = $runId ORDER BY MetricKey ASC;";
                Add(command, "$runId", runId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        snapshots.Add(ReadSnapshot(reader));
                    }
                }
            }

            return snapshots;
        }

        public IReadOnlyList<BenchmarkMetricSnapshot> LoadLatestSnapshots(long definitionId)
        {
            var run = LoadLatestRun(definitionId);
            return run == null ? new List<BenchmarkMetricSnapshot>() : LoadSnapshotsForRun(run.Id);
        }

        private BenchmarkRunRecord? LoadRun(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, BenchmarkDefinitionId, RanAtUtc, PlayerCount, MetricCount, SafeMessage
                      FROM BenchmarkRun
                      WHERE Id = $id
                      LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRun(reader) : null;
                }
            }
        }

        private BenchmarkMetricSnapshot? LoadSnapshot(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SnapshotSelectSql() + " WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadSnapshot(reader) : null;
                }
            }
        }

        private static string DefinitionSelectSql()
        {
            return
                @"SELECT Id, BenchmarkName, Scope, SourceName, PositionGroup, RoleProfileName, TacticalRoleName,
                         TacticalRolePairName, MetricKeys, MinimumSampleSize, MinimumMinutes, IncludeFixtureData,
                         CreatedAtUtc, UpdatedAtUtc, IsArchived
                  FROM BenchmarkDefinition";
        }

        private static string SnapshotSelectSql()
        {
            return
                @"SELECT Id, BenchmarkRunId, MetricKey, FieldName, MetricType, SampleSize, MedianValue,
                         AverageValue, MinimumValue, MaximumValue, SourceName, ComparisonGroup,
                         IsGenericImportMetric, IsVerifiedFm26Metric
                  FROM BenchmarkMetricSnapshot";
        }

        private static void AddDefinitionParameters(SqliteCommand command, BenchmarkDefinition definition, DateTimeOffset updatedAtUtc)
        {
            Add(command, "$benchmarkName", SafeText(definition.BenchmarkName, "Untitled benchmark"));
            Add(command, "$scope", definition.Scope.ToString());
            Add(command, "$sourceName", SafeText(definition.SourceName, string.Empty));
            Add(command, "$positionGroup", SafeText(definition.PositionGroup, string.Empty));
            Add(command, "$roleProfileName", SafeText(definition.RoleProfileName, string.Empty));
            Add(command, "$tacticalRoleName", SafeText(definition.TacticalRoleName, string.Empty));
            Add(command, "$tacticalRolePairName", SafeText(definition.TacticalRolePairName, string.Empty));
            Add(command, "$metricKeys", JoinValues(definition.MetricKeys));
            Add(command, "$minimumSampleSize", Math.Max(1, definition.MinimumSampleSize));
            Add(command, "$minimumMinutes", Math.Max(0, definition.MinimumMinutes));
            Add(command, "$includeFixtureData", Bool(definition.IncludeFixtureData));
            Add(command, "$updatedAtUtc", updatedAtUtc.ToString("O"));
            Add(command, "$isArchived", Bool(definition.IsArchived));
        }

        private static BenchmarkDefinition ReadDefinition(SqliteDataReader reader)
        {
            return new BenchmarkDefinition(
                reader.GetInt64(0),
                ReadString(reader, 1),
                ParseEnum(ReadString(reader, 2), BenchmarkScope.Custom),
                ReadString(reader, 3),
                ReadString(reader, 4),
                ReadString(reader, 5),
                ReadString(reader, 6),
                ReadString(reader, 7),
                SplitValues(ReadString(reader, 8)),
                reader.GetInt32(9),
                reader.GetInt32(10),
                ReadBool(reader, 11),
                ParseDate(ReadString(reader, 12)),
                ParseDate(ReadString(reader, 13)),
                ReadBool(reader, 14));
        }

        private static BenchmarkRunRecord ReadRun(SqliteDataReader reader)
        {
            return new BenchmarkRunRecord(
                reader.GetInt64(0),
                reader.GetInt64(1),
                ParseDate(ReadString(reader, 2)),
                reader.GetInt32(3),
                reader.GetInt32(4),
                ReadString(reader, 5));
        }

        private static BenchmarkMetricSnapshot ReadSnapshot(SqliteDataReader reader)
        {
            return new BenchmarkMetricSnapshot(
                reader.GetInt64(0),
                reader.GetInt64(1),
                ReadString(reader, 2),
                ReadString(reader, 3),
                ParseEnum(ReadString(reader, 4), BenchmarkMetricType.PlayerStat),
                reader.GetInt32(5),
                ReadNullableDouble(reader, 6),
                ReadNullableDouble(reader, 7),
                ReadNullableDouble(reader, 8),
                ReadNullableDouble(reader, 9),
                ReadString(reader, 10),
                ReadString(reader, 11),
                ReadBool(reader, 12),
                ReadBool(reader, 13));
        }

        private static BenchmarkDefinition SanitizeDefinition(BenchmarkDefinition definition)
        {
            return new BenchmarkDefinition(
                definition.Id,
                SafeText(definition.BenchmarkName, "Untitled benchmark"),
                definition.Scope,
                SafeText(definition.SourceName, string.Empty),
                SafeText(definition.PositionGroup, string.Empty),
                SafeText(definition.RoleProfileName, string.Empty),
                SafeText(definition.TacticalRoleName, string.Empty),
                SafeText(definition.TacticalRolePairName, string.Empty),
                SanitizeValues(definition.MetricKeys),
                Math.Max(1, definition.MinimumSampleSize),
                Math.Max(0, definition.MinimumMinutes),
                definition.IncludeFixtureData,
                definition.CreatedAtUtc,
                definition.UpdatedAtUtc,
                definition.IsArchived);
        }

        private static BenchmarkMetricSnapshot SanitizeSnapshot(BenchmarkMetricSnapshot snapshot)
        {
            return new BenchmarkMetricSnapshot(
                snapshot.Id,
                snapshot.BenchmarkRunId,
                SafeText(snapshot.MetricKey, "Output"),
                SafeText(snapshot.FieldName, "Output"),
                snapshot.MetricType,
                Math.Max(0, snapshot.SampleSize),
                snapshot.MedianValue,
                snapshot.AverageValue,
                snapshot.MinimumValue,
                snapshot.MaximumValue,
                SafeText(snapshot.SourceName, string.Empty),
                SafeText(snapshot.ComparisonGroup, string.Empty),
                snapshot.IsGenericImportMetric,
                false);
        }

        private static IReadOnlyList<string> SanitizeValues(IReadOnlyList<string> values)
        {
            var safe = new List<string>();
            foreach (var value in values ?? new List<string>())
            {
                var sanitized = SafeText(value, string.Empty);
                if (!string.IsNullOrWhiteSpace(sanitized))
                {
                    safe.Add(sanitized);
                }
            }

            return safe;
        }

        private static long? FindDefinitionId(SqliteConnection connection, string benchmarkName)
        {
            if (string.IsNullOrWhiteSpace(benchmarkName))
            {
                return null;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id FROM BenchmarkDefinition
                      WHERE lower(BenchmarkName) = lower($benchmarkName)
                      ORDER BY Id ASC
                      LIMIT 1;";
                Add(command, "$benchmarkName", benchmarkName);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static string SafeText(string value, string fallback)
        {
            var sanitized = ScoutTextSanitizer.Sanitize(value ?? string.Empty);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return fallback ?? string.Empty;
            }

            return sanitized;
        }

        private static DateTimeOffset ParseDate(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
        }
    }
}
