using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone23Tests
    {
        [Fact]
        public void ScoutDeskEnumsSerializeWithoutHiddenTerminology()
        {
            var labels = string.Join(" ", Enum.GetNames(typeof(ScoutReportRecommendation))
                .Concat(Enum.GetNames(typeof(ScoutObservationRating)))
                .Concat(Enum.GetNames(typeof(ScoutAssignmentStatus)))
                .Concat(Enum.GetNames(typeof(ScoutFollowUpAction))));

            Assert.Contains("StrongTarget", labels);
            Assert.Contains("VeryGood", labels);
            Assert.Contains("ReportSubmitted", labels);
            Assert.Contains("CheckMedical", labels);
            Assert.DoesNotContain("CurrentAbility", labels);
            Assert.DoesNotContain("PotentialAbility", labels);
            Assert.DoesNotContain("Professionalism", labels);
            Assert.DoesNotContain("RawValue", labels);
        }

        [Fact]
        public void ScoutDeskSchemaContainsSafeTablesIndexesAndIsIdempotent()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);
            using var factory = RuntimeDatabaseFactory.CreateInMemory();

            new StatlynDatabaseInitializer(factory).Initialize();
            new StatlynDatabaseInitializer(factory).Initialize();

            Assert.Contains("CREATE TABLE IF NOT EXISTS ScoutAssignment", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS ScoutReport", schema);
            Assert.Contains("CREATE TABLE IF NOT EXISTS ScoutReportQuestion", schema);
            Assert.Contains("IX_ScoutAssignment_StatlynPlayerId", schema);
            Assert.Contains("IX_ScoutAssignment_Status", schema);
            Assert.Contains("IX_ScoutAssignment_ShortlistId", schema);
            Assert.Contains("IX_ScoutReport_StatlynPlayerId", schema);
            Assert.Contains("IX_ScoutReport_AssignmentId", schema);
            Assert.Contains("IX_ScoutReport_ReportDateUtc", schema);
            Assert.Contains("Strengths TEXT NOT NULL", schema);
            Assert.Contains("MentalRating TEXT NOT NULL", schema);
            Assert.DoesNotContain("CurrentAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RawValue", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HiddenPersonality", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics().SchemaVersion);
            Assert.Equal(1, CountRows(factory, "SchemaVersion"));
        }

        [Fact]
        public void ScoutTextSanitizerRedactsHiddenAssignmentsButPreservesQualitativeNotes()
        {
            var sanitized = ScoutTextSanitizer.Sanitize("Professionalism: 20 CA 155 PA=180 Pressure = 18 Consistency 17");
            var qualitative = ScoutTextSanitizer.Sanitize("He looks professional and composed. Handles pressure well.");

            Assert.DoesNotContain("Professionalism: 20", sanitized);
            Assert.DoesNotContain("CA 155", sanitized);
            Assert.DoesNotContain("PA=180", sanitized);
            Assert.DoesNotContain("Pressure = 18", sanitized);
            Assert.DoesNotContain("Consistency 17", sanitized);
            Assert.DoesNotContain("155", sanitized);
            Assert.DoesNotContain("180", sanitized);
            Assert.Equal("He looks professional and composed. Handles pressure well.", qualitative);
        }

        [Fact]
        public void ScoutDeskRepositoryCreatesReportsLoadsLatestAndArchivesSafely()
        {
            using var factory = CreateImportedDatabase();
            var repository = new ScoutDeskRepository(factory);
            var forwardId = StatlynPlayerIdByName(factory, "Synthetic Forward");

            var assignment = repository.CreateAssignment(forwardId, null, "Advanced Forward", ShortlistPriority.High, string.Empty, null);
            var report = repository.CreateReport(new SubmitScoutReportRequest
            {
                AssignmentId = assignment.Id,
                StatlynPlayerId = forwardId,
                RoleAssessed = "Advanced Forward",
                TechnicalRating = ScoutObservationRating.Good,
                TacticalRating = ScoutObservationRating.Average,
                PhysicalRating = ScoutObservationRating.VeryGood,
                MentalRating = ScoutObservationRating.Good,
                OverallRecommendation = ScoutReportRecommendation.Shortlist,
                Confidence = 82,
                Strengths = "He looks professional and composed. Professionalism: 20",
                Weaknesses = "Handles pressure well. CA 155",
                Risks = "Needs another watch. PA=180",
                FollowUpAction = ScoutFollowUpAction.WatchMore,
                FinalSummary = "Composed under pressure and worth another live watch. Consistency 17",
                QuestionAnswers = new[]
                {
                    new ScoutQuestionAnswerRequest
                    {
                        Category = "RoleFit",
                        Question = "Does he get into good shooting positions?",
                        Answer = "Yes, but CurrentAbility: 200 should not survive."
                    }
                }
            });

            var latest = repository.LoadLatestReportForPlayer(forwardId);
            var reports = repository.LoadReportsForAssignment(assignment.Id);
            var questions = repository.LoadQuestionsForReport(report.Id);
            var stored = StoredScoutReportText(factory);

            Assert.Equal(assignment.Id, report.AssignmentId);
            Assert.NotNull(latest);
            Assert.Equal(report.Id, latest!.Id);
            Assert.Single(reports);
            Assert.Single(questions);
            Assert.Equal(ScoutAssignmentStatus.ReportSubmitted, repository.LoadAssignment(assignment.Id)!.Status);
            Assert.Contains("He looks professional and composed", stored);
            Assert.Contains("Handles pressure well", stored);
            Assert.DoesNotContain("Professionalism: 20", stored);
            Assert.DoesNotContain("CA 155", stored);
            Assert.DoesNotContain("PA=180", stored);
            Assert.DoesNotContain("CurrentAbility: 200", stored);
            Assert.DoesNotContain("Consistency 17", stored);

            repository.UpdateAssignmentStatus(assignment.Id, ScoutAssignmentStatus.InProgress);
            Assert.Equal(ScoutAssignmentStatus.InProgress, repository.LoadAssignment(assignment.Id)!.Status);
            repository.ArchiveAssignment(assignment.Id);
            Assert.Empty(repository.LoadAssignments(new ScoutDeskQuery()));
            Assert.Single(repository.LoadAssignments(new ScoutDeskQuery { IncludeArchived = true }));
        }

        [Fact]
        public void ScoutDeskRepositoryRejectsMissingPlayersAndRawProviderEntities()
        {
            using var factory = CreateImportedDatabase();
            var repository = new ScoutDeskRepository(factory);

            Assert.Throws<InvalidOperationException>(() => repository.CreateAssignment("missing-player", null, "Role", ShortlistPriority.Medium, string.Empty, null));
            Assert.Throws<InvalidOperationException>(() => repository.CreateAssignment(TestPlayers.CreateExternalPlayer(), null, "Role", ShortlistPriority.Medium, string.Empty, null));
        }

        [Fact]
        public void ScoutQuestionGeneratorCreatesOutputSpecificSafePrompts()
        {
            var generator = new ScoutQuestionGenerator();
            var striker = QuestionText(generator.Generate("Striker", new[] { "xG" }, 0, 90));
            var wide = QuestionText(generator.Generate("Wide Attacker", new[] { "xA", "key passes" }, 0, 90));
            var centreBack = QuestionText(generator.Generate("Centre-back", new[] { "aerial duels" }, 0, 90));
            var keeper = QuestionText(generator.Generate("Goalkeeper", new[] { "save percentage" }, 0, 90));
            var blocked = QuestionText(generator.Generate("Midfielder", Array.Empty<string>(), 2, 60));
            var all = striker + " " + wide + " " + centreBack + " " + keeper + " " + blocked;

            Assert.Contains("high-quality shooting positions", striker);
            Assert.Contains("wide or half-space", wide);
            Assert.Contains("aerial duels", centreBack);
            Assert.Contains("routine saves", keeper);
            Assert.Contains("observe behaviour directly", blocked);
            Assert.Contains("trusted enough", blocked);
            Assert.DoesNotContain("CA", all);
            Assert.DoesNotContain("PA", all);
            Assert.DoesNotContain("Professionalism", all);
            Assert.DoesNotContain("CurrentAbility", all);
            Assert.DoesNotContain("PotentialAbility", all);
        }

        [Fact]
        public void ScoutDeskWorkflowCreatesFromShortlistSubmitsReportAndUpdatesShortlist()
        {
            using var factory = CreateImportedDatabase();
            var shortlistResult = new ShortlistWorkflowService(factory).AddToShortlist(new ShortlistAddPlayerRequest
            {
                StatlynPlayerId = StatlynPlayerIdByName(factory, "Synthetic Forward"),
                ShortlistName = ShortlistWorkflowService.DefaultShortlistName,
                Priority = ShortlistPriority.Urgent,
                RoleName = "Advanced Forward"
            });
            var service = new ScoutDeskWorkflowService(factory);

            var assignmentResult = service.CreateAssignmentFromShortlistPlayer(shortlistResult.PlayerSummary!.ShortlistPlayer.Id, "Scout A", null);
            var reportResult = service.SubmitReport(new SubmitScoutReportRequest
            {
                AssignmentId = assignmentResult.Assignment!.Id,
                StatlynPlayerId = assignmentResult.Assignment.StatlynPlayerId,
                RoleAssessed = "Advanced Forward",
                OverallRecommendation = ScoutReportRecommendation.TooRisky,
                Confidence = 71,
                Strengths = "Direct runner.",
                Weaknesses = "Needs defensive recovery watch.",
                Risks = "Professionalism: 20",
                FollowUpAction = ScoutFollowUpAction.CheckMedical,
                FinalSummary = "Good output profile, risk needs review.",
                UpdateShortlistFromReport = true
            });
            var updatedShortlistPlayer = new ShortlistRepository(factory).LoadPlayer(shortlistResult.PlayerSummary.ShortlistPlayer.Id);
            var page = service.BuildPageViewModel(new ScoutDeskQuery());
            var detail = service.BuildAssignmentDetailViewModel(assignmentResult.Assignment.Id);
            var text = WorkflowText(assignmentResult) + " " + WorkflowText(reportResult) + " " + ScoutPageText(page) + " " + ScoutDetailText(detail);

            Assert.True(assignmentResult.Success);
            Assert.True(reportResult.Success);
            Assert.Equal(ShortlistPriority.Urgent, assignmentResult.Assignment.Priority);
            Assert.Equal(ShortlistStatus.TooRisky, updatedShortlistPlayer!.Status);
            Assert.Equal(ShortlistFollowUpAction.CheckMedical, updatedShortlistPlayer.FollowUpAction);
            Assert.Contains("No scout report yet", service.BuildLatestReportSummary(StatlynPlayerIdByName(factory, "Synthetic Wide Player")).Summary);
            Assert.Contains("TooRisky", text);
            Assert.Contains("Good output profile", text);
            Assert.Contains("No live FM26 data", text);
            Assert.DoesNotContain("Professionalism: 20", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("IRawFootballEntity", text);
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

        private static int CountRows(StatlynDbConnectionFactory factory, string tableName)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM " + tableName + ";";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static string StoredScoutReportText(StatlynDbConnectionFactory factory)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                @"SELECT
                    COALESCE((SELECT group_concat(RoleAssessed || ' ' || TechnicalRating || ' ' || TacticalRating || ' ' || PhysicalRating || ' ' || MentalRating || ' ' || OverallRecommendation || ' ' || Strengths || ' ' || Weaknesses || ' ' || Risks || ' ' || FollowUpAction || ' ' || FinalSummary, ' ') FROM ScoutReport), '') ||
                    ' ' ||
                    COALESCE((SELECT group_concat(Category || ' ' || Question || ' ' || Answer, ' ') FROM ScoutReportQuestion), '');";
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static string QuestionText(IReadOnlyList<ScoutQuestionPrompt> prompts)
        {
            return string.Join(" ", prompts.Select(prompt => prompt.Category + " " + prompt.Question + " " + prompt.WhyItMatters + " " + prompt.SuggestedObservationType));
        }

        private static string WorkflowText(ScoutDeskWorkflowResult result)
        {
            return result.SafeMessage + " " + string.Join(" ", result.Warnings) + " " + string.Join(" ", result.Errors);
        }

        private static string ScoutPageText(ScoutDeskPageViewModel page)
        {
            var builder = new StringBuilder();
            builder.Append(page.SafeMessage).Append(' ');
            builder.Append(string.Join(" ", page.Assignments.Select(AssignmentText)));
            if (page.SelectedAssignment != null)
            {
                builder.Append(' ').Append(ScoutDetailText(page.SelectedAssignment));
            }

            return builder.ToString();
        }

        private static string ScoutDetailText(ScoutAssignmentDetailViewModel detail)
        {
            var builder = new StringBuilder();
            builder.Append(detail.SafeNotice).Append(' ')
                .Append(AssignmentText(detail.Assignment)).Append(' ')
                .Append(string.Join(" ", detail.Questions.Select(prompt => prompt.Category + " " + prompt.Question + " " + prompt.WhyItMatters + " " + prompt.SuggestedObservationType))).Append(' ')
                .Append(detail.ReportHistory.EmptyMessage).Append(' ')
                .Append(string.Join(" ", detail.ReportHistory.Reports.Select(ReportText)));
            return builder.ToString();
        }

        private static string AssignmentText(ScoutAssignmentCardViewModel assignment)
        {
            return assignment.PlayerName + " " +
                   assignment.StatlynPlayerId + " " +
                   assignment.Position + " " +
                   assignment.Source + " " +
                   assignment.Role + " " +
                   assignment.ShortlistStatus + " " +
                   assignment.AssignmentStatus + " " +
                   assignment.Priority + " " +
                   assignment.AssignedTo + " " +
                   assignment.LatestReportRecommendation + " " +
                   assignment.ScoutConfidence + " " +
                   assignment.LatestReportSummary + " " +
                   assignment.NoLiveFm26Label + " " +
                   string.Join(" ", assignment.Warnings);
        }

        private static string ReportText(ScoutReportViewModel report)
        {
            return report.RoleAssessed + " " +
                   report.Ratings + " " +
                   report.Recommendation + " " +
                   report.Confidence + " " +
                   report.Strengths + " " +
                   report.Weaknesses + " " +
                   report.Risks + " " +
                   report.FollowUpAction + " " +
                   report.FinalSummary + " " +
                   string.Join(" ", report.QuestionAnswers) + " " +
                   report.SafeNotice;
        }
    }
}
