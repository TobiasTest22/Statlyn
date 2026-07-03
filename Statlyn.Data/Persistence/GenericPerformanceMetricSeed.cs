using System.Collections.Generic;
using Statlyn.Core;

namespace Statlyn.Data.Persistence
{
    public static class GenericPerformanceMetricSeed
    {
        public static IReadOnlyList<PerformanceMetricDefinition> Create()
        {
            var allOutfield = new[] { "CentreBack", "FullBackWingBack", "DefensiveMidfield", "CentralMidfield", "AttackingMidfield", "WingerWideForward", "StrikerForward" };
            var attacking = new[] { "AttackingMidfield", "WingerWideForward", "StrikerForward" };
            var creative = new[] { "CentralMidfield", "AttackingMidfield", "WingerWideForward" };
            var defensive = new[] { "CentreBack", "FullBackWingBack", "DefensiveMidfield", "CentralMidfield" };
            var keeper = new[] { "Goalkeeper" };

            return new[]
            {
                Stat("xG", "xG", attacking, new[] { "Attacking", "Finishing" }, true, "goals"),
                Stat("xA", "xA", creative, new[] { "Creative", "Wide" }, true, "assists"),
                Stat("Goals", "Goals", attacking, new[] { "Attacking" }, true, "goals"),
                Stat("Assists", "Assists", creative, new[] { "Creative" }, true, "assists"),
                Stat("Shots", "Shots", attacking, new[] { "Attacking" }, true, "shots"),
                Stat("ShotsOnTarget", "Shots on target", attacking, new[] { "Attacking" }, true, "shots"),
                Stat("KeyPasses", "Key passes", creative, new[] { "Creative" }, true, "passes"),
                Stat("ProgressivePasses", "Progressive passes", allOutfield, new[] { "BuildUp", "Creative" }, true, "passes"),
                Stat("ProgressiveCarries", "Progressive carries", new[] { "FullBackWingBack", "CentralMidfield", "AttackingMidfield", "WingerWideForward" }, new[] { "Carrying", "Wide" }, true, "carries"),
                Stat("SuccessfulDribbles", "Successful dribbles", new[] { "AttackingMidfield", "WingerWideForward" }, new[] { "Wide", "Carrying" }, true, "dribbles"),
                Stat("Crosses", "Crosses", new[] { "FullBackWingBack", "WingerWideForward" }, new[] { "Wide", "Creative" }, true, "crosses"),
                Stat("PassesIntoFinalThird", "Passes into final third", new[] { "CentreBack", "FullBackWingBack", "DefensiveMidfield", "CentralMidfield" }, new[] { "BuildUp" }, true, "passes"),
                Stat("Tackles", "Tackles", defensive, new[] { "Defensive" }, true, "tackles"),
                Stat("Interceptions", "Interceptions", defensive, new[] { "Defensive" }, true, "interceptions"),
                Stat("Blocks", "Blocks", new[] { "CentreBack", "FullBackWingBack", "DefensiveMidfield" }, new[] { "Defensive" }, true, "blocks"),
                Stat("Clearances", "Clearances", new[] { "CentreBack", "FullBackWingBack" }, new[] { "Defensive" }, true, "clearances"),
                Stat("AerialDuelsWonPct", "Aerial duel success", new[] { "CentreBack", "StrikerForward", "Goalkeeper" }, new[] { "Defensive", "Target" }, false, "percent"),
                Stat("GroundDuelsWonPct", "Ground duel success", defensive, new[] { "Defensive" }, false, "percent"),
                Stat("PassCompletionPct", "Pass completion", allOutfield, new[] { "BuildUp" }, false, "percent"),
                Stat("Minutes", "Minutes", allOutfield, new[] { "Sample" }, false, "minutes"),
                Physical("TopSpeed", "Top speed", allOutfield, new[] { "Physical" }, false, "km/h"),
                Physical("SprintDistance", "Sprint distance", allOutfield, new[] { "Physical" }, false, "metres"),
                Physical("HighSpeedRunning", "High-speed running", allOutfield, new[] { "Physical" }, false, "metres"),
                Physical("DistanceCovered", "Distance covered", allOutfield, new[] { "Physical" }, false, "metres"),
                Stat("Saves", "Saves", keeper, new[] { "Goalkeeping" }, true, "saves"),
                Stat("SavePercentage", "Save percentage", keeper, new[] { "Goalkeeping" }, false, "percent"),
                Stat("GoalsPrevented", "Goals prevented", keeper, new[] { "Goalkeeping" }, false, "goals"),
                Stat("CleanSheets", "Clean sheets", keeper, new[] { "Goalkeeping" }, false, "matches"),
                Stat("KeeperDistributionAccuracy", "Keeper distribution accuracy", keeper, new[] { "Goalkeeping", "Distribution" }, false, "percent")
            };
        }

        private static PerformanceMetricDefinition Stat(string key, string displayName, IReadOnlyList<string> positionGroups, IReadOnlyList<string> roleFamilies, bool per90, string unit)
        {
            return Definition(key, displayName, PlayerFieldKey.PlayerStat, positionGroups, roleFamilies, per90, unit);
        }

        private static PerformanceMetricDefinition Physical(string key, string displayName, IReadOnlyList<string> positionGroups, IReadOnlyList<string> roleFamilies, bool per90, string unit)
        {
            return Definition(key, displayName, PlayerFieldKey.PhysicalData, positionGroups, roleFamilies, per90, unit);
        }

        private static PerformanceMetricDefinition Definition(string key, string displayName, PlayerFieldKey fieldKey, IReadOnlyList<string> positionGroups, IReadOnlyList<string> roleFamilies, bool per90, string unit)
        {
            return new PerformanceMetricDefinition(
                key,
                displayName,
                "Generic import/test metric definition. Not verified as an official FM26 stat.",
                fieldKey,
                key,
                ProviderType.FutureExternalProvider,
                isGenericFootballMetric: true,
                isVerifiedFm26Metric: false,
                isPer90Capable: per90,
                defaultUnit: unit,
                higherIsBetter: true,
                lowerIsBetter: false,
                requiresMinutes: per90,
                minimumMinutesRecommended: per90 ? 900 : 0,
                positionGroups: positionGroups,
                roleFamilies: roleFamilies,
                sourceConfidenceRequired: 50,
                canScore: true,
                canStore: true,
                notes: "Generic metric only; FM26 support requires later validation from visible/exported data or a validated memory map.");
        }
    }
}
