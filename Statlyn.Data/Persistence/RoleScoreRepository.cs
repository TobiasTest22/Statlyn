using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Analytics;

namespace Statlyn.Data.Persistence
{
    public sealed class RoleScoreRepository : SqliteRepository
    {
        public RoleScoreRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public long Save(long playerId, RoleScore roleScore)
        {
            if (roleScore == null)
            {
                throw new ArgumentNullException(nameof(roleScore));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                return Save(playerId, roleScore, connection, null);
            }
        }

        public long Save(long playerId, RoleScore roleScore, SqliteConnection connection, SqliteTransaction? transaction)
        {
            if (roleScore == null)
            {
                throw new ArgumentNullException(nameof(roleScore));
            }

            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText =
                    @"INSERT INTO RoleScore (
                        PlayerId, RoleModelId, RoleFit, TechnicalFit, StatisticalFit, PhysicalFit, TacticalFit,
                        RiskScore, Confidence, Recommendation, PositiveEvidence, NegativeEvidence, MissingData, BlockedDataNotice, CreatedAtUtc)
                      VALUES (
                        $playerId, NULL, $roleFit, $technicalFit, $statisticalFit, $physicalFit, $tacticalFit,
                        $riskScore, $confidence, $recommendation, $positiveEvidence, $negativeEvidence, $missingData, $blockedDataNotice, $createdAtUtc);";
                Add(command, "$playerId", playerId);
                Add(command, "$roleFit", roleScore.RoleFit);
                Add(command, "$technicalFit", roleScore.TechnicalFit);
                Add(command, "$statisticalFit", roleScore.StatisticalFit);
                Add(command, "$physicalFit", roleScore.PhysicalFit);
                Add(command, "$tacticalFit", roleScore.TacticalFit);
                Add(command, "$riskScore", roleScore.RiskScore);
                Add(command, "$confidence", roleScore.Confidence);
                Add(command, "$recommendation", roleScore.Recommendation.ToString());
                Add(command, "$positiveEvidence", JoinValues(roleScore.PositiveEvidence.Select(item => item.FieldName + ":" + item.Message)));
                Add(command, "$negativeEvidence", JoinValues(roleScore.NegativeEvidence.Select(item => item.FieldName + ":" + item.Message)));
                Add(command, "$missingData", JoinValues(roleScore.MissingData));
                Add(command, "$blockedDataNotice", roleScore.BlockedDataNotice);
                Add(command, "$createdAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
                return LastInsertRowId(connection);
            }
        }

        public void DeleteForPlayer(long playerId, SqliteConnection connection, SqliteTransaction? transaction)
        {
            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText = "DELETE FROM RoleScore WHERE PlayerId = $playerId;";
                Add(command, "$playerId", playerId);
                command.ExecuteNonQuery();
            }
        }

        public RoleScore? LoadLatest(long playerId, string roleName)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT RoleFit, TechnicalFit, StatisticalFit, PhysicalFit, TacticalFit, RiskScore, Confidence,
                             Recommendation, PositiveEvidence, NegativeEvidence, MissingData, BlockedDataNotice
                      FROM RoleScore
                      WHERE PlayerId = $playerId
                      ORDER BY CreatedAtUtc DESC, Id DESC
                      LIMIT 1;";
                Add(command, "$playerId", playerId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var recommendation = Enum.TryParse<RecruitmentRecommendation>(reader.GetString(7), out var parsedRecommendation)
                        ? parsedRecommendation
                        : RecruitmentRecommendation.ScoutFurther;
                    return new RoleScore(
                        roleName,
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetInt32(2),
                        reader.GetInt32(3),
                        ReadNullableInt(reader, 4),
                        reader.GetInt32(5),
                        reader.GetInt32(6),
                        recommendation,
                        SplitEvidence(reader.GetString(8), true),
                        SplitEvidence(reader.GetString(9), false),
                        SplitValues(reader.GetString(10)),
                        ReadString(reader, 11));
                }
            }
        }

        private static IReadOnlyList<EvidenceItem> SplitEvidence(string value, bool positive)
        {
            var items = new List<EvidenceItem>();
            foreach (var item in SplitValues(value))
            {
                var parts = item.Split(new[] { ':' }, 2);
                items.Add(new EvidenceItem(parts[0], parts.Length == 2 ? parts[1] : item, positive));
            }

            return items;
        }
    }
}
