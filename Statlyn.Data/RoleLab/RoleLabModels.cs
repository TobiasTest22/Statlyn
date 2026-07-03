using System;
using System.Collections.Generic;

namespace Statlyn.Data.RoleLab
{
    public enum TacticalPhase
    {
        InPossession = 0,
        OutOfPossession = 1,
        DualPhasePair = 2
    }

    public enum TacticalRoleSource
    {
        BuiltInSeed = 0,
        UserCreated = 1,
        Imported = 2,
        FM26Mapped = 3
    }

    public enum TacticalRoleFamily
    {
        Goalkeeper = 0,
        CentreBack = 1,
        FullBackWingBack = 2,
        DefensiveMidfield = 3,
        CentralMidfield = 4,
        AttackingMidfield = 5,
        WideAttacker = 6,
        Forward = 7,
        HighPress = 8,
        MidBlock = 9,
        LowBlock = 10,
        RecoveryCover = 11,
        CentralScreening = 12,
        WideDefensive = 13,
        BuildUp = 14,
        ChanceCreation = 15,
        GoalThreat = 16
    }

    public enum TacticalSlot
    {
        GK = 0,
        CBL = 1,
        CBC = 2,
        CBR = 3,
        FBL = 4,
        FBR = 5,
        WBL = 6,
        WBR = 7,
        DM = 8,
        CML = 9,
        CMC = 10,
        CMR = 11,
        AML = 12,
        AMC = 13,
        AMR = 14,
        WL = 15,
        WR = 16,
        ST = 17
    }

    public enum RoleMetricImportance
    {
        Core = 0,
        Important = 1,
        Useful = 2,
        ContextOnly = 3
    }

    public enum RoleMetricDirection
    {
        HigherBetter = 0,
        LowerBetter = 1,
        Range = 2
    }

    public sealed class TacticalRoleModel
    {
        public TacticalRoleModel(
            long id,
            string roleName,
            TacticalPhase tacticalPhase,
            TacticalRoleFamily roleFamily,
            TacticalRoleSource source,
            bool isOfficialFm26Role,
            string fm26RoleId,
            string positionGroup,
            IReadOnlyList<TacticalSlot> validSlots,
            string movementBehaviour,
            string buildUpBehaviour,
            string finalThirdBehaviour,
            string pressingBehaviour,
            string defensiveBlockBehaviour,
            string transitionBehaviour,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc,
            bool isArchived)
        {
            Id = id;
            RoleName = roleName ?? string.Empty;
            TacticalPhase = tacticalPhase;
            RoleFamily = roleFamily;
            Source = source;
            IsOfficialFm26Role = isOfficialFm26Role;
            Fm26RoleId = fm26RoleId ?? string.Empty;
            PositionGroup = positionGroup ?? string.Empty;
            ValidSlots = validSlots ?? new List<TacticalSlot>();
            MovementBehaviour = movementBehaviour ?? string.Empty;
            BuildUpBehaviour = buildUpBehaviour ?? string.Empty;
            FinalThirdBehaviour = finalThirdBehaviour ?? string.Empty;
            PressingBehaviour = pressingBehaviour ?? string.Empty;
            DefensiveBlockBehaviour = defensiveBlockBehaviour ?? string.Empty;
            TransitionBehaviour = transitionBehaviour ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            IsArchived = isArchived;
        }

        public long Id { get; }

        public string RoleName { get; }

        public TacticalPhase TacticalPhase { get; }

        public TacticalRoleFamily RoleFamily { get; }

        public TacticalRoleSource Source { get; }

        public bool IsOfficialFm26Role { get; }

        public string Fm26RoleId { get; }

        public string PositionGroup { get; }

        public IReadOnlyList<TacticalSlot> ValidSlots { get; }

        public string MovementBehaviour { get; }

        public string BuildUpBehaviour { get; }

        public string FinalThirdBehaviour { get; }

        public string PressingBehaviour { get; }

        public string DefensiveBlockBehaviour { get; }

        public string TransitionBehaviour { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }

        public bool IsArchived { get; }

        public TacticalRoleModel WithId(long id, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
        {
            return new TacticalRoleModel(
                id,
                RoleName,
                TacticalPhase,
                RoleFamily,
                Source,
                IsOfficialFm26Role,
                Fm26RoleId,
                PositionGroup,
                ValidSlots,
                MovementBehaviour,
                BuildUpBehaviour,
                FinalThirdBehaviour,
                PressingBehaviour,
                DefensiveBlockBehaviour,
                TransitionBehaviour,
                createdAtUtc,
                updatedAtUtc,
                IsArchived);
        }
    }

    public sealed class TacticalRolePairModel
    {
        public TacticalRolePairModel(
            long id,
            string pairName,
            long inPossessionRoleId,
            long outOfPossessionRoleId,
            TacticalSlot inPossessionSlot,
            TacticalSlot outOfPossessionSlot,
            string inPossessionFormation,
            string outOfPossessionFormation,
            int transitionComplexityScore,
            int tacticalRiskScore,
            string positionalFamiliarityNeed,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc,
            bool isArchived)
        {
            Id = id;
            PairName = pairName ?? string.Empty;
            InPossessionRoleId = inPossessionRoleId;
            OutOfPossessionRoleId = outOfPossessionRoleId;
            InPossessionSlot = inPossessionSlot;
            OutOfPossessionSlot = outOfPossessionSlot;
            InPossessionFormation = inPossessionFormation ?? string.Empty;
            OutOfPossessionFormation = outOfPossessionFormation ?? string.Empty;
            TransitionComplexityScore = transitionComplexityScore;
            TacticalRiskScore = tacticalRiskScore;
            PositionalFamiliarityNeed = positionalFamiliarityNeed ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
            IsArchived = isArchived;
        }

        public long Id { get; }

        public string PairName { get; }

        public long InPossessionRoleId { get; }

        public long OutOfPossessionRoleId { get; }

        public TacticalSlot InPossessionSlot { get; }

        public TacticalSlot OutOfPossessionSlot { get; }

        public string InPossessionFormation { get; }

        public string OutOfPossessionFormation { get; }

        public int TransitionComplexityScore { get; }

        public int TacticalRiskScore { get; }

        public string PositionalFamiliarityNeed { get; }

        public DateTimeOffset CreatedAtUtc { get; }

        public DateTimeOffset UpdatedAtUtc { get; }

        public bool IsArchived { get; }
    }

    public sealed class RoleOutputMetricRequirementModel
    {
        public RoleOutputMetricRequirementModel(
            long id,
            long? tacticalRoleId,
            long? rolePairId,
            string metricKey,
            string fieldName,
            double weight,
            RoleMetricImportance importance,
            RoleMetricDirection direction,
            int minimumSampleMinutes,
            bool per90Required,
            string normalizationHint,
            string evidenceTemplate,
            string missingDataImpact)
        {
            Id = id;
            TacticalRoleId = tacticalRoleId;
            RolePairId = rolePairId;
            MetricKey = metricKey ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Weight = weight;
            Importance = importance;
            Direction = direction;
            MinimumSampleMinutes = minimumSampleMinutes;
            Per90Required = per90Required;
            NormalizationHint = normalizationHint ?? string.Empty;
            EvidenceTemplate = evidenceTemplate ?? string.Empty;
            MissingDataImpact = missingDataImpact ?? string.Empty;
        }

        public long Id { get; }

        public long? TacticalRoleId { get; }

        public long? RolePairId { get; }

        public string MetricKey { get; }

        public string FieldName { get; }

        public double Weight { get; }

        public RoleMetricImportance Importance { get; }

        public RoleMetricDirection Direction { get; }

        public int MinimumSampleMinutes { get; }

        public bool Per90Required { get; }

        public string NormalizationHint { get; }

        public string EvidenceTemplate { get; }

        public string MissingDataImpact { get; }
    }

    public sealed class RoleScoutQuestionModel
    {
        public RoleScoutQuestionModel(long id, long? tacticalRoleId, long? rolePairId, string category, string question, string whyItMatters, string suggestedObservationType)
        {
            Id = id;
            TacticalRoleId = tacticalRoleId;
            RolePairId = rolePairId;
            Category = category ?? string.Empty;
            Question = question ?? string.Empty;
            WhyItMatters = whyItMatters ?? string.Empty;
            SuggestedObservationType = suggestedObservationType ?? string.Empty;
        }

        public long Id { get; }

        public long? TacticalRoleId { get; }

        public long? RolePairId { get; }

        public string Category { get; }

        public string Question { get; }

        public string WhyItMatters { get; }

        public string SuggestedObservationType { get; }
    }

    public sealed class RoleRedFlagModel
    {
        public RoleRedFlagModel(long id, long? tacticalRoleId, long? rolePairId, string fieldName, string operatorValue, string threshold, string message, TacticalPhase appliesToPhase)
        {
            Id = id;
            TacticalRoleId = tacticalRoleId;
            RolePairId = rolePairId;
            FieldName = fieldName ?? string.Empty;
            Operator = operatorValue ?? string.Empty;
            Threshold = threshold ?? string.Empty;
            Message = message ?? string.Empty;
            AppliesToPhase = appliesToPhase;
        }

        public long Id { get; }

        public long? TacticalRoleId { get; }

        public long? RolePairId { get; }

        public string FieldName { get; }

        public string Operator { get; }

        public string Threshold { get; }

        public string Message { get; }

        public TacticalPhase AppliesToPhase { get; }
    }
}
