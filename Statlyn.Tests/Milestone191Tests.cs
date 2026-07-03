using System;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Persistence;
using Statlyn.Data.Recruitment;
using Statlyn.DataProviders;
using Statlyn.DataProviders.Import;
using Statlyn.UI;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone191Tests
    {
        [Fact]
        public void RoleScorePersistsReloadsAndSanitizesRoleName()
        {
            using var factory = CreateImportedDatabase();
            var playerId = PlayerIdByName(factory, "Synthetic Wide Player");
            var repository = new RoleScoreRepository(factory);

            repository.Save(playerId, CreateScore("Wide Forward Output Preview"));
            var reloaded = repository.LoadLatest(playerId)!;

            Assert.Equal("Wide Forward Output Preview", reloaded.RoleName);

            repository.Save(playerId, CreateScore("CurrentAbility: 200"));
            var sanitized = repository.LoadLatest(playerId)!;

            Assert.Equal("Unknown role", sanitized.RoleName);
            Assert.DoesNotContain("CurrentAbility", sanitized.RoleName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RoleName TEXT NOT NULL", string.Join(Environment.NewLine, StatlynDatabaseSchema.CreateStatements));
        }

        [Fact]
        public void RecruitmentCentreRowDisplaysPersistedRoleNameAndNotScoredFallback()
        {
            using var factory = CreateImportedDatabase();
            var wideId = PlayerIdByName(factory, "Synthetic Wide Player");
            var forwardId = PlayerIdByName(factory, "Synthetic Forward");
            new RoleScoreRepository(factory).Save(wideId, CreateScore("Wide Forward Output Preview"));
            DeleteRoleScores(factory, forwardId);

            var result = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery());
            var wide = result.Players.Single(player => player.DisplayName == "Synthetic Wide Player");
            var forward = result.Players.Single(player => player.DisplayName == "Synthetic Forward");

            Assert.Equal("Wide Forward Output Preview", wide.LatestRoleName);
            Assert.Equal("Not scored", forward.LatestRoleName);
        }

        [Fact]
        public void RecruitmentCentreUsesPersistedOutputProfileWhenPresent()
        {
            using var factory = CreateImportedDatabase();
            new RoleOutputExpectationRepository(factory).Save(CustomWideProfile());

            var row = new RecruitmentCentreQueryService(factory)
                .Query(new RecruitmentCentreQuery { SearchText = "Wide" })
                .Players.Single();

            var metrics = string.Join(" ", row.KeyOutputMetrics);
            Assert.Contains("Assists", metrics);
            Assert.DoesNotContain("xA", metrics);

            var diagnostics = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery()).Diagnostics;
            Assert.Contains("persisted SQLite profiles", string.Join(" ", diagnostics), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RecruitmentCentreFallsBackToGenericSeedWhenNoPersistedProfileExists()
        {
            using var factory = CreateImportedDatabase();

            var row = new RecruitmentCentreQueryService(factory)
                .Query(new RecruitmentCentreQuery { SearchText = "Wide" })
                .Players.Single();
            var metrics = string.Join(" ", row.KeyOutputMetrics);

            Assert.Contains("xA", metrics);
            Assert.Contains("xG", metrics);
        }

        [Fact]
        public void OutputProfilesStayPositionSpecificGenericAndOutputFirst()
        {
            var service = new RecruitmentOutputSummaryService();
            var goalkeeper = service.Build("GK", Array.Empty<PlayerStatRecord>(), Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("Goalkeeper"), null);
            var wide = service.Build("RW", new[] { Stat("xA", 0.31), Stat("ProgressiveCarries", 7), Stat("xG", 0.24) }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("WingerWideForward"), null);
            var centreBack = service.Build("CB", new[] { Stat("AerialDuelsWonPct", 70), Stat("Clearances", 6), Stat("Blocks", 2), Stat("ProgressivePasses", 5) }, Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("CentreBack"), null);
            var strikerMissing = service.Build("ST", Array.Empty<PlayerStatRecord>(), Array.Empty<PhysicalMetricRecord>(), service.FindDefaultProfile("StrikerForward"), null);
            var genericProfiles = GenericRoleOutputExpectationSeed.Create();

            Assert.DoesNotContain(goalkeeper.MissingCoreMetrics, metric => metric == "xA" || metric == "ProgressiveCarries");
            Assert.Contains(wide.CoreMetrics, metric => metric.StartsWith("xA", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(wide.CoreMetrics, metric => metric.StartsWith("ProgressiveCarries", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(centreBack.CoreMetrics, metric => metric.StartsWith("AerialDuelsWonPct", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(centreBack.CoreMetrics, metric => metric.StartsWith("Clearances", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(centreBack.CoreMetrics, metric => metric.StartsWith("Blocks", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("xG", strikerMissing.MissingCoreMetrics);
            Assert.DoesNotContain("xG 0", string.Join(" ", strikerMissing.CoreMetrics));
            Assert.All(genericProfiles, profile => Assert.False(profile.IsFm26Specific));
            Assert.All(genericProfiles, profile => Assert.Contains("supporting", profile.AttributeSupportWeights, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void OfficialLogoAssetsAreCopiedAndUnityReferencesResourceKeys()
        {
            var root = FindRepositoryRoot();
            var branding = Path.Combine(root, "Statlyn.UnityApp", "Assets", "Resources", "Branding");

            Assert.True(File.Exists(Path.Combine(root, "StatLyn_Logo.png")));
            Assert.True(File.Exists(Path.Combine(root, "StatLyn_Logo_Reversed.png")));
            Assert.True(File.Exists(Path.Combine(root, "Statlyn_Logo_Black-text.png")));
            Assert.True(File.Exists(Path.Combine(root, "Statlyn_Logo_White-text.png")));
            Assert.True(File.Exists(Path.Combine(branding, "StatLyn_Logo.png")));
            Assert.True(File.Exists(Path.Combine(branding, "Statlyn_Logo_Black-text.png")));

            var uiFactory = File.ReadAllText(Path.Combine(root, "Statlyn.UnityApp", "Assets", "Scripts", "Components", "StatlynUiFactory.cs"));
            var bootstrap = File.ReadAllText(Path.Combine(root, "Statlyn.UnityApp", "Assets", "Scripts", "StatlynBootstrap.cs"));

            Assert.Contains("Branding/Statlyn_Logo_Black-text", uiFactory);
            Assert.Contains("MakeBrandLockup", bootstrap);
            Assert.DoesNotContain("placeholder logo", uiFactory, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RecruitmentCentreViewModelDefaultResetAndEmptyStateStaySafe()
        {
            using var empty = RuntimeDatabaseFactory.CreateInMemory();
            var emptyResult = new RecruitmentCentreQueryService(empty).Query(new RecruitmentCentreQuery());

            Assert.Empty(emptyResult.Players);
            Assert.Contains("No imported players", emptyResult.SafeMessage);

            using var factory = CreateImportedDatabase();
            var query = new RecruitmentCentreQuery();
            var viewModel = RecruitmentCentreViewModel.From(new RecruitmentCentreQueryService(factory).Query(query), query, factory.DatabasePath);
            var text = ViewModelText(viewModel);

            Assert.Equal(2, viewModel.TotalCount);
            Assert.Equal("DisplayName", viewModel.Filters.SortBy);
            Assert.DoesNotContain("CurrentAbility 200", text);
            Assert.DoesNotContain("Professionalism 20", text);
            Assert.DoesNotContain("fake live", text, StringComparison.OrdinalIgnoreCase);
            Assert.All(viewModel.Players, player => Assert.False(player.IsLiveFm26Data));
        }

        [Fact]
        public void ProfilePreviewUsesPersistedRoleNameAndSafeNotices()
        {
            using var factory = CreateImportedDatabase();
            var wideId = PlayerIdByName(factory, "Synthetic Wide Player");
            new RoleScoreRepository(factory).Save(wideId, CreateScore("Wide Forward Output Preview"));
            var statlynPlayerId = StatlynPlayerIdByName(factory, "Synthetic Wide Player");

            var preview = new RecruitmentCentreProfilePreviewService(factory).LoadProfilePreview(statlynPlayerId)!;
            var text = preview.PlayerName + " " + preview.SourceName + " " + preview.ModeLabel + " " +
                       preview.RoleName + " " + string.Join(" ", preview.OutputMetrics) + " " +
                       preview.MissingDataWarning + " " + preview.BlockedDataSafeNotice;

            Assert.Equal("Wide Forward Output Preview", preview.RoleName);
            Assert.Contains("Synthetic Wide Player", preview.PlayerName);
            Assert.Contains("Fixture/import mode", preview.ModeLabel);
            Assert.False(preview.IsLiveFm26Data);
            Assert.Contains("Raw values are not shown", preview.BlockedDataSafeNotice);
            Assert.Contains("xA", string.Join(" ", preview.OutputMetrics));
            Assert.DoesNotContain("CurrentAbility", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Professionalism 20", text, StringComparison.OrdinalIgnoreCase);
        }

        private static StatlynDbConnectionFactory CreateImportedDatabase()
        {
            var factory = RuntimeDatabaseFactory.CreateInMemory();
            new ImportPipelineService(factory).Import(CreateCsvProvider(), ImportPipelineOptions.CreateDefault());
            return factory;
        }

        private static CsvImportProvider CreateCsvProvider()
        {
            return new CsvImportProvider(Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv"), CreateFixtureMetadata(), new FieldMappingSet(Array.Empty<FieldMapping>()));
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

        private static RoleScore CreateScore(string roleName)
        {
            return new RoleScore(
                roleName,
                76,
                70,
                78,
                68,
                null,
                24,
                82,
                RecruitmentRecommendation.Shortlist,
                Array.Empty<EvidenceItem>(),
                Array.Empty<EvidenceItem>(),
                Array.Empty<string>(),
                "Blocked data was excluded safely.");
        }

        private static RoleOutputExpectationProfile CustomWideProfile()
        {
            return new RoleOutputExpectationProfile(
                "Persisted Wide Output Override",
                "WingerWideForward",
                "Wide",
                string.Empty,
                false,
                true,
                new[]
                {
                    new MetricExpectation("Assists", "Assists", 2.5, "Core", "HigherBetter", 900, true, "Persisted test profile.", "Assists are role-output evidence.", "Missing assists lowers confidence.")
                },
                "attributes=supporting evidence only",
                "Persisted prompts stay generic.",
                "Missing output data lowers confidence.",
                "Prefer adequate minutes.",
                "Persisted generic/import-only test profile.");
        }

        private static PlayerStatRecord Stat(string name, double value)
        {
            return new PlayerStatRecord(1, "PlayerStat:" + name, name, value, 1000, false, "test", "test", 80);
        }

        private static long PlayerIdByName(StatlynDbConnectionFactory factory, string name)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM Player WHERE DisplayName = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static string StatlynPlayerIdByName(StatlynDbConnectionFactory factory, string name)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT StatlynPlayerId FROM Player WHERE DisplayName = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", name);
            return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
        }

        private static void DeleteRoleScores(StatlynDbConnectionFactory factory, long playerId)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RoleScore WHERE PlayerId = $playerId;";
            command.Parameters.AddWithValue("$playerId", playerId);
            command.ExecuteNonQuery();
        }

        private static string ViewModelText(RecruitmentCentreViewModel viewModel)
        {
            var builder = new StringBuilder();
            foreach (var row in viewModel.Players)
            {
                builder.Append(row.Name).Append(' ')
                    .Append(row.RoleName).Append(' ')
                    .Append(row.Recommendation).Append(' ')
                    .Append(string.Join(" ", row.KeyOutputMetrics)).Append(' ')
                    .Append(string.Join(" ", row.Warnings)).Append(' ');
            }

            return builder.ToString();
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Statlyn.sln")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException("Could not locate Statlyn.sln from test output directory.");
            }

            return directory.FullName;
        }
    }
}
