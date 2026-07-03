using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Data.Persistence;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;

namespace Statlyn.Data.Shortlists
{
    public sealed class ShortlistWorkflowService
    {
        public const string DefaultShortlistName = "Main Recruitment List";

        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly ShortlistRepository _repository;
        private readonly PlayerProfileQueryService _profiles;
        private readonly RecruitmentCentreQueryService _recruitmentCentre;

        public ShortlistWorkflowService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _repository = new ShortlistRepository(connectionFactory);
            _profiles = new PlayerProfileQueryService(connectionFactory);
            _recruitmentCentre = new RecruitmentCentreQueryService(connectionFactory);
        }

        public ShortlistRecord CreateShortlist(string name, string description)
        {
            return _repository.CreateShortlist(name, description);
        }

        public ShortlistWorkflowResult AddToShortlist(ShortlistAddPlayerRequest request)
        {
            request = request ?? new ShortlistAddPlayerRequest();
            if (string.IsNullOrWhiteSpace(request.StatlynPlayerId))
            {
                return ShortlistWorkflowResult.Failure("A persisted player must be selected before adding to a shortlist.", new[] { "Missing StatlynPlayerId." });
            }

            var profile = _profiles.Query(new PlayerProfileQuery { StatlynPlayerId = request.StatlynPlayerId });
            if (!profile.Success || profile.Player == null)
            {
                return ShortlistWorkflowResult.Failure("Player was not found in persisted safe data.", profile.Errors);
            }

            return AddFromProfile(request, profile);
        }

        public ShortlistWorkflowResult AddFromRecruitmentCentreRow(ShortlistAddPlayerRequest request, RecruitmentCentrePlayerRowViewModel row)
        {
            if (row == null)
            {
                return ShortlistWorkflowResult.Failure("Recruitment Centre row was unavailable.", new[] { "No safe row view model was supplied." });
            }

            request = request ?? new ShortlistAddPlayerRequest();
            request.StatlynPlayerId = string.IsNullOrWhiteSpace(request.StatlynPlayerId) ? row.StatlynPlayerId : request.StatlynPlayerId;
            request.RoleName = string.IsNullOrWhiteSpace(request.RoleName) ? row.RoleName : request.RoleName;
            request.Recommendation = string.IsNullOrWhiteSpace(request.Recommendation) ? row.Recommendation : request.Recommendation;
            return AddToShortlist(request);
        }

        public ShortlistWorkflowResult AddFromPlayerProfileResult(ShortlistAddPlayerRequest request, PlayerProfileResult profile)
        {
            if (profile == null || !profile.Success || profile.Player == null)
            {
                return ShortlistWorkflowResult.Failure("Player Profile result was not available from persisted safe data.", profile == null ? new[] { "Profile result was null." } : profile.Errors);
            }

            request = request ?? new ShortlistAddPlayerRequest();
            request.StatlynPlayerId = string.IsNullOrWhiteSpace(request.StatlynPlayerId) ? profile.Player.StatlynPlayerId : request.StatlynPlayerId;
            return AddFromProfile(request, profile);
        }

        public ShortlistsPageViewModel BuildPageViewModel(bool includeArchived)
        {
            var records = _repository.LoadShortlists(includeArchived);
            var cards = records.Select(record => new ShortlistCardViewModel(record)).ToList();
            var selected = records.Count == 0
                ? new ShortlistDetailViewModel(null, new List<ShortlistPlayerRowViewModel>())
                : BuildDetailViewModel(records[0].Id);

            return new ShortlistsPageViewModel(
                cards,
                selected,
                new ShortlistStatusViewModel(),
                records.Count == 0 ? "No shortlists yet. Add players from Recruitment Centre or Player Profile." : "Shortlists loaded from persisted safe data.",
                _connectionFactory.DatabasePath);
        }

        public ShortlistDetailViewModel BuildDetailViewModel(long shortlistId)
        {
            var shortlist = _repository.LoadShortlist(shortlistId);
            if (shortlist == null)
            {
                return new ShortlistDetailViewModel(null, new List<ShortlistPlayerRowViewModel>());
            }

            return new ShortlistDetailViewModel(shortlist, BuildSummaries(shortlistId).Select(summary => new ShortlistPlayerRowViewModel(summary)).ToList());
        }

        public ShortlistPlayerRecord UpdatePlayer(ShortlistUpdatePlayerRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _repository.UpdatePlayer(
                request.ShortlistPlayerId,
                request.Status,
                request.Priority,
                request.FollowUpAction,
                request.RoleName,
                request.Recommendation,
                request.UserNote);
        }

        public void RemovePlayer(long shortlistPlayerId)
        {
            _repository.RemovePlayer(shortlistPlayerId);
        }

        public IReadOnlyList<ShortlistPlayerRecord> LoadMembershipsForPlayer(string statlynPlayerId)
        {
            return _repository.LoadShortlistMembershipsForPlayer(statlynPlayerId);
        }

        private ShortlistWorkflowResult AddFromProfile(ShortlistAddPlayerRequest request, PlayerProfileResult profile)
        {
            var shortlist = ResolveShortlist(request);
            if (shortlist == null)
            {
                return ShortlistWorkflowResult.Failure("Shortlist was not found and was not created.", new[] { "No target shortlist." });
            }

            var missingCount = profile.RoleOutputSummary == null ? 0 : profile.RoleOutputSummary.MissingCoreMetrics.Count;
            var recommendation = profile.LatestRoleScore == null ? "Not scored" : profile.LatestRoleScore.Recommendation.ToString();
            var decision = ShortlistDecisionHelper.Suggest(
                recommendation,
                profile.LatestRoleScore == null ? (int?)null : profile.LatestRoleScore.Confidence,
                missingCount,
                profile.BlockedFields.Count,
                profile.LatestRoleScore == null ? (int?)null : profile.LatestRoleScore.RoleFit,
                profile.SourceMetadata == null ? (int?)null : profile.SourceMetadata.SourceConfidence);

            var existed = _repository.IsPlayerInShortlist(shortlist.Id, profile.Player!.StatlynPlayerId);
            var status = request.Status ?? decision.SuggestedStatus;
            var priority = request.Priority ?? decision.SuggestedPriority;
            var followUp = request.FollowUpAction ?? decision.SuggestedFollowUpAction;
            var roleName = string.IsNullOrWhiteSpace(request.RoleName)
                ? profile.LatestRoleScore == null ? "Not scored" : profile.LatestRoleScore.RoleName
                : request.RoleName;
            var storedRecommendation = string.IsNullOrWhiteSpace(request.Recommendation) ? recommendation : request.Recommendation;
            var reason = string.IsNullOrWhiteSpace(request.AddedReason) ? decision.SafeReason : request.AddedReason;

            var player = _repository.AddPlayer(
                shortlist.Id,
                profile.Player.StatlynPlayerId,
                status,
                priority,
                followUp,
                roleName,
                storedRecommendation,
                reason);

            if (!string.IsNullOrWhiteSpace(request.UserNote))
            {
                player = _repository.UpdatePlayer(player.Id, player.Status, player.Priority, player.FollowUpAction, player.RoleName, player.Recommendation, request.UserNote);
            }

            var summary = BuildSummary(player, LoadRecruitmentRowsById());
            var warnings = decision.Warnings.Concat(profile.Warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return new ShortlistWorkflowResult(
                true,
                existed ? "Player was already on this shortlist; safe workflow labels were refreshed." : "Player added to shortlist from persisted safe data.",
                _repository.LoadShortlist(shortlist.Id),
                summary,
                warnings,
                new List<string>());
        }

        private ShortlistRecord? ResolveShortlist(ShortlistAddPlayerRequest request)
        {
            if (request.ShortlistId.HasValue)
            {
                return _repository.LoadShortlist(request.ShortlistId.Value);
            }

            var name = string.IsNullOrWhiteSpace(request.ShortlistName) ? DefaultShortlistName : request.ShortlistName.Trim();
            var existing = _repository.LoadShortlistByName(name, includeArchived: false);
            if (existing != null)
            {
                return existing;
            }

            return request.CreateShortlistIfMissing
                ? _repository.CreateShortlist(name, "Default persisted safe recruitment list.")
                : null;
        }

        private IReadOnlyList<ShortlistPlayerSummary> BuildSummaries(long shortlistId)
        {
            var rows = LoadRecruitmentRowsById();
            return _repository.LoadPlayers(shortlistId).Select(player => BuildSummary(player, rows)).ToList();
        }

        private ShortlistPlayerSummary BuildSummary(ShortlistPlayerRecord player, IReadOnlyDictionary<string, RecruitmentCentrePlayerRowViewModel> rowsById)
        {
            if (rowsById.TryGetValue(player.StatlynPlayerId, out var row))
            {
                return new ShortlistPlayerSummary(
                    player,
                    row.Name,
                    row.Age,
                    row.Nationality,
                    row.Position,
                    row.Source,
                    row.RoleFit,
                    row.Confidence,
                    row.KeyOutputMetrics,
                    row.MissingDataCount,
                    row.BlockedFieldCount,
                    row.IsFixtureMode,
                    row.IsLiveFm26Data,
                    row.Warnings);
            }

            return new ShortlistPlayerSummary(
                player,
                "Persisted player",
                "Unknown",
                "Unknown",
                "Unknown",
                string.Empty,
                "Not scored",
                "Unknown",
                new List<string>(),
                0,
                0,
                false,
                false,
                new[] { "Player summary was unavailable; persisted shortlist row is still safe." });
        }

        private IReadOnlyDictionary<string, RecruitmentCentrePlayerRowViewModel> LoadRecruitmentRowsById()
        {
            var result = _recruitmentCentre.Query(new RecruitmentCentreQuery { Limit = 500, SortBy = "DisplayName", SortDirection = "Ascending" });
            return result.Players
                .Select(RecruitmentCentrePlayerRowViewModel.From)
                .GroupBy(row => row.StatlynPlayerId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
