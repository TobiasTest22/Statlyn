using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class PhysicalMetricRepository : SqliteRepository
    {
        public PhysicalMetricRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public int SaveFromFields(long playerId, object player)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist physical metrics");
            if (!(player is MaskedPlayer masked))
            {
                throw new System.InvalidOperationException("Physical metrics can only be persisted from masked players.");
            }

            var stored = 0;
            using (var connection = ConnectionFactory.OpenConnection())
            {
                foreach (var field in masked.Fields.Values)
                {
                    if (field.Key != PlayerFieldKey.PhysicalData || !field.IsKnown || field.IsBlocked || !field.CanStore || !field.CanScore || !field.NumericValue.HasValue)
                    {
                        continue;
                    }

                    Save(connection, playerId, field);
                    stored++;
                }
            }

            return stored;
        }

        public IReadOnlyList<PhysicalMetricRecord> LoadForPlayer(long playerId)
        {
            var metrics = new List<PhysicalMetricRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT FieldInstanceKey, MetricName, MetricValue, Unit, SourceName, Confidence
                      FROM PhysicalMetric
                      WHERE PlayerId = $playerId
                      ORDER BY Id;";
                Add(command, "$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        metrics.Add(new PhysicalMetricRecord(playerId, reader.GetString(0), reader.GetString(1), reader.GetDouble(2), ReadString(reader, 3), reader.GetString(4), reader.GetInt32(5)));
                    }
                }
            }

            return metrics;
        }

        private static void Save(SqliteConnection connection, long playerId, VisiblePlayerField field)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO PhysicalMetric (PlayerId, FieldInstanceKey, MetricName, MetricValue, Unit, SourceName, Confidence)
                      VALUES ($playerId, $fieldInstanceKey, $metricName, $metricValue, $unit, $sourceName, $confidence);";
                Add(command, "$playerId", playerId);
                Add(command, "$fieldInstanceKey", field.InstanceKey.StableId);
                Add(command, "$metricName", field.FieldName);
                Add(command, "$metricValue", field.NumericValue!.Value);
                Add(command, "$unit", string.Empty);
                Add(command, "$sourceName", field.SourceProvider);
                Add(command, "$confidence", field.Confidence);
                command.ExecuteNonQuery();
            }
        }
    }
}
