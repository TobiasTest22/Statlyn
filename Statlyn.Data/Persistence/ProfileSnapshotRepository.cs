using System;

namespace Statlyn.Data.Persistence
{
    public sealed class ProfileSnapshotRepository : SqliteRepository
    {
        public ProfileSnapshotRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public long Save(long playerId, string sourceName, bool isFixtureMode, bool isLiveFm26Data, int confidence, int dataCompleteness)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            {
                return Save(playerId, sourceName, isFixtureMode, isLiveFm26Data, confidence, dataCompleteness, connection, null);
            }
        }

        public long Save(long playerId, string sourceName, bool isFixtureMode, bool isLiveFm26Data, int confidence, int dataCompleteness, Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction? transaction)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText =
                    @"INSERT INTO PlayerProfileSnapshot (PlayerId, SourceName, IsFixtureMode, IsLiveFm26Data, Confidence, DataCompleteness, CreatedAtUtc)
                      VALUES ($playerId, $sourceName, $isFixtureMode, $isLiveFm26Data, $confidence, $dataCompleteness, $createdAtUtc);";
                Add(command, "$playerId", playerId);
                Add(command, "$sourceName", sourceName);
                Add(command, "$isFixtureMode", Bool(isFixtureMode));
                Add(command, "$isLiveFm26Data", Bool(isLiveFm26Data));
                Add(command, "$confidence", confidence);
                Add(command, "$dataCompleteness", dataCompleteness);
                Add(command, "$createdAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
                return LastInsertRowId(connection);
            }
        }

        public void DeleteForPlayer(long playerId, Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction? transaction)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText = "DELETE FROM PlayerProfileSnapshot WHERE PlayerId = $playerId;";
                Add(command, "$playerId", playerId);
                command.ExecuteNonQuery();
            }
        }
    }
}
