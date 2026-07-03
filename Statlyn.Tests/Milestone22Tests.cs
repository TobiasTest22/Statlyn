using System;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.Data.Shortlists;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone22Tests
    {
        [Fact]
        public void ShortlistWorkflowLabelsSerializeWithoutHiddenTerminology()
        {
            var labels = string.Join(" ", Enum.GetNames(typeof(ShortlistStatus))
                .Concat(Enum.GetNames(typeof(ShortlistPriority)))
                .Concat(Enum.GetNames(typeof(ShortlistFollowUpAction))));

            Assert.Contains("ScoutFurther", labels);
            Assert.Contains("Urgent", labels);
            Assert.Contains("CheckAvailability", labels);
            Assert.DoesNotContain("CurrentAbility", labels);
            Assert.DoesNotContain("PotentialAbility", labels);
            Assert.DoesNotContain("Professionalism", labels);
            Assert.DoesNotContain("RawValue", labels);
        }

        [Fact]
        public void ShortlistSchemaContainsSafeColumnsAndIsIdempotent()
        {
            var schema = string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements);
            using var factory = RuntimeDatabaseFactory.CreateInMemory();

            new StatlynDatabaseInitializer(factory).Initialize();
            new StatlynDatabaseInitializer(factory).Initialize();

            Assert.Contains("CREATE TABLE IF NOT EXISTS Shortlist", schema);
            Assert.Contains("UpdatedAtUtc TEXT NOT NULL", schema);
            Assert.Contains("IsArchived INTEGER NOT NULL", schema);
            Assert.Contains("StatlynPlayerId TEXT NOT NULL", schema);
            Assert.Contains("Priority TEXT NOT NULL", schema);
            Assert.Contains("FollowUpAction TEXT NOT NULL", schema);
            Assert.Contains("UserNote TEXT NOT NULL", schema);
            Assert.Contains("UX_ShortlistPlayer_Shortlist_StatlynPlayer", schema);
            Assert.DoesNotContain("CurrentAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PotentialAbility", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism", schema, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RawValue", schema, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(factory).ReadDiagnostics().SchemaVersion);
        }

        [Fact]
        public void ShortlistRepositoryCreatesAddsUpdatesRemovesAndArchivesSafely()
        {
            using var factory = CreateImportedDatabase();
            var repository = new ShortlistRepository(factory);
            var id = StatlynPlayerIdByName(factory, "Synthetic Wide Player");
            var shortlist = repository.CreateShortlist("Wide watchlist", "Local CSV decisions only");

            var first = repository.AddPlayer(shortlist.Id, id, ShortlistStatus.Watchlist, ShortlistPriority.Medium, ShortlistFollowUpAction.WatchMore, "Wide Forward Output", "Monitor", "xA available.");
            var second = repository.AddPlayer(shortlist.Id, id, ShortlistStatus.ScoutFurther, ShortlistPriority.High, ShortlistFollowUpAction.ScoutAgain, "CurrentAbility 199", "Professionalism 19", "Professionalism 19");
            var updated = repository.UpdatePlayer(second.Id, ShortlistStatus.Shortlist, ShortlistPriority.High, ShortlistFollowUpAction.CompareAlternatives, "Wide Forward Output", "Shortlist", "CurrentAbility 199");

            Assert.Equal(first.Id, second.Id);
            Assert.Single(repository.LoadPlayers(shortlist.Id));
            Assert.Equal(ShortlistStatus.Shortlist, updated.Status);
            Assert.Equal(ShortlistPriority.High, updated.Priority);
            Assert.Equal(ShortlistFollowUpAction.CompareAlternatives, updated.FollowUpAction);
            Assert.DoesNotContain("CurrentAbility 199", StoredShortlistText(factory));
            Assert.DoesNotContain("Professionalism 19", StoredShortlistText(factory));

            repository.RemovePlayer(updated.Id);
            Assert.Empty(repository.LoadPlayers(shortlist.Id));

            repository.DeleteOrArchiveShortlist(shortlist.Id);
            Assert.DoesNotContain(repository.LoadShortlists(includeArchived: false), item => item.Id == shortlist.Id);
            Assert.Contains(repository.LoadShortlists(includeArchived: true), item => item.Id == shortlist.Id);
        }

        [Fact]
        public void ShortlistRepositoryRejectsRawProviderEntities()
        {
            using var factory = CreateImportedDatabase();
            var repository = new ShortlistRepository(factory);
            var shortlist = repository.CreateShortlist("Raw rejection", string.Empty);

            Assert.Throws<InvalidOperationException>(() => repository.AddPlayer(shortlist.Id, TestPlayers.CreateExternalPlayer(), ShortlistStatus.Watchlist, ShortlistPriority.Medium, ShortlistFollowUpAction.None, "Role", "Monitor", "Raw object test"));
        }

        [Fact]
        public void ShortlistWorkflowAddsDefaultRecruitmentAndProfilePlayers()
        {
            using var factory = CreateImportedDatabase();
            var service = new ShortlistWorkflowService(factory);
            var wideRow = RecruitmentCentrePlayerRowViewModel.From(new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { SearchText = "Wide" }).Players.Single());
            var forwardId = StatlynPlayerIdByName(factory, "Synthetic Forward");
            var profile = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = forwardId });

            var fromDefault = service.AddToShortlist(new ShortlistAddPlayerRequest { StatlynPlayerId = wideRow.StatlynPlayerId });
            var fromRecruitment = service.AddFromRecruitmentCentreRow(new ShortlistAddPlayerRequest { ShortlistName = ShortlistWorkflowService.DefaultShortlistName }, wideRow);
            var fromProfile = service.AddFromPlayerProfileResult(new ShortlistAddPlayerRequest(), profile);
            var page = service.BuildPageViewModel(includeArchived: false);
            var text = PageText(page) + " " + fromDefault.SafeMessage + " " + fromRecruitment.SafeMessage + " " + fromProfile.SafeMessage;

            Assert.True(fromDefault.Success);
            Assert.True(fromRecruitment.Success);
            Assert.True(fromProfile.Success);
            Assert.Single(page.Shortlists);
            Assert.True(page.SelectedShortlist.Players.Count >= 2);
            Assert.Contains(page.SelectedShortlist.Players, player => player.KeyOutputMetrics.Count > 0);
            Assert.Contains(page.SelectedShortlist.Players, player => !player.IsLiveFm26Data);
            Assert.DoesNotContain("CurrentAbility 199", text);
            Assert.DoesNotContain("Professionalism 19", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("IRawFootballEntity", text);
        }

        [Fact]
        public void ShortlistWorkflowHandlesMissingPlayerSafely()
        {
            using var factory = CreateImportedDatabase();

            var result = new ShortlistWorkflowService(factory).AddToShortlist(new ShortlistAddPlayerRequest { StatlynPlayerId = "missing-player" });

            Assert.False(result.Success);
            Assert.Contains("not found", result.SafeMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PlayerRawSnapshot", WorkflowText(result));
        }

        [Fact]
        public void DecisionHelperKeepsRecommendationsCautiousAndSafe()
        {
            var lowConfidence = ShortlistDecisionHelper.Suggest("Monitor", 42, 0, 0, 80, 80);
            var missingOutput = ShortlistDecisionHelper.Suggest("Shortlist", 80, 2, 0, 82, 80);
            var strong = ShortlistDecisionHelper.Suggest("Sign", 88, 0, 0, 91, 85);
            var blocked = ShortlistDecisionHelper.Suggest("Shortlist", 80, 0, 3, 82, 80);
            var text = lowConfidence.SafeReason + " " + missingOutput.SafeReason + " " + strong.SafeReason + " " + blocked.SafeReason;

            Assert.Equal(ShortlistStatus.ScoutFurther, lowConfidence.SuggestedStatus);
            Assert.Equal(ShortlistStatus.ScoutFurther, missingOutput.SuggestedStatus);
            Assert.True(strong.SuggestedStatus == ShortlistStatus.Shortlist || strong.SuggestedStatus == ShortlistStatus.StrongTarget);
            Assert.NotEqual("Sign", strong.SuggestedStatus.ToString());
            Assert.NotEmpty(blocked.Warnings);
            Assert.DoesNotContain("CurrentAbility", text);
            Assert.DoesNotContain("Professionalism", text);
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

        private static string StoredShortlistText(StatlynDbConnectionFactory factory)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT group_concat(Status || ' ' || Priority || ' ' || FollowUpAction || ' ' || RoleName || ' ' || Recommendation || ' ' || AddedReason || ' ' || UserNote, ' ') FROM ShortlistPlayer;";
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static string PageText(ShortlistsPageViewModel page)
        {
            var builder = new StringBuilder();
            builder.Append(page.SafeMessage).Append(' ')
                .Append(string.Join(" ", page.Shortlists.Select(item => item.Name + " " + item.Description + " " + item.PlayerCount + " " + item.Status))).Append(' ')
                .Append(page.SelectedShortlist.Name).Append(' ')
                .Append(string.Join(" ", page.SelectedShortlist.Players.Select(PlayerText)));
            return builder.ToString();
        }

        private static string PlayerText(ShortlistPlayerRowViewModel player)
        {
            return player.PlayerName + " " +
                   player.StatlynPlayerId + " " +
                   player.SourceName + " " +
                   player.RoleName + " " +
                   player.RoleFit + " " +
                   player.Confidence + " " +
                   player.Recommendation + " " +
                   player.Status + " " +
                   player.Priority + " " +
                   player.FollowUpAction + " " +
                   player.AddedReason + " " +
                   player.UserNote + " " +
                   string.Join(" ", player.KeyOutputMetrics) + " " +
                   string.Join(" ", player.SafeWarnings);
        }

        private static string WorkflowText(ShortlistWorkflowResult result)
        {
            return result.SafeMessage + " " + string.Join(" ", result.Warnings) + " " + string.Join(" ", result.Errors);
        }
    }
}
