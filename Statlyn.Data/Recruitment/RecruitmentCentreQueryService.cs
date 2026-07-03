using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Analytics;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Persistence;
using Statlyn.Core;

namespace Statlyn.Data.Recruitment
{
    public sealed class RecruitmentCentreQueryService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly RecruitmentOutputSummaryService _summaryService;
        private readonly RoleOutputExpectationRepository _roleOutputExpectations;
        private readonly BenchmarkWorkflowService _benchmarkWorkflow;

        public RecruitmentCentreQueryService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _summaryService = new RecruitmentOutputSummaryService();
            _roleOutputExpectations = new RoleOutputExpectationRepository(connectionFactory);
            _benchmarkWorkflow = new BenchmarkWorkflowService(connectionFactory);
        }

        public RecruitmentCentreResult Query(RecruitmentCentreQuery? query)
        {
            query = query ?? new RecruitmentCentreQuery();
            var diagnostics = new List<string>();
            var profiles = _roleOutputExpectations.LoadAll();
            var rows = LoadRows(profiles);
            var sources = rows.Select(row => row.SourceName).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList();

            var filtered = ApplyFilters(rows, query).ToList();
            var total = filtered.Count;
            filtered = ApplySort(filtered, query).Take(SafeLimit(query.Limit)).ToList();

            diagnostics.Add("Recruitment Centre loaded persisted safe player rows only.");
            diagnostics.Add(profiles.Count == 0
                ? "Role output expectations used generic import-only fallback profiles."
                : "Role output expectations used persisted SQLite profiles when position groups matched.");
            diagnostics.Add("No raw provider snapshots, hidden FM26 values or blocked raw values are returned.");

            return new RecruitmentCentreResult(
                filtered,
                total,
                sources,
                diagnostics,
                filtered.Count == 0
                    ? "No imported players matched this Recruitment Centre query."
                    : "Recruitment Centre query returned persisted safe player rows.");
        }

        private IReadOnlyList<RecruitmentCentrePlayerRow> LoadRows(IReadOnlyList<RoleOutputExpectationProfile> profiles)
        {
            var rows = new List<RecruitmentCentrePlayerRow>();
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT
                        P.Id,
                        P.StatlynPlayerId,
                        P.DisplayName,
                        P.Age,
                        P.Nationality,
                        P.PrimaryPosition,
                        P.SourceName,
                        P.SourceConfidence,
                        P.DataCompleteness,
                        RS.RoleFit,
                        RS.RoleName,
                        RS.TechnicalFit,
                        RS.StatisticalFit,
                        RS.PhysicalFit,
                        RS.TacticalFit,
                        RS.RiskScore,
                        RS.Confidence,
                        RS.Recommendation,
                        RS.MissingData,
                        DS.AllowedUsage,
                        DS.ProviderType,
                        DS.IsLive,
                        (SELECT COUNT(*) FROM BlockedFieldAudit B WHERE B.SourceEntityId = P.StatlynPlayerId) AS BlockedCount
                      FROM Player P
                      LEFT JOIN RoleScore RS ON RS.Id = (
                        SELECT Id FROM RoleScore WHERE PlayerId = P.Id ORDER BY CreatedAtUtc DESC, Id DESC LIMIT 1)
                      LEFT JOIN DataSource DS ON DS.Id = (
                        SELECT Id FROM DataSource WHERE SourceName = P.SourceName ORDER BY ImportedAtUtc DESC, Id DESC LIMIT 1)
                      ORDER BY P.DisplayName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(ReadRow(connection, reader, profiles));
                    }
                }
            }

            return rows;
        }

        private RecruitmentCentrePlayerRow ReadRow(SqliteConnection connection, SqliteDataReader reader, IReadOnlyList<RoleOutputExpectationProfile> profiles)
        {
            var playerId = reader.GetInt64(0);
            var statlynPlayerId = ReadString(reader, 1);
            var displayName = ReadString(reader, 2);
            var age = ReadNullableInt(reader, 3);
            var nationality = ReadString(reader, 4);
            var primaryPosition = ReadString(reader, 5);
            var sourceName = ReadString(reader, 6);
            var sourceConfidence = reader.GetInt32(7);
            var dataCompleteness = reader.GetInt32(8);
            var roleFit = ReadNullableInt(reader, 9);
            var roleName = roleFit.HasValue
                ? RoleNameSanitizer.SanitizeForDisplay(ReadString(reader, 10), "Unknown role")
                : "Not scored";
            var technicalFit = ReadNullableInt(reader, 11);
            var statisticalFit = ReadNullableInt(reader, 12);
            var physicalFit = ReadNullableInt(reader, 13);
            var tacticalFit = ReadNullableInt(reader, 14);
            var risk = ReadNullableInt(reader, 15);
            var confidence = ReadNullableInt(reader, 16);
            var recommendation = ReadRecommendation(reader, 17);
            var missingData = SplitValues(ReadString(reader, 18)).ToList();
            var allowedUsage = ReadString(reader, 19);
            var providerType = ReadString(reader, 20);
            var isLive = !reader.IsDBNull(21) && reader.GetInt32(21) != 0;
            var blockedCount = reader.GetInt32(22);
            var positionGroup = RecruitmentOutputSummaryService.ResolvePositionGroup(primaryPosition);
            var stats = LoadPlayerStats(connection, playerId);
            var metrics = LoadPhysicalMetrics(connection, playerId);
            var selectedProfile = _summaryService.SelectProfile(positionGroup, string.Empty, profiles);
            var summary = _summaryService.Build(primaryPosition, stats, metrics, selectedProfile, null);
            var missingCount = missingData.Count + summary.MissingCoreMetrics.Count;
            var benchmarkIndicator = BuildBenchmarkIndicator(_benchmarkWorkflow.BuildPlayerBenchmarkSummary(statlynPlayerId));
            var warnings = new List<string>();

            if (summary.MissingCoreMetrics.Count > 0)
            {
                warnings.Add(summary.ConfidenceImpactText);
            }

            if (blockedCount > 0)
            {
                warnings.Add(blockedCount.ToString(CultureInfo.InvariantCulture) + " blocked field(s) audited safely.");
            }

            if (roleFit == null)
            {
                warnings.Add("Not scored yet.");
            }

            return new RecruitmentCentrePlayerRow(
                statlynPlayerId,
                displayName,
                age.HasValue ? age.Value.ToString(CultureInfo.InvariantCulture) : "Unknown",
                string.IsNullOrWhiteSpace(nationality) ? "Unknown" : nationality,
                positionGroup,
                string.IsNullOrWhiteSpace(primaryPosition) ? "Unknown" : primaryPosition,
                sourceName,
                sourceConfidence,
                dataCompleteness,
                roleName,
                roleFit,
                technicalFit,
                statisticalFit,
                physicalFit,
                tacticalFit.HasValue ? tacticalFit.Value.ToString(CultureInfo.InvariantCulture) : "Unknown",
                risk,
                confidence,
                recommendation,
                blockedCount,
                missingCount,
                summary.CoreMetrics.Concat(summary.SupportingMetrics).Take(5).ToList(),
                benchmarkIndicator,
                warnings,
                IsFixture(sourceName, allowedUsage),
                string.Equals(providerType, ProviderType.FM26LiveMemory.ToString(), StringComparison.OrdinalIgnoreCase) && isLive);
        }

        private static RecruitmentBenchmarkIndicatorViewModel BuildBenchmarkIndicator(BenchmarkPlayerSummary summary)
        {
            if (summary == null || summary.OverallStatus == BenchmarkStatus.NoBenchmark || summary.Results.Count == 0)
            {
                return RecruitmentBenchmarkIndicatorViewModel.NoBenchmark();
            }

            var metric = summary.Results.FirstOrDefault(result => result.Status == BenchmarkStatus.Available) ?? summary.Results.First();
            var percentile = metric.Status == BenchmarkStatus.Available && metric.Percentile.HasValue
                ? metric.Percentile.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : string.Empty;
            var keyMetric = metric.Status == BenchmarkStatus.Available
                ? metric.MetricKey
                : metric.Status == BenchmarkStatus.InsufficientSample
                    ? "Insufficient sample"
                    : metric.Status == BenchmarkStatus.MissingMetric
                        ? "Missing metric"
                        : "No benchmark yet";

            return new RecruitmentBenchmarkIndicatorViewModel(
                summary.OverallStatus.ToString(),
                keyMetric,
                percentile,
                metric.SampleSize,
                summary.SafeMessage);
        }

        private IReadOnlyList<PlayerStatRecord> LoadPlayerStats(SqliteConnection connection, long playerId)
        {
            var stats = new List<PlayerStatRecord>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT FieldInstanceKey, StatName, StatValue, Minutes, SampleMinutesMissing, MinutesSource, SourceName, Confidence
                      FROM PlayerStat
                      WHERE PlayerId = $playerId
                      ORDER BY Id;";
                command.Parameters.AddWithValue("$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats.Add(new PlayerStatRecord(playerId, reader.GetString(0), reader.GetString(1), reader.GetDouble(2), reader.GetInt32(3), reader.GetInt32(4) != 0, reader.GetString(5), reader.GetString(6), reader.GetInt32(7)));
                    }
                }
            }

            return stats;
        }

        private IReadOnlyList<PhysicalMetricRecord> LoadPhysicalMetrics(SqliteConnection connection, long playerId)
        {
            var metrics = new List<PhysicalMetricRecord>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT FieldInstanceKey, MetricName, MetricValue, Unit, SourceName, Confidence
                      FROM PhysicalMetric
                      WHERE PlayerId = $playerId
                      ORDER BY Id;";
                command.Parameters.AddWithValue("$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        metrics.Add(new PhysicalMetricRecord(playerId, reader.GetString(0), reader.GetString(1), reader.GetDouble(2), reader.IsDBNull(3) ? string.Empty : reader.GetString(3), reader.GetString(4), reader.GetInt32(5)));
                    }
                }
            }

            return metrics;
        }

        private static IEnumerable<RecruitmentCentrePlayerRow> ApplyFilters(IEnumerable<RecruitmentCentrePlayerRow> rows, RecruitmentCentreQuery query)
        {
            foreach (var row in rows)
            {
                if (!Matches(row.DisplayName, query.SearchText) && !Matches(row.StatlynPlayerId, query.SearchText))
                {
                    continue;
                }

                if (!Matches(row.SourceName, query.SourceName) ||
                    !Matches(row.PositionGroup, query.PositionGroup) ||
                    !Matches(row.PrimaryPosition, query.PrimaryPosition) ||
                    !Matches(row.Nationality, query.Nationality))
                {
                    continue;
                }

                var age = int.TryParse(row.AgeDisplay, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAge) ? parsedAge : (int?)null;
                if (query.MinimumAge.HasValue && (!age.HasValue || age.Value < query.MinimumAge.Value))
                {
                    continue;
                }

                if (query.MaximumAge.HasValue && (!age.HasValue || age.Value > query.MaximumAge.Value))
                {
                    continue;
                }

                if (query.MinimumRoleFit.HasValue && (!row.RoleFit.HasValue || row.RoleFit.Value < query.MinimumRoleFit.Value))
                {
                    continue;
                }

                if (query.MinimumConfidence.HasValue && (!row.Confidence.HasValue || row.Confidence.Value < query.MinimumConfidence.Value))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(query.Recommendation) &&
                    !string.Equals(row.Recommendation.HasValue ? row.Recommendation.Value.ToString() : "Not scored", query.Recommendation, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (query.HasBlockedFields.HasValue && (row.BlockedFieldCount > 0) != query.HasBlockedFields.Value)
                {
                    continue;
                }

                if (query.HasMissingData.HasValue && (row.MissingDataCount > 0) != query.HasMissingData.Value)
                {
                    continue;
                }

                yield return row;
            }
        }

        private static IReadOnlyList<RecruitmentCentrePlayerRow> ApplySort(IReadOnlyList<RecruitmentCentrePlayerRow> rows, RecruitmentCentreQuery query)
        {
            var descending = string.Equals(query.SortDirection, "Descending", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(query.SortDirection, "Desc", StringComparison.OrdinalIgnoreCase);
            Func<RecruitmentCentrePlayerRow, object> key = (query.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "rolefit" => row => row.RoleFit ?? -1,
                "confidence" => row => row.Confidence ?? -1,
                "sourceconfidence" => row => row.SourceConfidence,
                "datacompleteness" => row => row.DataCompleteness,
                "risk" => row => row.RiskScore ?? 999,
                "position" => row => row.PrimaryPosition,
                "source" => row => row.SourceName,
                _ => row => row.DisplayName
            };

            return descending ? rows.OrderByDescending(key).ToList() : rows.OrderBy(key).ToList();
        }

        private static bool Matches(string value, string filter)
        {
            return string.IsNullOrWhiteSpace(filter) ||
                   (value ?? string.Empty).IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int SafeLimit(int limit)
        {
            if (limit <= 0)
            {
                return 100;
            }

            return Math.Min(limit, 500);
        }

        private static RecruitmentRecommendation? ReadRecommendation(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return Enum.TryParse<RecruitmentRecommendation>(reader.GetString(ordinal), out var parsed)
                ? parsed
                : RecruitmentRecommendation.ScoutFurther;
        }

        private static int? ReadNullableInt(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        private static string ReadString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static string[] SplitValues(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsFixture(string sourceName, string allowedUsage)
        {
            return (sourceName ?? string.Empty).IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (allowedUsage ?? string.Empty).IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
