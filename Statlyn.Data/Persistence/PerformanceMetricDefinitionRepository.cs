using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class PerformanceMetricDefinitionRepository : SqliteRepository
    {
        public PerformanceMetricDefinitionRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public int SeedGenericDefaults()
        {
            var count = 0;
            foreach (var definition in GenericPerformanceMetricSeed.Create())
            {
                Save(definition);
                count++;
            }

            return count;
        }

        public void Save(PerformanceMetricDefinition definition)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO PerformanceMetricDefinition (
                        MetricKey, DisplayName, Description, FieldKey, FieldName, ProviderType, IsGenericFootballMetric,
                        IsVerifiedFm26Metric, IsPer90Capable, DefaultUnit, HigherIsBetter, LowerIsBetter, RequiresMinutes,
                        MinimumMinutesRecommended, PositionGroups, RoleFamilies, SourceConfidenceRequired, CanScore, CanStore, Notes)
                      VALUES (
                        $metricKey, $displayName, $description, $fieldKey, $fieldName, $providerType, $isGenericFootballMetric,
                        $isVerifiedFm26Metric, $isPer90Capable, $defaultUnit, $higherIsBetter, $lowerIsBetter, $requiresMinutes,
                        $minimumMinutesRecommended, $positionGroups, $roleFamilies, $sourceConfidenceRequired, $canScore, $canStore, $notes)
                      ON CONFLICT(MetricKey) DO UPDATE SET
                        DisplayName = excluded.DisplayName,
                        Description = excluded.Description,
                        FieldKey = excluded.FieldKey,
                        FieldName = excluded.FieldName,
                        ProviderType = excluded.ProviderType,
                        IsGenericFootballMetric = excluded.IsGenericFootballMetric,
                        IsVerifiedFm26Metric = excluded.IsVerifiedFm26Metric,
                        IsPer90Capable = excluded.IsPer90Capable,
                        DefaultUnit = excluded.DefaultUnit,
                        HigherIsBetter = excluded.HigherIsBetter,
                        LowerIsBetter = excluded.LowerIsBetter,
                        RequiresMinutes = excluded.RequiresMinutes,
                        MinimumMinutesRecommended = excluded.MinimumMinutesRecommended,
                        PositionGroups = excluded.PositionGroups,
                        RoleFamilies = excluded.RoleFamilies,
                        SourceConfidenceRequired = excluded.SourceConfidenceRequired,
                        CanScore = excluded.CanScore,
                        CanStore = excluded.CanStore,
                        Notes = excluded.Notes;";
                Bind(command, definition);
                command.ExecuteNonQuery();
            }
        }

        public void SaveAlias(PerformanceMetricAlias alias)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO PerformanceMetricAlias (MetricKey, ProviderType, AliasName, IsVerifiedFm26Alias, Notes)
                      VALUES ($metricKey, $providerType, $aliasName, $isVerifiedFm26Alias, $notes);";
                Add(command, "$metricKey", alias.MetricKey);
                Add(command, "$providerType", alias.ProviderType.ToString());
                Add(command, "$aliasName", alias.AliasName);
                Add(command, "$isVerifiedFm26Alias", Bool(alias.IsVerifiedFm26Alias));
                Add(command, "$notes", alias.Notes);
                command.ExecuteNonQuery();
            }
        }

        public IReadOnlyList<PerformanceMetricDefinition> LoadAll()
        {
            var definitions = new List<PerformanceMetricDefinition>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT MetricKey, DisplayName, Description, FieldKey, FieldName, ProviderType, IsGenericFootballMetric,
                             IsVerifiedFm26Metric, IsPer90Capable, DefaultUnit, HigherIsBetter, LowerIsBetter, RequiresMinutes,
                             MinimumMinutesRecommended, PositionGroups, RoleFamilies, SourceConfidenceRequired, CanScore, CanStore, Notes
                      FROM PerformanceMetricDefinition
                      ORDER BY MetricKey;";
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

        public PerformanceMetricDefinition? FindByMetricKey(string metricKey)
        {
            return LoadAll().FirstOrDefault(definition => string.Equals(definition.MetricKey, metricKey, StringComparison.OrdinalIgnoreCase));
        }

        public PerformanceMetricDefinition? FindByField(PlayerFieldKey fieldKey, string fieldName)
        {
            return LoadAll().FirstOrDefault(definition => definition.FieldKey == fieldKey && string.Equals(definition.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        }

        public PerformanceMetricDefinition? FindByFieldInstanceKey(FieldInstanceKey key)
        {
            return FindByField(key.Key, key.FieldName);
        }

        private static void Bind(SqliteCommand command, PerformanceMetricDefinition definition)
        {
            Add(command, "$metricKey", definition.MetricKey);
            Add(command, "$displayName", definition.DisplayName);
            Add(command, "$description", definition.Description);
            Add(command, "$fieldKey", definition.FieldKey.ToString());
            Add(command, "$fieldName", definition.FieldName);
            Add(command, "$providerType", definition.ProviderType.ToString());
            Add(command, "$isGenericFootballMetric", Bool(definition.IsGenericFootballMetric));
            Add(command, "$isVerifiedFm26Metric", Bool(definition.IsVerifiedFm26Metric));
            Add(command, "$isPer90Capable", Bool(definition.IsPer90Capable));
            Add(command, "$defaultUnit", definition.DefaultUnit);
            Add(command, "$higherIsBetter", Bool(definition.HigherIsBetter));
            Add(command, "$lowerIsBetter", Bool(definition.LowerIsBetter));
            Add(command, "$requiresMinutes", Bool(definition.RequiresMinutes));
            Add(command, "$minimumMinutesRecommended", definition.MinimumMinutesRecommended);
            Add(command, "$positionGroups", JoinValues(definition.PositionGroups));
            Add(command, "$roleFamilies", JoinValues(definition.RoleFamilies));
            Add(command, "$sourceConfidenceRequired", definition.SourceConfidenceRequired);
            Add(command, "$canScore", Bool(definition.CanScore));
            Add(command, "$canStore", Bool(definition.CanStore));
            Add(command, "$notes", definition.Notes);
        }

        private static PerformanceMetricDefinition ReadDefinition(SqliteDataReader reader)
        {
            var fieldKey = Enum.TryParse<PlayerFieldKey>(reader.GetString(3), out var parsedFieldKey) ? parsedFieldKey : PlayerFieldKey.Unknown;
            var providerType = Enum.TryParse<ProviderType>(reader.GetString(5), out var parsedProvider) ? parsedProvider : ProviderType.FutureExternalProvider;
            return new PerformanceMetricDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                fieldKey,
                reader.GetString(4),
                providerType,
                ReadBool(reader, 6),
                ReadBool(reader, 7),
                ReadBool(reader, 8),
                reader.GetString(9),
                ReadBool(reader, 10),
                ReadBool(reader, 11),
                ReadBool(reader, 12),
                reader.GetInt32(13),
                SplitValues(reader.GetString(14)),
                SplitValues(reader.GetString(15)),
                reader.GetInt32(16),
                ReadBool(reader, 17),
                ReadBool(reader, 18),
                reader.GetString(19));
        }
    }
}
