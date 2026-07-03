using System.Collections.Generic;
using System.Globalization;
using Statlyn.UI;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Components
{
    public static class StatlynScoreCardComponent
    {
        public static VisualElement Build(StatlynScoreCardVisual visual)
        {
            var card = new VisualElement();
            card.AddToClassList("visual-score-card");
            card.Add(VisualComponentHelpers.Label(visual.Title, "visual-title"));
            card.Add(VisualComponentHelpers.Label(visual.Value, "visual-score-value"));
            card.Add(VisualComponentHelpers.Label(visual.Caption, "visual-caption"));
            if (!string.IsNullOrWhiteSpace(visual.Status))
            {
                card.Add(VisualComponentHelpers.Label(visual.Status, "visual-status"));
            }

            return card;
        }
    }

    public static class StatlynHorizontalBarComponent
    {
        public static VisualElement Build(StatlynHorizontalBarVisual visual)
        {
            var row = new VisualElement();
            row.AddToClassList("visual-bar-row");

            var header = new VisualElement();
            header.AddToClassList("visual-bar-header");
            header.Add(VisualComponentHelpers.Label(visual.Label, "visual-bar-label"));
            header.Add(VisualComponentHelpers.Label(visual.Value, "visual-bar-value"));
            row.Add(header);

            var track = new VisualElement();
            track.AddToClassList("visual-bar-track");
            var fill = new VisualElement();
            fill.AddToClassList("visual-bar-fill");
            fill.style.width = Length.Percent(visual.IsAvailable && visual.Percent.HasValue ? ClampPercent(visual.Percent.Value) : 0);
            track.Add(fill);
            row.Add(track);

            if (!string.IsNullOrWhiteSpace(visual.Caption))
            {
                row.Add(VisualComponentHelpers.Label(visual.Caption, "visual-caption"));
            }

            return row;
        }

        private static float ClampPercent(double value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return (float)value;
        }
    }

    public static class StatlynMetricTileComponent
    {
        public static VisualElement Build(StatlynMetricTileVisual visual)
        {
            var card = new VisualElement();
            card.AddToClassList("visual-metric-tile");
            if (visual.IsMissing)
            {
                card.AddToClassList("visual-missing");
            }

            card.Add(VisualComponentHelpers.Label(visual.Label, "visual-title"));
            card.Add(VisualComponentHelpers.Label(visual.Value, "visual-metric-value"));
            card.Add(VisualComponentHelpers.Label(visual.Section, "visual-caption"));
            card.Add(VisualComponentHelpers.Label("Source: " + visual.Source, "visual-caption"));
            card.Add(VisualComponentHelpers.Label("Confidence: " + visual.Confidence, "visual-caption"));
            card.Add(VisualComponentHelpers.Label(visual.Sample, "visual-caption"));
            card.Add(VisualComponentHelpers.Label(visual.VerificationLabel, visual.IsGenericImportMetric ? "visual-warning-text" : "visual-caption"));
            return card;
        }
    }

    public static class StatlynMetricGroupComponent
    {
        public static VisualElement Build(StatlynMetricGroupVisual visual)
        {
            var section = new VisualElement();
            section.AddToClassList("visual-section");
            section.Add(VisualComponentHelpers.Label(visual.Title, "visual-section-title"));
            section.Add(VisualComponentHelpers.Label(visual.Summary, "visual-section-summary"));

            var grid = new VisualElement();
            grid.AddToClassList("visual-metric-grid");
            section.Add(grid);

            foreach (var metric in visual.Metrics)
            {
                grid.Add(StatlynMetricTileComponent.Build(metric));
            }

            if (visual.Metrics.Count == 0)
            {
                grid.Add(StatlynMetricTileComponent.Build(new StatlynMetricTileVisual(
                    visual.Title,
                    "Missing",
                    visual.Title,
                    "Persisted safe report",
                    "n/a",
                    "No sample",
                    "Missing output is not treated as zero",
                    false,
                    true)));
            }

            foreach (var missing in visual.MissingMetrics)
            {
                if (missing.IsMissing)
                {
                    section.Add(StatlynMissingDataComponent.Build(missing));
                }
            }

            return section;
        }
    }

    public static class StatlynDataQualityComponent
    {
        public static VisualElement Build(StatlynDataQualityVisual visual)
        {
            var card = new VisualElement();
            card.AddToClassList("visual-data-quality");
            if (visual.IsWarning)
            {
                card.AddToClassList("visual-warning");
            }

            card.Add(VisualComponentHelpers.Label(visual.Label, "visual-title"));
            card.Add(VisualComponentHelpers.Label(visual.Value, "visual-metric-value"));
            card.Add(VisualComponentHelpers.Label(visual.Caption, "visual-caption"));
            return card;
        }
    }

    public static class StatlynEvidenceCardComponent
    {
        public static VisualElement Build(StatlynEvidenceVisual visual)
        {
            var card = new VisualElement();
            card.AddToClassList("visual-evidence-card");
            card.Add(VisualComponentHelpers.Label(visual.Category, "visual-kicker"));
            card.Add(VisualComponentHelpers.Label(visual.Title, "visual-title"));
            card.Add(VisualComponentHelpers.Label(visual.Body, "visual-body"));
            card.Add(VisualComponentHelpers.Label("Source: " + visual.Source + " | Confidence: " + visual.Confidence, "visual-caption"));
            return card;
        }
    }

    public static class StatlynWarningPanelComponent
    {
        public static VisualElement Build(StatlynWarningVisual visual)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.AddToClassList("visual-warning-panel");
            panel.Add(VisualComponentHelpers.Label(visual.Title, "visual-section-title"));
            panel.Add(VisualComponentHelpers.Label(visual.Message, "visual-body"));
            if (!string.IsNullOrWhiteSpace(visual.Severity))
            {
                panel.Add(VisualComponentHelpers.Label("Severity: " + visual.Severity, "visual-caption"));
            }

            foreach (var row in visual.Rows)
            {
                panel.Add(VisualComponentHelpers.Label(row, "visual-caption"));
            }

            return panel;
        }
    }

    public static class StatlynMissingDataComponent
    {
        public static VisualElement Build(StatlynMissingDataVisual visual)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-missing-panel");
            panel.Add(VisualComponentHelpers.Label(visual.Label, "visual-title"));
            panel.Add(VisualComponentHelpers.Label(visual.SafeMessage, "visual-body"));
            panel.Add(VisualComponentHelpers.Label(visual.Caption, "visual-caption"));
            return panel;
        }
    }

    public static class StatlynBlockedDataComponent
    {
        public static VisualElement Build(StatlynWarningVisual visual)
        {
            var panel = StatlynWarningPanelComponent.Build(visual);
            panel.AddToClassList("visual-blocked-panel");
            return panel;
        }
    }

    public static class StatlynBenchmarkStatusComponent
    {
        public static VisualElement Build(StatlynBenchmarkStatusVisual visual)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(VisualComponentHelpers.Label("Benchmark Status", "visual-section-title"));
            panel.Add(VisualComponentHelpers.Label(visual.SafeMessage, "visual-body"));
            panel.Add(VisualComponentHelpers.Label(visual.HasBenchmark ? "Benchmark available" : "No comparison group loaded", "visual-caption"));
            panel.Add(VisualComponentHelpers.Label(visual.Percentile.HasValue ? "Percentile: " + visual.Percentile.Value.ToString(CultureInfo.InvariantCulture) : "Percentile: unavailable", "visual-caption"));
            return panel;
        }
    }

    public static class StatlynBadgeRowComponent
    {
        public static VisualElement Build(IReadOnlyList<string> badges)
        {
            var row = new VisualElement();
            row.AddToClassList("visual-badge-row");
            foreach (var badge in badges)
            {
                row.Add(VisualComponentHelpers.Label(badge, "visual-badge"));
            }

            return row;
        }
    }

    internal static class VisualComponentHelpers
    {
        public static Label Label(string text, string className)
        {
            var label = new Label(text ?? string.Empty);
            label.AddToClassList(className);
            return label;
        }
    }
}
