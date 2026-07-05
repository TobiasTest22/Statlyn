using System;
using System.Collections.Generic;
using System.Linq;

namespace Statlyn.Analytics.PlayerIntelligence
{
    public sealed class RoleParameterDefinitionService
    {
        private readonly IReadOnlyList<RoleParameterDefinition> _definitions;

        public RoleParameterDefinitionService()
        {
            _definitions = BuildDefinitions();
        }

        public IReadOnlyList<RoleParameterDefinition> GetDefaultDefinitions()
        {
            return _definitions;
        }

        public RoleParameterDefinition? FindByRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return null;
            }

            return _definitions.FirstOrDefault(definition => string.Equals(definition.RoleName, roleName, StringComparison.OrdinalIgnoreCase)) ??
                   _definitions.FirstOrDefault(definition => roleName.IndexOf(definition.RoleName, StringComparison.OrdinalIgnoreCase) >= 0) ??
                   _definitions.FirstOrDefault(definition => definition.RoleName.IndexOf(roleName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IReadOnlyList<RoleParameterDefinition> BuildDefinitions()
        {
            return new[]
            {
                Definition(
                    "Goalkeeper",
                    "Goalkeeper",
                    1800,
                    Primary(("savePercentage", "Save percentage"), ("shotsFaced", "Shots faced"), ("claims", "Aerial claims")),
                    Secondary(("passCompletion", "Pass completion"), ("longPassAccuracy", "Long pass accuracy")),
                    Risk(("errorsLeadingToShots", "Errors leading to shots")),
                    "shot prevention", "box control"),
                Definition(
                    "Shot-stopping Goalkeeper",
                    "Goalkeeper",
                    1800,
                    Primary(("savePercentage", "Save percentage"), ("goalsPrevented", "Goals prevented"), ("shotsFaced", "Shots faced")),
                    Secondary(("xgAgainst", "xG against"), ("handlingActions", "Handling actions"), ("claims", "Aerial claims")),
                    Risk(("errorsLeadingToGoals", "Errors leading to goals")),
                    "shot-stopping", "aerial security"),
                Definition(
                    "Sweeper Keeper",
                    "Goalkeeper",
                    1800,
                    Primary(("defensiveActionsOutsideBox", "Defensive actions outside box"), ("passRange", "Pass range"), ("passCompletion", "Pass completion")),
                    Secondary(("longPassAccuracy", "Long pass accuracy"), ("throughBallPrevention", "Through-ball prevention")),
                    Risk(("turnoversInBuildUp", "Build-up turnovers")),
                    "advanced starting position", "distribution range"),
                Definition(
                    "Centre-back",
                    "Defender",
                    1800,
                    Primary(("defensiveDuelSuccess", "Defensive duel success"), ("aerialSuccess", "Aerial success"), ("interceptions", "Interceptions")),
                    Secondary(("passCompletion", "Pass completion"), ("progressivePasses", "Progressive passes")),
                    Risk(("errorsLeadingToShots", "Errors leading to shots"), ("fouls", "Fouls")),
                    "duel security", "defensive reading"),
                Definition(
                    "Ball-carrying Centre-back",
                    "Defender",
                    1800,
                    Primary(("progressiveCarries", "Progressive carries"), ("progressivePasses", "Progressive passes"), ("passCompletion", "Pass completion")),
                    Secondary(("carriesUnderPressure", "Carries under pressure"), ("defensiveDuelSuccess", "Defensive duel success"), ("aerialSuccess", "Aerial success")),
                    Risk(("errorsLeadingToShots", "Errors leading to shots"), ("turnoversInOwnHalf", "Own-half turnovers")),
                    "ball progression", "defensive security"),
                Definition(
                    "Defensive Stopper",
                    "Defender",
                    1800,
                    Primary(("defensiveDuelSuccess", "Defensive duel success"), ("tackles", "Tackles"), ("blocks", "Blocks")),
                    Secondary(("aerialSuccess", "Aerial success"), ("clearances", "Clearances")),
                    Risk(("fouls", "Fouls"), ("cards", "Cards")),
                    "front-foot defending", "duel intensity"),
                Definition(
                    "Aerial Dominant Centre-back",
                    "Defender",
                    1800,
                    Primary(("aerialSuccess", "Aerial success"), ("aerialDuels", "Aerial duels"), ("clearances", "Clearances")),
                    Secondary(("defensiveDuelSuccess", "Defensive duel success"), ("setPieceThreat", "Set-piece threat")),
                    Risk(("cards", "Cards")),
                    "aerial dominance", "set-piece value"),
                Definition(
                    "Full-back",
                    "Wide Defender",
                    1500,
                    Primary(("progressivePasses", "Progressive passes"), ("crossingAccuracy", "Crossing accuracy"), ("defensiveDuelSuccess", "Defensive duel success")),
                    Secondary(("carriesIntoFinalThird", "Carries into final third"), ("keyPasses", "Key passes")),
                    Risk(("turnovers", "Turnovers"), ("dribbledPast", "Dribbled past")),
                    "wide progression", "recovery defending"),
                Definition(
                    "Wing-back",
                    "Wide Defender",
                    1500,
                    Primary(("progressiveCarries", "Progressive carries"), ("touchesInFinalThird", "Touches in final third"), ("chancesCreated", "Chances created")),
                    Secondary(("crossingAccuracy", "Crossing accuracy"), ("defensiveActions", "Defensive actions")),
                    Risk(("turnovers", "Turnovers")),
                    "high-width running", "two-way output"),
                Definition(
                    "Progression Full-back",
                    "Wide Defender",
                    1500,
                    Primary(("progressivePasses", "Progressive passes"), ("progressiveCarries", "Progressive carries"), ("passesIntoBox", "Passes into box")),
                    Secondary(("passCompletion", "Pass completion"), ("keyPasses", "Key passes")),
                    Risk(("turnovers", "Turnovers")),
                    "build-up progression", "wide chance creation"),
                Definition(
                    "Defensive Midfielder",
                    "Midfielder",
                    1500,
                    Primary(("interceptions", "Interceptions"), ("defensiveActions", "Defensive actions"), ("passCompletion", "Pass completion")),
                    Secondary(("progressivePasses", "Progressive passes"), ("duelSuccess", "Duel success")),
                    Risk(("turnoversInMiddleThird", "Middle-third turnovers"), ("cards", "Cards")),
                    "screening", "secure circulation"),
                Definition(
                    "Controller Midfielder",
                    "Midfielder",
                    1500,
                    Primary(("passVolume", "Pass volume"), ("progressivePasses", "Progressive passes"), ("passCompletion", "Pass completion")),
                    Secondary(("keyPasses", "Key passes"), ("pressResistance", "Press resistance"), ("touchesMiddleThird", "Touches in middle third")),
                    Risk(("turnoversUnderPressure", "Turnovers under pressure")),
                    "tempo control", "press resistance"),
                Definition(
                    "Box-to-box Midfielder",
                    "Midfielder",
                    1500,
                    Primary(("progressiveCarries", "Progressive carries"), ("defensiveActions", "Defensive actions"), ("touchesInBox", "Touches in box")),
                    Secondary(("shots", "Shots"), ("keyPasses", "Key passes"), ("duelSuccess", "Duel success")),
                    Risk(("turnovers", "Turnovers")),
                    "two-way range", "late-box arrival"),
                Definition(
                    "Ball-winning Midfielder",
                    "Midfielder",
                    1500,
                    Primary(("tackles", "Tackles"), ("interceptions", "Interceptions"), ("defensiveDuelSuccess", "Defensive duel success")),
                    Secondary(("pressures", "Pressures"), ("recoveries", "Recoveries")),
                    Risk(("cards", "Cards"), ("fouls", "Fouls")),
                    "ball winning", "counter-pressure"),
                Definition(
                    "Attacking Midfielder",
                    "Creator",
                    1200,
                    Primary(("keyPasses", "Key passes"), ("chancesCreated", "Chances created"), ("passesIntoBox", "Passes into box")),
                    Secondary(("shots", "Shots"), ("touchesInFinalThird", "Touches in final third")),
                    Risk(("turnovers", "Turnovers")),
                    "between-line creation", "final-third volume"),
                Definition(
                    "Creative 10",
                    "Creator",
                    1200,
                    Primary(("throughBalls", "Through balls"), ("keyPasses", "Key passes"), ("xa", "xA")),
                    Secondary(("passesIntoBox", "Passes into box"), ("carriesIntoBox", "Carries into box"), ("shotAssists", "Shot assists")),
                    Risk(("turnovers", "Turnovers")),
                    "defence breaking", "central creativity"),
                Definition(
                    "Winger",
                    "Wide Attacker",
                    1200,
                    Primary(("chancesCreated", "Chances created"), ("touchesInBox", "Touches in box"), ("crossingAccuracy", "Crossing accuracy")),
                    Secondary(("dribblesAttempted", "Dribbles attempted"), ("progressiveCarries", "Progressive carries")),
                    Risk(("turnovers", "Turnovers")),
                    "wide chance creation", "touchline threat"),
                Definition(
                    "Take-on Winger",
                    "Wide Attacker",
                    1200,
                    Primary(("dribblesAttempted", "Dribbles attempted"), ("dribbleSuccess", "Dribble success"), ("carriesIntoFinalThird", "Carries into final third")),
                    Secondary(("progressiveCarries", "Progressive carries"), ("touchesInBox", "Touches in box"), ("chancesCreated", "Chances created")),
                    Risk(("turnovers", "Turnovers"), ("dispossessed", "Dispossessed")),
                    "1v1 threat", "carry progression"),
                Definition(
                    "Inside Forward",
                    "Wide Attacker",
                    1200,
                    Primary(("shots", "Shots"), ("touchesInBox", "Touches in box"), ("carriesIntoBox", "Carries into box")),
                    Secondary(("xg", "xG"), ("keyPasses", "Key passes"), ("pressures", "Pressures")),
                    Risk(("turnovers", "Turnovers")),
                    "inside scoring threat", "box arrival"),
                Definition(
                    "Pressing Forward",
                    "Forward",
                    1200,
                    Primary(("pressures", "Pressures"), ("defensiveActionsFinalThird", "Final-third defensive actions"), ("shots", "Shots")),
                    Secondary(("xg", "xG"), ("linkPlayPasses", "Link-play passes")),
                    Risk(("fouls", "Fouls"), ("lowShotVolume", "Low shot volume")),
                    "front-line pressing", "transition pressure"),
                Definition(
                    "Target Forward",
                    "Forward",
                    1200,
                    Primary(("aerialSuccess", "Aerial success"), ("holdUpActions", "Hold-up actions"), ("touchesInBox", "Touches in box")),
                    Secondary(("shotAssists", "Shot assists"), ("xg", "xG")),
                    Risk(("turnovers", "Turnovers")),
                    "aerial outlet", "central reference"),
                Definition(
                    "Poacher / Finisher",
                    "Forward",
                    1200,
                    Primary(("xg", "xG"), ("shots", "Shots"), ("touchesInBox", "Touches in box")),
                    Secondary(("goals", "Goals"), ("shotQuality", "Shot quality")),
                    Risk(("lowInvolvement", "Low involvement")),
                    "box finishing", "shot selection")
            };
        }

        private static RoleParameterDefinition Definition(
            string roleName,
            string roleFamily,
            int minimumMinutes,
            IReadOnlyList<RoleParameterMetric> primaryMetrics,
            IReadOnlyList<RoleParameterMetric> secondaryMetrics,
            IReadOnlyList<RoleParameterMetric> riskMetrics,
            params string[] styleTraits)
        {
            return new RoleParameterDefinition(
                roleName,
                roleFamily,
                primaryMetrics,
                secondaryMetrics,
                riskMetrics,
                styleTraits,
                minimumMinutes,
                new[]
                {
                    "Missing required role metrics.",
                    "Minutes below role sample threshold.",
                    "Safe local data quality below confidence threshold."
                });
        }

        private static IReadOnlyList<RoleParameterMetric> Primary(params (string key, string label)[] metrics)
        {
            return metrics.Select(item => new RoleParameterMetric(item.key, item.label, "Primary", true, 1200)).ToList();
        }

        private static IReadOnlyList<RoleParameterMetric> Secondary(params (string key, string label)[] metrics)
        {
            return metrics.Select(item => new RoleParameterMetric(item.key, item.label, "Secondary", false, 900)).ToList();
        }

        private static IReadOnlyList<RoleParameterMetric> Risk(params (string key, string label)[] metrics)
        {
            return metrics.Select(item => new RoleParameterMetric(item.key, item.label, "Risk", false, 900)).ToList();
        }
    }
}
