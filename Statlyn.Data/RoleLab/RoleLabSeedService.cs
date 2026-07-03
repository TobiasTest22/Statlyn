using System;
using System.Collections.Generic;
using System.Linq;

namespace Statlyn.Data.RoleLab
{
    public sealed class RoleLabSeedService
    {
        private readonly RoleLabRepository _repository;

        public RoleLabSeedService(StatlynDbConnectionFactory connectionFactory)
        {
            _repository = new RoleLabRepository(connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory)));
        }

        public RoleLabSeedResult SeedBuiltInRoles()
        {
            var roles = new List<TacticalRoleModel>();
            foreach (var seed in CreateSeeds())
            {
                var role = _repository.SaveRole(seed.Role);
                _repository.DeleteMetricRequirementsForRole(role.Id);
                _repository.DeleteScoutQuestionsForRole(role.Id);
                _repository.DeleteRedFlagsForRole(role.Id);

                foreach (var requirement in seed.Requirements)
                {
                    _repository.SaveMetricRequirement(new RoleOutputMetricRequirementModel(
                        0,
                        role.Id,
                        null,
                        requirement.MetricKey,
                        requirement.FieldName,
                        requirement.Weight,
                        requirement.Importance,
                        requirement.Direction,
                        requirement.MinimumSampleMinutes,
                        requirement.Per90Required,
                        requirement.NormalizationHint,
                        requirement.EvidenceTemplate,
                        requirement.MissingDataImpact));
                }

                foreach (var question in seed.Questions)
                {
                    _repository.SaveScoutQuestion(new RoleScoutQuestionModel(
                        0,
                        role.Id,
                        null,
                        question.Category,
                        question.Question,
                        question.WhyItMatters,
                        question.SuggestedObservationType));
                }

                foreach (var redFlag in seed.RedFlags)
                {
                    _repository.SaveRedFlag(new RoleRedFlagModel(
                        0,
                        role.Id,
                        null,
                        redFlag.FieldName,
                        redFlag.Operator,
                        redFlag.Threshold,
                        redFlag.Message,
                        redFlag.AppliesToPhase));
                }

                roles.Add(role);
            }

            return new RoleLabSeedResult(
                roles.Count,
                roles.Count(role => role.TacticalPhase == TacticalPhase.InPossession),
                roles.Count(role => role.TacticalPhase == TacticalPhase.OutOfPossession),
                "Built-in Role Lab templates seeded as generic/import-ready, not official FM26 mappings.");
        }

        private static IReadOnlyList<RoleSeed> CreateSeeds()
        {
            return new[]
            {
                Role("No-Nonsense Goalkeeper", TacticalPhase.InPossession, TacticalRoleFamily.Goalkeeper, "Goalkeeper", Slots(TacticalSlot.GK),
                    "Hold position and keep rest defence stable.",
                    "Prefer safe circulation and clearances when pressure is high.",
                    "Support territory rather than joining chance creation.",
                    "Reset the team shape after turnovers.",
                    "Command the box and protect central goal space.",
                    "First pass after recovery should be safe.",
                    Metrics(Core("Saves"), Core("SavePercentage"), Important("KeeperDistributionAccuracy")),
                    Questions("Goalkeeper", "Does he handle shot difficulty and crosses without creating second-ball chaos?"),
                    Flags("GoalsPrevented", "missing", "review", "Missing goalkeeper prevention output lowers confidence.", TacticalPhase.InPossession)),
                Role("Ball-Playing Goalkeeper", TacticalPhase.InPossession, TacticalRoleFamily.Goalkeeper, "Goalkeeper", Slots(TacticalSlot.GK),
                    "Hold a higher support position when circulation is controlled.",
                    "Find safe build-up passes and avoid forced central turnovers.",
                    "Support quick restarts without pretending to be an outfield creator.",
                    "Recover shape after aggressive distribution.",
                    "Protect the box before joining build-up.",
                    "Balance distribution ambition with shot-prevention security.",
                    Metrics(Core("KeeperDistributionAccuracy"), Important("Saves"), Important("SavePercentage")),
                    Questions("Distribution", "Does his distribution choice help build-up without adding avoidable risk?"),
                    Flags("KeeperDistributionAccuracy", "missing", "review", "Missing keeper distribution output lowers confidence.", TacticalPhase.InPossession)),
                Role("Overlapping Centre-Back", TacticalPhase.InPossession, TacticalRoleFamily.CentreBack, "CentreBack", Slots(TacticalSlot.CBL, TacticalSlot.CBR, TacticalSlot.CBC),
                    "Step outside the block when support and cover are present.",
                    "Carry or pass forward from the back line without leaving rest defence exposed.",
                    "Arrive as a support runner rather than a primary attacker.",
                    "Counter-press only when cover remains behind him.",
                    "Recover into the back line quickly when possession is lost.",
                    "Transition risk is managed by the nearest holding player.",
                    Metrics(Core("ProgressivePasses"), Important("Interceptions"), Important("AerialDuelsWonPct")),
                    Questions("BuildUp", "Can he step forward without leaving the defensive line exposed?"),
                    Flags("AerialDuelsWonPct", "missing", "review", "Missing centre-back aerial output lowers confidence.", TacticalPhase.InPossession)),
                Role("Playmaking Wing-Back", TacticalPhase.InPossession, TacticalRoleFamily.FullBackWingBack, "FullBackWingBack", Slots(TacticalSlot.WBL, TacticalSlot.WBR, TacticalSlot.FBL, TacticalSlot.FBR),
                    "Occupy wide support lanes and invert only when cover exists.",
                    "Progress play through passes and switches rather than blind crossing.",
                    "Create from wide and half-space receiving angles.",
                    "Press recovery lane after losing the ball.",
                    "Track the outside lane before joining attacks again.",
                    "Recovery runs must protect the far-post channel.",
                    Metrics(Core("ProgressivePasses"), Important("KeyPasses"), Important("Crosses")),
                    Questions("ChanceCreation", "Does he create clean chances from wide or half-space zones?"),
                    Flags("KeyPasses", "missing", "review", "Missing chance-creation output lowers confidence.", TacticalPhase.InPossession)),
                Role("Advanced Wing-Back", TacticalPhase.InPossession, TacticalRoleFamily.FullBackWingBack, "FullBackWingBack", Slots(TacticalSlot.WBL, TacticalSlot.WBR),
                    "Stretch the pitch high and wide.",
                    "Receive early and progress the ball down the outside lane.",
                    "Supply crosses, cutbacks and late support runs.",
                    "Counter-press near the touchline before recovery.",
                    "Recover behind the wide attacker when possession is lost.",
                    "Transition load is high and needs recovery evidence.",
                    Metrics(Core("Crosses"), Important("SuccessfulDribbles"), Important("ProgressiveCarries")),
                    Questions("Physical", "Can he repeat high wide runs and still recover defensively?"),
                    Flags("ProgressiveCarries", "missing", "review", "Missing carry output lowers confidence.", TacticalPhase.InPossession)),
                Role("Inside Wing-Back", TacticalPhase.InPossession, TacticalRoleFamily.FullBackWingBack, "FullBackWingBack", Slots(TacticalSlot.FBL, TacticalSlot.FBR, TacticalSlot.WBL, TacticalSlot.WBR),
                    "Move into an inside support lane when the wide lane is occupied.",
                    "Offer central passing support and protect counter-press structure.",
                    "Arrive around the box as a support option, not a hidden playmaker role.",
                    "Press central exits after turnovers.",
                    "Screen central transition before recovering wide.",
                    "Switch between inside support and wide recovery.",
                    Metrics(Core("ProgressivePasses"), Important("Interceptions"), Useful("Tackles")),
                    Questions("RoleFit", "Does he understand when to invert and when to hold the wide lane?"),
                    Flags("Interceptions", "missing", "review", "Missing screening output lowers confidence.", TacticalPhase.InPossession)),
                Role("Midfield Playmaker", TacticalPhase.InPossession, TacticalRoleFamily.CentralMidfield, "CentralMidfield", Slots(TacticalSlot.CML, TacticalSlot.CMC, TacticalSlot.CMR, TacticalSlot.DM),
                    "Find pockets that connect defence and attack.",
                    "Progress with safe forward passes and tempo control.",
                    "Create entries into the final third without forcing low-value passes.",
                    "Press passing lanes after turnovers.",
                    "Hold central access when the team is stretched.",
                    "Transition decisions should reduce counter risk.",
                    Metrics(Core("ProgressivePasses"), Core("PassesIntoFinalThird"), Important("KeyPasses")),
                    Questions("BuildUp", "Does he progress play under pressure without forcing turnovers?"),
                    Flags("ProgressivePasses", "missing", "review", "Missing progression output lowers confidence.", TacticalPhase.InPossession)),
                Role("Wide Central Midfielder", TacticalPhase.InPossession, TacticalRoleFamily.CentralMidfield, "CentralMidfield", Slots(TacticalSlot.CML, TacticalSlot.CMR),
                    "Drift toward the channel to support wide overloads.",
                    "Link central build-up with wide chance creation.",
                    "Arrive around the half-space for support and cutbacks.",
                    "Jump to press wide exits when shape allows.",
                    "Recover into the central midfield line.",
                    "Transition requires awareness of the vacated central lane.",
                    Metrics(Core("ProgressivePasses"), Important("KeyPasses"), Important("Tackles")),
                    Questions("RoleFit", "Can he support wide overloads without abandoning central protection?"),
                    Flags("Tackles", "missing", "review", "Missing recovery/defensive output lowers confidence.", TacticalPhase.InPossession)),
                Role("Wide Forward", TacticalPhase.InPossession, TacticalRoleFamily.WideAttacker, "WingerWideForward", Slots(TacticalSlot.AML, TacticalSlot.AMR, TacticalSlot.WL, TacticalSlot.WR),
                    "Attack the channel from a wide starting point.",
                    "Carry forward and combine before entering the box.",
                    "Balance chance creation with shot threat.",
                    "Counter-press the full-back lane after turnovers.",
                    "Recover enough to protect the wide defender.",
                    "Decision speed in transition is a key risk.",
                    Metrics(Core("xA"), Core("ProgressiveCarries"), Important("SuccessfulDribbles"), Useful("Shots"), Useful("xG")),
                    Questions("ChanceCreation", "Does he create chances from wide or half-space zones while keeping shot selection clean?"),
                    Flags("xA", "missing", "review", "Missing wide creation output lowers confidence.", TacticalPhase.InPossession)),
                Role("Channel Forward", TacticalPhase.InPossession, TacticalRoleFamily.Forward, "StrikerForward", Slots(TacticalSlot.ST),
                    "Move between centre-back and full-back channels.",
                    "Offer depth runs and link play when isolated.",
                    "Get into high-quality shooting positions.",
                    "Lead pressure onto the nearest centre-back when possession turns over.",
                    "Screen central build-up before pressing outside.",
                    "Recovery is more about re-press timing than deep defending.",
                    Metrics(Core("xG"), Core("Shots"), Important("Goals"), Useful("ProgressiveCarries")),
                    Questions("GoalThreat", "Does he consistently arrive in high-quality shooting positions?"),
                    Flags("xG", "missing", "review", "Missing shot-quality output lowers confidence.", TacticalPhase.InPossession)),
                Role("High Press Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.HighPress, "OutOfPossession", Slots(TacticalSlot.ST, TacticalSlot.AMC, TacticalSlot.AML, TacticalSlot.AMR),
                    "Move aggressively toward the ball-side trigger.",
                    "Build-up behaviour is secondary to pressing readiness.",
                    "Final-third positioning should support immediate pressure.",
                    "Press early and force predictable exits.",
                    "Keep the block compact behind the pressing player.",
                    "Recover if the first press is bypassed.",
                    Metrics(Core("Pressures"), Important("Recoveries"), Useful("Tackles")),
                    Questions("Pressing", "Does he press on the right trigger without opening central lanes?"),
                    Flags("Recoveries", "missing", "review", "Missing recovery output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Mid Block Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.MidBlock, "OutOfPossession", Slots(TacticalSlot.DM, TacticalSlot.CMC, TacticalSlot.CML, TacticalSlot.CMR),
                    "Hold compact distances rather than chasing early.",
                    "Support reset passes after regains.",
                    "Attack only after the block has secured the ball.",
                    "Press once the opponent enters the block trigger zone.",
                    "Protect central spaces and delay progression.",
                    "Transition should be stable and repeatable.",
                    Metrics(Core("Interceptions"), Important("Tackles"), Important("Recoveries")),
                    Questions("DefensiveBlock", "Does he keep compact distances and delay progression in a mid block?"),
                    Flags("Interceptions", "missing", "review", "Missing block-interruption output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Low Block Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.LowBlock, "OutOfPossession", Slots(TacticalSlot.CBC, TacticalSlot.CBL, TacticalSlot.CBR, TacticalSlot.DM),
                    "Stay compact around the box.",
                    "Clear pressure before forcing build-up.",
                    "Attack transition only after defensive security is restored.",
                    "Press late and deny central shots.",
                    "Defend box entries and second balls.",
                    "Transition risk is managed through simple outlets.",
                    Metrics(Core("Blocks"), Core("Clearances"), Important("AerialDuelsWonPct")),
                    Questions("Defending", "Can he defend crosses, second balls and cutbacks under pressure?"),
                    Flags("Blocks", "missing", "review", "Missing box-defending output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Wide Defensive Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.WideDefensive, "OutOfPossession", Slots(TacticalSlot.FBL, TacticalSlot.FBR, TacticalSlot.WBL, TacticalSlot.WBR, TacticalSlot.WL, TacticalSlot.WR),
                    "Track the outside lane and pass runners on clearly.",
                    "Support wide exits after regains.",
                    "Join attacks only after wide security is established.",
                    "Press touchline traps and protect inside shoulder.",
                    "Deny crosses and far-post overloads.",
                    "Recovery timing is essential after high wide actions.",
                    Metrics(Core("Tackles"), Important("Interceptions"), Important("CrossesBlocked")),
                    Questions("WideDefending", "Does he protect the inside shoulder while stopping wide delivery?"),
                    Flags("Tackles", "missing", "review", "Missing wide defensive output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Central Screening Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.CentralScreening, "OutOfPossession", Slots(TacticalSlot.DM, TacticalSlot.CMC),
                    "Hold the space in front of centre-backs.",
                    "Provide safe first pass after regain.",
                    "Stay behind the ball until central access is secure.",
                    "Press only when the screening lane remains protected.",
                    "Block passes into the striker and attacking midfielder.",
                    "Transition focus is rest defence before attack.",
                    Metrics(Core("Interceptions"), Important("Blocks"), Important("Recoveries")),
                    Questions("Screening", "Does he screen central access without being pulled out too easily?"),
                    Flags("Interceptions", "missing", "review", "Missing central-screening output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Recovery Cover Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.RecoveryCover, "OutOfPossession", Slots(TacticalSlot.CBL, TacticalSlot.CBR, TacticalSlot.DM, TacticalSlot.FBL, TacticalSlot.FBR),
                    "Position to cover aggressive teammates.",
                    "Recycle possession simply after emergency regains.",
                    "Avoid overcommitting in the final third.",
                    "Delay counters rather than always jumping forward.",
                    "Cover vacated spaces and protect depth.",
                    "First recovery action should reduce danger.",
                    Metrics(Core("Recoveries"), Important("Interceptions"), Important("Tackles")),
                    Questions("Recovery", "Does he cover space early enough when teammates jump forward?"),
                    Flags("Recoveries", "missing", "review", "Missing recovery output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Aggressive Pressing Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.HighPress, "OutOfPossession", Slots(TacticalSlot.ST, TacticalSlot.AMC, TacticalSlot.AML, TacticalSlot.AMR, TacticalSlot.CMC),
                    "Step toward triggers and compress the receiver.",
                    "Build-up support depends on regaining close to goal.",
                    "Attack immediately after forcing errors.",
                    "Press with speed, timing and cover awareness.",
                    "Avoid leaving a free central lane behind the jump.",
                    "Recovery sprint after a failed press is non-negotiable.",
                    Metrics(Core("Pressures"), Core("Recoveries"), Important("Tackles")),
                    Questions("Pressing", "Can he jump forward and recover if the press is beaten?"),
                    Flags("Pressures", "missing", "review", "Missing pressing output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Passive Containment Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.LowBlock, "OutOfPossession", Slots(TacticalSlot.DM, TacticalSlot.CMC, TacticalSlot.CBL, TacticalSlot.CBC, TacticalSlot.CBR),
                    "Hold position and slow the opponent.",
                    "Use simple outlets after regains.",
                    "Attack only when the team has reset.",
                    "Contain rather than chase.",
                    "Protect compactness and deny central shots.",
                    "Transition decisions should avoid exposing the block.",
                    Metrics(Core("Blocks"), Important("Interceptions"), Useful("Clearances")),
                    Questions("Containment", "Does he delay attacks without being passive around dangerous zones?"),
                    Flags("Blocks", "missing", "review", "Missing containment output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Back Line Holding Role", TacticalPhase.OutOfPossession, TacticalRoleFamily.RecoveryCover, "OutOfPossession", Slots(TacticalSlot.CBL, TacticalSlot.CBC, TacticalSlot.CBR),
                    "Hold the defensive line and communicate depth.",
                    "Progress only when the team has secure possession.",
                    "Stay connected to rest defence.",
                    "Press only when the line can move together.",
                    "Protect depth, crosses and central shots.",
                    "Transition should restore the line quickly.",
                    Metrics(Core("Clearances"), Core("AerialDuelsWonPct"), Important("Blocks")),
                    Questions("BackLine", "Does he hold the line while still attacking aerial danger?"),
                    Flags("AerialDuelsWonPct", "missing", "review", "Missing aerial output lowers confidence.", TacticalPhase.OutOfPossession)),
                Role("Pressing Defensive Midfielder", TacticalPhase.OutOfPossession, TacticalRoleFamily.DefensiveMidfield, "DefensiveMidfield", Slots(TacticalSlot.DM, TacticalSlot.CMC),
                    "Start from screening space and jump forward on triggers.",
                    "Offer a simple outlet after regains.",
                    "Arrive late only when central protection remains.",
                    "Time the press without abandoning the zone.",
                    "Screen behind the first line and recover quickly.",
                    "The role needs recovery evidence after forward jumps.",
                    Metrics(Core("Tackles"), Core("Interceptions"), Important("Pressures"), Important("Recoveries")),
                    Questions("Pressing", "Does he time the press without leaving dangerous space behind him?"),
                    Flags("Recoveries", "missing", "review", "Missing recovery-after-press output lowers confidence.", TacticalPhase.OutOfPossession))
            };
        }

        private static RoleSeed Role(
            string name,
            TacticalPhase phase,
            TacticalRoleFamily family,
            string positionGroup,
            IReadOnlyList<TacticalSlot> slots,
            string movement,
            string buildUp,
            string finalThird,
            string pressing,
            string defensiveBlock,
            string transition,
            IReadOnlyList<MetricSeed> metrics,
            IReadOnlyList<QuestionSeed> questions,
            IReadOnlyList<RedFlagSeed> redFlags)
        {
            return new RoleSeed(
                new TacticalRoleModel(
                    0,
                    name,
                    phase,
                    family,
                    TacticalRoleSource.BuiltInSeed,
                    false,
                    string.Empty,
                    positionGroup,
                    slots,
                    movement,
                    buildUp,
                    finalThird,
                    pressing,
                    defensiveBlock,
                    transition,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    false),
                metrics,
                questions,
                redFlags);
        }

        private static IReadOnlyList<TacticalSlot> Slots(params TacticalSlot[] slots)
        {
            return slots;
        }

        private static IReadOnlyList<MetricSeed> Metrics(params MetricSeed[] metrics)
        {
            return metrics;
        }

        private static IReadOnlyList<QuestionSeed> Questions(string category, string question)
        {
            return new[]
            {
                new QuestionSeed(category, question, "Role Lab questions validate visible phase behaviour, not hidden attributes.", category),
                new QuestionSeed("DataQuality", "Is the available output sample strong enough for this role template?", "Missing output lowers confidence and should not be filled with zero.", "DataQuality")
            };
        }

        private static IReadOnlyList<RedFlagSeed> Flags(string fieldName, string operatorValue, string threshold, string message, TacticalPhase phase)
        {
            return new[] { new RedFlagSeed(fieldName, operatorValue, threshold, message, phase) };
        }

        private static MetricSeed Core(string fieldName)
        {
            return Metric(fieldName, 2.0, RoleMetricImportance.Core);
        }

        private static MetricSeed Important(string fieldName)
        {
            return Metric(fieldName, 1.25, RoleMetricImportance.Important);
        }

        private static MetricSeed Useful(string fieldName)
        {
            return Metric(fieldName, 0.75, RoleMetricImportance.Useful);
        }

        private static MetricSeed Metric(string fieldName, double weight, RoleMetricImportance importance)
        {
            return new MetricSeed(
                fieldName,
                fieldName,
                weight,
                importance,
                RoleMetricDirection.HigherBetter,
                900,
                true,
                "Role Lab generic/import normalization; FM26 validation pending.",
                fieldName + " supports phase-aware role output evidence.",
                "Missing " + fieldName + " lowers confidence rather than becoming zero.");
        }

        private sealed class RoleSeed
        {
            public RoleSeed(TacticalRoleModel role, IReadOnlyList<MetricSeed> requirements, IReadOnlyList<QuestionSeed> questions, IReadOnlyList<RedFlagSeed> redFlags)
            {
                Role = role;
                Requirements = requirements;
                Questions = questions;
                RedFlags = redFlags;
            }

            public TacticalRoleModel Role { get; }

            public IReadOnlyList<MetricSeed> Requirements { get; }

            public IReadOnlyList<QuestionSeed> Questions { get; }

            public IReadOnlyList<RedFlagSeed> RedFlags { get; }
        }

        private sealed class MetricSeed
        {
            public MetricSeed(string metricKey, string fieldName, double weight, RoleMetricImportance importance, RoleMetricDirection direction, int minimumSampleMinutes, bool per90Required, string normalizationHint, string evidenceTemplate, string missingDataImpact)
            {
                MetricKey = metricKey;
                FieldName = fieldName;
                Weight = weight;
                Importance = importance;
                Direction = direction;
                MinimumSampleMinutes = minimumSampleMinutes;
                Per90Required = per90Required;
                NormalizationHint = normalizationHint;
                EvidenceTemplate = evidenceTemplate;
                MissingDataImpact = missingDataImpact;
            }

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

        private sealed class QuestionSeed
        {
            public QuestionSeed(string category, string question, string whyItMatters, string suggestedObservationType)
            {
                Category = category;
                Question = question;
                WhyItMatters = whyItMatters;
                SuggestedObservationType = suggestedObservationType;
            }

            public string Category { get; }

            public string Question { get; }

            public string WhyItMatters { get; }

            public string SuggestedObservationType { get; }
        }

        private sealed class RedFlagSeed
        {
            public RedFlagSeed(string fieldName, string operatorValue, string threshold, string message, TacticalPhase appliesToPhase)
            {
                FieldName = fieldName;
                Operator = operatorValue;
                Threshold = threshold;
                Message = message;
                AppliesToPhase = appliesToPhase;
            }

            public string FieldName { get; }

            public string Operator { get; }

            public string Threshold { get; }

            public string Message { get; }

            public TacticalPhase AppliesToPhase { get; }
        }
    }

    public sealed class RoleLabSeedResult
    {
        public RoleLabSeedResult(int totalRoles, int inPossessionRoles, int outOfPossessionRoles, string safeMessage)
        {
            TotalRoles = totalRoles;
            InPossessionRoles = inPossessionRoles;
            OutOfPossessionRoles = outOfPossessionRoles;
            SafeMessage = safeMessage ?? string.Empty;
        }

        public int TotalRoles { get; }

        public int InPossessionRoles { get; }

        public int OutOfPossessionRoles { get; }

        public string SafeMessage { get; }
    }
}
