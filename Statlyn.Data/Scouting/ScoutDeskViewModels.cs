using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statlyn.Data.Scouting
{
    public sealed class ScoutDeskPageViewModel
    {
        public ScoutDeskPageViewModel(
            IReadOnlyList<ScoutAssignmentCardViewModel> assignments,
            ScoutAssignmentDetailViewModel? selectedAssignment,
            ScoutReportFormViewModel reportForm,
            string safeMessage,
            string databasePath)
        {
            Assignments = assignments ?? new List<ScoutAssignmentCardViewModel>();
            SelectedAssignment = selectedAssignment;
            ReportForm = reportForm ?? new ScoutReportFormViewModel();
            SafeMessage = safeMessage ?? string.Empty;
            DatabasePath = databasePath ?? string.Empty;
        }

        public IReadOnlyList<ScoutAssignmentCardViewModel> Assignments { get; }

        public ScoutAssignmentDetailViewModel? SelectedAssignment { get; }

        public ScoutReportFormViewModel ReportForm { get; }

        public string SafeMessage { get; }

        public string DatabasePath { get; }
    }

    public sealed class ScoutAssignmentCardViewModel
    {
        public ScoutAssignmentCardViewModel(ScoutDeskPlayerRow row)
        {
            var assignment = row == null ? null : row.Assignment;
            AssignmentId = assignment == null ? 0 : assignment.Id;
            StatlynPlayerId = assignment == null ? string.Empty : assignment.StatlynPlayerId;
            PlayerName = row == null ? string.Empty : row.PlayerName;
            Position = row == null ? "Unknown" : row.Position;
            Source = assignment == null ? string.Empty : assignment.SourceName;
            Role = assignment == null ? string.Empty : assignment.RoleName;
            ShortlistStatus = row == null ? string.Empty : row.ShortlistStatus;
            AssignmentStatus = assignment == null ? ScoutAssignmentStatus.Open.ToString() : assignment.Status.ToString();
            Priority = assignment == null ? string.Empty : assignment.Priority.ToString();
            AssignedTo = assignment == null ? string.Empty : assignment.AssignedTo;
            DueDate = assignment == null || !assignment.DueAtUtc.HasValue ? "No due date" : assignment.DueAtUtc.Value.ToString("u", CultureInfo.InvariantCulture);
            LatestReportRecommendation = row == null || row.LatestReport == null ? "No scout report yet" : row.LatestReport.OverallRecommendation.ToString();
            ScoutConfidence = row == null || row.LatestReport == null ? "Unknown" : row.LatestReport.Confidence.ToString(CultureInfo.InvariantCulture);
            LatestReportDate = row == null || row.LatestReport == null ? string.Empty : row.LatestReport.ReportDateUtc.ToString("u", CultureInfo.InvariantCulture);
            LatestReportSummary = row == null || row.LatestReport == null ? "No scout report yet." : ScoutTextSanitizer.Sanitize(row.LatestReport.FinalSummary);
            MissingOutputCount = row == null ? "0" : row.MissingOutputCount.ToString(CultureInfo.InvariantCulture);
            BlockedAuditCount = row == null ? "0" : row.BlockedAuditCount.ToString(CultureInfo.InvariantCulture);
            NoLiveFm26Label = row != null && row.IsLiveFm26Data ? "Live FM26 unsupported" : "No live FM26 data";
            Warnings = row == null ? new List<string>() : row.Warnings;
        }

        public long AssignmentId { get; }

        public string StatlynPlayerId { get; }

        public string PlayerName { get; }

        public string Position { get; }

        public string Source { get; }

        public string Role { get; }

        public string ShortlistStatus { get; }

        public string AssignmentStatus { get; }

        public string Priority { get; }

        public string AssignedTo { get; }

        public string DueDate { get; }

        public string LatestReportRecommendation { get; }

        public string ScoutConfidence { get; }

        public string LatestReportDate { get; }

        public string LatestReportSummary { get; }

        public string MissingOutputCount { get; }

        public string BlockedAuditCount { get; }

        public string NoLiveFm26Label { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class ScoutAssignmentDetailViewModel
    {
        public ScoutAssignmentDetailViewModel(
            ScoutAssignmentCardViewModel assignment,
            IReadOnlyList<ScoutQuestionPromptViewModel> questions,
            ScoutReportHistoryViewModel reportHistory,
            string safeNotice)
        {
            Assignment = assignment;
            Questions = questions ?? new List<ScoutQuestionPromptViewModel>();
            ReportHistory = reportHistory ?? new ScoutReportHistoryViewModel(new List<ScoutReportViewModel>());
            SafeNotice = safeNotice ?? "Qualitative scout notes only, no hidden values.";
        }

        public ScoutAssignmentCardViewModel Assignment { get; }

        public IReadOnlyList<ScoutQuestionPromptViewModel> Questions { get; }

        public ScoutReportHistoryViewModel ReportHistory { get; }

        public string SafeNotice { get; }
    }

    public sealed class ScoutReportViewModel
    {
        public ScoutReportViewModel(ScoutReportRecord report, IReadOnlyList<ScoutReportQuestionRecord> questions)
        {
            ReportId = report == null ? 0 : report.Id;
            RoleAssessed = report == null ? string.Empty : report.RoleAssessed;
            Ratings = report == null
                ? "Unknown"
                : "Technical " + report.TechnicalRating + " | Tactical " + report.TacticalRating + " | Physical " + report.PhysicalRating + " | Mental/character " + report.MentalRating;
            Recommendation = report == null ? ScoutReportRecommendation.Unclear.ToString() : report.OverallRecommendation.ToString();
            Confidence = report == null ? "Unknown" : report.Confidence.ToString(CultureInfo.InvariantCulture);
            Strengths = report == null ? string.Empty : ScoutTextSanitizer.Sanitize(report.Strengths);
            Weaknesses = report == null ? string.Empty : ScoutTextSanitizer.Sanitize(report.Weaknesses);
            Risks = report == null ? string.Empty : ScoutTextSanitizer.Sanitize(report.Risks);
            FollowUpAction = report == null ? ScoutFollowUpAction.None.ToString() : report.FollowUpAction.ToString();
            FinalSummary = report == null ? string.Empty : ScoutTextSanitizer.Sanitize(report.FinalSummary);
            ReportDate = report == null ? string.Empty : report.ReportDateUtc.ToString("u", CultureInfo.InvariantCulture);
            QuestionAnswers = (questions ?? new List<ScoutReportQuestionRecord>())
                .Select(item => ScoutTextSanitizer.Sanitize(item.Category + ": " + item.Question + " - " + item.Answer))
                .ToList();
            SafeNotice = "Qualitative scout notes only, no hidden values.";
        }

        public long ReportId { get; }

        public string RoleAssessed { get; }

        public string Ratings { get; }

        public string Recommendation { get; }

        public string Confidence { get; }

        public string Strengths { get; }

        public string Weaknesses { get; }

        public string Risks { get; }

        public string FollowUpAction { get; }

        public string FinalSummary { get; }

        public string ReportDate { get; }

        public IReadOnlyList<string> QuestionAnswers { get; }

        public string SafeNotice { get; }
    }

    public sealed class ScoutQuestionPromptViewModel
    {
        public ScoutQuestionPromptViewModel(ScoutQuestionPrompt prompt)
        {
            Category = prompt == null ? string.Empty : ScoutTextSanitizer.Sanitize(prompt.Category);
            Question = prompt == null ? string.Empty : ScoutTextSanitizer.Sanitize(prompt.Question);
            WhyItMatters = prompt == null ? string.Empty : ScoutTextSanitizer.Sanitize(prompt.WhyItMatters);
            SuggestedObservationType = prompt == null ? string.Empty : ScoutTextSanitizer.Sanitize(prompt.SuggestedObservationType);
        }

        public string Category { get; }

        public string Question { get; }

        public string WhyItMatters { get; }

        public string SuggestedObservationType { get; }
    }

    public sealed class ScoutReportFormViewModel
    {
        public ScoutReportFormViewModel()
        {
            Ratings = Enum.GetNames(typeof(ScoutObservationRating)).ToList();
            Recommendations = Enum.GetNames(typeof(ScoutReportRecommendation)).ToList();
            FollowUpActions = Enum.GetNames(typeof(ScoutFollowUpAction)).ToList();
            SafeNotice = "Qualitative scout notes only, no hidden values.";
        }

        public IReadOnlyList<string> Ratings { get; }

        public IReadOnlyList<string> Recommendations { get; }

        public IReadOnlyList<string> FollowUpActions { get; }

        public string SafeNotice { get; }
    }

    public sealed class ScoutReportHistoryViewModel
    {
        public ScoutReportHistoryViewModel(IReadOnlyList<ScoutReportViewModel> reports)
        {
            Reports = reports ?? new List<ScoutReportViewModel>();
            EmptyMessage = Reports.Count == 0 ? "No scout report yet." : string.Empty;
        }

        public IReadOnlyList<ScoutReportViewModel> Reports { get; }

        public string EmptyMessage { get; }
    }

    public sealed class ScoutLatestReportSummaryViewModel
    {
        public ScoutLatestReportSummaryViewModel(ScoutReportRecord? report)
        {
            HasReport = report != null;
            Recommendation = report == null ? "No scout report yet" : report.OverallRecommendation.ToString();
            Confidence = report == null ? "Unknown" : report.Confidence.ToString(CultureInfo.InvariantCulture);
            ReportDate = report == null ? string.Empty : report.ReportDateUtc.ToString("u", CultureInfo.InvariantCulture);
            Summary = report == null ? "No scout report yet." : ScoutTextSanitizer.Sanitize(report.FinalSummary);
            SafeNotice = "Qualitative scout notes only, no hidden values.";
        }

        public bool HasReport { get; }

        public string Recommendation { get; }

        public string Confidence { get; }

        public string ReportDate { get; }

        public string Summary { get; }

        public string SafeNotice { get; }
    }
}
