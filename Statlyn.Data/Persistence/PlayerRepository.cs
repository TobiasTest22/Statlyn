using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Core;
using Statlyn.DataProviders;

namespace Statlyn.Data.Persistence
{
    public sealed class PlayerRepository : SqliteRepository
    {
        public PlayerRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public long Save(object player, SourceMetadata metadata, DataCompletenessReport completeness)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist player");
            if (!(player is MaskedPlayer masked))
            {
                throw new InvalidOperationException("Only masked players can be persisted.");
            }

            return Save(masked, metadata, completeness);
        }

        public long Save(MaskedPlayer player, SourceMetadata metadata, DataCompletenessReport completeness)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                var existingId = FindPlayerId(connection, player.StatlynPlayerId);
                if (existingId.HasValue)
                {
                    Update(connection, existingId.Value, player, metadata, completeness);
                    SaveScoutKnowledge(connection, existingId.Value, player);
                    return existingId.Value;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO Player (
                            StatlynPlayerId, DisplayName, Age, Nationality, Club, PositionGroup, PrimaryPosition,
                            PreferredFoot, Height, ContractEnd, WageDisplay, MarketValueDisplay, SourceName,
                            SourceConfidence, DataCompleteness, LastUpdatedUtc)
                          VALUES (
                            $statlynPlayerId, $displayName, $age, $nationality, $club, $positionGroup, $primaryPosition,
                            $preferredFoot, $height, $contractEnd, $wageDisplay, $marketValueDisplay, $sourceName,
                            $sourceConfidence, $dataCompleteness, $lastUpdatedUtc);";
                    Bind(command, player, metadata, completeness);
                    command.ExecuteNonQuery();
                }

                var id = LastInsertRowId(connection);
                SaveScoutKnowledge(connection, id, player);
                return id;
            }
        }

        public StoredPlayerRecord? LoadByStatlynPlayerId(string statlynPlayerId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, StatlynPlayerId, DisplayName, SourceName, SourceConfidence, DataCompleteness, LastUpdatedUtc
                      FROM Player
                      WHERE StatlynPlayerId = $statlynPlayerId
                      LIMIT 1;";
                Add(command, "$statlynPlayerId", statlynPlayerId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRecord(reader) : null;
                }
            }
        }

        public IReadOnlyList<StoredPlayerRecord> LoadBySource(string sourceName)
        {
            var players = new List<StoredPlayerRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, StatlynPlayerId, DisplayName, SourceName, SourceConfidence, DataCompleteness, LastUpdatedUtc
                      FROM Player
                      WHERE SourceName = $sourceName
                      ORDER BY DisplayName;";
                Add(command, "$sourceName", sourceName);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(ReadRecord(reader));
                    }
                }
            }

            return players;
        }

        public int LoadScoutKnowledge(long playerId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT KnowledgePercentage FROM ScoutKnowledge WHERE PlayerId = $playerId ORDER BY LastUpdatedUtc DESC LIMIT 1;";
                Add(command, "$playerId", playerId);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private static void Update(SqliteConnection connection, long playerId, MaskedPlayer player, SourceMetadata metadata, DataCompletenessReport completeness)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"UPDATE Player SET
                        DisplayName = $displayName,
                        Age = $age,
                        Nationality = $nationality,
                        Club = $club,
                        PositionGroup = $positionGroup,
                        PrimaryPosition = $primaryPosition,
                        PreferredFoot = $preferredFoot,
                        Height = $height,
                        ContractEnd = $contractEnd,
                        WageDisplay = $wageDisplay,
                        MarketValueDisplay = $marketValueDisplay,
                        SourceName = $sourceName,
                        SourceConfidence = $sourceConfidence,
                        DataCompleteness = $dataCompleteness,
                        LastUpdatedUtc = $lastUpdatedUtc
                      WHERE Id = $id;";
                Bind(command, player, metadata, completeness);
                Add(command, "$id", playerId);
                command.ExecuteNonQuery();
            }
        }

        private static void Bind(SqliteCommand command, MaskedPlayer player, SourceMetadata metadata, DataCompletenessReport completeness)
        {
            Add(command, "$statlynPlayerId", player.StatlynPlayerId);
            Add(command, "$displayName", player.DisplayName);
            Add(command, "$age", FindNumeric(player, PlayerFieldKey.Age));
            Add(command, "$nationality", FindDisplay(player, PlayerFieldKey.Nationality));
            Add(command, "$club", FindDisplay(player, PlayerFieldKey.Club));
            Add(command, "$positionGroup", FindDisplay(player, PlayerFieldKey.PrimaryPosition));
            Add(command, "$primaryPosition", FindDisplay(player, PlayerFieldKey.PrimaryPosition));
            Add(command, "$preferredFoot", FindDisplay(player, PlayerFieldKey.PreferredFoot));
            Add(command, "$height", FindDisplay(player, PlayerFieldKey.Height));
            Add(command, "$contractEnd", FindDisplay(player, PlayerFieldKey.ContractEnd));
            Add(command, "$wageDisplay", FindDisplay(player, PlayerFieldKey.Wage));
            Add(command, "$marketValueDisplay", FindDisplay(player, PlayerFieldKey.MarketValue));
            Add(command, "$sourceName", metadata.SourceName);
            Add(command, "$sourceConfidence", metadata.SourceConfidence);
            Add(command, "$dataCompleteness", completeness == null ? 0 : completeness.CompletenessPercentage);
            Add(command, "$lastUpdatedUtc", DateTimeOffset.UtcNow.ToString("O"));
        }

        private static long? FindPlayerId(SqliteConnection connection, string statlynPlayerId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Player WHERE StatlynPlayerId = $statlynPlayerId LIMIT 1;";
                Add(command, "$statlynPlayerId", statlynPlayerId);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static string? FindDisplay(MaskedPlayer player, PlayerFieldKey key)
        {
            var field = player.Fields.Values.FirstOrDefault(value => value.Key == key && value.IsKnown && value.CanDisplay);
            return field == null ? null : field.DisplayValue;
        }

        private static int? FindNumeric(MaskedPlayer player, PlayerFieldKey key)
        {
            var field = player.Fields.Values.FirstOrDefault(value => value.Key == key && value.IsKnown && value.NumericValue.HasValue);
            return field == null ? (int?)null : Convert.ToInt32(Math.Round(field.NumericValue!.Value), CultureInfo.InvariantCulture);
        }

        private static void SaveScoutKnowledge(SqliteConnection connection, long playerId, MaskedPlayer player)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO ScoutKnowledge (PlayerId, KnowledgePercentage, HasScoutReport, LastUpdatedUtc)
                      VALUES ($playerId, $knowledgePercentage, $hasScoutReport, $lastUpdatedUtc);";
                Add(command, "$playerId", playerId);
                Add(command, "$knowledgePercentage", player.ScoutKnowledgePercentage);
                Add(command, "$hasScoutReport", player.ScoutKnowledgePercentage > 0 ? 1 : 0);
                Add(command, "$lastUpdatedUtc", DateTimeOffset.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
            }
        }

        private static StoredPlayerRecord ReadRecord(SqliteDataReader reader)
        {
            var updated = DateTimeOffset.TryParse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;
            return new StoredPlayerRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetInt32(5), updated);
        }
    }
}
