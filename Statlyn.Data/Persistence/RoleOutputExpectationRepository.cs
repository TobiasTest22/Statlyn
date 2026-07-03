using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Statlyn.Data.Persistence
{
    public sealed class RoleOutputExpectationRepository : SqliteRepository
    {
        public RoleOutputExpectationRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public int SeedGenericDefaults()
        {
            var count = 0;
            foreach (var profile in GenericRoleOutputExpectationSeed.Create())
            {
                Save(profile);
                count++;
            }

            return count;
        }

        public void Save(RoleOutputExpectationProfile profile)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO RoleOutputExpectationProfile (
                            ProfileName, PositionGroup, RoleFamily, TacticalPhase, IsFm26Specific, IsGenericTemplate,
                            AttributeSupportWeights, ScoutQuestionPrompts, RedFlagRules, MinimumSampleRules, Notes)
                          VALUES (
                            $profileName, $positionGroup, $roleFamily, $tacticalPhase, $isFm26Specific, $isGenericTemplate,
                            $attributeSupportWeights, $scoutQuestionPrompts, $redFlagRules, $minimumSampleRules, $notes)
                          ON CONFLICT(ProfileName) DO UPDATE SET
                            PositionGroup = excluded.PositionGroup,
                            RoleFamily = excluded.RoleFamily,
                            TacticalPhase = excluded.TacticalPhase,
                            IsFm26Specific = excluded.IsFm26Specific,
                            IsGenericTemplate = excluded.IsGenericTemplate,
                            AttributeSupportWeights = excluded.AttributeSupportWeights,
                            ScoutQuestionPrompts = excluded.ScoutQuestionPrompts,
                            RedFlagRules = excluded.RedFlagRules,
                            MinimumSampleRules = excluded.MinimumSampleRules,
                            Notes = excluded.Notes;";
                    Add(command, "$profileName", profile.ProfileName);
                    Add(command, "$positionGroup", profile.PositionGroup);
                    Add(command, "$roleFamily", profile.RoleFamily);
                    Add(command, "$tacticalPhase", profile.TacticalPhase);
                    Add(command, "$isFm26Specific", Bool(profile.IsFm26Specific));
                    Add(command, "$isGenericTemplate", Bool(profile.IsGenericTemplate));
                    Add(command, "$attributeSupportWeights", profile.AttributeSupportWeights);
                    Add(command, "$scoutQuestionPrompts", profile.ScoutQuestionPrompts);
                    Add(command, "$redFlagRules", profile.RedFlagRules);
                    Add(command, "$minimumSampleRules", profile.MinimumSampleRules);
                    Add(command, "$notes", profile.Notes);
                    command.ExecuteNonQuery();
                }

                using (var delete = connection.CreateCommand())
                {
                    delete.CommandText = "DELETE FROM RoleOutputMetricExpectation WHERE ProfileName = $profileName;";
                    Add(delete, "$profileName", profile.ProfileName);
                    delete.ExecuteNonQuery();
                }

                foreach (var expectation in profile.MetricExpectations)
                {
                    SaveExpectation(connection, profile.ProfileName, expectation);
                }
            }
        }

        public IReadOnlyList<RoleOutputExpectationProfile> LoadAll()
        {
            var profiles = new List<RoleOutputExpectationProfile>();
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT ProfileName, PositionGroup, RoleFamily, TacticalPhase, IsFm26Specific, IsGenericTemplate,
                             AttributeSupportWeights, ScoutQuestionPrompts, RedFlagRules, MinimumSampleRules, Notes
                      FROM RoleOutputExpectationProfile
                      ORDER BY ProfileName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var profileName = reader.GetString(0);
                        profiles.Add(new RoleOutputExpectationProfile(
                            profileName,
                            reader.GetString(1),
                            reader.GetString(2),
                            ReadString(reader, 3),
                            ReadBool(reader, 4),
                            ReadBool(reader, 5),
                            LoadExpectations(connection, profileName),
                            reader.GetString(6),
                            reader.GetString(7),
                            reader.GetString(8),
                            reader.GetString(9),
                            reader.GetString(10)));
                    }
                }
            }

            return profiles;
        }

        public RoleOutputExpectationProfile? FindByName(string profileName)
        {
            return LoadAll().FirstOrDefault(profile => string.Equals(profile.ProfileName, profileName, StringComparison.OrdinalIgnoreCase));
        }

        private static void SaveExpectation(SqliteConnection connection, string profileName, MetricExpectation expectation)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO RoleOutputMetricExpectation (
                        ProfileName, MetricKey, FieldName, Weight, Importance, Direction, MinimumSampleMinutes,
                        Per90Required, NormalizationHint, EvidenceTemplate, MissingDataImpact)
                      VALUES (
                        $profileName, $metricKey, $fieldName, $weight, $importance, $direction, $minimumSampleMinutes,
                        $per90Required, $normalizationHint, $evidenceTemplate, $missingDataImpact);";
                Add(command, "$profileName", profileName);
                Add(command, "$metricKey", expectation.MetricKey);
                Add(command, "$fieldName", expectation.FieldName);
                Add(command, "$weight", expectation.Weight);
                Add(command, "$importance", expectation.Importance);
                Add(command, "$direction", expectation.Direction);
                Add(command, "$minimumSampleMinutes", expectation.MinimumSampleMinutes);
                Add(command, "$per90Required", Bool(expectation.Per90Required));
                Add(command, "$normalizationHint", expectation.NormalizationHint);
                Add(command, "$evidenceTemplate", expectation.EvidenceTemplate);
                Add(command, "$missingDataImpact", expectation.MissingDataImpact);
                command.ExecuteNonQuery();
            }
        }

        private static IReadOnlyList<MetricExpectation> LoadExpectations(SqliteConnection connection, string profileName)
        {
            var expectations = new List<MetricExpectation>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT MetricKey, FieldName, Weight, Importance, Direction, MinimumSampleMinutes,
                             Per90Required, NormalizationHint, EvidenceTemplate, MissingDataImpact
                      FROM RoleOutputMetricExpectation
                      WHERE ProfileName = $profileName
                      ORDER BY Id;";
                Add(command, "$profileName", profileName);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        expectations.Add(new MetricExpectation(
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetDouble(2),
                            reader.GetString(3),
                            reader.GetString(4),
                            reader.GetInt32(5),
                            ReadBool(reader, 6),
                            reader.GetString(7),
                            reader.GetString(8),
                            reader.GetString(9)));
                    }
                }
            }

            return expectations;
        }
    }
}
