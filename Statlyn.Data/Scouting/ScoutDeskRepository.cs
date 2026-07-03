using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Persistence;
using Statlyn.Data.Shortlists;

namespace Statlyn.Data.Scouting
{
    public sealed class ScoutDeskRepository : SqliteRepository
    {
        public ScoutDeskRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public ScoutAssignmentRecord CreateAssignment(
            string statlynPlayerId,
            long? shortlistPlayerId,
            string roleName,
            ShortlistPriority priority,
            string assignedTo,
            DateTimeOffset? dueAtUtc)
        {
            return CreateAssignment(new CreateScoutAssignmentRequest
            {
                StatlynPlayerId = statlynPlayerId,
                ShortlistPlayerId = shortlistPlayerId,
                RoleName = roleName,
                Priority = priority,
                AssignedTo = assignedTo,
                DueAtUtc = dueAtUtc
            });
        }

        public ScoutAssignmentRecord CreateAssignment(object player, long? shortlistPlayerId, string roleName, ShortlistPriority priority, string assignedTo, DateTimeOffset? dueAtUtc)
        {
            SafePersistenceGuard.RejectRaw(player, "Create scout assignment");
            if (!(player is string statlynPlayerId))
            {
                throw new InvalidOperationException("Scout assignments must be created by persisted StatlynPlayerId.");
            }

            return CreateAssignment(statlynPlayerId, shortlistPlayerId, roleName, priority, assignedTo, dueAtUtc);
        }

        public ScoutAssignmentRecord CreateAssignment(CreateScoutAssignmentRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.StatlynPlayerId))
            {
                throw new InvalidOperationException("A persisted StatlynPlayerId is required.");
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                var player = FindPlayer(connection, request.StatlynPlayerId);
                if (player == null)
                {
                    throw new InvalidOperationException("Player was not found in persisted safe data.");
                }

                var now = DateTimeOffset.UtcNow;
                var roleName = RoleNameSanitizer.SanitizeForStorage(request.RoleName);
                var title = SafeText(request.AssignmentTitle, player.DisplayName + " - " + roleName);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO ScoutAssignment (
                            StatlynPlayerId, ShortlistPlayerId, ShortlistId, PlayerId, AssignmentTitle,
                            RoleName, PositionGroup, Priority, Status, AssignedTo, CreatedAtUtc, DueAtUtc,
                            UpdatedAtUtc, ClosedAtUtc, SourceName, IsArchived)
                          VALUES (
                            $statlynPlayerId, $shortlistPlayerId, $shortlistId, $playerId, $assignmentTitle,
                            $roleName, $positionGroup, $priority, $status, $assignedTo, $createdAtUtc, $dueAtUtc,
                            $updatedAtUtc, NULL, $sourceName, 0);";
                    Add(command, "$statlynPlayerId", player.StatlynPlayerId);
                    Add(command, "$shortlistPlayerId", request.ShortlistPlayerId);
                    Add(command, "$shortlistId", request.ShortlistId);
                    Add(command, "$playerId", player.PlayerId);
                    Add(command, "$assignmentTitle", title);
                    Add(command, "$roleName", roleName);
                    Add(command, "$positionGroup", string.IsNullOrWhiteSpace(player.PositionGroup) ? "Unknown" : player.PositionGroup);
                    Add(command, "$priority", request.Priority.ToString());
                    Add(command, "$status", ScoutAssignmentStatus.Open.ToString());
                    Add(command, "$assignedTo", SafeText(request.AssignedTo, string.Empty));
                    Add(command, "$createdAtUtc", now.ToString("O"));
                    Add(command, "$dueAtUtc", request.DueAtUtc.HasValue ? request.DueAtUtc.Value.ToString("O") : null);
                    Add(command, "$updatedAtUtc", now.ToString("O"));
                    Add(command, "$sourceName", player.SourceName);
                    command.ExecuteNonQuery();
                }

                return LoadAssignment(LastInsertRowId(connection))!;
            }
        }

        public ScoutAssignmentRecord UpdateAssignmentStatus(long assignmentId, ScoutAssignmentStatus status)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                var closedAt = status == ScoutAssignmentStatus.Closed || status == ScoutAssignmentStatus.Cancelled
                    ? DateTimeOffset.UtcNow.ToString("O")
                    : null;
                command.CommandText =
                    @"UPDATE ScoutAssignment SET
                        Status = $status,
                        UpdatedAtUtc = $updatedAtUtc,
                        ClosedAtUtc = $closedAtUtc
                      WHERE Id = $id;";
                Add(command, "$status", status.ToString());
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$closedAtUtc", closedAt);
                Add(command, "$id", assignmentId);
                command.ExecuteNonQuery();
            }

            var assignment = LoadAssignment(assignmentId);
            if (assignment == null)
            {
                throw new InvalidOperationException("Scout assignment was not found.");
            }

            return assignment;
        }

        public IReadOnlyList<ScoutAssignmentRecord> LoadAssignments(ScoutDeskQuery? query)
        {
            query = query ?? new ScoutDeskQuery();
            var assignments = new List<ScoutAssignmentRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, StatlynPlayerId, ShortlistPlayerId, ShortlistId, PlayerId, AssignmentTitle,
                             RoleName, PositionGroup, Priority, Status, AssignedTo, CreatedAtUtc, DueAtUtc,
                             UpdatedAtUtc, ClosedAtUtc, SourceName, IsArchived
                      FROM ScoutAssignment
                      WHERE ($includeArchived = 1 OR IsArchived = 0)
                        AND ($status = '' OR Status = $status)
                        AND ($statlynPlayerId = '' OR StatlynPlayerId = $statlynPlayerId)
                      ORDER BY UpdatedAtUtc DESC, Id DESC
                      LIMIT $limit;";
                Add(command, "$includeArchived", Bool(query.IncludeArchived));
                Add(command, "$status", query.Status.HasValue ? query.Status.Value.ToString() : string.Empty);
                Add(command, "$statlynPlayerId", query.StatlynPlayerId ?? string.Empty);
                Add(command, "$limit", SafeLimit(query.Limit));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        assignments.Add(ReadAssignment(reader));
                    }
                }
            }

            return assignments;
        }

        public ScoutAssignmentRecord? LoadAssignment(long assignmentId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, StatlynPlayerId, ShortlistPlayerId, ShortlistId, PlayerId, AssignmentTitle,
                             RoleName, PositionGroup, Priority, Status, AssignedTo, CreatedAtUtc, DueAtUtc,
                             UpdatedAtUtc, ClosedAtUtc, SourceName, IsArchived
                      FROM ScoutAssignment
                      WHERE Id = $id
                      LIMIT 1;";
                Add(command, "$id", assignmentId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadAssignment(reader) : null;
                }
            }
        }

        public ScoutReportRecord CreateReport(SubmitScoutReportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                var assignment = request.AssignmentId.HasValue ? LoadAssignment(request.AssignmentId.Value) : null;
                var statlynPlayerId = string.IsNullOrWhiteSpace(request.StatlynPlayerId) && assignment != null
                    ? assignment.StatlynPlayerId
                    : request.StatlynPlayerId;
                if (string.IsNullOrWhiteSpace(statlynPlayerId))
                {
                    throw new InvalidOperationException("A persisted StatlynPlayerId is required.");
                }

                if (assignment != null && !string.Equals(assignment.StatlynPlayerId, statlynPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Scout report assignment does not match the persisted player.");
                }

                var player = FindPlayer(connection, statlynPlayerId);
                if (player == null)
                {
                    throw new InvalidOperationException("Player was not found in persisted safe data.");
                }

                using (var transaction = connection.BeginTransaction())
                {
                    var now = DateTimeOffset.UtcNow;
                    long reportId;
                    using (var command = CreateCommand(connection, transaction))
                    {
                        command.CommandText =
                            @"INSERT INTO ScoutReport (
                                AssignmentId, PlayerId, StatlynPlayerId, ReportDateUtc, RoleAssessed,
                                TechnicalRating, TacticalRating, PhysicalRating, MentalRating,
                                OverallRecommendation, Confidence, Strengths, Weaknesses, Risks, Risk,
                                Recommendation, FollowUpAction, FinalSummary, CreatedAtUtc, UpdatedAtUtc)
                              VALUES (
                                $assignmentId, $playerId, $statlynPlayerId, $reportDateUtc, $roleAssessed,
                                $technicalRating, $tacticalRating, $physicalRating, $mentalRating,
                                $overallRecommendation, $confidence, $strengths, $weaknesses, $risks, $risk,
                                $recommendation, $followUpAction, $finalSummary, $createdAtUtc, $updatedAtUtc);";
                        Add(command, "$assignmentId", request.AssignmentId);
                        Add(command, "$playerId", player.PlayerId);
                        Add(command, "$statlynPlayerId", player.StatlynPlayerId);
                        Add(command, "$reportDateUtc", now.ToString("O"));
                        Add(command, "$roleAssessed", RoleNameSanitizer.SanitizeForStorage(request.RoleAssessed));
                        Add(command, "$technicalRating", request.TechnicalRating.ToString());
                        Add(command, "$tacticalRating", request.TacticalRating.ToString());
                        Add(command, "$physicalRating", request.PhysicalRating.ToString());
                        Add(command, "$mentalRating", request.MentalRating.ToString());
                        Add(command, "$overallRecommendation", request.OverallRecommendation.ToString());
                        Add(command, "$confidence", ClampPercent(request.Confidence));
                        Add(command, "$strengths", SafeText(request.Strengths, string.Empty));
                        Add(command, "$weaknesses", SafeText(request.Weaknesses, string.Empty));
                        Add(command, "$risks", SafeText(request.Risks, string.Empty));
                        Add(command, "$risk", SafeText(request.Risks, string.Empty));
                        Add(command, "$recommendation", request.OverallRecommendation.ToString());
                        Add(command, "$followUpAction", request.FollowUpAction.ToString());
                        Add(command, "$finalSummary", SafeText(request.FinalSummary, string.Empty));
                        Add(command, "$createdAtUtc", now.ToString("O"));
                        Add(command, "$updatedAtUtc", now.ToString("O"));
                        command.ExecuteNonQuery();
                    }

                    reportId = LastInsertRowId(connection, transaction);
                    foreach (var question in request.QuestionAnswers ?? new List<ScoutQuestionAnswerRequest>())
                    {
                        using (var command = CreateCommand(connection, transaction))
                        {
                            command.CommandText =
                                @"INSERT INTO ScoutReportQuestion (ReportId, Question, Answer, Category, CreatedAtUtc)
                                  VALUES ($reportId, $question, $answer, $category, $createdAtUtc);";
                            Add(command, "$reportId", reportId);
                            Add(command, "$question", SafeText(question.Question, "Scout observation"));
                            Add(command, "$answer", SafeText(question.Answer, string.Empty));
                            Add(command, "$category", SafeText(question.Category, "General"));
                            Add(command, "$createdAtUtc", now.ToString("O"));
                            command.ExecuteNonQuery();
                        }
                    }

                    if (request.AssignmentId.HasValue)
                    {
                        using (var command = CreateCommand(connection, transaction))
                        {
                            command.CommandText =
                                @"UPDATE ScoutAssignment SET
                                    Status = $status,
                                    UpdatedAtUtc = $updatedAtUtc
                                  WHERE Id = $id;";
                            Add(command, "$status", ScoutAssignmentStatus.ReportSubmitted.ToString());
                            Add(command, "$updatedAtUtc", now.ToString("O"));
                            Add(command, "$id", request.AssignmentId.Value);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return LoadReport(reportId)!;
                }
            }
        }

        public ScoutReportRecord? LoadReport(long reportId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = ReportSelectSql() + " WHERE SR.Id = $id LIMIT 1;";
                Add(command, "$id", reportId);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadReport(reader) : null;
                }
            }
        }

        public IReadOnlyList<ScoutReportRecord> LoadReportsForPlayer(string statlynPlayerId)
        {
            var reports = new List<ScoutReportRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = ReportSelectSql() +
                    @" WHERE SR.StatlynPlayerId = $statlynPlayerId
                          OR SR.PlayerId = (SELECT Id FROM Player WHERE StatlynPlayerId = $statlynPlayerId LIMIT 1)
                       ORDER BY SR.ReportDateUtc DESC, SR.Id DESC;";
                Add(command, "$statlynPlayerId", statlynPlayerId ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(ReadReport(reader));
                    }
                }
            }

            return reports;
        }

        public ScoutReportRecord? LoadLatestReportForPlayer(string statlynPlayerId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = ReportSelectSql() +
                    @" WHERE SR.StatlynPlayerId = $statlynPlayerId
                          OR SR.PlayerId = (SELECT Id FROM Player WHERE StatlynPlayerId = $statlynPlayerId LIMIT 1)
                       ORDER BY SR.ReportDateUtc DESC, SR.Id DESC
                       LIMIT 1;";
                Add(command, "$statlynPlayerId", statlynPlayerId ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadReport(reader) : null;
                }
            }
        }

        public IReadOnlyList<ScoutReportRecord> LoadReportsForAssignment(long assignmentId)
        {
            var reports = new List<ScoutReportRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = ReportSelectSql() +
                    @" WHERE SR.AssignmentId = $assignmentId
                       ORDER BY SR.ReportDateUtc DESC, SR.Id DESC;";
                Add(command, "$assignmentId", assignmentId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reports.Add(ReadReport(reader));
                    }
                }
            }

            return reports;
        }

        public IReadOnlyList<ScoutReportQuestionRecord> LoadQuestionsForReport(long reportId)
        {
            var questions = new List<ScoutReportQuestionRecord>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, ReportId, Question, Answer, Category, CreatedAtUtc
                      FROM ScoutReportQuestion
                      WHERE ReportId = $reportId
                      ORDER BY Id ASC;";
                Add(command, "$reportId", reportId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        questions.Add(new ScoutReportQuestionRecord(
                            reader.GetInt64(0),
                            reader.GetInt64(1),
                            ReadString(reader, 2),
                            ReadString(reader, 3),
                            ReadString(reader, 4),
                            ParseDate(ReadString(reader, 5))));
                    }
                }
            }

            return questions;
        }

        public void ArchiveAssignment(long assignmentId)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"UPDATE ScoutAssignment SET
                        IsArchived = 1,
                        UpdatedAtUtc = $updatedAtUtc
                      WHERE Id = $id;";
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", assignmentId);
                command.ExecuteNonQuery();
            }
        }

        private static string ReportSelectSql()
        {
            return
                @"SELECT
                    SR.Id,
                    SR.AssignmentId,
                    SR.PlayerId,
                    CASE WHEN SR.StatlynPlayerId IS NULL OR SR.StatlynPlayerId = '' THEN P.StatlynPlayerId ELSE SR.StatlynPlayerId END AS StatlynPlayerId,
                    SR.ReportDateUtc,
                    SR.RoleAssessed,
                    SR.TechnicalRating,
                    SR.TacticalRating,
                    SR.PhysicalRating,
                    SR.MentalRating,
                    CASE WHEN SR.OverallRecommendation IS NULL OR SR.OverallRecommendation = '' THEN SR.Recommendation ELSE SR.OverallRecommendation END AS OverallRecommendation,
                    SR.Confidence,
                    SR.Strengths,
                    SR.Weaknesses,
                    CASE WHEN SR.Risks IS NULL OR SR.Risks = '' THEN SR.Risk ELSE SR.Risks END AS Risks,
                    SR.FollowUpAction,
                    SR.FinalSummary,
                    SR.CreatedAtUtc,
                    SR.UpdatedAtUtc
                  FROM ScoutReport SR
                  INNER JOIN Player P ON P.Id = SR.PlayerId";
        }

        private static ScoutAssignmentRecord ReadAssignment(SqliteDataReader reader)
        {
            return new ScoutAssignmentRecord(
                reader.GetInt64(0),
                ReadString(reader, 1),
                ReadNullableLong(reader, 2),
                ReadNullableLong(reader, 3),
                reader.GetInt64(4),
                ReadString(reader, 5),
                ReadString(reader, 6),
                ReadString(reader, 7),
                ParseEnum(ReadString(reader, 8), ShortlistPriority.Medium),
                ParseEnum(ReadString(reader, 9), ScoutAssignmentStatus.Open),
                ReadString(reader, 10),
                ParseDate(ReadString(reader, 11)),
                ParseNullableDate(ReadString(reader, 12)),
                ParseDate(ReadString(reader, 13)),
                ParseNullableDate(ReadString(reader, 14)),
                ReadString(reader, 15),
                ReadBool(reader, 16));
        }

        private static ScoutReportRecord ReadReport(SqliteDataReader reader)
        {
            return new ScoutReportRecord(
                reader.GetInt64(0),
                ReadNullableLong(reader, 1),
                reader.GetInt64(2),
                ReadString(reader, 3),
                ParseDate(ReadString(reader, 4)),
                ReadString(reader, 5),
                ParseEnum(ReadString(reader, 6), ScoutObservationRating.Unknown),
                ParseEnum(ReadString(reader, 7), ScoutObservationRating.Unknown),
                ParseEnum(ReadString(reader, 8), ScoutObservationRating.Unknown),
                ParseEnum(ReadString(reader, 9), ScoutObservationRating.Unknown),
                ParseEnum(ReadString(reader, 10), ScoutReportRecommendation.ScoutFurther),
                reader.GetInt32(11),
                ReadString(reader, 12),
                ReadString(reader, 13),
                ReadString(reader, 14),
                ParseEnum(ReadString(reader, 15), ScoutFollowUpAction.None),
                ReadString(reader, 16),
                ParseDate(ReadString(reader, 17)),
                ParseDate(ReadString(reader, 18)));
        }

        private static PlayerLookup? FindPlayer(SqliteConnection connection, string statlynPlayerId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, StatlynPlayerId, DisplayName, PositionGroup, PrimaryPosition, SourceName
                      FROM Player
                      WHERE StatlynPlayerId = $statlynPlayerId
                      LIMIT 1;";
                Add(command, "$statlynPlayerId", statlynPlayerId.Trim());
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var positionGroup = ReadString(reader, 3);
                    if (string.IsNullOrWhiteSpace(positionGroup))
                    {
                        positionGroup = ReadString(reader, 4);
                    }

                    return new PlayerLookup(
                        reader.GetInt64(0),
                        ReadString(reader, 1),
                        ReadString(reader, 2),
                        positionGroup,
                        ReadString(reader, 5));
                }
            }
        }

        private static int SafeLimit(int limit)
        {
            if (limit <= 0)
            {
                return 100;
            }

            return Math.Min(limit, 500);
        }

        private static int ClampPercent(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }

        private static string SafeText(string value, string fallback)
        {
            var sanitized = ScoutTextSanitizer.Sanitize(value ?? string.Empty);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return fallback ?? string.Empty;
            }

            return sanitized;
        }

        private static long? ReadNullableLong(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (long?)null : reader.GetInt64(ordinal);
        }

        private static long LastInsertRowId(SqliteConnection connection, SqliteTransaction transaction)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText = "SELECT last_insert_rowid();";
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static DateTimeOffset ParseDate(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;
        }

        private static DateTimeOffset? ParseNullableDate(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : (DateTimeOffset?)null;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
        }

        private sealed class PlayerLookup
        {
            public PlayerLookup(long playerId, string statlynPlayerId, string displayName, string positionGroup, string sourceName)
            {
                PlayerId = playerId;
                StatlynPlayerId = statlynPlayerId ?? string.Empty;
                DisplayName = displayName ?? string.Empty;
                PositionGroup = positionGroup ?? string.Empty;
                SourceName = sourceName ?? string.Empty;
            }

            public long PlayerId { get; }

            public string StatlynPlayerId { get; }

            public string DisplayName { get; }

            public string PositionGroup { get; }

            public string SourceName { get; }
        }
    }
}
