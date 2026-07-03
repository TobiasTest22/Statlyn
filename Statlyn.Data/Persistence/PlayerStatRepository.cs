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

            using (var connection = ConnectionFactory.OpenConnection())
            {
                return SaveFromFields(playerId, masked, connection, null);
            }
        }

        public int SaveFromFields(long playerId, object player, SqliteConnection connection, SqliteTransaction? transaction)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist player stats");
            if (!(player is MaskedPlayer masked))
            {
                throw new System.InvalidOperationException("Player stats can only be persisted from masked players.");
            }

            var stored = 0;
            var sample = FindSampleMinutes(masked);
            foreach (var field in masked.Fields.Values)
            {
                if (field.Key != PlayerFieldKey.PlayerStat || !field.IsKnown || field.IsBlocked || !field.CanStore || !field.CanScore || !field.NumericValue.HasValue)
                {
                    continue;
                }

                Save(connection, transaction, playerId, field, sample.Minutes, sample.IsMissing, sample.Source);
                stored++;
            }

            return stored;
        }

        public void DeleteForPlayer(long playerId, SqliteConnection connection, SqliteTransaction? transaction)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText = "DELETE FROM PlayerStat WHERE PlayerId = $playerId;";
                Add(command, "$playerId", playerId);
                command.ExecuteNonQuery();
            }
        }

        public IReadOnlyList<PlayerStatRecord> LoadForPlayer(long playerId)
        {
            var stats = new List<PlayerStatRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT FieldInstanceKey, StatName, StatValue, Minutes, SampleMinutesMissing, MinutesSource, SourceName, Confidence
                      FROM PlayerStat
                      WHERE PlayerId = $playerId
                      ORDER BY Id;";
                Add(command, "$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats.Add(new PlayerStatRecord(playerId, reader.GetString(0), reader.GetString(1), reader.GetDouble(2), reader.GetInt32(3), ReadBool(reader, 4), reader.GetString(5), reader.GetString(6), reader.GetInt32(7)));
                    }
                }
            }

            return stats;
        }

        private static void Save(SqliteConnection connection, SqliteTransaction? transaction, long playerId, VisiblePlayerField field, int minutes, bool sampleMinutesMissing, string minutesSource)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText =
                    @"INSERT INTO PlayerStat (PlayerId, FieldInstanceKey, StatName, StatValue, Minutes, SampleMinutesMissing, MinutesSource, SourceName, Confidence)
                      VALUES ($playerId, $fieldInstanceKey, $statName, $statValue, $minutes, $sampleMinutesMissing, $minutesSource, $sourceName, $confidence);";
                Add(command, "$playerId", playerId);
                Add(command, "$fieldInstanceKey", field.InstanceKey.StableId);
                Add(command, "$statName", field.FieldName);
                Add(command, "$statValue", field.NumericValue!.Value);
                Add(command, "$minutes", minutes);
                Add(command, "$sampleMinutesMissing", Bool(sampleMinutesMissing));
                Add(command, "$minutesSource", minutesSource);
                Add(command, "$sourceName", field.SourceProvider);
                Add(command, "$confidence", field.Confidence);
                command.ExecuteNonQuery();
            }
        }

        private static SampleMinutes FindSampleMinutes(MaskedPlayer player)
        {
            foreach (var field in player.Fields.Values)
            {
                if (field.Key == PlayerFieldKey.PlayerStat && field.IsKnown && field.NumericValue.HasValue && string.Equals(field.FieldName, "Minutes", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new SampleMinutes((int)System.Math.Round(field.NumericValue.Value), false, field.InstanceKey.StableId);
                }
            }

            return new SampleMinutes(0, true, "missing");
        }

        private sealed class SampleMinutes
        {
            public SampleMinutes(int minutes, bool isMissing, string source)
            {
                Minutes = minutes < 0 ? 0 : minutes;
                IsMissing = isMissing;
                Source = source ?? string.Empty;
            }

            public int Minutes { get; }

            public bool IsMissing { get; }

            public string Source { get; }
        }
    }
}
