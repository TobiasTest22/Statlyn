using System;
using System.Collections.Generic;
using Statlyn.Core;

namespace Statlyn.Analytics
{
    public sealed class RoleScoringEngine
    {
        public RoleScore ScorePlayer(object player, RoleModel roleModel)
        {
            if (!(player is MaskedPlayer maskedPlayer))
            {
                throw new InvalidOperationException("Role scoring only accepts masked players from the Scouting Knowledge Firewall.");
            }

            return ScoreMaskedPlayer(maskedPlayer, roleModel);
        }

        private static RoleScore ScoreMaskedPlayer(MaskedPlayer player, RoleModel roleModel)
        {
            if (roleModel == null)
            {
                throw new ArgumentNullException(nameof(roleModel));
            }

            var positive = new List<EvidenceItem>();
            var negative = new List<EvidenceItem>();
            var missing = new List<string>();
            var technical = ScoreWeightedGroup(player, roleModel.AttributeWeights, positive, negative, missing, useAttributeScale: true);
            var statistical = ScoreWeightedGroup(player, roleModel.StatWeights, positive, negative, missing, useAttributeScale: false);
            var physical = ScoreWeightedGroup(player, roleModel.PhysicalWeights, positive, negative, missing, useAttributeScale: false);
            var scout = ScoreWeightedGroup(player, roleModel.ScoutObservationWeights, positive, negative, missing, useAttributeScale: false);
            var roleFit = CombineFits(technical.Score, statistical.Score, physical.Score, scout.Score);
            var completeness = CombineCompleteness(technical.Completeness, statistical.Completeness, physical.Completeness, scout.Completeness);
            var averageFieldConfidence = CombineCompleteness(technical.FieldConfidence, statistical.FieldConfidence, physical.FieldConfidence, scout.FieldConfidence);
            var confidence = Clamp((player.Confidence + completeness + averageFieldConfidence) / 3);
            var riskScore = Clamp(100 - confidence + roleModel.RedFlags.Count * 5);
            var recommendation = Recommend(player, roleFit, confidence, missing.Count);
            var blockedNotice = player.BlockedFields.Count == 0
                ? string.Empty
                : player.BlockedFields.Count + " blocked field(s) were excluded from scoring.";

            return new RoleScore(
                roleModel.RoleName,
                roleFit,
                technical.Score,
                statistical.Score,
                physical.Score,
                tacticalFit: 0,
                riskScore,
                confidence,
                recommendation,
                positive,
                negative,
                missing,
                blockedNotice);
        }

        private static WeightedScoreResult ScoreWeightedGroup(
            MaskedPlayer player,
            IReadOnlyDictionary<string, double> weights,
            ICollection<EvidenceItem> positive,
            ICollection<EvidenceItem> negative,
            ICollection<string> missing,
            bool useAttributeScale)
        {
            var weightedScore = 0.0;
            var totalWeight = 0.0;
            var knownWeight = 0.0;
            var fieldConfidence = 0;
            var knownFields = 0;

            foreach (var weight in weights)
            {
                totalWeight += weight.Value;
                var field = FindScorableField(player, weight.Key);
                if (field == null)
                {
                    missing.Add(weight.Key);
                    continue;
                }

                knownWeight += weight.Value;
                knownFields++;
                fieldConfidence += field.Confidence;
                var normalized = useAttributeScale
                    ? NormalizeFmAttribute((int)Math.Round(field.NumericValue.GetValueOrDefault()))
                    : Clamp((int)Math.Round(field.NumericValue.GetValueOrDefault()));
                weightedScore += normalized * weight.Value;

                if (normalized >= 75)
                {
                    positive.Add(new EvidenceItem(weight.Key, weight.Key + " is visible positive evidence.", true));
                }
                else if (normalized <= 40)
                {
                    negative.Add(new EvidenceItem(weight.Key, weight.Key + " is visible negative evidence.", false));
                }
            }

            var score = totalWeight <= 0 || knownWeight <= 0 ? 0 : Clamp((int)Math.Round(weightedScore / totalWeight));
            var completeness = totalWeight <= 0 ? 100 : Clamp((int)Math.Round(knownWeight / totalWeight * 100.0));
            var averageConfidence = knownFields == 0 ? 0 : fieldConfidence / knownFields;
            return new WeightedScoreResult(score, completeness, averageConfidence);
        }

        private static VisiblePlayerField? FindScorableField(MaskedPlayer player, string fieldName)
        {
            if (player.Attributes.TryGetValue(fieldName, out var attribute) && attribute.IsKnown && attribute.CanScore)
            {
                return new VisiblePlayerField(
                    PlayerFieldKey.TechnicalAttribute,
                    fieldName,
                    attribute.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    attribute.Value,
                    FieldValueKind.Number,
                    true,
                    false,
                    true,
                    true,
                    true,
                    attribute.Confidence,
                    attribute.SourceProvider,
                    string.Empty);
            }

            foreach (var field in player.Fields.Values)
            {
                if (field.CanScore && field.IsKnown && field.NumericValue.HasValue && string.Equals(field.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            return null;
        }

        private static int CombineFits(params int[] scores)
        {
            var known = 0;
            var sum = 0;
            foreach (var score in scores)
            {
                if (score > 0)
                {
                    known++;
                    sum += score;
                }
            }

            return known == 0 ? 0 : Clamp(sum / known);
        }

        private static int CombineCompleteness(params int[] scores)
        {
            if (scores.Length == 0)
            {
                return 0;
            }

            var sum = 0;
            foreach (var score in scores)
            {
                sum += score;
            }

            return Clamp(sum / scores.Length);
        }

        private static RecruitmentRecommendation Recommend(MaskedPlayer player, int roleFit, int confidence, int missingCount)
        {
            if (player.ProviderType == ProviderType.FM26LiveMemory && player.ScoutKnowledgePercentage < 50)
            {
                return RecruitmentRecommendation.ScoutFurther;
            }

            if (confidence < 55 || missingCount > 0 && confidence < 70)
            {
                return RecruitmentRecommendation.ScoutFurther;
            }

            if (roleFit >= 82 && confidence >= 75)
            {
                return RecruitmentRecommendation.Sign;
            }

            if (roleFit >= 72)
            {
                return RecruitmentRecommendation.Shortlist;
            }

            if (roleFit >= 58)
            {
                return RecruitmentRecommendation.Monitor;
            }

            return RecruitmentRecommendation.Avoid;
        }

        private static int NormalizeFmAttribute(int attributeValue)
        {
            if (attributeValue <= 1)
            {
                return 0;
            }

            if (attributeValue >= 20)
            {
                return 100;
            }

            return Clamp((int)Math.Round((attributeValue - 1) / 19.0 * 100.0));
        }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }

        private sealed class WeightedScoreResult
        {
            public WeightedScoreResult(int score, int completeness, int fieldConfidence)
            {
                Score = score;
                Completeness = completeness;
                FieldConfidence = fieldConfidence;
            }

            public int Score { get; }

            public int Completeness { get; }

            public int FieldConfidence { get; }
        }
    }
}
