using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Shortlists
{
    public sealed class ShortlistRepository : SqliteRepository
    {
        private static readonly string[] HiddenTerms =
        {
            "CurrentAbility",
            "PotentialAbility",
            "Professionalism",
            "HiddenPersonality",
            "InjuryProneness",
            "Consistency",
            "ImportantMatches",
            "Pressure",
            "Ambition",
            "Loyalty",
            "Adaptability",
            "Temperament",
            "RawValue"
        };

        public ShortlistRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public ShortlistRecord CreateShortlist(string name, string description)
        {
            var now = DateTimeOffset.UtcNow;
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO Shortlist (Name, Description, CreatedAtUtc, UpdatedAtUtc, IsArchived)
                      VALUES ($name, $description, $createdAtUtc, $updatedAtUtc, 0);";
                Add(command, "$name", SafeName(name));
                Add(command, "$description", SanitizeWorkflowText(description, string.Empty));
                Add(command, "$createdAtUtc", now.ToString("O"));
                Add(command, "$updatedAtUtc", now.ToString("O"));
                command.ExecuteNonQuery();
                return LoadShortlist(LastInsertRowId(connection))!;
            }
        }

        public ShortlistRecord UpdateShortlist(long id, string name, string description, bool isArchived)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"UPDATE Shortlist SET
                        Name = $name,
                        Description = $description,
                        UpdatedAtUtc = $updatedAtUtc,
                        IsArchived = $isArchived
                      WHERE Id = $id;";
                Add(command, "$name", SafeName(name));
                Add(command, "$description", SanitizeWorkflowText(description, string.Empty));
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$isArchived", Bool(isArchived));
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }

            var shortlist = LoadShortlist(id);
            if (shortlist == null)
            {
                throw new InvalidOperationException("Shortlist was not found.");
            }

            return shortlist;
        }

        public void DeleteOrArchiveShortlist(long id)
        {
            UpdateShortlistArchive(id, true);
        }

        public IReadOnlyList<ShortlistRecord> LoadShortlists(bool includeArchived)
        {
            var shortlists = new List<ShortlistRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT S.Id, S.Name, S.Description, S.CreatedAtUtc, S.UpdatedAtUtc, S.IsArchived,
                             (SELECT COUNT(*) FROM ShortlistPlayer SP WHERE SP.ShortlistId = S.Id) AS PlayerCount
                      FROM Shortlist S
                      WHERE $includeArchived = 1 OR S.IsArchived = 0
                      ORDER BY S.UpdatedAtUtc DESC, S.Name ASC;";
                Add(command, "$includeArchived", Bool(includeArchived));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        shortlists.Add(ReadShortlist(reader));
                    }
                }
            }

            return shortlists;
        }

        public ShortlistRecord? LoadShortlist(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT S.Id, S.Name, S.Description, S.CreatedAtUtc, S.UpdatedAtUtc, S.IsArchived,
                             (SELECT COUNT(*) FROM ShortlistPlayer SP WHERE SP.ShortlistId = S.Id) AS PlayerCount
                      FROM Shortlist S
                      WHERE S.Id = $id
                      LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadShortlist(reader) : null;
                }
            }
        }

        public ShortlistRecord? LoadShortlistByName(string name, bool includeArchived)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT S.Id, S.Name, S.Description, S.CreatedAtUtc, S.UpdatedAtUtc, S.IsArchived,
                             (SELECT COUNT(*) FROM ShortlistPlayer SP WHERE SP.ShortlistId = S.Id) AS PlayerCount
                      FROM Shortlist S
                      WHERE lower(S.Name) = lower($name)
                        AND ($includeArchived = 1 OR S.IsArchived = 0)
                      ORDER BY S.UpdatedAtUtc DESC, S.Id DESC
                      LIMIT 1;";
                Add(command, "$name", SafeName(name));
                Add(command, "$includeArchived", Bool(includeArchived));
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadShortlist(reader) : null;
                }
            }
        }

        public ShortlistPlayerRecord AddPlayer(
            long shortlistId,
            string statlynPlayerId,
            ShortlistStatus status,
            ShortlistPriority priority,
            ShortlistFollowUpAction followUpAction,
            string roleName,
            string recommendation,
            string addedReason)
        {
            if (string.IsNullOrWhiteSpace(statlynPlayerId))
            {
                throw new InvalidOperationException("A persisted StatlynPlayerId is required.");
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                var playerId = FindPlayerId(connection, statlynPlayerId);
                if (!playerId.HasValue)
                {
                    throw new InvalidOperationException("Player was not found in persisted safe data.");
                }

                var existing = FindShortlistPlayerId(connection, shortlistId, statlynPlayerId);
                if (existing.HasValue)
                {
                    UpdatePlayerInternal(
                        connection,
                        existing.Value,
                        status,
                        priority,
                        followUpAction,
                        roleName,
                        recommendation,
                        null,
                        addedReason);
                    TouchShortlist(connection, shortlistId);
                    return LoadPlayer(existing.Value)!;
                }

                using (var command = connection.CreateCommand())
                {
                    var now = DateTimeOffset.UtcNow.ToString("O");
                    command.CommandText =
                        @"INSERT INTO ShortlistPlayer (
                            ShortlistId, PlayerId, StatlynPlayerId, Status, Priority, FollowUpAction,
                            RoleName, Recommendation, AddedReason, UserNote, AddedAtUtc, UpdatedAtUtc)
                          VALUES (
                            $shortlistId, $playerId, $statlynPlayerId, $status, $priority, $followUpAction,
                            $roleName, $recommendation, $addedReason, '', $addedAtUtc, $updatedAtUtc);";
                    Add(command, "$shortlistId", shortlistId);
                    Add(command, "$playerId", playerId.Value);
                    Add(command, "$statlynPlayerId", statlynPlayerId.Trim());
                    Add(command, "$status", status.ToString());
                    Add(command, "$priority", priority.ToString());
                    Add(command, "$followUpAction", followUpAction.ToString());
                    Add(command, "$roleName", RoleNameSanitizer.SanitizeForStorage(roleName));
                    Add(command, "$recommendation", SanitizeWorkflowText(recommendation, "Not scored"));
                    Add(command, "$addedReason", SanitizeWorkflowText(addedReason, string.Empty));
                    Add(command, "$addedAtUtc", now);
                    Add(command, "$updatedAtUtc", now);
                    command.ExecuteNonQuery();
                }

                var id = LastInsertRowId(connection);
                TouchShortlist(connection, shortlistId);
                return LoadPlayer(id)!;
            }
        }

        public ShortlistPlayerRecord AddPlayer(
            long shortlistId,
            object player,
            ShortlistStatus status,
            ShortlistPriority priority,
            ShortlistFollowUpAction followUpAction,
            string roleName,
            string recommendation,
            string addedReason)
        {
            SafePersistenceGuard.RejectRaw(player, "Add shortlist player");
            if (!(player is string statlynPlayerId))
            {
                throw new InvalidOperationException("Shortlist players must be added by persisted StatlynPlayerId.");
            }

            return AddPlayer(shortlistId, statlynPlayerId, status, priority, followUpAction, roleName, recommendation, addedReason);
        }

        public ShortlistPlayerRecord UpdatePlayer(
            long shortlistPlayerId,
            ShortlistStatus status,
            ShortlistPriority priority,
            ShortlistFollowUpAction followUpAction,
            string roleName,
            string recommendation,
            string userNote)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            {
                UpdatePlayerInternal(connection, shortlistPlayerId, status, priority, followUpAction, roleName, recommendation, userNote, null);
                var player = LoadPlayer(shortlistPlayerId);
                if (player == null)
                {
                    throw new InvalidOperationException("Shortlist player was not found.");
                }

                TouchShortlist(connection, player.ShortlistId);
                return player;
            }
        }

        public void RemovePlayer(long shortlistPlayerId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            {
                var player = LoadPlayer(shortlistPlayerId);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM ShortlistPlayer WHERE Id = $id;";
                    Add(command, "$id", shortlistPlayerId);
                    command.ExecuteNonQuery();
                }

                if (player != null)
                {
                    TouchShortlist(connection, player.ShortlistId);
                }
            }
        }

        public IReadOnlyList<ShortlistPlayerRecord> LoadPlayers(long shortlistId)
        {
            var players = new List<ShortlistPlayerRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, ShortlistId, PlayerId, StatlynPlayerId, Status, Priority, FollowUpAction,
                             RoleName, Recommendation, AddedReason, UserNote, AddedAtUtc, UpdatedAtUtc
                      FROM ShortlistPlayer
                      WHERE ShortlistId = $shortlistId
                      ORDER BY UpdatedAtUtc DESC, Id DESC;";
                Add(command, "$shortlistId", shortlistId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(ReadShortlistPlayer(reader));
                    }
                }
            }

            return players;
        }

        public ShortlistPlayerRecord? LoadPlayer(long shortlistPlayerId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, ShortlistId, PlayerId, StatlynPlayerId, Status, Priority, FollowUpAction,
                             RoleName, Recommendation, AddedReason, UserNote, AddedAtUtc, UpdatedAtUtc
                      FROM ShortlistPlayer
                      WHERE Id = $id
                      LIMIT 1;";
                Add(command, "$id", shortlistPlayerId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadShortlistPlayer(reader) : null;
                }
            }
        }

        public bool IsPlayerInShortlist(long shortlistId, string statlynPlayerId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            {
                return FindShortlistPlayerId(connection, shortlistId, statlynPlayerId).HasValue;
            }
        }

        public IReadOnlyList<ShortlistPlayerRecord> LoadShortlistMembershipsForPlayer(string statlynPlayerId)
        {
            var players = new List<ShortlistPlayerRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT SP.Id, SP.ShortlistId, SP.PlayerId, SP.StatlynPlayerId, SP.Status, SP.Priority, SP.FollowUpAction,
                             SP.RoleName, SP.Recommendation, SP.AddedReason, SP.UserNote, SP.AddedAtUtc, SP.UpdatedAtUtc
                      FROM ShortlistPlayer SP
                      INNER JOIN Shortlist S ON S.Id = SP.ShortlistId
                      WHERE SP.StatlynPlayerId = $statlynPlayerId
                        AND S.IsArchived = 0
                      ORDER BY SP.UpdatedAtUtc DESC, SP.Id DESC;";
                Add(command, "$statlynPlayerId", statlynPlayerId ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(ReadShortlistPlayer(reader));
                    }
                }
            }

            return players;
        }

        private static void UpdatePlayerInternal(
            SqliteConnection connection,
            long shortlistPlayerId,
            ShortlistStatus status,
            ShortlistPriority priority,
            ShortlistFollowUpAction followUpAction,
            string roleName,
            string recommendation,
            string? userNote,
            string? addedReason)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"UPDATE ShortlistPlayer SET
                        Status = $status,
                        Priority = $priority,
                        FollowUpAction = $followUpAction,
                        RoleName = $roleName,
                        Recommendation = $recommendation,
                        UserNote = CASE WHEN $userNote IS NULL THEN UserNote ELSE $userNote END,
                        AddedReason = CASE WHEN $addedReason IS NULL THEN AddedReason ELSE $addedReason END,
                        UpdatedAtUtc = $updatedAtUtc
                      WHERE Id = $id;";
                Add(command, "$status", status.ToString());
                Add(command, "$priority", priority.ToString());
                Add(command, "$followUpAction", followUpAction.ToString());
                Add(command, "$roleName", RoleNameSanitizer.SanitizeForStorage(roleName));
                Add(command, "$recommendation", SanitizeWorkflowText(recommendation, "Not scored"));
                Add(command, "$userNote", userNote == null ? null : SanitizeWorkflowText(userNote, string.Empty));
                Add(command, "$addedReason", addedReason == null ? null : SanitizeWorkflowText(addedReason, string.Empty));
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", shortlistPlayerId);
                command.ExecuteNonQuery();
            }
        }

        private void UpdateShortlistArchive(long id, bool isArchived)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Shortlist SET IsArchived = $isArchived, UpdatedAtUtc = $updatedAtUtc WHERE Id = $id;";
                Add(command, "$isArchived", Bool(isArchived));
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }
        }

        private static void TouchShortlist(SqliteConnection connection, long shortlistId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Shortlist SET UpdatedAtUtc = $updatedAtUtc WHERE Id = $id;";
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", shortlistId);
                command.ExecuteNonQuery();
            }
        }

        private static long? FindPlayerId(SqliteConnection connection, string statlynPlayerId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Player WHERE StatlynPlayerId = $statlynPlayerId LIMIT 1;";
                Add(command, "$statlynPlayerId", statlynPlayerId.Trim());
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static long? FindShortlistPlayerId(SqliteConnection connection, long shortlistId, string statlynPlayerId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM ShortlistPlayer WHERE ShortlistId = $shortlistId AND StatlynPlayerId = $statlynPlayerId LIMIT 1;";
                Add(command, "$shortlistId", shortlistId);
                Add(command, "$statlynPlayerId", statlynPlayerId ?? string.Empty);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static ShortlistRecord ReadShortlist(SqliteDataReader reader)
        {
            return new ShortlistRecord(
                reader.GetInt64(0),
                ReadString(reader, 1),
                ReadString(reader, 2),
                ParseDate(ReadString(reader, 3)),
                ParseDate(ReadString(reader, 4)),
                ReadBool(reader, 5),
                reader.GetInt32(6));
        }

        private static ShortlistPlayerRecord ReadShortlistPlayer(SqliteDataReader reader)
        {
            return new ShortlistPlayerRecord(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                ReadString(reader, 3),
                ParseEnum(ReadString(reader, 4), ShortlistStatus.Watchlist),
                ParseEnum(ReadString(reader, 5), ShortlistPriority.Medium),
                ParseEnum(ReadString(reader, 6), ShortlistFollowUpAction.None),
                ReadString(reader, 7),
                ReadString(reader, 8),
                ReadString(reader, 9),
                ReadString(reader, 10),
                ParseDate(ReadString(reader, 11)),
                ParseDate(ReadString(reader, 12)));
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
        }

        private static DateTimeOffset ParseDate(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;
        }

        private static string SafeName(string name)
        {
            var sanitized = SanitizeWorkflowText(name, "Untitled shortlist");
            return string.IsNullOrWhiteSpace(sanitized) ? "Untitled shortlist" : sanitized.Trim();
        }

        private static string SanitizeWorkflowText(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback ?? string.Empty;
            }

            var sanitized = DiagnosticSanitizer.Sanitize(value.Trim());
            foreach (var term in HiddenTerms)
            {
                if (sanitized.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return fallback ?? string.Empty;
                }
            }

            return sanitized;
        }
    }
}
