using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Core;
using Statlyn.Data.Persistence;
using Statlyn.Data.Recruitment;
using Statlyn.Data.RoleLab;

namespace Statlyn.Data.Benchmarks
{
    public sealed class BenchmarkCalculationService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly PerformanceMetricDefinitionRepository _metricDefinitions;
        private readonly RoleOutputExpectationRepository _roleProfiles;
        private readonly RoleLabRepository _roleLab;

        public BenchmarkCalculationService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _metricDefinitions = new PerformanceMetricDefinitionRepository(connectionFactory);
            _roleProfiles = new RoleOutputExpectationRepository(connectionFactory);
            _roleLab = new RoleLabRepository(connectionFactory);
        }

        public BenchmarkCalculationResult Calculate(BenchmarkDefinition definition, string statlynPlayerId = "")
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var effectivePositionGroup = ResolvePositionGroup(definition);
            using (var connection = _connectionFactory.OpenConnection())
            {
                var comparisonPlayers = LoadComparisonPlayers(connection, definition, effectivePositionGroup).ToList();
                var results = new List<BenchmarkMetricResult>();
                foreach (var metricKey in definition.MetricKeys.Where(value => !string.IsNullOrWhiteSpace(value)))
                {
                    var descriptor = ResolveMetric(connection, metricKey);
                    var result = CalculateMetric(connection, definition, descriptor, comparisonPlayers, statlynPlayerId, effectivePositionGroup);
                    results.Add(result);
                }

                results = results
                    .OrderBy(result => result.MetricType == BenchmarkMetricType.AttributeSupport ? 1 : 0)
                    .ThenBy(result => result.MetricKey, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var overall = ResolveOverallStatus(results, comparisonPlayers.Count);
                var message = BuildSummaryMessage(overall, results, comparisonPlayers.Count);
                var warnings = BuildWarnings(definition, results).ToList();
                var summary = new BenchmarkPlayerSummary(
                    statlynPlayerId,
                    definition.BenchmarkName,
                    BuildComparisonGroup(definition, effectivePositionGroup),
                    results,
                    overall,
                    message,
                    warnings);

                return new BenchmarkCalculationResult(definition, summary, comparisonPlayers.Count);
            }
        }

        public BenchmarkMetricSnapshot ToSnapshot(long benchmarkRunId, BenchmarkMetricResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new BenchmarkMetricSnapshot(
                0,
                benchmarkRunId,
                result.MetricKey,
                result.FieldName,
                result.MetricType,
                result.SampleSize,
                result.BenchmarkMedian,
                result.BenchmarkAverage,
                result.BenchmarkMin,
                result.BenchmarkMax,
                result.SourceName,
                result.ComparisonGroup,
                result.IsGenericImportMetric,
                false);
        }

        private BenchmarkMetricResult CalculateMetric(
            SqliteConnection connection,
            BenchmarkDefinition definition,
            MetricDescriptor descriptor,
            IReadOnlyList<BenchmarkPlayerCandidate> comparisonPlayers,
            string statlynPlayerId,
            string effectivePositionGroup)
        {
            var comparisonGroup = BuildComparisonGroup(definition, effectivePositionGroup);
            var sourceName = string.IsNullOrWhiteSpace(definition.SourceName) ? "All imported sources" : definition.SourceName;
            if (comparisonPlayers.Count == 0)
            {
                return Result(
                    descriptor,
                    null,
                    new List<double>(),
                    null,
                    BenchmarkStatus.NoBenchmark,
                    "No benchmark yet. No persisted safe comparison group exists.",
                    sourceName,
                    comparisonGroup);
            }

            var values = new List<double>();
            double? selectedValue = null;
            foreach (var player in comparisonPlayers)
            {
                var value = LoadMetricValue(connection, player.Id, descriptor, definition.MinimumMinutes);
                if (!value.HasValue)
                {
                    continue;
                }

                values.Add(value.Value);
                if (!string.IsNullOrWhiteSpace(statlynPlayerId) && string.Equals(player.StatlynPlayerId, statlynPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedValue = value.Value;
                }
            }

            if (values.Count == 0)
            {
                return Result(
                    descriptor,
                    selectedValue,
                    values,
                    null,
                    BenchmarkStatus.MissingMetric,
                    "Missing metric. Missing values are not treated as zero.",
                    sourceName,
                    comparisonGroup);
            }

            if (values.Count < definition.MinimumSampleSize)
            {
                return Result(
                    descriptor,
                    selectedValue,
                    values,
                    null,
                    BenchmarkStatus.InsufficientSample,
                    "Insufficient sample. Percentile is hidden until the minimum sample exists.",
                    sourceName,
                    comparisonGroup);
            }

            if (!string.IsNullOrWhiteSpace(statlynPlayerId) && !selectedValue.HasValue)
            {
                return Result(
                    descriptor,
                    null,
                    values,
                    null,
                    BenchmarkStatus.MissingMetric,
                    "Selected player is missing this metric; no percentile is shown.",
                    sourceName,
                    comparisonGroup);
            }

            var percentile = selectedValue.HasValue ? ComputePercentile(values, selectedValue.Value, descriptor.HigherIsBetter) : (double?)null;
            return Result(
                descriptor,
                selectedValue,
                values,
                percentile,
                BenchmarkStatus.Available,
                selectedValue.HasValue
                    ? "Benchmark available from persisted safe comparison data."
                    : "Benchmark aggregate available from persisted safe comparison data.",
                sourceName,
                comparisonGroup);
        }

        private BenchmarkMetricResult Result(
            MetricDescriptor descriptor,
            double? selectedValue,
            IReadOnlyList<double> values,
            double? percentile,
            BenchmarkStatus status,
            string safeMessage,
            string sourceName,
            string comparisonGroup)
        {
            var aggregate = Aggregate(values);
            return new BenchmarkMetricResult(
                descriptor.MetricKey,
                descriptor.FieldName,
                descriptor.MetricType,
                selectedValue,
                aggregate.Median,
                aggregate.Average,
                aggregate.Minimum,
                aggregate.Maximum,
                percentile,
                values.Count,
                status,
                safeMessage,
                sourceName,
                comparisonGroup,
                descriptor.IsGenericImportMetric,
                false);
        }

        private IReadOnlyList<BenchmarkPlayerCandidate> LoadComparisonPlayers(SqliteConnection connection, BenchmarkDefinition definition, string effectivePositionGroup)
        {
            var players = new List<BenchmarkPlayerCandidate>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT
                        P.Id,
                        P.StatlynPlayerId,
                        P.SourceName,
                        P.PositionGroup,
                        P.PrimaryPosition,
                        COALESCE(DS.AllowedUsage, '')
                      FROM Player P
                      LEFT JOIN DataSource DS ON DS.Id = (
                        SELECT Id FROM DataSource WHERE SourceName = P.SourceName ORDER BY ImportedAtUtc DESC, Id DESC LIMIT 1)
                      ORDER BY P.Id;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var player = new BenchmarkPlayerCandidate(
                            reader.GetInt64(0),
                            ReadString(reader, 1),
                            ReadString(reader, 2),
                            ReadString(reader, 3),
                            ReadString(reader, 4),
                            ReadString(reader, 5));

                        if (!string.IsNullOrWhiteSpace(definition.SourceName) &&
                            !string.Equals(player.SourceName, definition.SourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!definition.IncludeFixtureData && IsFixture(player.SourceName, player.AllowedUsage))
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(effectivePositionGroup) &&
                            !string.Equals(player.ResolvedPositionGroup, effectivePositionGroup, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        players.Add(player);
                    }
                }
            }

            return players;
        }

        private string ResolvePositionGroup(BenchmarkDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.PositionGroup))
            {
                return definition.PositionGroup;
            }

            if (!string.IsNullOrWhiteSpace(definition.RoleProfileName))
            {
                var profile = _roleProfiles.FindByName(definition.RoleProfileName);
                if (profile != null)
                {
                    return profile.PositionGroup;
                }
            }

            if (!string.IsNullOrWhiteSpace(definition.TacticalRoleName))
            {
                var role = _roleLab.LoadRoleByName(definition.TacticalRoleName);
                if (role != null)
                {
                    return role.PositionGroup;
                }
            }

            return string.Empty;
        }

        private MetricDescriptor ResolveMetric(SqliteConnection connection, string metricKey)
        {
            if (TryParseAttributeMetric(metricKey, out var attributeName))
            {
                return new MetricDescriptor(metricKey, attributeName, BenchmarkMetricType.AttributeSupport, true, false, true);
            }

            var definition = _metricDefinitions.FindByMetricKey(metricKey);
            if (definition != null)
            {
                var metricType = definition.FieldKey == PlayerFieldKey.PhysicalData
                    ? BenchmarkMetricType.PhysicalMetric
                    : definition.FieldKey == PlayerFieldKey.TechnicalAttribute || definition.FieldKey == PlayerFieldKey.PhysicalAttribute
                        ? BenchmarkMetricType.AttributeSupport
                        : BenchmarkMetricType.PlayerStat;
                return new MetricDescriptor(
                    definition.MetricKey,
                    definition.FieldName,
                    metricType,
                    definition.HigherIsBetter || !definition.LowerIsBetter,
                    definition.IsGenericFootballMetric,
                    false);
            }

            return MetricExistsInPhysicalTable(connection, metricKey)
                ? new MetricDescriptor(metricKey, metricKey, BenchmarkMetricType.PhysicalMetric, true, true, false)
                : new MetricDescriptor(metricKey, metricKey, BenchmarkMetricType.PlayerStat, true, true, false);
        }

        private double? LoadMetricValue(SqliteConnection connection, long playerId, MetricDescriptor descriptor, int minimumMinutes)
        {
            if (descriptor.MetricType == BenchmarkMetricType.PhysicalMetric)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT MetricValue
                          FROM PhysicalMetric
                          WHERE PlayerId = $playerId AND lower(MetricName) = lower($fieldName)
                          ORDER BY Id DESC
                          LIMIT 1;";
                    command.Parameters.AddWithValue("$playerId", playerId);
                    command.Parameters.AddWithValue("$fieldName", descriptor.FieldName);
                    var value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? (double?)null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
            }

            if (descriptor.MetricType == BenchmarkMetricType.AttributeSupport)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT NumericValue
                          FROM VisibleField
                          WHERE PlayerId = $playerId
                            AND lower(FieldName) = lower($fieldName)
                            AND FieldKey IN ('TechnicalAttribute', 'PhysicalAttribute')
                            AND NumericValue IS NOT NULL
                            AND CanScore = 1
                            AND CanStore = 1
                          ORDER BY Id DESC
                          LIMIT 1;";
                    command.Parameters.AddWithValue("$playerId", playerId);
                    command.Parameters.AddWithValue("$fieldName", descriptor.FieldName);
                    var value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? (double?)null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT StatValue, Minutes, SampleMinutesMissing
                      FROM PlayerStat
                      WHERE PlayerId = $playerId AND lower(StatName) = lower($fieldName)
                      ORDER BY Id DESC
                      LIMIT 1;";
                command.Parameters.AddWithValue("$playerId", playerId);
                command.Parameters.AddWithValue("$fieldName", descriptor.FieldName);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var minutes = reader.GetInt32(1);
                    var minutesMissing = reader.GetInt32(2) != 0;
                    if (minimumMinutes > 0 && !minutesMissing && minutes < minimumMinutes)
                    {
                        return null;
                    }

                    return reader.GetDouble(0);
                }
            }
        }

        private bool MetricExistsInPhysicalTable(SqliteConnection connection, string metricKey)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM PhysicalMetric WHERE lower(MetricName) = lower($metricKey);";
                command.Parameters.AddWithValue("$metricKey", metricKey ?? string.Empty);
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        private static AggregateValues Aggregate(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return new AggregateValues(null, null, null, null);
            }

            var ordered = values.OrderBy(value => value).ToList();
            var middle = ordered.Count / 2;
            var median = ordered.Count % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) / 2.0
                : ordered[middle];
            return new AggregateValues(
                median,
                values.Average(),
                values.Min(),
                values.Max());
        }

        private static double ComputePercentile(IReadOnlyList<double> values, double playerValue, bool higherIsBetter)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            var count = higherIsBetter
                ? values.Count(value => value <= playerValue)
                : values.Count(value => value >= playerValue);
            return Math.Round((count / (double)values.Count) * 100.0, 2);
        }

        private static BenchmarkStatus ResolveOverallStatus(IReadOnlyList<BenchmarkMetricResult> results, int comparisonPlayerCount)
        {
            if (comparisonPlayerCount == 0 || results.Count == 0)
            {
                return BenchmarkStatus.NoBenchmark;
            }

            if (results.Any(result => result.Status == BenchmarkStatus.Available))
            {
                return BenchmarkStatus.Available;
            }

            if (results.Any(result => result.Status == BenchmarkStatus.InsufficientSample))
            {
                return BenchmarkStatus.InsufficientSample;
            }

            if (results.Any(result => result.Status == BenchmarkStatus.MissingMetric))
            {
                return BenchmarkStatus.MissingMetric;
            }

            return BenchmarkStatus.NoBenchmark;
        }

        private static string BuildSummaryMessage(BenchmarkStatus status, IReadOnlyList<BenchmarkMetricResult> results, int comparisonPlayerCount)
        {
            if (status == BenchmarkStatus.Available)
            {
                return "Benchmark available from persisted safe comparison data.";
            }

            if (status == BenchmarkStatus.InsufficientSample)
            {
                return "Insufficient sample. No percentile is shown until the comparison group is large enough.";
            }

            if (status == BenchmarkStatus.MissingMetric)
            {
                return "Missing metric. Missing values are not treated as zero.";
            }

            return comparisonPlayerCount == 0
                ? "No benchmark yet. No valid comparison group exists."
                : "No benchmark yet.";
        }

        private static IEnumerable<string> BuildWarnings(BenchmarkDefinition definition, IReadOnlyList<BenchmarkMetricResult> results)
        {
            yield return "Benchmarks use persisted safe SQLite data only.";
            yield return "Generic/import metrics are not FM26-verified.";
            yield return "Missing metrics are not treated as zero.";

            if (results.Any(result => result.Status == BenchmarkStatus.InsufficientSample))
            {
                yield return "At least one metric has an insufficient sample; percentile is hidden.";
            }

            if (definition.MinimumMinutes > 0)
            {
                yield return "Minimum minutes filter applied where PlayerStat minutes are available.";
            }
        }

        private static string BuildComparisonGroup(BenchmarkDefinition definition, string effectivePositionGroup)
        {
            var parts = new List<string>();
            if (string.IsNullOrWhiteSpace(definition.SourceName))
            {
                parts.Add("All imported sources");
            }
            else
            {
                parts.Add("Source: " + definition.SourceName);
            }

            if (!string.IsNullOrWhiteSpace(effectivePositionGroup))
            {
                parts.Add("Position group: " + effectivePositionGroup);
            }

            if (!string.IsNullOrWhiteSpace(definition.RoleProfileName))
            {
                parts.Add("Role profile: " + definition.RoleProfileName);
            }

            if (!string.IsNullOrWhiteSpace(definition.TacticalRoleName))
            {
                parts.Add("Tactical role: " + definition.TacticalRoleName);
            }

            if (!string.IsNullOrWhiteSpace(definition.TacticalRolePairName))
            {
                parts.Add("Tactical role pair: " + definition.TacticalRolePairName);
            }

            return string.Join(" | ", parts);
        }

        private static bool TryParseAttributeMetric(string metricKey, out string attributeName)
        {
            attributeName = string.Empty;
            var value = metricKey ?? string.Empty;
            var prefixes = new[] { "AttributeSupport:", "Attribute:" };
            foreach (var prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    attributeName = value.Substring(prefix.Length).Trim();
                    return !string.IsNullOrWhiteSpace(attributeName);
                }
            }

            return false;
        }

        private static bool IsFixture(string sourceName, string allowedUsage)
        {
            return (sourceName ?? string.Empty).IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (allowedUsage ?? string.Empty).IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReadString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private sealed class BenchmarkPlayerCandidate
        {
            public BenchmarkPlayerCandidate(long id, string statlynPlayerId, string sourceName, string positionGroup, string primaryPosition, string allowedUsage)
            {
                Id = id;
                StatlynPlayerId = statlynPlayerId ?? string.Empty;
                SourceName = sourceName ?? string.Empty;
                PositionGroup = positionGroup ?? string.Empty;
                PrimaryPosition = primaryPosition ?? string.Empty;
                AllowedUsage = allowedUsage ?? string.Empty;
                var positionValue = string.IsNullOrWhiteSpace(primaryPosition) ? positionGroup : primaryPosition;
                ResolvedPositionGroup = RecruitmentOutputSummaryService.ResolvePositionGroup(positionValue ?? string.Empty);
            }

            public long Id { get; }

            public string StatlynPlayerId { get; }

            public string SourceName { get; }

            public string PositionGroup { get; }

            public string PrimaryPosition { get; }

            public string AllowedUsage { get; }

            public string ResolvedPositionGroup { get; }
        }

        private sealed class MetricDescriptor
        {
            public MetricDescriptor(string metricKey, string fieldName, BenchmarkMetricType metricType, bool higherIsBetter, bool isGenericImportMetric, bool isVerifiedFm26Metric)
            {
                MetricKey = metricKey ?? string.Empty;
                FieldName = fieldName ?? string.Empty;
                MetricType = metricType;
                HigherIsBetter = higherIsBetter;
                IsGenericImportMetric = isGenericImportMetric;
                IsVerifiedFm26Metric = isVerifiedFm26Metric;
            }

            public string MetricKey { get; }

            public string FieldName { get; }

            public BenchmarkMetricType MetricType { get; }

            public bool HigherIsBetter { get; }

            public bool IsGenericImportMetric { get; }

            public bool IsVerifiedFm26Metric { get; }
        }

        private sealed class AggregateValues
        {
            public AggregateValues(double? median, double? average, double? minimum, double? maximum)
            {
                Median = median;
                Average = average;
                Minimum = minimum;
                Maximum = maximum;
            }

            public double? Median { get; }

            public double? Average { get; }

            public double? Minimum { get; }

            public double? Maximum { get; }
        }
    }

    public sealed class BenchmarkCalculationResult
    {
        public BenchmarkCalculationResult(BenchmarkDefinition definition, BenchmarkPlayerSummary summary, int comparisonPlayerCount)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            ComparisonPlayerCount = comparisonPlayerCount < 0 ? 0 : comparisonPlayerCount;
        }

        public BenchmarkDefinition Definition { get; }

        public BenchmarkPlayerSummary Summary { get; }

        public int ComparisonPlayerCount { get; }
    }
}
