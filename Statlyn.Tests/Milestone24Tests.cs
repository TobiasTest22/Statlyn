using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.Data.RoleLab;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone24Tests
    {
        [Fact]
        public void RoleLabEnumsAndModelsArePhaseBasedAndSafe()
        {
            var labels = string.Join(" ", Enum.GetNames(typeof(TacticalPhase))
                .Concat(Enum.GetNames(typeof(TacticalRoleSource)))
                .Concat(Enum.GetNames(typeof(TacticalRoleFamily)))
                .Concat(Enum.GetNames(typeof(TacticalSlot))));
            var role = SampleRole("Phase Builder", TacticalPhase.InPossession, TacticalRoleFamily.BuildUp);
            var pair = new TacticalRolePairModel(0, "Different slot pair", 1, 2, TacticalSlot.AMR, TacticalSlot.CMR, "3-2-5", "4-4-2", 60, 55, "Needs channel familiarity.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false);

            Assert.Contains("InPossession", labels);
            Assert.Contains("OutOfPossession", labels);
            Assert.Contains("DualPhasePair", labels);
            Assert.Contains("BuiltInSeed", labels);
            Assert.Contains("UserCreated", labels);
            Assert.False(role.IsOfficialFm26Role);
            Assert.Equal(TacticalRoleSource.UserCreated, role.Source);
            Assert.NotEqual(pair.InPossessionSlot, pair.OutOfPossessionSlot);
            Assert.DoesNotContain("Duty", labels, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CurrentAbility", labels, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", labels, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", labels, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RoleLabSchemaContainsSafeTablesIndexesAndDefaults()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);
            using var factory = RuntimeDatabaseFactory.CreateInMemory();

            new StatlynDatabaseInitializer(factory).Initialize();
            new StatlynDatabaseInitializer(factory).Initialize();

            Assert.Contains("CREATE TABLE IF NOT EXISTS TacticalRole", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS TacticalRolePair", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS RoleOutputMetricRequirement", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS RoleScoutQuestion", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS RoleRedFlag", schema);
            Assert.Contains("IsOfficialFm26Role INTEGER NOT NULL DEFAULT 0", schema);
            Assert.Contains("InPossessionRoleId INTEGER NOT NULL", schema);
            Assert.Contains("OutOfPossessionRoleId INTEGER NOT NULL", schema);
            Assert.Contains("IX_TacticalRole_RoleName", schema);
            Assert.Contains("IX_TacticalRole_TacticalPhase", schema);
            Assert.Contains("IX_TacticalRole_RoleFamily", schema);
            Assert.Contains("IX_TacticalRole_Source", schema);
            Assert.Contains("IX_TacticalRolePair_PairName", schema);
            Assert.Contains("IX_RoleOutputMetricRequirement_TacticalRoleId", schema);
            Assert.Contains("IX_RoleOutputMetricRequirement_RolePairId", schema);
            Assert.DoesNotContain("CurrentAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HiddenPersonality", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RawValue", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics().SchemaVersion);
        }

        [Fact]
        public void RoleLabRepositorySavesLoadsArchivesAndSanitizes()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var repository = new RoleLabRepository(factory);

            var ip = repository.SaveRole(SampleRole("CurrentAbility: 200 Wide Builder", TacticalPhase.InPossession, TacticalRoleFamily.WideAttacker));
            var oop = repository.SaveRole(SampleRole("Pressing Holder", TacticalPhase.OutOfPossession, TacticalRoleFamily.HighPress));
            var pair = repository.SaveRolePair(new TacticalRolePairModel(0, "CA 155 Pair", ip.Id, oop.Id, TacticalSlot.AMR, TacticalSlot.CMR, "3-2-5", "4-4-2", 55, 44, "Professionalism: 20", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false));
            var metric = repository.SaveMetricRequirement(new RoleOutputMetricRequirementModel(0, ip.Id, null, "xA", "xA", 2.0, RoleMetricImportance.Core, RoleMetricDirection.HigherBetter, 900, true, "Generic", "xA supports chance creation.", "Missing xA lowers confidence."));
            var question = repository.SaveScoutQuestion(new RoleScoutQuestionModel(0, ip.Id, null, "RoleFit", "Does he create chances? PA=180", "Watch visible behaviour.", "Tactical"));
            var redFlag = repository.SaveRedFlag(new RoleRedFlagModel(0, ip.Id, null, "xA", "missing", "review", "Consistency 17 should redact.", TacticalPhase.InPossession));
            var stored = StoredRoleLabText(factory);

            Assert.NotEqual(0, ip.Id);
            Assert.NotEqual(0, oop.Id);
            Assert.NotEqual(0, pair.Id);
            Assert.NotEqual(pair.InPossessionSlot, pair.OutOfPossessionSlot);
            Assert.Equal(TacticalPhase.InPossession, repository.LoadRole(ip.Id)!.TacticalPhase);
            Assert.Single(repository.LoadRolesByPhase(TacticalPhase.OutOfPossession));
            Assert.Single(repository.LoadMetricRequirementsForRole(ip.Id));
            Assert.Single(repository.LoadScoutQuestionsForRole(ip.Id));
            Assert.Single(repository.LoadRedFlagsForRole(ip.Id));
            Assert.Equal(metric.Id, repository.LoadMetricRequirementsForRole(ip.Id)[0].Id);
            Assert.Equal(question.Id, repository.LoadScoutQuestionsForRole(ip.Id)[0].Id);
            Assert.Equal(redFlag.Id, repository.LoadRedFlagsForRole(ip.Id)[0].Id);
            Assert.DoesNotContain("CurrentAbility: 200", stored);
            Assert.DoesNotContain("CA 155", stored);
            Assert.DoesNotContain("Professionalism: 20", stored);
            Assert.DoesNotContain("PA=180", stored);
            Assert.DoesNotContain("Consistency 17", stored);

            repository.ArchiveRole(ip.Id);
            Assert.DoesNotContain(repository.LoadRoles(includeArchived: false), role => role.Id == ip.Id);
            Assert.Contains(repository.LoadRoles(includeArchived: true), role => role.Id == ip.Id);
            Assert.Throws<InvalidOperationException>(() => repository.SaveRole(TestPlayers.CreateExternalPlayer()));
        }

        [Fact]
        public void RoleLabSeedCreatesSafeIdempotentTemplates()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var service = new RoleLabSeedService(factory);
            var repository = new RoleLabRepository(factory);

            var first = service.SeedBuiltInRoles();
            var second = service.SeedBuiltInRoles();
            var roles = repository.LoadRoles(includeArchived: false);
            var text = StoredRoleLabText(factory);

            Assert.True(first.InPossessionRoles >= 10);
            Assert.True(first.OutOfPossessionRoles >= 10);
            Assert.Equal(first.TotalRoles, second.TotalRoles);
            Assert.Equal(first.TotalRoles, roles.Count);
            Assert.Contains(roles, role => role.RoleName == "Wide Forward" && role.TacticalPhase == TacticalPhase.InPossession);
            Assert.Contains(roles, role => role.RoleName == "Pressing Defensive Midfielder" && role.TacticalPhase == TacticalPhase.OutOfPossession);
            Assert.All(roles, role => Assert.False(role.IsOfficialFm26Role));
            Assert.All(roles, role => Assert.Equal(TacticalRoleSource.BuiltInSeed, role.Source));
            Assert.All(roles, role => Assert.NotEmpty(repository.LoadMetricRequirementsForRole(role.Id)));
            Assert.All(roles, role => Assert.NotEmpty(repository.LoadScoutQuestionsForRole(role.Id)));
            Assert.All(roles, role => Assert.NotEmpty(repository.LoadRedFlagsForRole(role.Id)));
            Assert.DoesNotContain("Duty", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Mezzala", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Enganche", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Trequartista", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("official FM26 mapping", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RoleLabBridgeFeedsOutputSummariesAndExistingFallbacksRemain()
        {
            using var factory = CreateImportedDatabase();
            new RoleLabSeedService(factory).SeedBuiltInRoles();
            var repository = new RoleLabRepository(factory);
            var wideRole = repository.LoadRoleByName("Wide Forward")!;
            var pressingRole = repository.LoadRoleByName("Pressing Defensive Midfielder")!;
            var pair = repository.SaveRolePair(new TacticalRolePairModel(0, "Wide Forward Press Pair", wideRole.Id, pressingRole.Id, TacticalSlot.AMR, TacticalSlot.CMR, "3-2-5", "4-4-2", 50, 45, "Needs wide-to-central transition familiarity.", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false));
            var bridge = new RoleLabOutputProfileBridge(factory);
            var roleProfile = bridge.CreateProfileForRole(wideRole);
            var pairProfile = bridge.CreateProfileForPair(pair);
            var wideId = StatlynPlayerIdByName(factory, "Synthetic Wide Player");
            var profile = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = wideId, OptionalRoleOutputProfileName = "Wide Forward" });
            var fallback = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { Limit = 10 });

            Assert.Equal("WideAttacker", roleProfile.RoleFamily);
            Assert.Equal("DualPhasePair", pairProfile.TacticalPhase);
            Assert.NotEmpty(pairProfile.MetricExpectations);
            Assert.True(profile.Success);
            Assert.Equal("WideAttacker", profile.RoleOutputSummary!.RoleFamily);
            Assert.Contains(profile.RoleOutputSummary.CoreMetrics.Concat(profile.RoleOutputSummary.MissingCoreMetrics), item => item.Contains("xA", StringComparison.OrdinalIgnoreCase));
            Assert.NotEmpty(fallback.Players);
        }

        [Fact]
        public void RoleLabWorkflowBuildsSafeViewModelsAndAddsChildren()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var workflow = new RoleLabWorkflowService(factory);

            var seed = workflow.SeedBuiltInRoles();
            var roleResult = workflow.CreateUserRole(new CreateTacticalRoleRequest
            {
                RoleName = "Professionalism: 20 Connector",
                TacticalPhase = TacticalPhase.InPossession,
                RoleFamily = TacticalRoleFamily.CentralMidfield,
                PositionGroup = "CentralMidfield",
                ValidSlots = new[] { TacticalSlot.CMC },
                MovementBehaviour = "Finds pockets.",
                BuildUpBehaviour = "CA 155",
                PressingBehaviour = "Protects central access."
            });
            var role = roleResult.Role!;
            workflow.AddMetricRequirement(new RoleOutputMetricRequirementModel(0, role.Id, null, "ProgressivePasses", "ProgressivePasses", 2.0, RoleMetricImportance.Core, RoleMetricDirection.HigherBetter, 900, true, "Generic", "Progression evidence.", "Missing progression lowers confidence."));
            workflow.AddScoutQuestion(new RoleScoutQuestionModel(0, role.Id, null, "BuildUp", "Does he progress under pressure? PA=180", "Observe behaviour.", "Tactical"));
            workflow.AddRedFlag(new RoleRedFlagModel(0, role.Id, null, "ProgressivePasses", "missing", "review", "CurrentAbility: 200", TacticalPhase.InPossession));
            var pairResult = workflow.CreateRolePair(new CreateTacticalRolePairRequest
            {
                PairName = "Connector Press Pair",
                InPossessionRoleId = role.Id,
                OutOfPossessionRoleId = new RoleLabRepository(factory).LoadRoleByName("High Press Role")!.Id,
                InPossessionSlot = TacticalSlot.CMC,
                OutOfPossessionSlot = TacticalSlot.DM,
                TransitionComplexityScore = 64,
                TacticalRiskScore = 51
            });
            var page = workflow.BuildPageViewModel(includeArchived: false);
            var detail = workflow.BuildRoleDetailViewModel(role.Id)!;
            var text = seed.SafeMessage + " " + WorkflowText(roleResult) + " " + WorkflowText(pairResult) + " " + PageText(page) + " " + DetailText(detail);

            Assert.True(roleResult.Success);
            Assert.True(pairResult.Success);
            Assert.NotNull(page.SelectedRole);
            Assert.Contains(page.Roles, item => item.RoleId == role.Id);
            Assert.Contains(page.RolePairs, item => item.PairId == pairResult.Pair!.Id);
            Assert.Contains(detail.MetricRequirements, item => item.FieldName == "ProgressivePasses");
            Assert.DoesNotContain("Professionalism: 20", text);
            Assert.DoesNotContain("CA 155", text);
            Assert.DoesNotContain("PA=180", text);
            Assert.DoesNotContain("CurrentAbility: 200", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("IRawFootballEntity", text);
            Assert.DoesNotContain("Official FM26 mapping", text);
        }

        [Fact]
        public void ScoutDeskUsesRoleLabQuestionsWhenAssignmentRoleMatches()
        {
            using var factory = CreateImportedDatabase();
            new RoleLabSeedService(factory).SeedBuiltInRoles();
            var wideId = StatlynPlayerIdByName(factory, "Synthetic Wide Player");
            var scout = new ScoutDeskWorkflowService(factory);

            var matched = scout.CreateAssignment(new CreateScoutAssignmentRequest
            {
                StatlynPlayerId = wideId,
                RoleName = "Wide Forward",
                Priority = ShortlistPriority.High
            });
            var matchedDetail = scout.BuildAssignmentDetailViewModel(matched.Assignment!.Id);

            var fallback = scout.CreateAssignment(new CreateScoutAssignmentRequest
            {
                StatlynPlayerId = wideId,
                RoleName = "Unseeded Safe Role",
                Priority = ShortlistPriority.Medium
            });
            var fallbackDetail = scout.BuildAssignmentDetailViewModel(fallback.Assignment!.Id);
            var matchedText = string.Join(" ", matchedDetail.Questions.Select(question => question.Category + " " + question.Question + " " + question.WhyItMatters));
            var fallbackText = string.Join(" ", fallbackDetail.Questions.Select(question => question.Category + " " + question.Question + " " + question.WhyItMatters));

            Assert.Contains("Role Lab questions validate visible phase behaviour", matchedText);
            Assert.Contains("Does he create chances", matchedText);
            Assert.Contains("wide or half-space", fallbackText);
            Assert.DoesNotContain("CurrentAbility", matchedText + " " + fallbackText);
            Assert.DoesNotContain("Professionalism", matchedText + " " + fallbackText);
        }

        private static TacticalRoleModel SampleRole(string name, TacticalPhase phase, TacticalRoleFamily family)
        {
            return new TacticalRoleModel(
                0,
                name,
                phase,
                family,
                TacticalRoleSource.UserCreated,
                false,
                string.Empty,
                family == TacticalRoleFamily.WideAttacker ? "WingerWideForward" : "CentralMidfield",
                new[] { TacticalSlot.CMC, TacticalSlot.AMR },
                "Move into safe support spaces.",
                "Progress possession through visible output.",
                "Support final-third entries.",
                "Press on visible triggers.",
                "Protect compactness.",
                "Recover into team shape.",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                false);
        }

        private static StatlynDbConnectionFactory CreateImportedDatabase()
        {
            var factory = RuntimeDatabaseFactory.CreateInMemory();
            new ImportPipelineService(factory).Import(CreateCsvProvider(FixturePath()), ImportPipelineOptions.CreateDefault());
            return factory;
        }

        private static CsvImportProvider CreateCsvProvider(string path)
        {
            return new CsvImportProvider(path, CreateFixtureMetadata(), new FieldMappingSet(Array.Empty<FieldMapping>()));
        }

        private static SourceMetadata CreateFixtureMetadata()
        {
            return new SourceMetadata(
                "Synthetic CSV fixture",
                ProviderType.Csv,
                false,
                true,
                "synthetic test fixture",
                "development fixture only",
                false,
                false,
                true,
                false,
                true,
                DateTimeOffset.UtcNow,
                80);
        }

        private static string FixturePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
        }

        private static string StatlynPlayerIdByName(StatlynDbConnectionFactory factory, string name)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT StatlynPlayerId FROM Player WHERE DisplayName = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static string StoredRoleLabText(StatlynDbConnectionFactory factory)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                @"SELECT
                    COALESCE((SELECT group_concat(RoleName || ' ' || TacticalPhase || ' ' || RoleFamily || ' ' || Source || ' ' || IsOfficialFm26Role || ' ' || Fm26RoleId || ' ' || PositionGroup || ' ' || ValidSlots || ' ' || MovementBehaviour || ' ' || BuildUpBehaviour || ' ' || FinalThirdBehaviour || ' ' || PressingBehaviour || ' ' || DefensiveBlockBehaviour || ' ' || TransitionBehaviour, ' ') FROM TacticalRole), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(PairName || ' ' || InPossessionSlot || ' ' || OutOfPossessionSlot || ' ' || PositionalFamiliarityNeed, ' ') FROM TacticalRolePair), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(MetricKey || ' ' || FieldName || ' ' || Importance || ' ' || Direction || ' ' || NormalizationHint || ' ' || EvidenceTemplate || ' ' || MissingDataImpact, ' ') FROM RoleOutputMetricRequirement), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(Category || ' ' || Question || ' ' || WhyItMatters || ' ' || SuggestedObservationType, ' ') FROM RoleScoutQuestion), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(FieldName || ' ' || Operator || ' ' || Threshold || ' ' || Message || ' ' || AppliesToPhase, ' ') FROM RoleRedFlag), '');";
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static string WorkflowText(RoleLabWorkflowResult result)
        {
            return result.SafeMessage + " " + string.Join(" ", result.Warnings) + " " + string.Join(" ", result.Errors);
        }

        private static string PageText(RoleLabPageViewModel page)
        {
            var builder = new StringBuilder();
            builder.Append(page.SafeMessage).Append(' ')
                .Append(string.Join(" ", page.Roles.Select(role => role.RoleName + " " + role.Phase + " " + role.Family + " " + role.Source + " " + role.OfficialStatus + " " + role.ValidSlots))).Append(' ')
                .Append(string.Join(" ", page.RolePairs.Select(pair => pair.PairName + " " + pair.InPossessionRole + " " + pair.OutOfPossessionRole + " " + pair.PositionalFamiliarityNeed)));
            return builder.ToString();
        }

        private static string DetailText(TacticalRoleDetailViewModel detail)
        {
            return detail.SafeNotice + " " +
                   detail.Role.RoleName + " " +
                   detail.MovementBehaviour + " " +
                   detail.BuildUpBehaviour + " " +
                   detail.FinalThirdBehaviour + " " +
                   detail.PressingBehaviour + " " +
                   detail.DefensiveBlockBehaviour + " " +
                   detail.TransitionBehaviour + " " +
                   string.Join(" ", detail.MetricRequirements.Select(item => item.MetricKey + " " + item.FieldName + " " + item.EvidenceTemplate + " " + item.MissingDataImpact)) + " " +
                   string.Join(" ", detail.ScoutQuestions.Select(item => item.Category + " " + item.Question + " " + item.WhyItMatters)) + " " +
                   string.Join(" ", detail.RedFlags.Select(item => item.FieldName + " " + item.Operator + " " + item.Threshold + " " + item.Message));
        }
    }
}
