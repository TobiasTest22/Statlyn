using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Persistence;
using Statlyn.Data.Scouting;

namespace Statlyn.Data.RoleLab
{
    public sealed class RoleLabRepository : SqliteRepository
    {
        public RoleLabRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public TacticalRoleModel SaveRole(TacticalRoleModel role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            var safeRole = SanitizeRole(role);
            using (var connection = ConnectionFactory.OpenConnection())
            {
                var now = DateTimeOffset.UtcNow;
                if (safeRole.Id > 0)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"UPDATE TacticalRole SET
                                RoleName = $roleName,
                                TacticalPhase = $tacticalPhase,
                                RoleFamily = $roleFamily,
                                Source = $source,
                                IsOfficialFm26Role = $isOfficialFm26Role,
                                Fm26RoleId = $fm26RoleId,
                                PositionGroup = $positionGroup,
                                ValidSlots = $validSlots,
                                MovementBehaviour = $movementBehaviour,
                                BuildUpBehaviour = $buildUpBehaviour,
                                FinalThirdBehaviour = $finalThirdBehaviour,
                                PressingBehaviour = $pressingBehaviour,
                                DefensiveBlockBehaviour = $defensiveBlockBehaviour,
                                TransitionBehaviour = $transitionBehaviour,
                                UpdatedAtUtc = $updatedAtUtc,
                                IsArchived = $isArchived
                              WHERE Id = $id;";
                        AddRoleParameters(command, safeRole, now);
                        Add(command, "$id", safeRole.Id);
                        command.ExecuteNonQuery();
                    }

                    return LoadRole(safeRole.Id)!;
                }

                var existing = FindRoleId(connection, safeRole.RoleName, safeRole.TacticalPhase);
                if (existing.HasValue && safeRole.Source == TacticalRoleSource.BuiltInSeed)
                {
                    var existingRole = LoadRole(existing.Value)!;
                    var replacement = new TacticalRoleModel(
                        existingRole.Id,
                        safeRole.RoleName,
                        safeRole.TacticalPhase,
                        safeRole.RoleFamily,
                        safeRole.Source,
                        false,
                        string.Empty,
                        safeRole.PositionGroup,
                        safeRole.ValidSlots,
                        safeRole.MovementBehaviour,
                        safeRole.BuildUpBehaviour,
                        safeRole.FinalThirdBehaviour,
                        safeRole.PressingBehaviour,
                        safeRole.DefensiveBlockBehaviour,
                        safeRole.TransitionBehaviour,
                        existingRole.CreatedAtUtc,
                        now,
                        false);
                    return SaveRole(replacement);
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO TacticalRole (
                            RoleName, TacticalPhase, RoleFamily, Source, IsOfficialFm26Role, Fm26RoleId,
                            PositionGroup, ValidSlots, MovementBehaviour, BuildUpBehaviour, FinalThirdBehaviour,
                            PressingBehaviour, DefensiveBlockBehaviour, TransitionBehaviour, CreatedAtUtc, UpdatedAtUtc, IsArchived)
                          VALUES (
                            $roleName, $tacticalPhase, $roleFamily, $source, $isOfficialFm26Role, $fm26RoleId,
                            $positionGroup, $validSlots, $movementBehaviour, $buildUpBehaviour, $finalThirdBehaviour,
                            $pressingBehaviour, $defensiveBlockBehaviour, $transitionBehaviour, $createdAtUtc, $updatedAtUtc, $isArchived);";
                    AddRoleParameters(command, safeRole, now);
                    Add(command, "$createdAtUtc", now.ToString("O"));
                    command.ExecuteNonQuery();
                }

                return LoadRole(LastInsertRowId(connection))!;
            }
        }

        public TacticalRoleModel SaveRole(object role)
        {
            SafePersistenceGuard.RejectRaw(role, "Save Role Lab role");
            if (!(role is TacticalRoleModel model))
            {
                throw new InvalidOperationException("Role Lab roles must be saved from safe TacticalRoleModel data.");
            }

            return SaveRole(model);
        }

        public TacticalRoleModel? LoadRole(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = RoleSelectSql() + " WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRole(reader) : null;
                }
            }
        }

        public TacticalRoleModel? LoadRoleByName(string roleName)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = RoleSelectSql() +
                    @" WHERE lower(RoleName) = lower($roleName)
                         AND IsArchived = 0
                       ORDER BY TacticalPhase ASC, Id ASC
                       LIMIT 1;";
                Add(command, "$roleName", roleName ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRole(reader) : null;
                }
            }
        }

        public IReadOnlyList<TacticalRoleModel> LoadRoles(bool includeArchived)
        {
            var roles = new List<TacticalRoleModel>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = RoleSelectSql() +
                    @" WHERE $includeArchived = 1 OR IsArchived = 0
                       ORDER BY TacticalPhase ASC, RoleName ASC;";
                Add(command, "$includeArchived", Bool(includeArchived));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(ReadRole(reader));
                    }
                }
            }

            return roles;
        }

        public IReadOnlyList<TacticalRoleModel> LoadRolesByPhase(TacticalPhase phase)
        {
            var roles = new List<TacticalRoleModel>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = RoleSelectSql() +
                    @" WHERE TacticalPhase = $phase AND IsArchived = 0
                       ORDER BY RoleName ASC;";
                Add(command, "$phase", phase.ToString());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(ReadRole(reader));
                    }
                }
            }

            return roles;
        }

        public void ArchiveRole(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE TacticalRole SET IsArchived = 1, UpdatedAtUtc = $updatedAtUtc WHERE Id = $id;";
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }
        }

        public TacticalRolePairModel SaveRolePair(TacticalRolePairModel pair)
        {
            if (pair == null)
            {
                throw new ArgumentNullException(nameof(pair));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            {
                EnsureRoleExists(connection, pair.InPossessionRoleId);
                EnsureRoleExists(connection, pair.OutOfPossessionRoleId);
                var now = DateTimeOffset.UtcNow;
                var safePair = SanitizePair(pair);

                if (safePair.Id > 0)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            @"UPDATE TacticalRolePair SET
                                PairName = $pairName,
                                InPossessionRoleId = $inPossessionRoleId,
                                OutOfPossessionRoleId = $outOfPossessionRoleId,
                                InPossessionSlot = $inPossessionSlot,
                                OutOfPossessionSlot = $outOfPossessionSlot,
                                InPossessionFormation = $inPossessionFormation,
                                OutOfPossessionFormation = $outOfPossessionFormation,
                                TransitionComplexityScore = $transitionComplexityScore,
                                TacticalRiskScore = $tacticalRiskScore,
                                PositionalFamiliarityNeed = $positionalFamiliarityNeed,
                                UpdatedAtUtc = $updatedAtUtc,
                                IsArchived = $isArchived
                              WHERE Id = $id;";
                        AddPairParameters(command, safePair, now);
                        Add(command, "$id", safePair.Id);
                        command.ExecuteNonQuery();
                    }

                    return LoadRolePair(safePair.Id)!;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO TacticalRolePair (
                            PairName, InPossessionRoleId, OutOfPossessionRoleId, InPossessionSlot, OutOfPossessionSlot,
                            InPossessionFormation, OutOfPossessionFormation, TransitionComplexityScore, TacticalRiskScore,
                            PositionalFamiliarityNeed, CreatedAtUtc, UpdatedAtUtc, IsArchived)
                          VALUES (
                            $pairName, $inPossessionRoleId, $outOfPossessionRoleId, $inPossessionSlot, $outOfPossessionSlot,
                            $inPossessionFormation, $outOfPossessionFormation, $transitionComplexityScore, $tacticalRiskScore,
                            $positionalFamiliarityNeed, $createdAtUtc, $updatedAtUtc, $isArchived);";
                    AddPairParameters(command, safePair, now);
                    Add(command, "$createdAtUtc", now.ToString("O"));
                    command.ExecuteNonQuery();
                }

                return LoadRolePair(LastInsertRowId(connection))!;
            }
        }

        public TacticalRolePairModel? LoadRolePair(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = PairSelectSql() + " WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadPair(reader) : null;
                }
            }
        }

        public TacticalRolePairModel? LoadRolePairByName(string pairName)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = PairSelectSql() +
                    @" WHERE lower(PairName) = lower($pairName)
                         AND IsArchived = 0
                       ORDER BY Id ASC
                       LIMIT 1;";
                Add(command, "$pairName", pairName ?? string.Empty);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadPair(reader) : null;
                }
            }
        }

        public IReadOnlyList<TacticalRolePairModel> LoadRolePairs(bool includeArchived)
        {
            var pairs = new List<TacticalRolePairModel>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = PairSelectSql() +
                    @" WHERE $includeArchived = 1 OR IsArchived = 0
                       ORDER BY PairName ASC;";
                Add(command, "$includeArchived", Bool(includeArchived));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pairs.Add(ReadPair(reader));
                    }
                }
            }

            return pairs;
        }

        public void ArchiveRolePair(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE TacticalRolePair SET IsArchived = 1, UpdatedAtUtc = $updatedAtUtc WHERE Id = $id;";
                Add(command, "$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }
        }

        public RoleOutputMetricRequirementModel SaveMetricRequirement(RoleOutputMetricRequirementModel requirement)
        {
            if (requirement == null)
            {
                throw new ArgumentNullException(nameof(requirement));
            }

            ValidateOwner(requirement.TacticalRoleId, requirement.RolePairId);
            var safe = SanitizeRequirement(requirement);
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO RoleOutputMetricRequirement (
                        TacticalRoleId, RolePairId, MetricKey, FieldName, Weight, Importance, Direction,
                        MinimumSampleMinutes, Per90Required, NormalizationHint, EvidenceTemplate, MissingDataImpact)
                      VALUES (
                        $tacticalRoleId, $rolePairId, $metricKey, $fieldName, $weight, $importance, $direction,
                        $minimumSampleMinutes, $per90Required, $normalizationHint, $evidenceTemplate, $missingDataImpact);";
                AddRequirementParameters(command, safe);
                command.ExecuteNonQuery();
                return LoadMetricRequirement(LastInsertRowId(connection))!;
            }
        }

        public IReadOnlyList<RoleOutputMetricRequirementModel> LoadMetricRequirementsForRole(long roleId)
        {
            return LoadMetricRequirements("TacticalRoleId", roleId);
        }

        public IReadOnlyList<RoleOutputMetricRequirementModel> LoadMetricRequirementsForPair(long pairId)
        {
            return LoadMetricRequirements("RolePairId", pairId);
        }

        public RoleScoutQuestionModel SaveScoutQuestion(RoleScoutQuestionModel question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            ValidateOwner(question.TacticalRoleId, question.RolePairId);
            var safe = SanitizeQuestion(question);
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO RoleScoutQuestion (
                        TacticalRoleId, RolePairId, Category, Question, WhyItMatters, SuggestedObservationType)
                      VALUES (
                        $tacticalRoleId, $rolePairId, $category, $question, $whyItMatters, $suggestedObservationType);";
                Add(command, "$tacticalRoleId", safe.TacticalRoleId);
                Add(command, "$rolePairId", safe.RolePairId);
                Add(command, "$category", SafeText(safe.Category, "RoleFit"));
                Add(command, "$question", SafeText(safe.Question, "Observe role behaviour directly."));
                Add(command, "$whyItMatters", SafeText(safe.WhyItMatters, string.Empty));
                Add(command, "$suggestedObservationType", SafeText(safe.SuggestedObservationType, "RoleFit"));
                command.ExecuteNonQuery();
                return LoadScoutQuestion(LastInsertRowId(connection))!;
            }
        }

        public IReadOnlyList<RoleScoutQuestionModel> LoadScoutQuestionsForRole(long roleId)
        {
            return LoadScoutQuestions("TacticalRoleId", roleId);
        }

        public IReadOnlyList<RoleScoutQuestionModel> LoadScoutQuestionsForPair(long pairId)
        {
            return LoadScoutQuestions("RolePairId", pairId);
        }

        public RoleRedFlagModel SaveRedFlag(RoleRedFlagModel redFlag)
        {
            if (redFlag == null)
            {
                throw new ArgumentNullException(nameof(redFlag));
            }

            ValidateOwner(redFlag.TacticalRoleId, redFlag.RolePairId);
            var safe = SanitizeRedFlag(redFlag);
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO RoleRedFlag (
                        TacticalRoleId, RolePairId, FieldName, Operator, Threshold, Message, AppliesToPhase)
                      VALUES (
                        $tacticalRoleId, $rolePairId, $fieldName, $operator, $threshold, $message, $appliesToPhase);";
                Add(command, "$tacticalRoleId", safe.TacticalRoleId);
                Add(command, "$rolePairId", safe.RolePairId);
                Add(command, "$fieldName", SafeText(safe.FieldName, "Output"));
                Add(command, "$operator", SafeText(safe.Operator, "Review"));
                Add(command, "$threshold", SafeText(safe.Threshold, string.Empty));
                Add(command, "$message", SafeText(safe.Message, "Review role-output context."));
                Add(command, "$appliesToPhase", safe.AppliesToPhase.ToString());
                command.ExecuteNonQuery();
                return LoadRedFlag(LastInsertRowId(connection))!;
            }
        }

        public IReadOnlyList<RoleRedFlagModel> LoadRedFlagsForRole(long roleId)
        {
            return LoadRedFlags("TacticalRoleId", roleId);
        }

        public IReadOnlyList<RoleRedFlagModel> LoadRedFlagsForPair(long pairId)
        {
            return LoadRedFlags("RolePairId", pairId);
        }

        public void DeleteMetricRequirementsForRole(long roleId)
        {
            DeleteOwnedRows("RoleOutputMetricRequirement", "TacticalRoleId", roleId);
        }

        public void DeleteMetricRequirement(long id)
        {
            DeleteById("RoleOutputMetricRequirement", id);
        }

        public void DeleteScoutQuestionsForRole(long roleId)
        {
            DeleteOwnedRows("RoleScoutQuestion", "TacticalRoleId", roleId);
        }

        public void DeleteScoutQuestion(long id)
        {
            DeleteById("RoleScoutQuestion", id);
        }

        public void DeleteRedFlagsForRole(long roleId)
        {
            DeleteOwnedRows("RoleRedFlag", "TacticalRoleId", roleId);
        }

        public void DeleteRedFlag(long id)
        {
            DeleteById("RoleRedFlag", id);
        }

        private RoleOutputMetricRequirementModel? LoadMetricRequirement(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = RequirementSelectSql() + " WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRequirement(reader) : null;
                }
            }
        }

        private RoleScoutQuestionModel? LoadScoutQuestion(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, TacticalRoleId, RolePairId, Category, Question, WhyItMatters, SuggestedObservationType
                      FROM RoleScoutQuestion
                      WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadQuestion(reader) : null;
                }
            }
        }

        private RoleRedFlagModel? LoadRedFlag(long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, TacticalRoleId, RolePairId, FieldName, Operator, Threshold, Message, AppliesToPhase
                      FROM RoleRedFlag
                      WHERE Id = $id LIMIT 1;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadRedFlag(reader) : null;
                }
            }
        }

        private IReadOnlyList<RoleOutputMetricRequirementModel> LoadMetricRequirements(string column, long id)
        {
            var output = new List<RoleOutputMetricRequirementModel>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = RequirementSelectSql() + " WHERE " + column + " = $id ORDER BY Id ASC;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        output.Add(ReadRequirement(reader));
                    }
                }
            }

            return output;
        }

        private IReadOnlyList<RoleScoutQuestionModel> LoadScoutQuestions(string column, long id)
        {
            var output = new List<RoleScoutQuestionModel>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, TacticalRoleId, RolePairId, Category, Question, WhyItMatters, SuggestedObservationType
                      FROM RoleScoutQuestion
                      WHERE " + column + @" = $id
                      ORDER BY Id ASC;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        output.Add(ReadQuestion(reader));
                    }
                }
            }

            return output;
        }

        private IReadOnlyList<RoleRedFlagModel> LoadRedFlags(string column, long id)
        {
            var output = new List<RoleRedFlagModel>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id, TacticalRoleId, RolePairId, FieldName, Operator, Threshold, Message, AppliesToPhase
                      FROM RoleRedFlag
                      WHERE " + column + @" = $id
                      ORDER BY Id ASC;";
                Add(command, "$id", id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        output.Add(ReadRedFlag(reader));
                    }
                }
            }

            return output;
        }

        private static string RoleSelectSql()
        {
            return
                @"SELECT Id, RoleName, TacticalPhase, RoleFamily, Source, IsOfficialFm26Role, Fm26RoleId,
                         PositionGroup, ValidSlots, MovementBehaviour, BuildUpBehaviour, FinalThirdBehaviour,
                         PressingBehaviour, DefensiveBlockBehaviour, TransitionBehaviour, CreatedAtUtc, UpdatedAtUtc, IsArchived
                  FROM TacticalRole";
        }

        private static string PairSelectSql()
        {
            return
                @"SELECT Id, PairName, InPossessionRoleId, OutOfPossessionRoleId, InPossessionSlot, OutOfPossessionSlot,
                         InPossessionFormation, OutOfPossessionFormation, TransitionComplexityScore, TacticalRiskScore,
                         PositionalFamiliarityNeed, CreatedAtUtc, UpdatedAtUtc, IsArchived
                  FROM TacticalRolePair";
        }

        private static string RequirementSelectSql()
        {
            return
                @"SELECT Id, TacticalRoleId, RolePairId, MetricKey, FieldName, Weight, Importance, Direction,
                         MinimumSampleMinutes, Per90Required, NormalizationHint, EvidenceTemplate, MissingDataImpact
                  FROM RoleOutputMetricRequirement";
        }

        private static void AddRoleParameters(SqliteCommand command, TacticalRoleModel role, DateTimeOffset updatedAtUtc)
        {
            Add(command, "$roleName", SafeText(role.RoleName, "Untitled role"));
            Add(command, "$tacticalPhase", role.TacticalPhase.ToString());
            Add(command, "$roleFamily", role.RoleFamily.ToString());
            Add(command, "$source", role.Source.ToString());
            Add(command, "$isOfficialFm26Role", Bool(role.IsOfficialFm26Role && role.Source == TacticalRoleSource.FM26Mapped));
            Add(command, "$fm26RoleId", role.IsOfficialFm26Role && role.Source == TacticalRoleSource.FM26Mapped ? SafeText(role.Fm26RoleId, string.Empty) : string.Empty);
            Add(command, "$positionGroup", SafeText(role.PositionGroup, "Unknown"));
            Add(command, "$validSlots", JoinValues((role.ValidSlots ?? new List<TacticalSlot>()).Select(slot => slot.ToString())));
            Add(command, "$movementBehaviour", SafeText(role.MovementBehaviour, string.Empty));
            Add(command, "$buildUpBehaviour", SafeText(role.BuildUpBehaviour, string.Empty));
            Add(command, "$finalThirdBehaviour", SafeText(role.FinalThirdBehaviour, string.Empty));
            Add(command, "$pressingBehaviour", SafeText(role.PressingBehaviour, string.Empty));
            Add(command, "$defensiveBlockBehaviour", SafeText(role.DefensiveBlockBehaviour, string.Empty));
            Add(command, "$transitionBehaviour", SafeText(role.TransitionBehaviour, string.Empty));
            Add(command, "$updatedAtUtc", updatedAtUtc.ToString("O"));
            Add(command, "$isArchived", Bool(role.IsArchived));
        }

        private static void AddPairParameters(SqliteCommand command, TacticalRolePairModel pair, DateTimeOffset updatedAtUtc)
        {
            Add(command, "$pairName", SafeText(pair.PairName, "Untitled pair"));
            Add(command, "$inPossessionRoleId", pair.InPossessionRoleId);
            Add(command, "$outOfPossessionRoleId", pair.OutOfPossessionRoleId);
            Add(command, "$inPossessionSlot", pair.InPossessionSlot.ToString());
            Add(command, "$outOfPossessionSlot", pair.OutOfPossessionSlot.ToString());
            Add(command, "$inPossessionFormation", SafeText(pair.InPossessionFormation, string.Empty));
            Add(command, "$outOfPossessionFormation", SafeText(pair.OutOfPossessionFormation, string.Empty));
            Add(command, "$transitionComplexityScore", ClampScore(pair.TransitionComplexityScore));
            Add(command, "$tacticalRiskScore", ClampScore(pair.TacticalRiskScore));
            Add(command, "$positionalFamiliarityNeed", SafeText(pair.PositionalFamiliarityNeed, string.Empty));
            Add(command, "$updatedAtUtc", updatedAtUtc.ToString("O"));
            Add(command, "$isArchived", Bool(pair.IsArchived));
        }

        private static void AddRequirementParameters(SqliteCommand command, RoleOutputMetricRequirementModel requirement)
        {
            Add(command, "$tacticalRoleId", requirement.TacticalRoleId);
            Add(command, "$rolePairId", requirement.RolePairId);
            Add(command, "$metricKey", SafeText(requirement.MetricKey, "Output"));
            Add(command, "$fieldName", SafeText(requirement.FieldName, "Output"));
            Add(command, "$weight", requirement.Weight);
            Add(command, "$importance", requirement.Importance.ToString());
            Add(command, "$direction", requirement.Direction.ToString());
            Add(command, "$minimumSampleMinutes", Math.Max(0, requirement.MinimumSampleMinutes));
            Add(command, "$per90Required", Bool(requirement.Per90Required));
            Add(command, "$normalizationHint", SafeText(requirement.NormalizationHint, string.Empty));
            Add(command, "$evidenceTemplate", SafeText(requirement.EvidenceTemplate, string.Empty));
            Add(command, "$missingDataImpact", SafeText(requirement.MissingDataImpact, string.Empty));
        }

        private static TacticalRoleModel ReadRole(SqliteDataReader reader)
        {
            return new TacticalRoleModel(
                reader.GetInt64(0),
                ReadString(reader, 1),
                ParseEnum(ReadString(reader, 2), TacticalPhase.InPossession),
                ParseEnum(ReadString(reader, 3), TacticalRoleFamily.BuildUp),
                ParseEnum(ReadString(reader, 4), TacticalRoleSource.BuiltInSeed),
                ReadBool(reader, 5),
                ReadString(reader, 6),
                ReadString(reader, 7),
                SplitValues(ReadString(reader, 8)).Select(value => ParseEnum(value, TacticalSlot.CMC)).ToList(),
                ReadString(reader, 9),
                ReadString(reader, 10),
                ReadString(reader, 11),
                ReadString(reader, 12),
                ReadString(reader, 13),
                ReadString(reader, 14),
                ParseDate(ReadString(reader, 15)),
                ParseDate(ReadString(reader, 16)),
                ReadBool(reader, 17));
        }

        private static TacticalRolePairModel ReadPair(SqliteDataReader reader)
        {
            return new TacticalRolePairModel(
                reader.GetInt64(0),
                ReadString(reader, 1),
                reader.GetInt64(2),
                reader.GetInt64(3),
                ParseEnum(ReadString(reader, 4), TacticalSlot.CMC),
                ParseEnum(ReadString(reader, 5), TacticalSlot.CMC),
                ReadString(reader, 6),
                ReadString(reader, 7),
                reader.GetInt32(8),
                reader.GetInt32(9),
                ReadString(reader, 10),
                ParseDate(ReadString(reader, 11)),
                ParseDate(ReadString(reader, 12)),
                ReadBool(reader, 13));
        }

        private static RoleOutputMetricRequirementModel ReadRequirement(SqliteDataReader reader)
        {
            return new RoleOutputMetricRequirementModel(
                reader.GetInt64(0),
                ReadNullableLong(reader, 1),
                ReadNullableLong(reader, 2),
                ReadString(reader, 3),
                ReadString(reader, 4),
                reader.GetDouble(5),
                ParseEnum(ReadString(reader, 6), RoleMetricImportance.Core),
                ParseEnum(ReadString(reader, 7), RoleMetricDirection.HigherBetter),
                reader.GetInt32(8),
                ReadBool(reader, 9),
                ReadString(reader, 10),
                ReadString(reader, 11),
                ReadString(reader, 12));
        }

        private static RoleScoutQuestionModel ReadQuestion(SqliteDataReader reader)
        {
            return new RoleScoutQuestionModel(
                reader.GetInt64(0),
                ReadNullableLong(reader, 1),
                ReadNullableLong(reader, 2),
                ReadString(reader, 3),
                ReadString(reader, 4),
                ReadString(reader, 5),
                ReadString(reader, 6));
        }

        private static RoleRedFlagModel ReadRedFlag(SqliteDataReader reader)
        {
            return new RoleRedFlagModel(
                reader.GetInt64(0),
                ReadNullableLong(reader, 1),
                ReadNullableLong(reader, 2),
                ReadString(reader, 3),
                ReadString(reader, 4),
                ReadString(reader, 5),
                ReadString(reader, 6),
                ParseEnum(ReadString(reader, 7), TacticalPhase.InPossession));
        }

        private static TacticalRoleModel SanitizeRole(TacticalRoleModel role)
        {
            var source = role.Source == TacticalRoleSource.FM26Mapped && !role.IsOfficialFm26Role ? TacticalRoleSource.Imported : role.Source;
            return new TacticalRoleModel(
                role.Id,
                SafeText(role.RoleName, "Untitled role"),
                role.TacticalPhase,
                role.RoleFamily,
                source,
                role.IsOfficialFm26Role && source == TacticalRoleSource.FM26Mapped,
                role.IsOfficialFm26Role && source == TacticalRoleSource.FM26Mapped ? SafeText(role.Fm26RoleId, string.Empty) : string.Empty,
                SafeText(role.PositionGroup, "Unknown"),
                role.ValidSlots,
                SafeText(role.MovementBehaviour, string.Empty),
                SafeText(role.BuildUpBehaviour, string.Empty),
                SafeText(role.FinalThirdBehaviour, string.Empty),
                SafeText(role.PressingBehaviour, string.Empty),
                SafeText(role.DefensiveBlockBehaviour, string.Empty),
                SafeText(role.TransitionBehaviour, string.Empty),
                role.CreatedAtUtc,
                role.UpdatedAtUtc,
                role.IsArchived);
        }

        private static TacticalRolePairModel SanitizePair(TacticalRolePairModel pair)
        {
            return new TacticalRolePairModel(
                pair.Id,
                SafeText(pair.PairName, "Untitled pair"),
                pair.InPossessionRoleId,
                pair.OutOfPossessionRoleId,
                pair.InPossessionSlot,
                pair.OutOfPossessionSlot,
                SafeText(pair.InPossessionFormation, string.Empty),
                SafeText(pair.OutOfPossessionFormation, string.Empty),
                ClampScore(pair.TransitionComplexityScore),
                ClampScore(pair.TacticalRiskScore),
                SafeText(pair.PositionalFamiliarityNeed, string.Empty),
                pair.CreatedAtUtc,
                pair.UpdatedAtUtc,
                pair.IsArchived);
        }

        private static RoleOutputMetricRequirementModel SanitizeRequirement(RoleOutputMetricRequirementModel requirement)
        {
            return new RoleOutputMetricRequirementModel(
                requirement.Id,
                requirement.TacticalRoleId,
                requirement.RolePairId,
                SafeText(requirement.MetricKey, "Output"),
                SafeText(requirement.FieldName, "Output"),
                requirement.Weight,
                requirement.Importance,
                requirement.Direction,
                Math.Max(0, requirement.MinimumSampleMinutes),
                requirement.Per90Required,
                SafeText(requirement.NormalizationHint, string.Empty),
                SafeText(requirement.EvidenceTemplate, string.Empty),
                SafeText(requirement.MissingDataImpact, string.Empty));
        }

        private static RoleScoutQuestionModel SanitizeQuestion(RoleScoutQuestionModel question)
        {
            return new RoleScoutQuestionModel(
                question.Id,
                question.TacticalRoleId,
                question.RolePairId,
                SafeText(question.Category, "RoleFit"),
                SafeText(question.Question, "Observe role behaviour directly."),
                SafeText(question.WhyItMatters, string.Empty),
                SafeText(question.SuggestedObservationType, "RoleFit"));
        }

        private static RoleRedFlagModel SanitizeRedFlag(RoleRedFlagModel redFlag)
        {
            return new RoleRedFlagModel(
                redFlag.Id,
                redFlag.TacticalRoleId,
                redFlag.RolePairId,
                SafeText(redFlag.FieldName, "Output"),
                SafeText(redFlag.Operator, "Review"),
                SafeText(redFlag.Threshold, string.Empty),
                SafeText(redFlag.Message, "Review role-output context."),
                redFlag.AppliesToPhase);
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

        private static void EnsureRoleExists(SqliteConnection connection, long roleId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM TacticalRole WHERE Id = $id;";
                Add(command, "$id", roleId);
                if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                {
                    throw new InvalidOperationException("Role Lab pair references a missing persisted role.");
                }
            }
        }

        private static long? FindRoleId(SqliteConnection connection, string roleName, TacticalPhase phase)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT Id FROM TacticalRole
                      WHERE lower(RoleName) = lower($roleName)
                        AND TacticalPhase = $phase
                      ORDER BY Id ASC
                      LIMIT 1;";
                Add(command, "$roleName", roleName ?? string.Empty);
                Add(command, "$phase", phase.ToString());
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static void ValidateOwner(long? tacticalRoleId, long? rolePairId)
        {
            if (!tacticalRoleId.HasValue && !rolePairId.HasValue)
            {
                throw new InvalidOperationException("Role Lab child rows require a role or role pair owner.");
            }

            if (tacticalRoleId.HasValue && rolePairId.HasValue)
            {
                throw new InvalidOperationException("Role Lab child rows can belong to a role or a pair, not both.");
            }
        }

        private void DeleteOwnedRows(string tableName, string columnName, long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM " + tableName + " WHERE " + columnName + " = $id;";
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }
        }

        private void DeleteById(string tableName, long id)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM " + tableName + " WHERE Id = $id;";
                Add(command, "$id", id);
                command.ExecuteNonQuery();
            }
        }

        private static long? ReadNullableLong(SqliteDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (long?)null : reader.GetInt64(ordinal);
        }

        private static DateTimeOffset ParseDate(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
        }

        private static int ClampScore(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }
    }
}
