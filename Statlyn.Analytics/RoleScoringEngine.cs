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
            var roleFit = CombineFits(technical, statistical, physical, scout);
            var completeness = CombineCompleteness(technical, statistical, physical, scout);
            var averageFieldConfidence = CombineFieldConfidence(technical, statistical, physical, scout);
            var sourceLimitedConfidence = Math.Min(player.Confidence, averageFieldConfidence == 0 ? player.Confidence : averageFieldConfidence);
            var calculatedConfidence = Clamp((sourceLimitedConfidence + completeness + averageFieldConfidence) / 3);
            var confidence = Clamp(Math.Min(sourceLimitedConfidence, calculatedConfidence));
            var triggeredRedFlags = EvaluateRedFlags(player, roleModel.RedFlags, negative);
            var riskScore = Clamp(100 - confidence + triggeredRedFlags * 12);
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
                tacticalFit: null,
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
                    : NormalizeGenericMetric(field.NumericValue.GetValueOrDefault());
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
            var completeness = totalWeight <= 0 ? 0 : Clamp((int)Math.Round(knownWeight / totalWeight * 100.0));
            var averageConfidence = knownFields == 0 ? 0 : fieldConfidence / knownFields;
            return new WeightedScoreResult(score, completeness, averageConfidence, knownFields > 0, totalWeight > 0);
        }

        private static VisiblePlayerField? FindScorableField(MaskedPlayer player, string fieldName)
        {
            if (player.Attributes.TryGetValue(fieldName, out var attribute) && attribute.IsKnown && attribute.CanScore)
            {
                return new VisiblePlayerField(
                    PlayerFieldKey.TechnicalAttribute,
                    fieldName,
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

        private static int EvaluateRedFlags(MaskedPlayer player, IReadOnlyList<RedFlag> redFlags, ICollection<EvidenceItem> negative)
        {
            var triggered = 0;
            foreach (var redFlag in redFlags)
            {
                var field = FindScorableField(player, redFlag.FieldName);
                if (field == null || !field.NumericValue.HasValue)
                {
                    continue;
                }

                if (IsTriggered(field.NumericValue.Value, redFlag.OperatorKind, redFlag.Threshold))
                {
                    triggered++;
                    negative.Add(new EvidenceItem(redFlag.FieldName, string.IsNullOrWhiteSpace(redFlag.Message) ? redFlag.FieldName + " triggered a risk flag." : redFlag.Message, false));
                }
            }

            return triggered;
        }

        private static bool IsTriggered(double value, RedFlagOperator operatorKind, double threshold)
        {
            switch (operatorKind)
            {
                case RedFlagOperator.LessThan:
                    return value < threshold;
                case RedFlagOperator.LessThanOrEqual:
                    return value <= threshold;
                case RedFlagOperator.GreaterThan:
                    return value > threshold;
                case RedFlagOperator.GreaterThanOrEqual:
                    return value >= threshold;
                case RedFlagOperator.Equal:
                    return Math.Abs(value - threshold) < 0.0001;
                default:
                    return false;
            }
        }

        private static int CombineFits(params WeightedScoreResult[] groups)
        {
            var known = 0;
            var sum = 0;
            foreach (var group in groups)
            {
                if (group.HasData)
                {
                    known++;
                    sum += group.Score;
                }
            }

            return known == 0 ? 0 : Clamp(sum / known);
        }

        private static int CombineCompleteness(params WeightedScoreResult[] groups)
        {
            var known = 0;
            var sum = 0;
            foreach (var group in groups)
            {
                if (group.HasWeights)
                {
                    known++;
                    sum += group.Completeness;
                }
            }

            return known == 0 ? 0 : Clamp(sum / known);
        }

        private static int CombineFieldConfidence(params WeightedScoreResult[] groups)
        {
            var known = 0;
            var sum = 0;
            foreach (var group in groups)
            {
                if (group.HasData)
                {
                    known++;
                    sum += group.FieldConfidence;
                }
            }

            return known == 0 ? 0 : Clamp(sum / known);
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

        private static int NormalizeGenericMetric(double value)
        {
            if (value >= 0 && value <= 1)
            {
                return Clamp((int)Math.Round(value * 100.0));
            }

            return Clamp((int)Math.Round(value));
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
            public WeightedScoreResult(int score, int completeness, int fieldConfidence, bool hasData, bool hasWeights)
            {
                Score = score;
                Completeness = completeness;
                FieldConfidence = fieldConfidence;
                HasData = hasData;
                HasWeights = hasWeights;
            }

            public int Score { get; }

            public int Completeness { get; }

            public int FieldConfidence { get; }

            public bool HasData { get; }

            public bool HasWeights { get; }
        }
    }
}
