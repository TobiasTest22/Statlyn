using System.Collections.Generic;

namespace Statlyn.Core.Tactics
{
    public enum GamePhase
    {
        InPossession = 0,
        OutOfPossession = 1,
        AttackingTransition = 2,
        DefensiveTransition = 3,
        SetPieces = 4
    }

    public sealed class TacticalSystem
    {
        public TacticalSystem(string name, string formation, TeamStyle style, IReadOnlyList<TacticalRequirement> requirements)
        {
            Name = name ?? string.Empty;
            Formation = formation ?? string.Empty;
            Style = style ?? new TeamStyle(string.Empty, string.Empty, string.Empty);
            Requirements = requirements ?? new List<TacticalRequirement>();
        }

        public string Name { get; }

        public string Formation { get; }

        public TeamStyle Style { get; }

        public IReadOnlyList<TacticalRequirement> Requirements { get; }
    }

    public sealed class TeamStyle
    {
        public TeamStyle(string possessionIntent, string pressingIntent, string transitionIntent)
        {
            PossessionIntent = possessionIntent ?? string.Empty;
            PressingIntent = pressingIntent ?? string.Empty;
            TransitionIntent = transitionIntent ?? string.Empty;
        }

        public string PossessionIntent { get; }

        public string PressingIntent { get; }

        public string TransitionIntent { get; }
    }

    public sealed class TacticalRole
    {
        public TacticalRole(string name, GamePhase phase, string positionGroup, IReadOnlyList<TacticalRequirement> requirements)
        {
            Name = name ?? string.Empty;
            Phase = phase;
            PositionGroup = positionGroup ?? string.Empty;
            Requirements = requirements ?? new List<TacticalRequirement>();
        }

        public string Name { get; }

        public GamePhase Phase { get; }

        public string PositionGroup { get; }

        public IReadOnlyList<TacticalRequirement> Requirements { get; }
    }

    public sealed class RolePair
    {
        public RolePair(TacticalRole inPossessionRole, TacticalRole outOfPossessionRole, string transitionNote)
        {
            InPossessionRole = inPossessionRole;
            OutOfPossessionRole = outOfPossessionRole;
            TransitionNote = transitionNote ?? string.Empty;
        }

        public TacticalRole InPossessionRole { get; }

        public TacticalRole OutOfPossessionRole { get; }

        public string TransitionNote { get; }
    }

    public sealed class TacticalRequirement
    {
        public TacticalRequirement(string name, string evidenceMetricKey, int minimumConfidence, string missingDataImpact)
        {
            Name = name ?? string.Empty;
            EvidenceMetricKey = evidenceMetricKey ?? string.Empty;
            MinimumConfidence = minimumConfidence < 0 ? 0 : minimumConfidence > 100 ? 100 : minimumConfidence;
            MissingDataImpact = missingDataImpact ?? "Insufficient tactical evidence.";
        }

        public string Name { get; }

        public string EvidenceMetricKey { get; }

        public int MinimumConfidence { get; }

        public string MissingDataImpact { get; }
    }

    public sealed class PlayerTacticalFit
    {
        public PlayerTacticalFit(string statlynPlayerId, string roleName, int? fitScore, string safeSummary, IReadOnlyList<string> evidence, IReadOnlyList<string> missingEvidence)
        {
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            RoleName = roleName ?? string.Empty;
            FitScore = fitScore;
            SafeSummary = safeSummary ?? string.Empty;
            Evidence = evidence ?? new List<string>();
            MissingEvidence = missingEvidence ?? new List<string>();
        }

        public string StatlynPlayerId { get; }

        public string RoleName { get; }

        public int? FitScore { get; }

        public string SafeSummary { get; }

        public IReadOnlyList<string> Evidence { get; }

        public IReadOnlyList<string> MissingEvidence { get; }
    }

    public sealed class SquadTacticalNeed
    {
        public SquadTacticalNeed(string positionGroup, string roleName, string needSummary, int urgency)
        {
            PositionGroup = positionGroup ?? string.Empty;
            RoleName = roleName ?? string.Empty;
            NeedSummary = needSummary ?? string.Empty;
            Urgency = urgency < 0 ? 0 : urgency > 100 ? 100 : urgency;
        }

        public string PositionGroup { get; }

        public string RoleName { get; }

        public string NeedSummary { get; }

        public int Urgency { get; }
    }
}
