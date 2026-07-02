using System;
using System.Globalization;
using System.Linq;
using Statlyn.UI.Visuals;

namespace Statlyn.UI.UnityBridge
{
    public sealed class UnityProfileRenderModel
    {
        private UnityProfileRenderModel(
            string playerName,
            string initials,
            string detailLine,
            string flagLine,
            string sourceName,
            int sourceConfidence,
            int dataCompleteness,
            int scoutKnowledge,
            string dataCompletenessCaption,
            string roleFit,
            string roleFitCaption,
            string confidence,
            string confidenceCaption,
            string risk,
            string riskCaption,
            bool isFixtureMode,
            bool isLiveFm26Data,
            UnityRadarMetric[] radarMetrics,
            UnityPercentileBar[] percentileBars,
            UnityEvidenceCard[] evidenceCards,
            string missingDataMessage,
            string blockedDataMessage)
        {
            PlayerName = playerName;
            Initials = initials;
            DetailLine = detailLine;
            FlagLine = flagLine;
            SourceName = sourceName;
            SourceConfidence = sourceConfidence;
            DataCompleteness = dataCompleteness;
            ScoutKnowledge = scoutKnowledge;
            DataCompletenessCaption = dataCompletenessCaption;
            RoleFit = roleFit;
            RoleFitCaption = roleFitCaption;
            Confidence = confidence;
            ConfidenceCaption = confidenceCaption;
            Risk = risk;
            RiskCaption = riskCaption;
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
            RadarMetrics = radarMetrics;
            PercentileBars = percentileBars;
            EvidenceCards = evidenceCards;
            MissingDataMessage = missingDataMessage;
            BlockedDataMessage = blockedDataMessage;
        }

        public string PlayerName { get; }

        public string Initials { get; }

        public string DetailLine { get; }

        public string FlagLine { get; }

        public string SourceName { get; }

        public int SourceConfidence { get; }

        public int DataCompleteness { get; }

        public int ScoutKnowledge { get; }

        public string DataCompletenessCaption { get; }

        public string RoleFit { get; }

        public string RoleFitCaption { get; }

        public string Confidence { get; }

        public string ConfidenceCaption { get; }

        public string Risk { get; }

        public string RiskCaption { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public UnityRadarMetric[] RadarMetrics { get; }

        public UnityPercentileBar[] PercentileBars { get; }

        public UnityEvidenceCard[] EvidenceCards { get; }

        public string MissingDataMessage { get; }

        public string BlockedDataMessage { get; }

        public static UnityProfileRenderModel From(MaskedPlayerProfileViewModel profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var missingNames = profile.MissingDataWarnings.Select(warning => warning.FieldName).ToArray();
            var missingDataMessage = profile.MissingDataWarnings.Count == 0
                ? "No role-critical fields are missing from the masked fixture profile."
                : "Missing role data: " + string.Join(", ", missingNames) + ". Scout or import these fields before making a stronger decision.";

            var blockedDataMessage = profile.BlockedDataNotice.Count == 0
                ? profile.BlockedDataNotice.SafeMessage
                : profile.BlockedDataNotice.Count.ToString(CultureInfo.InvariantCulture) +
                  " blocked field category/categories excluded: " +
                  string.Join(", ", profile.BlockedDataNotice.Categories) +
                  ". Raw values are not shown.";

            return new UnityProfileRenderModel(
                profile.PlayerName,
                profile.Initials,
                profile.AgeDisplay + " - " + profile.NationalityDisplay + " - " + profile.PositionDisplay + " - " + LiveDataLabel(profile),
                BuildFlagLine(profile),
                profile.SourceName,
                profile.SourceConfidence,
                profile.DataCompleteness,
                profile.ScoutKnowledge,
                profile.DataCompleteness.ToString(CultureInfo.InvariantCulture) + "% known after field policy masking",
                profile.RoleFit.ToString(CultureInfo.InvariantCulture),
                BuildRoleFitCaption(profile.RoleFitVisual),
                profile.ConfidenceVisual.Label,
                profile.ConfidenceVisual.Reason,
                profile.RiskVisual.Label,
                BuildRiskCaption(profile.RiskVisual),
                profile.IsFixtureMode,
                profile.IsLiveFm26Data,
                profile.RadarMetrics.Select(UnityRadarMetric.From).ToArray(),
                profile.PercentileBars.Select(UnityPercentileBar.From).ToArray(),
                profile.EvidenceCards.Select(UnityEvidenceCard.From).ToArray(),
                missingDataMessage,
                blockedDataMessage);
        }

        private static string LiveDataLabel(MaskedPlayerProfileViewModel profile)
        {
            return profile.IsLiveFm26Data ? "Live FM26 source" : "No live FM26 data";
        }

        private static string BuildFlagLine(MaskedPlayerProfileViewModel profile)
        {
            switch (profile.FlagDisplayMode)
            {
                case FlagDisplayMode.ProviderFlag:
                    return "Flag mode: provider flag permitted";
                case FlagDisplayMode.BundledSafeFlag:
                    return "Flag mode: bundled-safe placeholder";
                default:
                    return "Flag mode: unavailable";
            }
        }

        private static string BuildRoleFitCaption(RoleFitVisual visual)
        {
            if (visual.IsTacticalFitUnknown)
            {
                return "Tactical fit unknown; role fit uses visible attributes, stats and physical data only.";
            }

            return string.IsNullOrWhiteSpace(visual.MissingDataWarning)
                ? visual.TacticalFitLabel
                : visual.MissingDataWarning;
        }

        private static string BuildRiskCaption(RiskVisual visual)
        {
            if (visual.IsLowConfidence)
            {
                return "Low-confidence fixture; risk is directional, not precise.";
            }

            if (visual.MainRiskReasons.Count == 0)
            {
                return "No specific risk evidence is available yet.";
            }

            return string.Join("; ", visual.MainRiskReasons);
        }
    }

    public sealed class UnityRadarMetric
    {
        private UnityRadarMetric(string label, double value, double maximumValue, int confidence, string sourceName)
        {
            Label = label;
            Value = value;
            MaximumValue = maximumValue;
            Confidence = confidence;
            SourceName = sourceName;
        }

        public string Label { get; }

        public double Value { get; }

        public double MaximumValue { get; }

        public int Confidence { get; }

        public string SourceName { get; }

        public static UnityRadarMetric From(RadarMetric metric)
        {
            return new UnityRadarMetric(metric.Label, metric.Value, metric.MaximumValue, metric.Confidence, metric.SourceName);
        }
    }

    public sealed class UnityPercentileBar
    {
        private UnityPercentileBar(string label, int percentile, string comparisonGroup, int confidence, string sourceName)
        {
            Label = label;
            Percentile = percentile;
            ComparisonGroup = comparisonGroup;
            Confidence = confidence;
            SourceName = sourceName;
        }

        public string Label { get; }

        public int Percentile { get; }

        public string ComparisonGroup { get; }

        public int Confidence { get; }

        public string SourceName { get; }

        public static UnityPercentileBar From(PercentileBar bar)
        {
            return new UnityPercentileBar(bar.Label, bar.Percentile, bar.ComparisonGroup, bar.Confidence, bar.SourceName);
        }
    }

    public sealed class UnityEvidenceCard
    {
        private UnityEvidenceCard(string title, string body)
        {
            Title = title;
            Body = body;
        }

        public string Title { get; }

        public string Body { get; }

        public static UnityEvidenceCard From(EvidenceCard card)
        {
            return new UnityEvidenceCard(card.Category + " Evidence", card.Body);
        }
    }
}
