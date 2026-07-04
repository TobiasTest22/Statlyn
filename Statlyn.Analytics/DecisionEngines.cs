using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Core;

namespace Statlyn.Analytics
{
    public sealed class RecruitmentDecisionEngine
    {
        public DecisionResult Evaluate(MaskedPlayer player, RoleScore roleScore)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (roleScore == null)
            {
                return Insufficient("No role score is available; scout or score the player before making a recruitment decision.", "Role score");
            }

            var evidence = new List<DecisionEvidence>();
            var warnings = new List<string>();
            if (roleScore.RoleFit >= 75)
            {
                evidence.Add(new DecisionEvidence("Role fit", "Role fit is strong enough to justify recruitment attention.", true));
            }
            else
            {
                evidence.Add(new DecisionEvidence("Role fit", "Role fit is not yet strong enough for a clear recruitment push.", false));
            }

            if (roleScore.Confidence < 60 || roleScore.MissingData.Count > 0)
            {
                warnings.Add("Decision confidence is limited by missing or low-confidence evidence.");
            }

            if (player.BlockedFields.Count > 0)
            {
                warnings.Add(player.BlockedFields.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " blocked field(s) were excluded.");
            }

            var score = Clamp((roleScore.RoleFit + roleScore.Confidence + (100 - roleScore.RiskScore)) / 3);
            return new DecisionResult(
                DecisionStatus.Available,
                "Recruitment decision uses masked evidence only; no live FM26 data or hidden values are required.",
                score,
                roleScore.Confidence,
                evidence,
                roleScore.MissingData,
                warnings);
        }

        private static DecisionResult Insufficient(string message, string missing)
        {
            return new DecisionResult(DecisionStatus.InsufficientData, message, null, 0, new List<DecisionEvidence>(), new[] { missing }, new List<string>());
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    public sealed class RoleFitEngine
    {
        private readonly RoleScoringEngine _scoringEngine = new RoleScoringEngine();

        public RoleScore Evaluate(MaskedPlayer player, RoleModel roleModel)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            return _scoringEngine.ScorePlayer(player, roleModel);
        }
    }

    public sealed class OutperformanceEngine
    {
        public DecisionResult Evaluate(string playerLabel, double? playerMetric, double? benchmarkMedian, int sampleSize, int minimumSampleSize)
        {
            if (!playerMetric.HasValue)
            {
                return Insufficient("Missing player output metric; outperformance cannot be claimed.", "Player metric");
            }

            if (!benchmarkMedian.HasValue || sampleSize < minimumSampleSize)
            {
                return Insufficient("Insufficient comparison sample; outperformance cannot be claimed.", "Benchmark sample");
            }

            var delta = playerMetric.Value - benchmarkMedian.Value;
            var score = Clamp((int)Math.Round(50 + delta * 10.0));
            return new DecisionResult(
                DecisionStatus.Available,
                delta > 0 ? "Player is outperforming the comparison median on this safe metric." : "Player is not outperforming the comparison median on this safe metric.",
                score,
                sampleSize >= minimumSampleSize * 2 ? 80 : 60,
                new[] { new DecisionEvidence(playerLabel, "Metric delta versus benchmark median: " + delta.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), delta > 0) },
                new List<string>(),
                new List<string>());
        }

        private static DecisionResult Insufficient(string message, string missing)
        {
            return new DecisionResult(DecisionStatus.InsufficientData, message, null, 0, new List<DecisionEvidence>(), new[] { missing }, new List<string>());
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    public sealed class SquadGapEngine
    {
        public DecisionResult Evaluate(string positionGroup, int currentPlayerCount, int targetPlayerCount)
        {
            if (targetPlayerCount <= 0)
            {
                return new DecisionResult(DecisionStatus.InsufficientData, "No squad target is defined; gap cannot be assessed.", null, 0, new List<DecisionEvidence>(), new[] { "Target player count" }, new List<string>());
            }

            var gap = targetPlayerCount - currentPlayerCount;
            return new DecisionResult(
                DecisionStatus.Available,
                gap > 0 ? "Squad need exists for " + (positionGroup ?? "position group") + "." : "No immediate squad count gap for " + (positionGroup ?? "position group") + ".",
                Clamp(gap * 25),
                70,
                new[] { new DecisionEvidence("Squad count", "Current " + currentPlayerCount + " vs target " + targetPlayerCount + ".", gap > 0) },
                new List<string>(),
                new List<string>());
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    public sealed class BenchmarkEngine
    {
        public DecisionResult EvaluateSample(string benchmarkName, int sampleSize, int minimumSampleSize)
        {
            if (sampleSize < minimumSampleSize)
            {
                return new DecisionResult(
                    DecisionStatus.InsufficientData,
                    "Insufficient sample for benchmark '" + (benchmarkName ?? "Benchmark") + "'. No fake percentile is produced.",
                    null,
                    0,
                    new List<DecisionEvidence>(),
                    new[] { "Minimum sample size " + minimumSampleSize.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    new List<string>());
            }

            return new DecisionResult(
                DecisionStatus.Available,
                "Benchmark sample is available; percentile may be calculated only from real comparison data.",
                Clamp(sampleSize),
                75,
                new[] { new DecisionEvidence("Sample", sampleSize.ToString(System.Globalization.CultureInfo.InvariantCulture) + " comparison players.", true) },
                new List<string>(),
                new List<string>());
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    public sealed class PlayerComparisonEngine
    {
        public DecisionResult Compare(PlayerComparisonInput candidate, PlayerComparisonInput referencePlayer)
        {
            if (candidate == null || referencePlayer == null || !candidate.RoleFit.HasValue || !referencePlayer.RoleFit.HasValue)
            {
                return new DecisionResult(DecisionStatus.InsufficientData, "Comparison needs safe role-fit evidence for both players.", null, 0, new List<DecisionEvidence>(), new[] { "Role fit for both players" }, new List<string>());
            }

            var delta = candidate.RoleFit.Value - referencePlayer.RoleFit.Value;
            var confidence = Math.Min(candidate.Confidence ?? 0, referencePlayer.Confidence ?? 0);
            IReadOnlyList<string> warnings = candidate.MissingDataCount + referencePlayer.MissingDataCount > 0
                ? new[] { "Comparison is limited by missing data." }
                : new List<string>();
            return new DecisionResult(
                DecisionStatus.Available,
                delta > 0 ? "Candidate compares favourably on safe role-fit evidence." : "Candidate does not clearly exceed the reference player.",
                Clamp(50 + delta),
                confidence,
                new[] { new DecisionEvidence("Role fit delta", candidate.Label + " vs " + referencePlayer.Label + ": " + delta.ToString(System.Globalization.CultureInfo.InvariantCulture), delta > 0) },
                new List<string>(),
                warnings);
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    public sealed class RedFlagEngine
    {
        public DecisionResult Evaluate(IReadOnlyList<EvidenceItem> negativeEvidence, int blockedFieldCount)
        {
            var negatives = negativeEvidence ?? new List<EvidenceItem>();
            var warnings = new List<string>();
            if (blockedFieldCount > 0)
            {
                warnings.Add(blockedFieldCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " blocked hidden/raw field(s) excluded from red-flag review.");
            }

            return new DecisionResult(
                negatives.Count == 0 ? DecisionStatus.InsufficientData : DecisionStatus.Available,
                negatives.Count == 0 ? "No negative safe evidence has been recorded yet." : "Safe negative evidence is available for red-flag review.",
                negatives.Count == 0 ? (int?)null : Clamp(negatives.Count * 20),
                negatives.Count == 0 ? 0 : 65,
                negatives.Select(item => new DecisionEvidence(item.FieldName, item.Message, false)).ToList(),
                negatives.Count == 0 ? new[] { "Negative safe evidence" } : new List<string>(),
                warnings);
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }
}
