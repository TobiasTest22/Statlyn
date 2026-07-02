using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Core.Abstractions;
using Statlyn.DataProviders;

namespace Statlyn.UI.Visuals
{
    public sealed class VisualIntelligenceBuilder
    {
        public VisualIntelligenceBundle Build(object player, RoleScore roleScore, SourceMetadata sourceMetadata, DataCompletenessReport completeness)
        {
            if (player is IRawFootballEntity)
            {
                throw new InvalidOperationException("Visual intelligence models cannot be built from raw provider data.");
            }

            if (!(player is MaskedPlayer maskedPlayer))
            {
                throw new InvalidOperationException("Visual intelligence models require a masked player.");
            }

            var isFixtureMode = IsFixtureMode(sourceMetadata);
            var comparisonGroup = isFixtureMode ? "Fixture comparison group" : "No validated comparison group";
            var radar = maskedPlayer.Fields.Values
                .Where(field => field.CanScore && field.IsKnown && field.NumericValue.HasValue)
                .Take(6)
                .Select(field => new RadarMetric(field.FieldName, field.NumericValue!.Value, field.Key == PlayerFieldKey.TechnicalAttribute ? 20 : 100, field.Confidence, sourceMetadata.SourceName, false, string.Empty))
                .ToList();

            var bars = maskedPlayer.Fields.Values
                .Where(field => field.CanScore && field.IsKnown && field.NumericValue.HasValue)
                .Take(6)
                .Select(field => new PercentileBar(field.FieldName, field.NumericValue!.Value, ToPercentile(field), comparisonGroup, field.Confidence, sourceMetadata.SourceName, false, string.Empty))
                .ToList();

            var missing = roleScore.MissingData
                .Select(field => new MissingDataWarning(field, "Field is missing or unavailable after masking.", "Reduces confidence.", "Scout or import this field before making a stronger decision."))
                .ToList();

            var evidence = new List<EvidenceCard>();
            evidence.AddRange(roleScore.PositiveEvidence.Select(item => new EvidenceCard(item.FieldName, EvidenceCategory.Positive, item.Message, sourceMetadata.SourceName, roleScore.Confidence, "Keep as positive evidence.")));
            evidence.AddRange(roleScore.NegativeEvidence.Select(item => new EvidenceCard(item.FieldName, EvidenceCategory.Negative, item.Message, sourceMetadata.SourceName, roleScore.Confidence, "Review risk with scout.")));
            evidence.AddRange(missing.Select(item => new EvidenceCard(item.FieldName, EvidenceCategory.Missing, item.Reason, sourceMetadata.SourceName, 0, item.SuggestedAction)));

            var categories = maskedPlayer.BlockedFields.Select(field => field.Key.ToString()).Distinct().OrderBy(value => value).ToList();
            var blocked = new BlockedDataNoticeView(maskedPlayer.BlockedFields.Count, categories, maskedPlayer.BlockedFields.Count == 0 ? "No blocked fields were present." : maskedPlayer.BlockedFields.Count + " blocked field(s) were excluded. Raw blocked values are not exposed.");
            if (maskedPlayer.BlockedFields.Count > 0)
            {
                evidence.Add(new EvidenceCard("Blocked data", EvidenceCategory.Blocked, blocked.SafeMessage, sourceMetadata.SourceName, 0, "Keep blocked values out of UI, scoring and storage."));
            }

            if (roleScore.RiskScore > 0)
            {
                var riskMessage = roleScore.Confidence < 55
                    ? "Risk is directional because source confidence, scout knowledge or completeness is low."
                    : "Risk is provisional and should be read with the current confidence level.";
                evidence.Add(new EvidenceCard("Risk confidence", EvidenceCategory.Risk, riskMessage, sourceMetadata.SourceName, roleScore.Confidence, "Scout further before treating risk as precise."));
            }

            return new VisualIntelligenceBundle(
                radar,
                bars,
                new RoleFitVisual(roleScore.RoleName, roleScore.RoleFit, roleScore.Confidence, roleScore.Recommendation, roleScore.Recommendation.ToString(), roleScore.MissingData.Count == 0 ? string.Empty : "Missing data reduces confidence.", !roleScore.TacticalFit.HasValue, roleScore.TacticalFit.HasValue ? "Tactical fit: " + roleScore.TacticalFit.Value : "Tactical fit unknown"),
                new ConfidenceVisual(roleScore.Confidence, roleScore.Confidence < 55 ? "Low" : roleScore.Confidence < 75 ? "Medium" : "High", BuildConfidenceReason(roleScore, sourceMetadata, maskedPlayer, completeness), sourceMetadata.SourceConfidence, maskedPlayer.ScoutKnowledgePercentage, completeness.CompletenessPercentage),
                new RiskVisual(roleScore.RiskScore, roleScore.Confidence < 55 ? "Directional" : roleScore.RiskScore > 65 ? "Elevated" : "Controlled", roleScore.NegativeEvidence.Select(item => item.Message).ToList(), completeness.CompletenessPercentage < 50, roleScore.Confidence < 55, maskedPlayer.BlockedFields.Count > 0),
                evidence,
                new List<TrendVisual> { new TrendVisual("Performance trend", new List<double>(), "Unavailable", sourceMetadata.SourceName, false, "No trend data available yet.") },
                new List<ComparisonCard>(),
                missing,
                blocked);
        }

        private static int ToPercentile(VisiblePlayerField field)
        {
            if (!field.NumericValue.HasValue)
            {
                return 0;
            }

            var max = field.Key == PlayerFieldKey.TechnicalAttribute ? 20.0 : 100.0;
            return Math.Max(0, Math.Min(100, (int)Math.Round(field.NumericValue.Value / max * 100.0)));
        }

        private static bool IsFixtureMode(SourceMetadata sourceMetadata)
        {
            return sourceMetadata.SourceName.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sourceMetadata.AllowedUsage.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildConfidenceReason(RoleScore roleScore, SourceMetadata sourceMetadata, MaskedPlayer maskedPlayer, DataCompletenessReport completeness)
        {
            var reasons = new List<string>();
            if (roleScore.MissingData.Count > 0 || completeness.CompletenessPercentage < 100)
            {
                reasons.Add("missing role data");
            }

            if (sourceMetadata.SourceConfidence < 75)
            {
                reasons.Add("source confidence");
            }

            if (maskedPlayer.ScoutKnowledgePercentage < 70)
            {
                reasons.Add("scout knowledge");
            }

            if (reasons.Count == 0)
            {
                return "Sufficient for a provisional view.";
            }

            return "Confidence is limited by " + string.Join(", ", reasons) + ".";
        }
    }
}
