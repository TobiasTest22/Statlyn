using Statlyn.UI.ProfileFixtures;
using Statlyn.UI.UnityBridge;
using Statlyn.UnityApp.Components;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class DashboardPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var header = new VisualElement();
            header.AddToClassList("header");
            main.Add(header);

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            header.Add(titleStack);

            var title = new Label("Recruitment Intelligence");
            title.AddToClassList("screen-title");
            titleStack.Add(title);

            var subtitle = new Label("Fixture mode preview - no live FM26 data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Scouting firewall active");
            status.AddToClassList("status-pill");
            header.Add(status);

            var dashboard = new VisualElement();
            dashboard.AddToClassList("dashboard-grid");
            main.Add(dashboard);

            dashboard.Add(StatlynUiFactory.MakeCard("Active Source", new[] { "Mode: Fixture preview", "Status: No live FM26 data", "Players: 1 synthetic preview" }));
            dashboard.Add(StatlynUiFactory.MakeCard("Connection", new[] { "FM26 process: Not checked", "Build support: Unsupported until mapped", "Snapshot: Not loaded" }));
            dashboard.Add(StatlynUiFactory.MakeCard("Recruitment", new[] { "Squad needs: Awaiting data", "Targets: Awaiting data", "Alerts: 0" }));
            dashboard.Add(StatlynUiFactory.MakeCard("Protection", new[] { "Hidden values blocked", "Raw entities blocked from UI", "Low confidence requires scouting" }));

            main.Add(MakePlayerProfileSlice(UnityProfileRenderModel.From(FixtureProfileFactory.CreateDevelopmentPreviewProfile())));
            main.Add(DiagnosticsPanelBuilder.BuildAdvancedDiagnostics());
        }

        private static VisualElement MakePlayerProfileSlice(UnityProfileRenderModel model)
        {
            var profile = new VisualElement();
            profile.AddToClassList("profile-slice");

            var header = new VisualElement();
            header.AddToClassList("profile-header");
            profile.Add(header);

            var avatar = new VisualElement();
            avatar.AddToClassList("profile-avatar");
            var initials = new Label(model.Initials);
            initials.AddToClassList("profile-avatar-text");
            avatar.Add(initials);
            header.Add(avatar);

            var identity = new VisualElement();
            identity.AddToClassList("profile-identity");
            header.Add(identity);

            var titleRow = new VisualElement();
            titleRow.AddToClassList("profile-title-row");
            identity.Add(titleRow);

            var name = new Label(model.PlayerName);
            name.AddToClassList("profile-name");
            titleRow.Add(name);

            var mode = new Label(model.IsFixtureMode ? "Fixture Mode" : model.SourceName);
            mode.AddToClassList("fixture-pill");
            titleRow.Add(mode);

            var detail = new Label(model.DetailLine);
            detail.AddToClassList("profile-detail");
            identity.Add(detail);

            var flag = new Label(model.FlagLine);
            flag.AddToClassList("profile-flag");
            identity.Add(flag);

            var summaryGrid = new VisualElement();
            summaryGrid.AddToClassList("profile-summary-grid");
            profile.Add(summaryGrid);

            summaryGrid.Add(StatlynUiFactory.MakeMetricCard("Source Confidence", model.SourceConfidence.ToString(), model.SourceName));
            summaryGrid.Add(StatlynUiFactory.MakeMetricCard("Data Completeness", model.DataCompleteness.ToString(), model.DataCompletenessCaption));
            summaryGrid.Add(StatlynUiFactory.MakeMetricCard("Role Fit", model.RoleFit, "Role fit visual placeholder"));
            summaryGrid.Add(StatlynUiFactory.MakeMetricCard("Confidence", model.Confidence, model.ConfidenceCaption));
            summaryGrid.Add(StatlynUiFactory.MakeMetricCard("Risk", model.Risk, model.RiskCaption));

            var visualGrid = new VisualElement();
            visualGrid.AddToClassList("profile-visual-grid");
            profile.Add(visualGrid);

            var radar = new VisualElement();
            radar.AddToClassList("radar-card");
            radar.Add(StatlynUiFactory.MakeSectionTitle("Radar Chart"));
            foreach (var metric in model.RadarMetrics)
            {
                radar.Add(new Label(metric.Label + ": " + metric.Value + " / " + metric.MaximumValue + " (" + metric.Confidence + "% confidence)"));
            }

            radar.AddToClassList("placeholder-text");
            visualGrid.Add(radar);

            var bars = new VisualElement();
            bars.AddToClassList("percentile-card");
            bars.Add(StatlynUiFactory.MakeSectionTitle("Percentile Bars"));
            foreach (var bar in model.PercentileBars)
            {
                bars.Add(MakePercentileBar(bar.Label + " vs " + bar.ComparisonGroup, bar.Percentile));
            }

            visualGrid.Add(bars);

            var evidence = new VisualElement();
            evidence.AddToClassList("evidence-grid");
            profile.Add(evidence);
            foreach (var card in model.EvidenceCards)
            {
                evidence.Add(MakeEvidenceCard(card.Title, card.Body));
            }

            var warning = new VisualElement();
            warning.AddToClassList("missing-warning");
            warning.Add(new Label("Missing Data"));
            warning.Add(new Label(model.MissingDataMessage));
            warning.Add(new Label(model.BlockedDataMessage));
            profile.Add(warning);

            return profile;
        }

        private static VisualElement MakePercentileBar(string label, int value)
        {
            var row = new VisualElement();
            row.AddToClassList("percentile-row");
            row.Add(new Label(label));
            var track = new VisualElement();
            track.AddToClassList("percentile-track");
            var fill = new VisualElement();
            fill.AddToClassList("percentile-fill");
            fill.style.width = Length.Percent(value);
            track.Add(fill);
            row.Add(track);
            return row;
        }

        private static VisualElement MakeEvidenceCard(string title, string copy)
        {
            var card = new VisualElement();
            card.AddToClassList("evidence-card");
            card.Add(StatlynUiFactory.MakeSectionTitle(title));
            var body = new Label(copy);
            body.AddToClassList("evidence-copy");
            card.Add(body);
            return card;
        }
    }
}
