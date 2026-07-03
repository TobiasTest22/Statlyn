using System;
using System.Collections.Generic;
using Statlyn.Data.Shortlists;

namespace Statlyn.Data.Scouting
{
    public enum ScoutAssignmentStatus
    {
        Open = 0,
        InProgress = 1,
        ReportSubmitted = 2,
        Closed = 3,
        Cancelled = 4
    }

    public enum ScoutReportRecommendation
    {
        StrongTarget = 0,
        Shortlist = 1,
        ScoutFurther = 2,
        Watchlist = 3,
        DevelopmentTarget = 4,
        Reject = 5,
        NotForRole = 6,
        TooRisky = 7,
        Unclear = 8
    }

    public enum ScoutObservationRating
    {
        Unknown = 0,
        Poor = 1,
        BelowAverage = 2,
        Average = 3,
        Good = 4,
        VeryGood = 5,
        Excellent = 6
    }

    public enum ScoutFollowUpAction
    {
        None = 0,
        ScoutAgain = 1,
        WatchMore = 2,
        CompareAlternatives = 3,
        CheckAvailability = 4,
        CheckWage = 5,
        CheckMedical = 6,
        CheckWorkPermit = 7,
        ReviewRoleFit = 8,
        Reject = 9
    }

    public sealed class ScoutAssignmentRecord
    {
        public ScoutAssignmentRecord(
            long id,
            string statlynPlayerId,
            long? shortlistPlayerId,
            long? shortlistId,
            long playerId,
            string assignmentTitle,
            string roleName,
            string positionGroup,
            ShortlistPriority priority,
            ScoutAssignmentStatus status,
            string assignedTo,
            DateTimeOffset createdAtUtc,
            DateTimeOffset? dueAtUtc,
            DateTimeOffset updatedAtUtc,
            DateTimeOffset? closedAtUtc,
            string sourceName,
            bool isArchived)
        {
            Id = id;
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            ShortlistPlayerId = shortlistPlayerId;
            ShortlistId = shortlistId;
            PlayerId = playerId;
            AssignmentTitle = assignmentTitle ?? string.Empty;
            RoleName = roleName ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            Priority = priority;
            Status = status;
            AssignedTo = assignedTo ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            DueAtUtc = dueAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            ClosedAtUtc = closedAtUtc;
            SourceName = sourceName ?? string.Empty;
            IsArchived = isArchived;
        }

        public long Id { get; }

        public string StatlynPlayerId { get; }

        public long? ShortlistPlayerId { get; }

        public long? ShortlistId { get; }

        public long PlayerId { get; }

        public string AssignmentTitle { get; }

        public string RoleName { get; }

        public string PositionGroup { get; }

        public ShortlistPriority Priority { get; }

        public ScoutAssignmentStatus Status { get; }

        public string AssignedTo { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset? DueAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }

        public DateTimeOffset? ClosedAtUtc { get; }

        public string SourceName { get; }

        public bool IsArchived { get; }
    }

    public sealed class ScoutReportRecord
    {
        public ScoutReportRecord(
            long id,
            long? assignmentId,
            long playerId,
            string statlynPlayerId,
            DateTimeOffset reportDateUtc,
            string roleAssessed,
            ScoutObservationRating technicalRating,
            ScoutObservationRating tacticalRating,
            ScoutObservationRating physicalRating,
            ScoutObservationRating mentalRating,
            ScoutReportRecommendation overallRecommendation,
            int confidence,
            string strengths,
            string weaknesses,
            string risks,
            ScoutFollowUpAction followUpAction,
            string finalSummary,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            Id = id;
            AssignmentId = assignmentId;
            PlayerId = playerId;
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            ReportDateUtc = reportDateUtc;
            RoleAssessed = roleAssessed ?? string.Empty;
            TechnicalRating = technicalRating;
            TacticalRating = tacticalRating;
            PhysicalRating = physicalRating;
            MentalRating = mentalRating;
            OverallRecommendation = overallRecommendation;
            Confidence = confidence;
            Strengths = strengths ?? string.Empty;
            Weaknesses = weaknesses ?? string.Empty;
            Risks = risks ?? string.Empty;
            FollowUpAction = followUpAction;
            FinalSummary = finalSummary ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public long Id { get; }

        public long? AssignmentId { get; }

        public long PlayerId { get; }

        public string StatlynPlayerId { get; }

        public DateTimeOffset ReportDateUtc { get; }

        public string RoleAssessed { get; }

        public ScoutObservationRating TechnicalRating { get; }

        public ScoutObservationRating TacticalRating { get; }

        public ScoutObservationRating PhysicalRating { get; }

        public ScoutObservationRating MentalRating { get; }

        public ScoutReportRecommendation OverallRecommendation { get; }

        public int Confidence { get; }

        public string Strengths { get; }

        public string Weaknesses { get; }

        public string Risks { get; }

        public ScoutFollowUpAction FollowUpAction { get; }

        public string FinalSummary { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }
    }

    public sealed class ScoutReportQuestionRecord
    {
        public ScoutReportQuestionRecord(long id, long reportId, string question, string answer, string category, DateTimeOffset createdAtUtc)
        {
            Id = id;
            ReportId = reportId;
            Question = question ?? string.Empty;
            Answer = answer ?? string.Empty;
            Category = category ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
        }

        public long Id { get; }

        public long ReportId { get; }

        public string Question { get; }

        public string Answer { get; }

        public string Category { get; }

        public DateTimeOffset CreatedAtUtc { get; }
    }

    public sealed class ScoutDeskPlayerRow
    {
        public ScoutDeskPlayerRow(
            ScoutAssignmentRecord assignment,
            string playerName,
            string position,
            string shortlistStatus,
            ScoutReportRecord? latestReport,
            int missingOutputCount,
            int blockedAuditCount,
            bool isLiveFm26Data,
            IReadOnlyList<string> warnings)
        {
            Assignment = assignment;
            PlayerName = playerName ?? string.Empty;
            Position = position ?? string.Empty;
            ShortlistStatus = shortlistStatus ?? string.Empty;
            LatestReport = latestReport;
            MissingOutputCount = missingOutputCount;
            BlockedAuditCount = blockedAuditCount;
            IsLiveFm26Data = isLiveFm26Data;
            Warnings = warnings ?? new List<string>();
        }

        public ScoutAssignmentRecord Assignment { get; }

        public string PlayerName { get; }

        public string Position { get; }

        public string ShortlistStatus { get; }

        public ScoutReportRecord? LatestReport { get; }

        public int MissingOutputCount { get; }

        public int BlockedAuditCount { get; }

        public bool IsLiveFm26Data { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class ScoutDeskResult
    {
        public ScoutDeskResult(bool success, string safeMessage, IReadOnlyList<ScoutAssignmentRecord> assignments, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            Assignments = assignments ?? new List<ScoutAssignmentRecord>();
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public IReadOnlyList<ScoutAssignmentRecord> Assignments { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }
    }

    public sealed class ScoutDeskQuery
    {
        public string StatlynPlayerId { get; set; } = string.Empty;

        public ScoutAssignmentStatus? Status { get; set; }

        public bool IncludeArchived { get; set; }

        public int Limit { get; set; } = 100;
    }

    public sealed class CreateScoutAssignmentRequest
    {
        public string StatlynPlayerId { get; set; } = string.Empty;

        public long? ShortlistPlayerId { get; set; }

        public long? ShortlistId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public ShortlistPriority Priority { get; set; } = ShortlistPriority.Medium;

        public string AssignedTo { get; set; } = string.Empty;

        public DateTimeOffset? DueAtUtc { get; set; }

        public string AssignmentTitle { get; set; } = string.Empty;
    }

    public sealed class SubmitScoutReportRequest
    {
        public long? AssignmentId { get; set; }

        public string StatlynPlayerId { get; set; } = string.Empty;

        public string RoleAssessed { get; set; } = string.Empty;

        public ScoutObservationRating TechnicalRating { get; set; } = ScoutObservationRating.Unknown;

        public ScoutObservationRating TacticalRating { get; set; } = ScoutObservationRating.Unknown;

        public ScoutObservationRating PhysicalRating { get; set; } = ScoutObservationRating.Unknown;

        public ScoutObservationRating MentalRating { get; set; } = ScoutObservationRating.Unknown;

        public ScoutReportRecommendation OverallRecommendation { get; set; } = ScoutReportRecommendation.ScoutFurther;

        public int Confidence { get; set; } = 50;

        public string Strengths { get; set; } = string.Empty;

        public string Weaknesses { get; set; } = string.Empty;

        public string Risks { get; set; } = string.Empty;

        public ScoutFollowUpAction FollowUpAction { get; set; } = ScoutFollowUpAction.None;

        public string FinalSummary { get; set; } = string.Empty;

        public bool UpdateShortlistFromReport { get; set; }

        public IReadOnlyList<ScoutQuestionAnswerRequest> QuestionAnswers { get; set; } = new List<ScoutQuestionAnswerRequest>();
    }

    public sealed class ScoutQuestionAnswerRequest
    {
        public string Category { get; set; } = string.Empty;

        public string Question { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;
    }

    public sealed class ScoutQuestionPrompt
    {
        public ScoutQuestionPrompt(string category, string question, string whyItMatters, string suggestedObservationType)
        {
            Category = category ?? string.Empty;
            Question = question ?? string.Empty;
            WhyItMatters = whyItMatters ?? string.Empty;
            SuggestedObservationType = suggestedObservationType ?? string.Empty;
        }

        public string Category { get; }

        public string Question { get; }

        public string WhyItMatters { get; }

        public string SuggestedObservationType { get; }
    }

    public sealed class ScoutDeskWorkflowResult
    {
        public ScoutDeskWorkflowResult(bool success, string safeMessage, ScoutAssignmentRecord? assignment, ScoutReportRecord? report, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            Assignment = assignment;
            Report = report;
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public ScoutAssignmentRecord? Assignment { get; }

        public ScoutReportRecord? Report { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public static ScoutDeskWorkflowResult Failure(string message, IReadOnlyList<string> errors)
        {
            return new ScoutDeskWorkflowResult(false, message, null, null, new List<string>(), errors ?? new[] { message });
        }
    }
}
