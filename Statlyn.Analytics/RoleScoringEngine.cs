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
            var weightedScore = 0.0;
            var totalWeight = 0.0;
            var knownWeight = 0.0;
            var fieldConfidence = 0;
            var knownFields = 0;

            foreach (var weight in roleModel.AttributeWeights)
            {
                totalWeight += weight.Value;

                if (!player.Attributes.TryGetValue(weight.Key, out var field) || !field.IsKnown || !field.CanScore)
                {
                    missing.Add(weight.Key);
                    continue;
                }

                knownWeight += weight.Value;
                knownFields++;
                fieldConfidence += field.Confidence;
                var normalized = NormalizeFmAttribute(field.Value);
                weightedScore += normalized * weight.Value;

                if (field.Value >= 15)
                {
                    positive.Add(new EvidenceItem(weight.Key, weight.Key + " is a visible strength.", true));
                }
                else if (field.Value <= 8)
                {
                    negative.Add(new EvidenceItem(weight.Key, weight.Key + " is a visible weakness.", false));
                }
            }

            var roleFit = totalWeight <= 0 || knownWeight <= 0 ? 0 : Clamp((int)Math.Round(weightedScore / totalWeight));
            var completeness = totalWeight <= 0 ? 0 : Clamp((int)Math.Round(knownWeight / totalWeight * 100.0));
            var averageFieldConfidence = knownFields == 0 ? 0 : fieldConfidence / knownFields;
            var confidence = Clamp((player.Confidence + completeness + averageFieldConfidence) / 3);
            var recommendation = Recommend(player, roleFit, confidence, missing.Count);

            return new RoleScore(roleModel.RoleName, roleFit, confidence, recommendation, positive, negative, missing);
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
    }
}
