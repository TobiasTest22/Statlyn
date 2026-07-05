using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Statlyn.Analytics.PlayerIntelligence;
using Statlyn.Core;
using Statlyn.Data.Profile;

namespace Statlyn.Data.PlayerIntelligence
{
    public sealed class PlayerIntelligenceService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly RoleParameterDefinitionService _roleDefinitions;
        private readonly PlayerRadarService _radarService;
        private readonly PlayerPer90Service _per90Service;
        private readonly PlayerHeatmapService _heatmapService;
        private readonly PlayerValueEstimateService _valueService;
        private readonly PlayerSimilarityService _similarityService;
        private readonly LeagueAverageComparisonService _leagueComparisonService;
        private readonly PlayerFitProjectionService _fitProjectionService;
        private readonly PlayerArchetypeService _archetypeService;

        public PlayerIntelligenceService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _roleDefinitions = new RoleParameterDefinitionService();
            _radarService = new PlayerRadarService();
            _per90Service = new PlayerPer90Service();
            _heatmapService = new PlayerHeatmapService();
            _valueService = new PlayerValueEstimateService();
            _similarityService = new PlayerSimilarityService();
            _leagueComparisonService = new LeagueAverageComparisonService();
            _fitProjectionService = new PlayerFitProjectionService();
            _archetypeService = new PlayerArchetypeService();
        }

        public PlayerIntelligenceReadiness GetReadiness()
        {
            using (var connection = _connectionFactory.OpenConnection())
            {
                var importedPlayers = CountRows(connection, "Player");
                var eventRows = CountRows(connection, "player_event_locations");
                var marketRows = CountRows(connection, "player_market_context");
                var teamStyleRows = CountRows(connection, "team_style_profiles");
                var leagueRows = CountRows(connection, "league_average_metrics");
                var vectorRows = CountRows(connection, "player_style_vectors");

                var warnings = new List<string>();
                if (importedPlayers == 0)
                {
                    warnings.Add("No local players imported yet.");
                }

                if (eventRows == 0)
                {
                    warnings.Add("No safe event-location rows imported; heatmaps stay unavailable.");
                }

                if (marketRows == 0)
                {
                    warnings.Add("No dedicated valuation context imported; fair value needs a safe anchor or comparable sample.");
                }

                return new PlayerIntelligenceReadiness(
                    importedPlayers > 0,
                    importedPlayers > 0
                        ? "Player intelligence foundation is ready for safe imported data."
                        : "Player intelligence is awaiting local player data. No generated player data is used.",
                    importedPlayers,
                    eventRows,
                    marketRows,
                    teamStyleRows,
                    leagueRows,
                    vectorRows,
                    warnings);
            }
        }

        public PlayerIntelligenceResult GetIntelligence(string statlynPlayerId)
        {
            var profile = LoadProfile(statlynPlayerId);
            return new PlayerIntelligenceResult(
                profile,
                GetRadar(statlynPlayerId),
                GetPer90(statlynPlayerId),
                GetHeatmap(statlynPlayerId),
                GetValue(statlynPlayerId),
                GetFit(statlynPlayerId),
                GetArchetype(statlynPlayerId),
                GetSimilar(statlynPlayerId),
                GetLeagueComparison(statlynPlayerId),
                GetRoleAssessment(statlynPlayerId));
        }

        public PlayerIntelligenceProfile LoadProfile(string statlynPlayerId)
        {
            var query = QueryProfile(statlynPlayerId);
            if (!query.Success || query.Player == null)
            {
                return PlayerIntelligenceProfile.Missing(statlynPlayerId, query.SafeMessage);
            }

            var row = LoadPlayerRow(statlynPlayerId);
            var role = query.LatestRoleScore == null ? "Not assessed" : query.LatestRoleScore.RoleName;
            var confidence = query.LatestRoleScore == null ? query.Player.SourceConfidence : Math.Min(query.Player.SourceConfidence, query.LatestRoleScore.Confidence);
            return new PlayerIntelligenceProfile(
                true,
                "Player intelligence profile loaded from safe local data.",
                query.Player.StatlynPlayerId,
                query.Player.DisplayName,
                row == null ? "Unknown" : FirstNonEmpty(row.PrimaryPosition, row.PositionGroup, "Unknown"),
                role,
                query.SourceMetadata == null ? query.Player.SourceName : query.SourceMetadata.SourceName,
                row == null ? null : row.Age,
                row == null ? "Unknown" : FirstNonEmpty(row.Nationality, "Unknown"),
                DataQualityFromCompleteness(query.Player.DataCompleteness),
                confidence,
                query.LatestRoleScore == null ? (int?)null : query.LatestRoleScore.RoleFit,
                query.Warnings,
                new List<string>());
        }

        public PlayerSkillRadar GetRadar(string statlynPlayerId)
        {
            var profile = QueryProfile(statlynPlayerId);
            if (!profile.Success)
            {
                return _radarService.Build(new List<PlayerRadarAxis>(), "Role-specific");
            }

            var fields = profile.VisibleFields
                .Where(field => field.CanDisplay && field.CanScore && field.NumericValue.HasValue && !field.IsBlocked)
                .Take(8)
                .Select(field =>
                {
                    var value = field.NumericValue.GetValueOrDefault();
                    return new PlayerRadarAxis(
                        NormalizeMetricKey(field.FieldName),
                        field.FieldName,
                        Math.Round(Clamp(value, 0, 100), 2),
                        null,
                        field.FieldName,
                        "Visible/imported",
                        field.Confidence);
                })
                .ToList();

            return _radarService.Build(fields, profile.LatestRoleScore == null ? "Role-specific" : profile.LatestRoleScore.RoleName);
        }

        public PlayerPer90Summary GetPer90(string statlynPlayerId)
        {
            var profile = QueryProfile(statlynPlayerId);
            var metrics = profile.PlayerStats
                .Where(stat => stat.Minutes > 0 && !stat.SampleMinutesMissing)
                .Select(stat => new SafeMetricInput(NormalizeMetricKey(stat.StatName), stat.StatName, stat.StatValue, stat.Minutes, "per 90", stat.Confidence))
                .ToList();

            return _per90Service.Build(metrics);
        }

        public PlayerHeatmapSummary GetHeatmap(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT MatchId, Minute, X, Y, ActionType, Confidence
                      FROM player_event_locations
                      WHERE StatlynPlayerId = $statlynPlayerId
                      ORDER BY MatchId, Minute
                      LIMIT 750;";
                command.Parameters.AddWithValue("$statlynPlayerId", statlynPlayerId ?? string.Empty);
                var points = new List<PlayerHeatmapPoint>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        points.Add(new PlayerHeatmapPoint(
                            ReadString(reader, 0),
                            reader.GetDouble(1),
                            Clamp(reader.GetDouble(2), 0, 100),
                            Clamp(reader.GetDouble(3), 0, 100),
                            ReadString(reader, 4),
                            reader.GetInt32(5)));
                    }
                }

                return _heatmapService.Build(points);
            }
        }

        public PlayerValueEstimate GetValue(string statlynPlayerId)
        {
            var profile = QueryProfile(statlynPlayerId);
            if (!profile.Success || profile.Player == null)
            {
                return _valueService.Estimate(new FairValueInput());
            }

            var row = LoadPlayerRow(statlynPlayerId);
            var context = LoadMarketContext(statlynPlayerId);
            var comparableValues = LoadComparableValues(statlynPlayerId);
            var anchor = context == null ? ParseValueAnchor(row == null ? string.Empty : row.ValueDisplay) : ResolveContextAnchor(context);
            var currency = FirstNonEmpty(context == null ? string.Empty : context.Currency, CurrencyFromDisplay(row == null ? string.Empty : row.ValueDisplay));
            var contractMonths = context != null && context.ContractMonthsRemaining.HasValue
                ? context.ContractMonthsRemaining
                : ParseContractMonths(row == null ? string.Empty : row.ContractEnd);
            var minutes = profile.PlayerStats.Where(stat => !stat.SampleMinutesMissing).Select(stat => stat.Minutes).DefaultIfEmpty(0).Max();

            return _valueService.Estimate(new FairValueInput
            {
                Currency = currency,
                AnchorValue = anchor,
                AskingPrice = context == null ? null : context.AskingPrice,
                ComparableValues = comparableValues,
                Age = row == null ? null : row.Age,
                RoleName = profile.LatestRoleScore == null ? string.Empty : profile.LatestRoleScore.RoleName,
                PositionGroup = row == null ? string.Empty : row.PositionGroup,
                ContractMonthsRemaining = contractMonths,
                Minutes = minutes,
                PerformanceIndex = profile.LatestRoleScore == null ? (double?)null : profile.LatestRoleScore.RoleFit,
                RoleFit = profile.LatestRoleScore == null ? (int?)null : profile.LatestRoleScore.RoleFit,
                LeagueStrengthMultiplier = context == null ? null : context.LeagueStrengthMultiplier,
                TacticalFit = profile.LatestRoleScore == null ? null : profile.LatestRoleScore.TacticalFit,
                DataCompleteness = profile.Player.DataCompleteness,
                SafeRiskFlags = profile.Warnings
            });
        }

        public PlayerFitProjection GetFit(string statlynPlayerId)
        {
            var profile = QueryProfile(statlynPlayerId);
            var hasRole = profile.LatestRoleScore != null;
            var hasTeamStyle = CountRows("team_style_profiles") > 0;
            return _fitProjectionService.Build(
                hasTeamStyle,
                hasRole,
                profile.LatestRoleScore == null ? (int?)null : profile.LatestRoleScore.RoleFit,
                profile.LatestRoleScore == null ? string.Empty : profile.LatestRoleScore.RoleName);
        }

        public PlayerArchetypeResult GetArchetype(string statlynPlayerId)
        {
            var vectorMetrics = LoadStyleVectorMetrics(statlynPlayerId);
            return _archetypeService.Build(vectorMetrics);
        }

        public PlayerSimilarityResult GetSimilar(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            {
                var sample = CountDistinct(connection, "player_style_vectors", "StatlynPlayerId");
                return _similarityService.Build(new List<SimilarPlayerCandidate>(), sample);
            }
        }

        public LeagueAverageComparison GetLeagueComparison(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT MetricName, AverageValue, SampleSize, Confidence
                      FROM league_average_metrics
                      ORDER BY SampleSize DESC, MetricName
                      LIMIT 8;";
                var comparisons = new List<PlayerRadarAxis>();
                var sampleSize = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sampleSize = Math.Max(sampleSize, reader.GetInt32(2));
                        comparisons.Add(new PlayerRadarAxis(
                            NormalizeMetricKey(ReadString(reader, 0)),
                            ReadString(reader, 0),
                            null,
                            reader.GetDouble(1),
                            ReadString(reader, 0),
                            "League average",
                            reader.GetInt32(3)));
                    }
                }

                return _leagueComparisonService.Build("local-league-sample", "position or role", sampleSize, comparisons);
            }
        }

        public RoleSpecificAssessment GetRoleAssessment(string statlynPlayerId)
        {
            var profile = QueryProfile(statlynPlayerId);
            var roleName = profile.LatestRoleScore == null ? string.Empty : profile.LatestRoleScore.RoleName;
            var definition = _roleDefinitions.FindByRole(roleName) ?? _roleDefinitions.FindByRole(ResolveFallbackRole(profile));
            if (definition == null)
            {
                return new RoleSpecificAssessment(
                    false,
                    "Role assessment unavailable. No matching role parameter definition was found.",
                    string.IsNullOrWhiteSpace(roleName) ? "Not assessed" : roleName,
                    "Unavailable",
                    0,
                    new[] { "role parameter definition" },
                    new List<string>(),
                    null);
            }

            var metricNames = new HashSet<string>(
                profile.PlayerStats.Select(stat => NormalizeMetricKey(stat.StatName))
                    .Concat(profile.PhysicalMetrics.Select(metric => NormalizeMetricKey(metric.MetricName)))
                    .Concat(profile.VisibleFields.Select(field => NormalizeMetricKey(field.FieldName))),
                StringComparer.OrdinalIgnoreCase);

            var missing = definition.PrimaryMetrics
                .Where(metric => metric.Required && !metricNames.Contains(NormalizeMetricKey(metric.MetricKey)))
                .Select(metric => metric.Label)
                .ToList();
            var confidence = profile.LatestRoleScore == null ? 0 : profile.LatestRoleScore.Confidence;
            if (missing.Count > 0)
            {
                confidence = Math.Min(confidence, 35);
            }

            return new RoleSpecificAssessment(
                missing.Count == 0 && confidence > 0,
                missing.Count == 0
                    ? "Role assessment uses safe backend role parameters and imported metrics."
                    : "Role assessment unavailable. Missing role-specific safe metrics.",
                definition.RoleName,
                missing.Count == 0 ? "Imported" : "Limited",
                confidence,
                missing,
                profile.Warnings,
                definition);
        }

        public IReadOnlyList<RoleParameterDefinition> GetRoleDefinitions()
        {
            return _roleDefinitions.GetDefaultDefinitions();
        }

        private PlayerProfileResult QueryProfile(string statlynPlayerId)
        {
            return new PlayerProfileQueryService(_connectionFactory).Query(new PlayerProfileQuery { StatlynPlayerId = statlynPlayerId ?? string.Empty, IncludeAttributes = true });
        }

        private PlayerTableRow? LoadPlayerRow(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Age, Nationality, PositionGroup, PrimaryPosition, ContractEnd, WageDisplay, MarketValueDisplay
                      FROM Player
                      WHERE StatlynPlayerId = $statlynPlayerId
                      LIMIT 1;";
                command.Parameters.AddWithValue("$statlynPlayerId", statlynPlayerId ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new PlayerTableRow(
                        reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0),
                        ReadString(reader, 1),
                        ReadString(reader, 2),
                        ReadString(reader, 3),
                        ReadString(reader, 4),
                        ReadString(reader, 5),
                        ReadString(reader, 6));
                }
            }
        }

        private MarketContextRow? LoadMarketContext(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Currency, ValueLow, ValueMid, ValueHigh, AskingPrice, Wage, ContractEnd, LeagueLevel, Confidence
                      FROM player_market_context
                      WHERE StatlynPlayerId = $statlynPlayerId
                      ORDER BY UpdatedAtUtc DESC, Id DESC
                      LIMIT 1;";
                command.Parameters.AddWithValue("$statlynPlayerId", statlynPlayerId ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new MarketContextRow(
                        ReadString(reader, 0),
                        ReadNullableDouble(reader, 1),
                        ReadNullableDouble(reader, 2),
                        ReadNullableDouble(reader, 3),
                        ReadNullableDouble(reader, 4),
                        ReadNullableDouble(reader, 5),
                        ParseContractMonths(ReadString(reader, 6)),
                        ReadString(reader, 7),
                        reader.GetInt32(8));
                }
            }
        }

        private IReadOnlyList<double> LoadComparableValues(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT ValueMid, AskingPrice
                      FROM player_market_context
                      WHERE StatlynPlayerId <> $statlynPlayerId
                      ORDER BY UpdatedAtUtc DESC
                      LIMIT 50;";
                command.Parameters.AddWithValue("$statlynPlayerId", statlynPlayerId ?? string.Empty);
                var values = new List<double>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = ReadNullableDouble(reader, 0) ?? ReadNullableDouble(reader, 1);
                        if (value.HasValue && value.Value > 0)
                        {
                            values.Add(value.Value);
                        }
                    }
                }

                return values;
            }
        }

        private IReadOnlyList<SafeMetricInput> LoadStyleVectorMetrics(string statlynPlayerId)
        {
            using (var connection = _connectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT MetricName, MetricValue, Minutes, Confidence
                      FROM player_style_vectors
                      WHERE StatlynPlayerId = $statlynPlayerId
                      ORDER BY VectorKey, MetricName
                      LIMIT 20;";
                command.Parameters.AddWithValue("$statlynPlayerId", statlynPlayerId ?? string.Empty);
                var metrics = new List<SafeMetricInput>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = ReadString(reader, 0);
                        metrics.Add(new SafeMetricInput(NormalizeMetricKey(name), name, reader.GetDouble(1), reader.GetInt32(2), "style", reader.GetInt32(3)));
                    }
                }

                return metrics;
            }
        }

        private static double? ResolveContextAnchor(MarketContextRow context)
        {
            if (context.AskingPrice.HasValue)
            {
                return context.AskingPrice;
            }

            if (context.ValueMid.HasValue)
            {
                return context.ValueMid;
            }

            if (context.ValueLow.HasValue && context.ValueHigh.HasValue)
            {
                return (context.ValueLow.Value + context.ValueHigh.Value) / 2.0;
            }

            return context.ValueLow ?? context.ValueHigh;
        }

        private int CountRows(string tableName)
        {
            using (var connection = _connectionFactory.OpenConnection())
            {
                return CountRows(connection, tableName);
            }
        }

        private static int CountRows(SqliteConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + tableName + ";";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static int CountDistinct(SqliteConnection connection, string tableName, string columnName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(DISTINCT " + columnName + ") FROM " + tableName + ";";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static string ReadString(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static double? ReadNullableDouble(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (double?)null : reader.GetDouble(ordinal);
        }

        private static string ResolveFallbackRole(PlayerProfileResult profile)
        {
            var row = profile.Player == null ? null : profile.VisibleFields.FirstOrDefault(field => string.Equals(field.FieldName, "PrimaryPosition", StringComparison.OrdinalIgnoreCase));
            return row == null ? "Centre-back" : row.DisplayValue;
        }

        private static string DataQualityFromCompleteness(int completeness)
        {
            if (completeness >= 80)
            {
                return "Strong";
            }

            if (completeness >= 55)
            {
                return "Limited";
            }

            return "Low";
        }

        private static string NormalizeMetricKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToLowerInvariant();
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static double? ParseValueAnchor(string display)
        {
            if (string.IsNullOrWhiteSpace(display))
            {
                return null;
            }

            var matches = Regex.Matches(display, @"([0-9]+(?:[\.,][0-9]+)?)\s*([mMkK])?");
            var values = new List<double>();
            foreach (Match match in matches)
            {
                if (!double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    continue;
                }

                var suffix = match.Groups[2].Value;
                if (string.Equals(suffix, "m", StringComparison.OrdinalIgnoreCase))
                {
                    number *= 1000000.0;
                }
                else if (string.Equals(suffix, "k", StringComparison.OrdinalIgnoreCase))
                {
                    number *= 1000.0;
                }

                if (number > 0)
                {
                    values.Add(number);
                }
            }

            return values.Count == 0 ? (double?)null : values.Average();
        }

        private static string CurrencyFromDisplay(string display)
        {
            if (string.IsNullOrWhiteSpace(display))
            {
                return string.Empty;
            }

            if (display.IndexOf("€", StringComparison.Ordinal) >= 0)
            {
                return "EUR";
            }

            if (display.IndexOf("£", StringComparison.Ordinal) >= 0)
            {
                return "GBP";
            }

            if (display.IndexOf("$", StringComparison.Ordinal) >= 0)
            {
                return "USD";
            }

            return string.Empty;
        }

        private static int? ParseContractMonths(string contractEnd)
        {
            if (string.IsNullOrWhiteSpace(contractEnd))
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(contractEnd, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                return null;
            }

            var months = ((date.Year - DateTimeOffset.UtcNow.Year) * 12) + date.Month - DateTimeOffset.UtcNow.Month;
            return months < 0 ? 0 : months;
        }

        private sealed class PlayerTableRow
        {
            public PlayerTableRow(int? age, string nationality, string positionGroup, string primaryPosition, string contractEnd, string wageDisplay, string valueDisplay)
            {
                Age = age;
                Nationality = nationality ?? string.Empty;
                PositionGroup = positionGroup ?? string.Empty;
                PrimaryPosition = primaryPosition ?? string.Empty;
                ContractEnd = contractEnd ?? string.Empty;
                WageDisplay = wageDisplay ?? string.Empty;
                ValueDisplay = valueDisplay ?? string.Empty;
            }

            public int? Age { get; }

            public string Nationality { get; }

            public string PositionGroup { get; }

            public string PrimaryPosition { get; }

            public string ContractEnd { get; }

            public string WageDisplay { get; }

            public string ValueDisplay { get; }
        }

        private sealed class MarketContextRow
        {
            public MarketContextRow(
                string currency,
                double? valueLow,
                double? valueMid,
                double? valueHigh,
                double? askingPrice,
                double? wage,
                int? contractMonthsRemaining,
                string leagueLevel,
                int confidence)
            {
                Currency = currency ?? string.Empty;
                ValueLow = valueLow;
                ValueMid = valueMid;
                ValueHigh = valueHigh;
                AskingPrice = askingPrice;
                Wage = wage;
                ContractMonthsRemaining = contractMonthsRemaining;
                LeagueLevel = leagueLevel ?? string.Empty;
                Confidence = confidence;
            }

            public string Currency { get; }

            public double? ValueLow { get; }

            public double? ValueMid { get; }

            public double? ValueHigh { get; }

            public double? AskingPrice { get; }

            public double? Wage { get; }

            public int? ContractMonthsRemaining { get; }

            public string LeagueLevel { get; }

            public int Confidence { get; }

            public double? LeagueStrengthMultiplier
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(LeagueLevel))
                    {
                        return null;
                    }

                    return string.Equals(LeagueLevel, "strong", StringComparison.OrdinalIgnoreCase) ? 1.05 :
                        string.Equals(LeagueLevel, "developing", StringComparison.OrdinalIgnoreCase) ? 0.95 :
                        1.0;
                }
            }
        }
    }
}
