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

            var radar = maskedPlayer.Fields.Values
                .Where(field => field.CanScore && field.IsKnown && field.NumericValue.HasValue)
                .Take(6)
                .Select(field => new RadarMetric(field.FieldName, field.NumericValue!.Value, field.Key == PlayerFieldKey.TechnicalAttribute ? 20 : 100, field.Confidence, sourceMetadata.SourceName, false, string.Empty))
                .ToList();

            var bars = maskedPlayer.Fields.Values
                .Where(field => field.CanScore && field.IsKnown && field.NumericValue.HasValue)
                .Take(6)
                .Select(field => new PercentileBar(field.FieldName, field.NumericValue!.Value, ToPercentile(field), "Fixture comparison group", field.Confidence, sourceMetadata.SourceName, false, string.Empty))
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

            return new VisualIntelligenceBundle(
                radar,
                bars,
                new RoleFitVisual(roleScore.RoleName, roleScore.RoleFit, roleScore.Confidence, roleScore.Recommendation, roleScore.Recommendation.ToString(), roleScore.MissingData.Count == 0 ? string.Empty : "Missing data reduces confidence."),
                new ConfidenceVisual(roleScore.Confidence, roleScore.Confidence < 55 ? "Low" : roleScore.Confidence < 75 ? "Medium" : "High", roleScore.Confidence < 55 ? "More scouting or source validation needed." : "Sufficient for a provisional view.", sourceMetadata.SourceConfidence, maskedPlayer.ScoutKnowledgePercentage, completeness.CompletenessPercentage),
                new RiskVisual(roleScore.RiskScore, roleScore.RiskScore > 65 ? "Elevated" : "Controlled", roleScore.NegativeEvidence.Select(item => item.Message).ToList(), completeness.CompletenessPercentage < 50, roleScore.Confidence < 55, maskedPlayer.BlockedFields.Count > 0),
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
    }
}
