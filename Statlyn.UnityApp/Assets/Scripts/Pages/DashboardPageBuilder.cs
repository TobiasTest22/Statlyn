using Statlyn.UI;
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
            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                "Recruitment Intelligence",
                "Command-center baseline for local, masked recruitment analysis",
                "Scouting firewall active",
                CommandStatusCategory.Success));

            var dashboard = new VisualElement();
            dashboard.AddToClassList("dashboard-grid");
            dashboard.AddToClassList("command-kpi-row");
            main.Add(dashboard);

            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Data Mode", "Fixture preview", "Synthetic development profile only", CommandStatusCategory.Warning));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Source Integrity", "No live FM26 data", "CSV import remains manual/local", CommandStatusCategory.Accent));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("FM26 Support", "Unsupported", "No validated memory map or live binding", CommandStatusCategory.Warning));
            dashboard.Add(StatlynUiFactory.MakeCommandKpiCard("Protection", "Firewall active", "Raw provider values stay out of UI", CommandStatusCategory.Success));

            main.Add(StatlynUiFactory.MakeCommandWarningBanner("Safety Baseline", new[]
            {
                "Home may show one synthetic development profile as a labelled fixture preview.",
                "No live FM26 data, fake benchmark values or external API sources are available.",
                "Open Data Sources for local CSV import or Diagnostics for runtime checks."
            }));

            main.Add(MakePlayerProfileSlice(UnityProfileRenderModel.From(FixtureProfileFactory.CreateDevelopmentPreviewProfile())));
            main.Add(DiagnosticsPanelBuilder.BuildAdvancedDiagnostics());
        }

        private static VisualElement MakePlayerProfileSlice(UnityProfileRenderModel model)
        {
            var profile = new VisualElement();
            profile.AddToClassList("profile-slice");
            profile.AddToClassList("command-panel");

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

            var evidencePanel = new VisualElement();
            evidencePanel.AddToClassList("visual-panel");
            evidencePanel.Add(StatlynUiFactory.MakeSectionTitle("Masked Evidence"));
            foreach (var metric in model.RadarMetrics)
            {
                evidencePanel.Add(new Label(metric.Label + ": " + metric.Value + " / " + metric.MaximumValue + " (" + metric.Confidence + "% confidence)"));
            }

            visualGrid.Add(evidencePanel);

            var benchmark = new VisualElement();
            benchmark.AddToClassList("visual-panel");
            benchmark.Add(StatlynUiFactory.MakeSectionTitle("Benchmark Status"));
            benchmark.Add(new Label("No benchmark yet."));
            foreach (var bar in model.PercentileBars)
            {
                benchmark.Add(new Label(bar.Label + ": " + bar.ComparisonGroup));
            }

            visualGrid.Add(benchmark);

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
