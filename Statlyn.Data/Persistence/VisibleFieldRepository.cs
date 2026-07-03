using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class VisibleFieldRepository : SqliteRepository
    {
        public VisibleFieldRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public int SaveFields(long playerId, object player)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist visible fields");
            if (!(player is MaskedPlayer masked))
            {
                throw new InvalidOperationException("Visible fields can only be persisted from masked players.");
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                return SaveFields(playerId, masked, connection, null);
            }
        }

        public int SaveFields(long playerId, object player, SqliteConnection connection, SqliteTransaction? transaction)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist visible fields");
            if (!(player is MaskedPlayer masked))
            {
                throw new InvalidOperationException("Visible fields can only be persisted from masked players.");
            }

            var stored = 0;
            foreach (var field in masked.Fields.Values)
            {
                if (!field.IsKnown || field.IsBlocked || !field.CanStore)
                {
                    continue;
                }

                SaveField(connection, transaction, playerId, field);
                stored++;
            }

            return stored;
        }

        public void DeleteForPlayer(long playerId, SqliteConnection connection, SqliteTransaction? transaction)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText = "DELETE FROM VisibleField WHERE PlayerId = $playerId;";
                Add(command, "$playerId", playerId);
                command.ExecuteNonQuery();
            }
        }

        public IReadOnlyList<VisiblePlayerField> LoadFields(long playerId)
        {
            var fields = new List<VisiblePlayerField>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT FieldKey, FieldName, SourceFieldName, DisplayValue, NumericValue, CanDisplay, CanScore, CanStore, Confidence, SourceName
                      FROM VisibleField
                      WHERE PlayerId = $playerId
                      ORDER BY Id;";
                Add(command, "$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = Enum.TryParse<PlayerFieldKey>(reader.GetString(0), out var parsed) ? parsed : PlayerFieldKey.Unknown;
                        var numeric = ReadNullableDouble(reader, 4);
                        fields.Add(new VisiblePlayerField(
                            key,
                            reader.GetString(1),
                            reader.GetString(2),
                            ReadString(reader, 3),
                            numeric,
                            numeric.HasValue ? FieldValueKind.Number : FieldValueKind.Text,
                            isKnown: true,
                            isBlocked: false,
                            canDisplay: ReadBool(reader, 5),
                            canScore: ReadBool(reader, 6),
                            canStore: ReadBool(reader, 7),
                            confidence: reader.GetInt32(8),
                            sourceProvider: reader.GetString(9),
                            missingReason: string.Empty));
                    }
                }
            }

            return fields;
        }

        private static void SaveField(SqliteConnection connection, SqliteTransaction? transaction, long playerId, VisiblePlayerField field)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText =
                    @"INSERT INTO VisibleField (
                        PlayerId, FieldInstanceKey, FieldKey, FieldName, SourceFieldName, DisplayValue, NumericValue,
                        CanDisplay, CanScore, CanStore, Confidence, SourceName, LastUpdatedUtc)
                      VALUES (
                        $playerId, $fieldInstanceKey, $fieldKey, $fieldName, $sourceFieldName, $displayValue, $numericValue,
                        $canDisplay, $canScore, $canStore, $confidence, $sourceName, $lastUpdatedUtc);";
                Add(command, "$playerId", playerId);
                Add(command, "$fieldInstanceKey", field.InstanceKey.StableId);
                Add(command, "$fieldKey", field.Key.ToString());
                Add(command, "$fieldName", field.FieldName);
                Add(command, "$sourceFieldName", field.SourceFieldName);
                Add(command, "$displayValue", field.DisplayValue);
                Add(command, "$numericValue", field.NumericValue);
                Add(command, "$canDisplay", Bool(field.CanDisplay));
                Add(command, "$canScore", Bool(field.CanScore));
                Add(command, "$canStore", Bool(field.CanStore));
                Add(command, "$confidence", field.Confidence);
                Add(command, "$sourceName", field.SourceProvider);
                Add(command, "$lastUpdatedUtc", DateTimeOffset.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
            }
        }
    }
}
