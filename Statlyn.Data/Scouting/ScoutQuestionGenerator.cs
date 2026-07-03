using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Data.Profile;

namespace Statlyn.Data.Scouting
{
    public sealed class ScoutQuestionGenerator
    {
        public IReadOnlyList<ScoutQuestionPrompt> Generate(PlayerProfileResult profile)
        {
            if (profile == null || !profile.Success)
            {
                return new List<ScoutQuestionPrompt>();
            }

            var positionGroup = profile.RoleOutputSummary == null ? string.Empty : profile.RoleOutputSummary.PositionGroup;
            var missing = profile.RoleOutputSummary == null ? new List<string>() : profile.RoleOutputSummary.MissingCoreMetrics;
            var sourceConfidence = profile.SourceMetadata == null ? 100 : profile.SourceMetadata.SourceConfidence;
            return Generate(positionGroup, missing, profile.BlockedFields.Count, sourceConfidence);
        }

        public IReadOnlyList<ScoutQuestionPrompt> Generate(string positionGroup, IEnumerable<string> missingCoreMetrics, int blockedAuditCount, int sourceConfidence)
        {
            var prompts = new List<ScoutQuestionPrompt>();
            var position = (positionGroup ?? string.Empty).ToLowerInvariant();
            var missing = (missingCoreMetrics ?? Array.Empty<string>()).Select(value => (value ?? string.Empty).ToLowerInvariant()).ToList();

            if (IsStriker(position) || ContainsAny(missing, "xg", "shot", "goal"))
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "MissingOutput",
                    "Does he consistently get into high-quality shooting positions?",
                    "Imported output is missing shooting-quality context, so the scout should validate chance quality directly.",
                    "Technical"));
            }

            if (IsWideAttacker(position) || ContainsAny(missing, "xa", "key pass", "chance", "cross"))
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "RoleFit",
                    "Does he create chances from wide or half-space zones?",
                    "Chance creation from repeatable zones helps validate wide-attacker output when xA or key-pass data is thin.",
                    "Tactical"));
            }

            if (IsCentreBack(position) || ContainsAny(missing, "aerial", "clearance", "duel", "cross"))
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "Physical",
                    "Can he defend crosses and aerial duels when contested?",
                    "Centre-back evaluation needs observable defensive security when aerial or clearance data is missing.",
                    "Physical"));
            }

            if (IsGoalkeeper(position) || ContainsAny(missing, "save", "keeper", "goalkeeper", "psxg"))
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "Technical",
                    "Does he prevent goals beyond routine saves?",
                    "Goalkeeper output can be hard to trust without save context, so the scout should watch shot-stopping difficulty.",
                    "Technical"));
            }

            if (sourceConfidence < 70)
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "DataQuality",
                    "Can this source be trusted enough for recruitment decisions?",
                    "Low source confidence should be checked before the report supports a stronger recommendation.",
                    "DataQuality"));
            }

            if (blockedAuditCount > 0)
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "Risk",
                    "Do not infer hidden values; observe behaviour directly.",
                    "Blocked fields are not raw scouting inputs, so the report should stay qualitative and observable.",
                    "MentalCharacter"));
            }

            if (prompts.Count == 0)
            {
                prompts.Add(new ScoutQuestionPrompt(
                    "RoleFit",
                    "Does the player repeatedly perform the role actions expected from this assignment?",
                    "A scout report should validate visible role behaviour rather than hidden attributes.",
                    "RoleFit"));
            }

            return prompts;
        }

        private static bool ContainsAny(IEnumerable<string> values, params string[] needles)
        {
            return values.Any(value => needles.Any(needle => value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static bool IsStriker(string position)
        {
            return position.Contains("striker") || position.Contains("forward") || position == "st";
        }

        private static bool IsWideAttacker(string position)
        {
            return position.Contains("wide") || position.Contains("wing") || position.Contains("attacking midfield");
        }

        private static bool IsCentreBack(string position)
        {
            return position.Contains("centre-back") || position.Contains("center-back") || position.Contains("defender") || position == "cb";
        }

        private static bool IsGoalkeeper(string position)
        {
            return position.Contains("goalkeeper") || position.Contains("keeper") || position == "gk";
        }
    }
}
