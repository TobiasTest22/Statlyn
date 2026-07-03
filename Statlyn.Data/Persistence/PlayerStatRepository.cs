using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class PlayerStatRepository : SqliteRepository
    {
        public PlayerStatRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public int SaveFromFields(long playerId, object player)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist player stats");
            if (!(player is MaskedPlayer masked))
            {
                throw new System.InvalidOperationException("Player stats can only be persisted from masked players.");
            }

            var stored = 0;
            using (var connection = ConnectionFactory.OpenConnection())
            {
                foreach (var field in masked.Fields.Values)
                {
                    if (field.Key != PlayerFieldKey.PlayerStat || !field.IsKnown || field.IsBlocked || !field.CanStore || !field.CanScore || !field.NumericValue.HasValue)
                    {
                        continue;
                    }

                    Save(connection, playerId, field);
                    stored++;
                }
            }

            return stored;
        }

        public IReadOnlyList<PlayerStatRecord> LoadForPlayer(long playerId)
        {
            var stats = new List<PlayerStatRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT FieldInstanceKey, StatName, StatValue, Minutes, SourceName, Confidence
                      FROM PlayerStat
                      WHERE PlayerId = $playerId
                      ORDER BY Id;";
                Add(command, "$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats.Add(new PlayerStatRecord(playerId, reader.GetString(0), reader.GetString(1), reader.GetDouble(2), reader.GetInt32(3), reader.GetString(4), reader.GetInt32(5)));
                    }
                }
            }

            return stats;
        }

        private static void Save(SqliteConnection connection, long playerId, VisiblePlayerField field)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO PlayerStat (PlayerId, FieldInstanceKey, StatName, StatValue, Minutes, SourceName, Confidence)
                      VALUES ($playerId, $fieldInstanceKey, $statName, $statValue, $minutes, $sourceName, $confidence);";
                Add(command, "$playerId", playerId);
                Add(command, "$fieldInstanceKey", field.InstanceKey.StableId);
                Add(command, "$statName", field.FieldName);
                Add(command, "$statValue", field.NumericValue!.Value);
                Add(command, "$minutes", 0);
                Add(command, "$sourceName", field.SourceProvider);
                Add(command, "$confidence", field.Confidence);
                command.ExecuteNonQuery();
            }
        }
    }
}
