using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Data.Scouting;

namespace Statlyn.Data.RoleLab
{
    public sealed class RoleLabWorkflowResult
    {
        public RoleLabWorkflowResult(bool success, string safeMessage, TacticalRoleModel? role, TacticalRolePairModel? pair, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            Success = success;
            SafeMessage = safeMessage ?? string.Empty;
            Role = role;
            Pair = pair;
            Warnings = warnings ?? new List<string>();
            Errors = errors ?? new List<string>();
        }

        public bool Success { get; }

        public string SafeMessage { get; }

        public TacticalRoleModel? Role { get; }

        public TacticalRolePairModel? Pair { get; }

        public IReadOnlyList<string> Warnings { get; }

        public IReadOnlyList<string> Errors { get; }

        public static RoleLabWorkflowResult Failure(string message, IReadOnlyList<string> errors)
        {
            return new RoleLabWorkflowResult(false, message, null, null, new List<string>(), errors ?? new[] { message });
        }
    }

    public sealed class CreateTacticalRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;

        public TacticalPhase TacticalPhase { get; set; } = TacticalPhase.InPossession;

        public TacticalRoleFamily RoleFamily { get; set; } = TacticalRoleFamily.BuildUp;

        public string PositionGroup { get; set; } = string.Empty;

        public IReadOnlyList<TacticalSlot> ValidSlots { get; set; } = new List<TacticalSlot>();

        public string MovementBehaviour { get; set; } = string.Empty;

        public string BuildUpBehaviour { get; set; } = string.Empty;

        public string FinalThirdBehaviour { get; set; } = string.Empty;

        public string PressingBehaviour { get; set; } = string.Empty;

        public string DefensiveBlockBehaviour { get; set; } = string.Empty;

        public string TransitionBehaviour { get; set; } = string.Empty;
    }

    public sealed class CreateTacticalRolePairRequest
    {
        public string PairName { get; set; } = string.Empty;

        public long InPossessionRoleId { get; set; }

        public long OutOfPossessionRoleId { get; set; }

        public TacticalSlot InPossessionSlot { get; set; } = TacticalSlot.CMC;

        public TacticalSlot OutOfPossessionSlot { get; set; } = TacticalSlot.CMC;

        public string InPossessionFormation { get; set; } = string.Empty;

        public string OutOfPossessionFormation { get; set; } = string.Empty;

        public int TransitionComplexityScore { get; set; }

        public int TacticalRiskScore { get; set; }

        public string PositionalFamiliarityNeed { get; set; } = string.Empty;
    }

    public sealed class RoleLabPageViewModel
    {
        public RoleLabPageViewModel(
            IReadOnlyList<TacticalRoleCardViewModel> roles,
            IReadOnlyList<TacticalRolePairCardViewModel> rolePairs,
            TacticalRoleDetailViewModel? selectedRole,
            string safeMessage,
            string databasePath)
        {
            Roles = roles ?? new List<TacticalRoleCardViewModel>();
            RolePairs = rolePairs ?? new List<TacticalRolePairCardViewModel>();
            SelectedRole = selectedRole;
            SafeMessage = safeMessage ?? string.Empty;
            DatabasePath = databasePath ?? string.Empty;
            PhaseOptions = Enum.GetNames(typeof(TacticalPhase)).ToList();
            FamilyOptions = Enum.GetNames(typeof(TacticalRoleFamily)).ToList();
            SlotOptions = Enum.GetNames(typeof(TacticalSlot)).ToList();
        }

        public IReadOnlyList<TacticalRoleCardViewModel> Roles { get; }

        public IReadOnlyList<TacticalRolePairCardViewModel> RolePairs { get; }

        public TacticalRoleDetailViewModel? SelectedRole { get; }

        public string SafeMessage { get; }

        public string DatabasePath { get; }

        public IReadOnlyList<string> PhaseOptions { get; }

        public IReadOnlyList<string> FamilyOptions { get; }

        public IReadOnlyList<string> SlotOptions { get; }
    }

    public sealed class TacticalRoleCardViewModel
    {
        public TacticalRoleCardViewModel(TacticalRoleModel role, int metricRequirementCount, int scoutQuestionCount, int redFlagCount)
        {
            RoleId = role == null ? 0 : role.Id;
            RoleName = role == null ? string.Empty : ScoutTextSanitizer.Sanitize(role.RoleName);
            Phase = role == null ? string.Empty : role.TacticalPhase.ToString();
            Family = role == null ? string.Empty : role.RoleFamily.ToString();
            Source = role == null ? string.Empty : role.Source.ToString();
            OfficialStatus = role != null && role.IsOfficialFm26Role ? "Official FM26 mapping" : "Generic/import role template; FM26 validation pending.";
            PositionGroup = role == null ? string.Empty : ScoutTextSanitizer.Sanitize(role.PositionGroup);
            ValidSlots = role == null ? string.Empty : string.Join(", ", role.ValidSlots.Select(slot => slot.ToString()));
            MetricRequirementCount = metricRequirementCount.ToString(CultureInfo.InvariantCulture);
            ScoutQuestionCount = scoutQuestionCount.ToString(CultureInfo.InvariantCulture);
            RedFlagCount = redFlagCount.ToString(CultureInfo.InvariantCulture);
            IsArchived = role != null && role.IsArchived;
        }

        public long RoleId { get; }

        public string RoleName { get; }

        public string Phase { get; }

        public string Family { get; }

        public string Source { get; }

        public string OfficialStatus { get; }

        public string PositionGroup { get; }

        public string ValidSlots { get; }

        public string MetricRequirementCount { get; }

        public string ScoutQuestionCount { get; }

        public string RedFlagCount { get; }

        public bool IsArchived { get; }
    }

    public sealed class TacticalRoleDetailViewModel
    {
        public TacticalRoleDetailViewModel(
            TacticalRoleCardViewModel role,
            string movementBehaviour,
            string buildUpBehaviour,
            string finalThirdBehaviour,
            string pressingBehaviour,
            string defensiveBlockBehaviour,
            string transitionBehaviour,
            IReadOnlyList<MetricRequirementViewModel> metricRequirements,
            IReadOnlyList<ScoutQuestionViewModel> scoutQuestions,
            IReadOnlyList<RedFlagViewModel> redFlags)
        {
            Role = role;
            MovementBehaviour = ScoutTextSanitizer.Sanitize(movementBehaviour);
            BuildUpBehaviour = ScoutTextSanitizer.Sanitize(buildUpBehaviour);
            FinalThirdBehaviour = ScoutTextSanitizer.Sanitize(finalThirdBehaviour);
            PressingBehaviour = ScoutTextSanitizer.Sanitize(pressingBehaviour);
            DefensiveBlockBehaviour = ScoutTextSanitizer.Sanitize(defensiveBlockBehaviour);
            TransitionBehaviour = ScoutTextSanitizer.Sanitize(transitionBehaviour);
            MetricRequirements = metricRequirements ?? new List<MetricRequirementViewModel>();
            ScoutQuestions = scoutQuestions ?? new List<ScoutQuestionViewModel>();
            RedFlags = redFlags ?? new List<RedFlagViewModel>();
            SafeNotice = "Output metrics lead; attributes are support-only; FM26 validation pending.";
        }

        public TacticalRoleCardViewModel Role { get; }

        public string MovementBehaviour { get; }

        public string BuildUpBehaviour { get; }

        public string FinalThirdBehaviour { get; }

        public string PressingBehaviour { get; }

        public string DefensiveBlockBehaviour { get; }

        public string TransitionBehaviour { get; }

        public IReadOnlyList<MetricRequirementViewModel> MetricRequirements { get; }

        public IReadOnlyList<ScoutQuestionViewModel> ScoutQuestions { get; }

        public IReadOnlyList<RedFlagViewModel> RedFlags { get; }

        public string SafeNotice { get; }
    }

    public sealed class TacticalRolePairCardViewModel
    {
        public TacticalRolePairCardViewModel(TacticalRolePairModel pair, TacticalRoleModel? inPossessionRole, TacticalRoleModel? outOfPossessionRole)
        {
            PairId = pair == null ? 0 : pair.Id;
            PairName = pair == null ? string.Empty : ScoutTextSanitizer.Sanitize(pair.PairName);
            InPossessionRole = inPossessionRole == null ? "Missing IP role" : ScoutTextSanitizer.Sanitize(inPossessionRole.RoleName);
            OutOfPossessionRole = outOfPossessionRole == null ? "Missing OOP role" : ScoutTextSanitizer.Sanitize(outOfPossessionRole.RoleName);
            InPossessionSlot = pair == null ? string.Empty : pair.InPossessionSlot.ToString();
            OutOfPossessionSlot = pair == null ? string.Empty : pair.OutOfPossessionSlot.ToString();
            InPossessionFormation = pair == null ? string.Empty : ScoutTextSanitizer.Sanitize(pair.InPossessionFormation);
            OutOfPossessionFormation = pair == null ? string.Empty : ScoutTextSanitizer.Sanitize(pair.OutOfPossessionFormation);
            TransitionComplexityScore = pair == null ? "0" : pair.TransitionComplexityScore.ToString(CultureInfo.InvariantCulture);
            TacticalRiskScore = pair == null ? "0" : pair.TacticalRiskScore.ToString(CultureInfo.InvariantCulture);
            PositionalFamiliarityNeed = pair == null ? string.Empty : ScoutTextSanitizer.Sanitize(pair.PositionalFamiliarityNeed);
        }

        public long PairId { get; }

        public string PairName { get; }

        public string InPossessionRole { get; }

        public string OutOfPossessionRole { get; }

        public string InPossessionSlot { get; }

        public string OutOfPossessionSlot { get; }

        public string InPossessionFormation { get; }

        public string OutOfPossessionFormation { get; }

        public string TransitionComplexityScore { get; }

        public string TacticalRiskScore { get; }

        public string PositionalFamiliarityNeed { get; }
    }

    public sealed class MetricRequirementViewModel
    {
        public MetricRequirementViewModel(RoleOutputMetricRequirementModel requirement)
        {
            Id = requirement == null ? 0 : requirement.Id;
            MetricKey = requirement == null ? string.Empty : ScoutTextSanitizer.Sanitize(requirement.MetricKey);
            FieldName = requirement == null ? string.Empty : ScoutTextSanitizer.Sanitize(requirement.FieldName);
            Weight = requirement == null ? "0" : requirement.Weight.ToString("0.##", CultureInfo.InvariantCulture);
            Importance = requirement == null ? string.Empty : requirement.Importance.ToString();
            Direction = requirement == null ? string.Empty : requirement.Direction.ToString();
            MinimumSampleMinutes = requirement == null ? "0" : requirement.MinimumSampleMinutes.ToString(CultureInfo.InvariantCulture);
            Per90Required = requirement != null && requirement.Per90Required ? "Per 90" : "Raw";
            EvidenceTemplate = requirement == null ? string.Empty : ScoutTextSanitizer.Sanitize(requirement.EvidenceTemplate);
            MissingDataImpact = requirement == null ? string.Empty : ScoutTextSanitizer.Sanitize(requirement.MissingDataImpact);
        }

        public long Id { get; }

        public string MetricKey { get; }

        public string FieldName { get; }

        public string Weight { get; }

        public string Importance { get; }

        public string Direction { get; }

        public string MinimumSampleMinutes { get; }

        public string Per90Required { get; }

        public string EvidenceTemplate { get; }

        public string MissingDataImpact { get; }
    }

    public sealed class ScoutQuestionViewModel
    {
        public ScoutQuestionViewModel(RoleScoutQuestionModel question)
        {
            Id = question == null ? 0 : question.Id;
            Category = question == null ? string.Empty : ScoutTextSanitizer.Sanitize(question.Category);
            Question = question == null ? string.Empty : ScoutTextSanitizer.Sanitize(question.Question);
            WhyItMatters = question == null ? string.Empty : ScoutTextSanitizer.Sanitize(question.WhyItMatters);
            SuggestedObservationType = question == null ? string.Empty : ScoutTextSanitizer.Sanitize(question.SuggestedObservationType);
        }

        public long Id { get; }

        public string Category { get; }

        public string Question { get; }

        public string WhyItMatters { get; }

        public string SuggestedObservationType { get; }
    }

    public sealed class RedFlagViewModel
    {
        public RedFlagViewModel(RoleRedFlagModel redFlag)
        {
            Id = redFlag == null ? 0 : redFlag.Id;
            FieldName = redFlag == null ? string.Empty : ScoutTextSanitizer.Sanitize(redFlag.FieldName);
            Operator = redFlag == null ? string.Empty : ScoutTextSanitizer.Sanitize(redFlag.Operator);
            Threshold = redFlag == null ? string.Empty : ScoutTextSanitizer.Sanitize(redFlag.Threshold);
            Message = redFlag == null ? string.Empty : ScoutTextSanitizer.Sanitize(redFlag.Message);
            AppliesToPhase = redFlag == null ? string.Empty : redFlag.AppliesToPhase.ToString();
        }

        public long Id { get; }

        public string FieldName { get; }

        public string Operator { get; }

        public string Threshold { get; }

        public string Message { get; }

        public string AppliesToPhase { get; }
    }
}
