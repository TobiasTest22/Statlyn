using System.Collections.Generic;
using Statlyn.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Components
{
    public static class StatlynUiFactory
    {
        public const string LightLogoResourceKey = "Branding/Statlyn_Logo_Black-text";
        public const string DarkLogoResourceKey = "Branding/Statlyn_Logo_White-text";
        public const string FullLightLogoResourceKey = "Branding/StatLyn_Logo";
        public const string FullDarkLogoResourceKey = "Branding/StatLyn_Logo_Reversed";

        public static VisualElement MakeBrandLockup(string logoResourceKey = DarkLogoResourceKey)
        {
            var lockup = new VisualElement();
            lockup.AddToClassList("brand-lockup");

            var logo = MakeLogoImage(logoResourceKey, "brand-logo");
            if (logo != null)
            {
                lockup.Add(logo);
            }

            var label = new Label("Statlyn");
            label.AddToClassList("brand-wordmark");
            lockup.Add(label);
            return lockup;
        }

        public static Image MakeLogoImage(string resourceKey, string className)
        {
            var texture = Resources.Load<Texture2D>(resourceKey);
            if (texture == null)
            {
                return null;
            }

            var image = new Image();
            image.image = texture;
            image.scaleMode = ScaleMode.ScaleToFit;
            image.AddToClassList(className);
            return image;
        }

        public static VisualElement MakeCard(string heading, IEnumerable<string> rows)
        {
            var card = new VisualElement();
            card.AddToClassList("glass-card");

            var label = new Label(heading);
            label.AddToClassList("card-title");
            card.Add(label);

            foreach (var row in rows)
            {
                var rowLabel = new Label(row);
                rowLabel.AddToClassList("card-row");
                card.Add(rowLabel);
            }

            return card;
        }

        public static VisualElement MakePageHeader(string title, string subtitle, string statusText, string logoResourceKey = LightLogoResourceKey)
        {
            var header = new VisualElement();
            header.AddToClassList("header");

            var headerBrand = new VisualElement();
            headerBrand.AddToClassList("header-brand");
            header.Add(headerBrand);

            var logo = MakeLogoImage(logoResourceKey, "header-logo");
            if (logo != null)
            {
                headerBrand.Add(logo);
            }

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            headerBrand.Add(titleStack);

            var titleLabel = new Label(title ?? string.Empty);
            titleLabel.AddToClassList("screen-title");
            titleStack.Add(titleLabel);

            var subtitleLabel = new Label(subtitle ?? string.Empty);
            subtitleLabel.AddToClassList("screen-subtitle");
            titleStack.Add(subtitleLabel);

            var status = new Label(statusText ?? string.Empty);
            status.AddToClassList("status-pill");
            header.Add(status);

            return header;
        }

        public static VisualElement MakeCommandPageHeader(string title, string subtitle, string statusText, CommandStatusCategory statusCategory = CommandStatusCategory.Accent)
        {
            var header = new VisualElement();
            header.AddToClassList("header");
            header.AddToClassList("command-page-header");

            var headerBrand = new VisualElement();
            headerBrand.AddToClassList("header-brand");
            header.Add(headerBrand);

            var logo = MakeLogoImage(DarkLogoResourceKey, "header-logo");
            if (logo != null)
            {
                headerBrand.Add(logo);
            }

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            headerBrand.Add(titleStack);

            var titleLabel = new Label(title ?? string.Empty);
            titleLabel.AddToClassList("screen-title");
            titleStack.Add(titleLabel);

            var subtitleLabel = new Label(subtitle ?? string.Empty);
            subtitleLabel.AddToClassList("screen-subtitle");
            titleStack.Add(subtitleLabel);

            header.Add(MakeCommandStatusPill(statusText, statusCategory));
            return header;
        }

        public static Label MakeCommandStatusPill(string text, CommandStatusCategory category)
        {
            var status = new Label(text ?? string.Empty);
            status.AddToClassList("status-pill");
            status.AddToClassList("command-status-pill");
            status.AddToClassList(ThemeTokens.StatusClassFor(category));
            return status;
        }

        public static VisualElement MakeCommandKpiCard(string title, string value, string caption, CommandStatusCategory category = CommandStatusCategory.Neutral)
        {
            var card = new VisualElement();
            card.AddToClassList("command-kpi-card");
            card.AddToClassList(ThemeTokens.StatusClassFor(category));

            var titleLabel = new Label(title ?? string.Empty);
            titleLabel.AddToClassList("command-kpi-title");
            card.Add(titleLabel);

            var valueLabel = new Label(value ?? string.Empty);
            valueLabel.AddToClassList("command-kpi-value");
            card.Add(valueLabel);

            var captionLabel = new Label(caption ?? string.Empty);
            captionLabel.AddToClassList("command-kpi-caption");
            card.Add(captionLabel);
            return card;
        }

        public static VisualElement MakeCommandPanel(string title, IEnumerable<string> rows)
        {
            var panel = new VisualElement();
            panel.AddToClassList("command-panel");

            var heading = new Label(title ?? string.Empty);
            heading.AddToClassList("command-panel-title");
            panel.Add(heading);

            var hasRows = false;
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    hasRows = true;
                    var rowLabel = new Label(row ?? string.Empty);
                    rowLabel.AddToClassList("command-panel-row");
                    panel.Add(rowLabel);
                }
            }

            if (!hasRows)
            {
                var empty = new Label("No safe rows to show.");
                empty.AddToClassList("command-panel-row");
                panel.Add(empty);
            }

            return panel;
        }

        public static VisualElement MakeCommandMetricRow(string label, string value, string caption, CommandStatusCategory category = CommandStatusCategory.Neutral)
        {
            var row = new VisualElement();
            row.AddToClassList("command-metric-row");
            row.AddToClassList(ThemeTokens.StatusClassFor(category));

            var labelStack = new VisualElement();
            labelStack.AddToClassList("command-metric-label-stack");
            row.Add(labelStack);

            var name = new Label(label ?? string.Empty);
            name.AddToClassList("command-metric-label");
            labelStack.Add(name);

            var detail = new Label(caption ?? string.Empty);
            detail.AddToClassList("command-metric-caption");
            labelStack.Add(detail);

            var valueLabel = new Label(value ?? string.Empty);
            valueLabel.AddToClassList("command-metric-value");
            row.Add(valueLabel);
            return row;
        }

        public static VisualElement MakeCommandActionButtonRow(params VisualElement[] actions)
        {
            var row = new VisualElement();
            row.AddToClassList("action-row");
            row.AddToClassList("command-action-row");
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    if (action != null)
                    {
                        row.Add(action);
                    }
                }
            }

            return row;
        }

        public static VisualElement MakeCommandWarningBanner(string title, IEnumerable<string> rows)
        {
            var banner = MakeCommandPanel(title, rows);
            banner.AddToClassList("command-warning-banner");
            return banner;
        }

        public static VisualElement MakeCommandDataQualityPanel(string title, IEnumerable<string> rows, CommandStatusCategory category = CommandStatusCategory.Warning)
        {
            var panel = MakeCommandPanel(title, rows);
            panel.AddToClassList("command-data-quality-panel");
            panel.AddToClassList(ThemeTokens.StatusClassFor(category));
            return panel;
        }

        public static VisualElement MakeCommandEmptyState(string title, params string[] rows)
        {
            var safeRows = rows == null || rows.Length == 0 ? new[] { ThemeTokens.EmptyStateMessage(title) } : rows;
            var panel = MakeCommandPanel(title, safeRows);
            panel.AddToClassList("command-empty-state");
            return panel;
        }

        public static VisualElement MakeCommandSectionTabs(IReadOnlyList<string> tabs, string activeTab)
        {
            var row = new VisualElement();
            row.AddToClassList("command-section-tabs");
            if (tabs == null || tabs.Count == 0)
            {
                return row;
            }

            foreach (var tab in tabs)
            {
                var label = new Label(tab ?? string.Empty);
                label.AddToClassList("command-section-tab");
                if (!string.IsNullOrWhiteSpace(activeTab) && string.Equals(tab, activeTab, System.StringComparison.OrdinalIgnoreCase))
                {
                    label.AddToClassList("command-section-tab-active");
                }

                row.Add(label);
            }

            return row;
        }

        public static VisualElement MakeSafetyBanner(params string[] rows)
        {
            return MakeCard("Safety", rows == null || rows.Length == 0 ? new[] { "Persisted safe data only", "No live FM26 data" } : rows);
        }

        public static VisualElement MakeEmptyState(string title, params string[] rows)
        {
            return MakeCard(title, rows == null || rows.Length == 0 ? new[] { "No data is shown." } : rows);
        }

        public static VisualElement MakeErrorCard(string title, params string[] rows)
        {
            return MakeCard(title, rows == null || rows.Length == 0 ? new[] { "A safe error occurred." } : rows);
        }

        public static VisualElement MakeRuntimeStatusCard(string title, bool? status, string detail)
        {
            var label = status.HasValue ? status.Value ? "Passed" : "Failed" : "Not run";
            return MakeCard(title, new[] { label, detail ?? string.Empty });
        }

        public static Label MakeSectionTitle(string title)
        {
            var label = new Label(title);
            label.AddToClassList("card-title");
            return label;
        }

        public static VisualElement MakeMetricCard(string title, string value, string caption)
        {
            var card = new VisualElement();
            card.AddToClassList("metric-card");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("metric-title");
            card.Add(titleLabel);
            var valueLabel = new Label(value);
            valueLabel.AddToClassList("metric-value");
            card.Add(valueLabel);
            var captionLabel = new Label(caption);
            captionLabel.AddToClassList("metric-caption");
            card.Add(captionLabel);
            return card;
        }

        public static VisualElement MakeDiagnosticRow(string name, string state)
        {
            var row = new VisualElement();
            row.AddToClassList("diagnostic-row");

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("diagnostic-name");
            row.Add(nameLabel);

            var stateLabel = new Label(state);
            stateLabel.AddToClassList("diagnostic-state");
            row.Add(stateLabel);

            return row;
        }

        public static VisualElement MakeMessages(string title, IReadOnlyList<string> messages)
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");
            panel.Add(MakeSectionTitle(title));
            if (messages == null || messages.Count == 0)
            {
                panel.Add(new Label("None"));
                return panel;
            }

            foreach (var message in messages)
            {
                panel.Add(new Label(message));
            }

            return panel;
        }

        public static Toggle MakeToggle(string label, bool value)
        {
            var toggle = new Toggle(label);
            toggle.value = value;
            return toggle;
        }

        public static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return new[] { "None" };
            }

            var rows = new string[values.Count];
            for (var index = 0; index < values.Count; index++)
            {
                rows[index] = values[index];
            }

            return rows;
        }
    }
}
