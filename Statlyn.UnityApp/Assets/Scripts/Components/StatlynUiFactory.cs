using System.Collections.Generic;
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

        public static VisualElement MakeBrandLockup()
        {
            var lockup = new VisualElement();
            lockup.AddToClassList("brand-lockup");

            var logo = MakeLogoImage(LightLogoResourceKey, "brand-logo");
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
