using System.Collections.Generic;

namespace Statlyn.Data.Persistence
{
    public static class GenericRoleOutputExpectationSeed
    {
        public static IReadOnlyList<RoleOutputExpectationProfile> Create()
        {
            return new[]
            {
                Profile("Generic Goalkeeper Output", "Goalkeeper", "Goalkeeping", new[]
                {
                    Core("Saves", "Saves"),
                    Core("SavePercentage", "SavePercentage"),
                    Core("GoalsPrevented", "GoalsPrevented"),
                    Important("CleanSheets", "CleanSheets"),
                    Important("KeeperDistributionAccuracy", "KeeperDistributionAccuracy")
                }),
                Profile("Generic Centre-Back Output", "CentreBack", "Defensive", new[]
                {
                    Core("AerialDuelsWonPct", "AerialDuelsWonPct"),
                    Core("Clearances", "Clearances"),
                    Core("Blocks", "Blocks"),
                    Important("Interceptions", "Interceptions"),
                    Important("ProgressivePasses", "ProgressivePasses")
                }),
                Profile("Generic Wide Attacker Output", "WingerWideForward", "Wide", new[]
                {
                    Core("xA", "xA"),
                    Core("SuccessfulDribbles", "SuccessfulDribbles"),
                    Core("ProgressiveCarries", "ProgressiveCarries"),
                    Important("KeyPasses", "KeyPasses"),
                    Important("Crosses", "Crosses"),
                    Useful("Shots", "Shots"),
                    Useful("xG", "xG")
                }),
                Profile("Generic Striker Output", "StrikerForward", "Finishing", new[]
                {
                    Core("xG", "xG"),
                    Core("Shots", "Shots"),
                    Core("Goals", "Goals"),
                    Useful("Tackles", "Tackles")
                }),
                Profile("Generic Central Midfielder Output", "CentralMidfield", "BuildUp", new[]
                {
                    Core("ProgressivePasses", "ProgressivePasses"),
                    Core("PassesIntoFinalThird", "PassesIntoFinalThird"),
                    Important("KeyPasses", "KeyPasses"),
                    Important("Tackles", "Tackles"),
                    Important("Interceptions", "Interceptions")
                })
            };
        }

        private static RoleOutputExpectationProfile Profile(string name, string positionGroup, string roleFamily, IReadOnlyList<MetricExpectation> expectations)
        {
            return new RoleOutputExpectationProfile(
                name,
                positionGroup,
                roleFamily,
                tacticalPhase: string.Empty,
                isFm26Specific: false,
                isGenericTemplate: true,
                metricExpectations: expectations,
                attributeSupportWeights: "attributes=supporting evidence only",
                scoutQuestionPrompts: "Scout observations should explain context, role fit and sample concerns.",
                redFlagRules: "Missing output data lowers confidence; do not fill missing metrics with zero.",
                minimumSampleRules: "Prefer 900+ minutes for per-90 output metrics when available.",
                notes: "Generic role-output foundation only; not a final FM26 role template.");
        }

        private static MetricExpectation Core(string metricKey, string fieldName)
        {
            return Expectation(metricKey, fieldName, 2.0, "Core");
        }

        private static MetricExpectation Important(string metricKey, string fieldName)
        {
            return Expectation(metricKey, fieldName, 1.25, "Important");
        }

        private static MetricExpectation Useful(string metricKey, string fieldName)
        {
            return Expectation(metricKey, fieldName, 0.75, "Useful");
        }

        private static MetricExpectation Expectation(string metricKey, string fieldName, double weight, string importance)
        {
            return new MetricExpectation(
                metricKey,
                fieldName,
                weight,
                importance,
                "HigherBetter",
                900,
                per90Required: true,
                normalizationHint: "Role-specific normalization to be implemented later.",
                evidenceTemplate: fieldName + " contributes to role-specific performance output evidence.",
                missingDataImpact: "Missing performance output lowers confidence rather than becoming zero.");
        }
    }
}
