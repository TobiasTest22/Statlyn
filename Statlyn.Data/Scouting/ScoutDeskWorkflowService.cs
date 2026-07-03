using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.Data.Shortlists;

namespace Statlyn.Data.Scouting
{
    public sealed class ScoutDeskWorkflowService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly ScoutDeskRepository _repository;
        private readonly ShortlistRepository _shortlists;
        private readonly PlayerProfileQueryService _profiles;
        private readonly RecruitmentCentreQueryService _recruitmentCentre;
        private readonly ScoutQuestionGenerator _questionGenerator;

        public ScoutDeskWorkflowService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _repository = new ScoutDeskRepository(connectionFactory);
            _shortlists = new ShortlistRepository(connectionFactory);
            _profiles = new PlayerProfileQueryService(connectionFactory);
            _recruitmentCentre = new RecruitmentCentreQueryService(connectionFactory);
            _questionGenerator = new ScoutQuestionGenerator();
        }

        public ScoutDeskWorkflowResult CreateAssignmentFromShortlistPlayer(long shortlistPlayerId, string assignedTo, DateTimeOffset? dueAtUtc)
        {
            var shortlistPlayer = _shortlists.LoadPlayer(shortlistPlayerId);
            if (shortlistPlayer == null)
            {
                return ScoutDeskWorkflowResult.Failure("Shortlist player was not found in persisted safe data.", new[] { "Missing ShortlistPlayer row." });
            }

            return CreateAssignment(new CreateScoutAssignmentRequest
            {
                StatlynPlayerId = shortlistPlayer.StatlynPlayerId,
                ShortlistPlayerId = shortlistPlayer.Id,
                ShortlistId = shortlistPlayer.ShortlistId,
                RoleName = shortlistPlayer.RoleName,
                Priority = shortlistPlayer.Priority,
                AssignedTo = assignedTo,
                DueAtUtc = dueAtUtc,
                AssignmentTitle = "Scout " + shortlistPlayer.RoleName
            });
        }

        public ScoutDeskWorkflowResult CreateAssignment(CreateScoutAssignmentRequest request)
        {
            try
            {
                var assignment = _repository.CreateAssignment(request);
                return new ScoutDeskWorkflowResult(
                    true,
                    "Scout assignment created from persisted safe player data.",
                    assignment,
                    null,
                    new[] { "No live FM26 data or hidden values were used." },
                    new List<string>());
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return ScoutDeskWorkflowResult.Failure("Scout assignment could not be created safely.", new[] { ex.Message });
            }
        }

        public ScoutDeskWorkflowResult SubmitReport(SubmitScoutReportRequest request)
        {
            try
            {
                var report = _repository.CreateReport(request);
                var assignment = report.AssignmentId.HasValue ? _repository.LoadAssignment(report.AssignmentId.Value) : null;
                var warnings = new List<string> { "Scout report stored as qualitative local notes only." };

                if (request.UpdateShortlistFromReport && assignment != null && assignment.ShortlistPlayerId.HasValue)
                {
                    var current = _shortlists.LoadPlayer(assignment.ShortlistPlayerId.Value);
                    if (current != null)
                    {
                        _shortlists.UpdatePlayer(
                            current.Id,
                            ToShortlistStatus(report.OverallRecommendation),
                            current.Priority,
                            ToShortlistFollowUp(report.FollowUpAction),
                            assignment.RoleName,
                            report.OverallRecommendation.ToString(),
                            report.FinalSummary);
                        warnings.Add("Linked shortlist status was updated from the scout recommendation.");
                    }
                }

                return new ScoutDeskWorkflowResult(
                    true,
                    "Scout report submitted. No signing action was taken.",
                    assignment,
                    report,
                    warnings,
                    new List<string>());
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return ScoutDeskWorkflowResult.Failure("Scout report could not be submitted safely.", new[] { ex.Message });
            }
        }

        public ScoutDeskPageViewModel BuildPageViewModel(ScoutDeskQuery? query)
        {
            var assignments = _repository.LoadAssignments(query);
            var rowsById = LoadRecruitmentRowsById();
            var cards = assignments.Select(assignment => new ScoutAssignmentCardViewModel(BuildRow(assignment, rowsById))).ToList();
            var selected = assignments.Count == 0 ? null : BuildAssignmentDetailViewModel(assignments[0].Id);
            return new ScoutDeskPageViewModel(
                cards,
                selected,
                new ScoutReportFormViewModel(),
                assignments.Count == 0 ? "No scout assignments yet. Create one from Shortlists or a persisted player ID." : "Scout Desk loaded persisted local assignments.",
                _connectionFactory.DatabasePath);
        }

        public ScoutAssignmentDetailViewModel BuildAssignmentDetailViewModel(long assignmentId)
        {
            var assignment = _repository.LoadAssignment(assignmentId);
            if (assignment == null)
            {
                return null!;
            }

            var row = BuildRow(assignment, LoadRecruitmentRowsById());
            var profile = _profiles.Query(new PlayerProfileQuery { StatlynPlayerId = assignment.StatlynPlayerId, IncludeBlockedAudit = true });
            var prompts = profile.Success
                ? _questionGenerator.Generate(profile)
                : _questionGenerator.Generate(assignment.PositionGroup, Array.Empty<string>(), 0, 100);
            var reports = _repository.LoadReportsForAssignment(assignment.Id)
                .Select(report => new ScoutReportViewModel(report, _repository.LoadQuestionsForReport(report.Id)))
                .ToList();

            return new ScoutAssignmentDetailViewModel(
                new ScoutAssignmentCardViewModel(row),
                prompts.Select(prompt => new ScoutQuestionPromptViewModel(prompt)).ToList(),
                new ScoutReportHistoryViewModel(reports),
                "Qualitative scout notes only, no hidden values.");
        }

        public ScoutLatestReportSummaryViewModel BuildLatestReportSummary(string statlynPlayerId)
        {
            return new ScoutLatestReportSummaryViewModel(_repository.LoadLatestReportForPlayer(statlynPlayerId));
        }

        private ScoutDeskPlayerRow BuildRow(ScoutAssignmentRecord assignment, IReadOnlyDictionary<string, RecruitmentCentrePlayerRowViewModel> rowsById)
        {
            var latest = _repository.LoadLatestReportForPlayer(assignment.StatlynPlayerId);
            var shortlistStatus = string.Empty;
            if (assignment.ShortlistPlayerId.HasValue)
            {
                var shortlistPlayer = _shortlists.LoadPlayer(assignment.ShortlistPlayerId.Value);
                shortlistStatus = shortlistPlayer == null ? string.Empty : shortlistPlayer.Status.ToString();
            }

            if (rowsById.TryGetValue(assignment.StatlynPlayerId, out var row))
            {
                return new ScoutDeskPlayerRow(
                    assignment,
                    row.Name,
                    row.Position,
                    shortlistStatus,
                    latest,
                    row.MissingDataCount,
                    row.BlockedFieldCount,
                    row.IsLiveFm26Data,
                    row.Warnings);
            }

            var profile = _profiles.Query(new PlayerProfileQuery { StatlynPlayerId = assignment.StatlynPlayerId, IncludeBlockedAudit = true });
            return new ScoutDeskPlayerRow(
                assignment,
                profile.Success && profile.Player != null ? profile.Player.DisplayName : "Persisted player",
                string.IsNullOrWhiteSpace(assignment.PositionGroup) ? "Unknown" : assignment.PositionGroup,
                shortlistStatus,
                latest,
                profile.RoleOutputSummary == null ? 0 : profile.RoleOutputSummary.MissingCoreMetrics.Count,
                profile.BlockedFields.Count,
                profile.IsLiveFm26Data,
                profile.Warnings);
        }

        private IReadOnlyDictionary<string, RecruitmentCentrePlayerRowViewModel> LoadRecruitmentRowsById()
        {
            var result = _recruitmentCentre.Query(new RecruitmentCentreQuery { Limit = 500, SortBy = "DisplayName", SortDirection = "Ascending" });
            return result.Players
                .Select(RecruitmentCentrePlayerRowViewModel.From)
                .GroupBy(row => row.StatlynPlayerId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        private static ShortlistStatus ToShortlistStatus(ScoutReportRecommendation recommendation)
        {
            switch (recommendation)
            {
                case ScoutReportRecommendation.StrongTarget:
                    return ShortlistStatus.StrongTarget;
                case ScoutReportRecommendation.Shortlist:
                    return ShortlistStatus.Shortlist;
                case ScoutReportRecommendation.DevelopmentTarget:
                    return ShortlistStatus.DevelopmentTarget;
                case ScoutReportRecommendation.Reject:
                    return ShortlistStatus.Rejected;
                case ScoutReportRecommendation.NotForRole:
                    return ShortlistStatus.NotForRole;
                case ScoutReportRecommendation.TooRisky:
                    return ShortlistStatus.TooRisky;
                case ScoutReportRecommendation.Watchlist:
                case ScoutReportRecommendation.Unclear:
                    return ShortlistStatus.Watchlist;
                default:
                    return ShortlistStatus.ScoutFurther;
            }
        }

        private static ShortlistFollowUpAction ToShortlistFollowUp(ScoutFollowUpAction action)
        {
            switch (action)
            {
                case ScoutFollowUpAction.ScoutAgain:
                    return ShortlistFollowUpAction.ScoutAgain;
                case ScoutFollowUpAction.WatchMore:
                    return ShortlistFollowUpAction.WatchMore;
                case ScoutFollowUpAction.CompareAlternatives:
                    return ShortlistFollowUpAction.CompareAlternatives;
                case ScoutFollowUpAction.CheckAvailability:
                    return ShortlistFollowUpAction.CheckAvailability;
                case ScoutFollowUpAction.CheckWage:
                    return ShortlistFollowUpAction.CheckWage;
                case ScoutFollowUpAction.CheckMedical:
                    return ShortlistFollowUpAction.CheckMedical;
                case ScoutFollowUpAction.CheckWorkPermit:
                    return ShortlistFollowUpAction.CheckWorkPermit;
                case ScoutFollowUpAction.ReviewRoleFit:
                    return ShortlistFollowUpAction.ReviewRoleFit;
                case ScoutFollowUpAction.Reject:
                    return ShortlistFollowUpAction.Reject;
                default:
                    return ShortlistFollowUpAction.None;
            }
        }
    }
}
