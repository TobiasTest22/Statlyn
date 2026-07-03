using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public sealed class BlockedFieldAuditRepository : SqliteRepository
    {
        public BlockedFieldAuditRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public int SaveBlockedFields(string sourceEntityId, object player)
        {
            SafePersistenceGuard.RejectRaw(player, "Persist blocked field audit");
            if (!(player is MaskedPlayer masked))
            {
                throw new System.InvalidOperationException("Blocked field audit can only be persisted from masked players.");
            }

            var stored = 0;
            using (var connection = ConnectionFactory.OpenConnection())
            {
                foreach (var blocked in masked.BlockedFields)
                {
                    Save(connection, sourceEntityId, blocked);
                    stored++;
                }
            }

            return stored;
        }

        public IReadOnlyList<BlockedFieldNotice> LoadForEntity(string sourceEntityId)
        {
            var blocked = new List<BlockedFieldNotice>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT SourceName, FieldKey, FieldName, Reason
                      FROM BlockedFieldAudit
                      WHERE SourceEntityId = $sourceEntityId
                      ORDER BY Id;";
                Add(command, "$sourceEntityId", sourceEntityId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = System.Enum.TryParse<PlayerFieldKey>(reader.GetString(1), out var parsed) ? parsed : PlayerFieldKey.Unknown;
                        blocked.Add(new BlockedFieldNotice(key, reader.GetString(2), reader.GetString(3), reader.GetString(0)));
                    }
                }
            }

            return blocked;
        }

        private static void Save(SqliteConnection connection, string sourceEntityId, BlockedFieldNotice blocked)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO BlockedFieldAudit (SourceName, SourceEntityId, FieldKey, FieldName, Reason, CreatedAtUtc)
                      VALUES ($sourceName, $sourceEntityId, $fieldKey, $fieldName, $reason, $createdAtUtc);";
                Add(command, "$sourceName", blocked.SourceProvider);
                Add(command, "$sourceEntityId", sourceEntityId);
                Add(command, "$fieldKey", blocked.Key.ToString());
                Add(command, "$fieldName", blocked.FieldName);
                Add(command, "$reason", blocked.Reason);
                Add(command, "$createdAtUtc", System.DateTimeOffset.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
            }
        }
    }
}
