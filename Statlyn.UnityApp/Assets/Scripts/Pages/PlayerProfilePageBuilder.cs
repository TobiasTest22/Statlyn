using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class PlayerProfilePageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = Path.Combine(Application.persistentDataPath, "statlyn.db");
            BuildHeader(main);

            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            main.Add(form);

            var statlynPlayerId = new TextField("StatlynPlayerId");
            form.Add(statlynPlayerId);
            var helper = new TextField("Name/source helper");
            helper.SetEnabled(false);
            helper.value = "Use a Recruitment Centre row or load the first imported player.";
            form.Add(helper);

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            form.Add(actions);
            var load = new Button { text = "Load Profile" };
            var loadFirst = new Button { text = "Load First Imported Player" };
            actions.Add(load);
            actions.Add(loadFirst);

            var results = new VisualElement();
            results.AddToClassList("data-source-results");
            main.Add(results);
            RenderEmpty(results);

            load.clicked += () => RenderProfileReport(databasePath, statlynPlayerId.value, results);
            loadFirst.clicked += () =>
            {
                var first = FindFirstImportedPlayerId(databasePath);
                statlynPlayerId.value = first;
                RenderProfileReport(databasePath, first, results);
            };
        }

        public static void RenderProfileReport(string databasePath, string statlynPlayerId, VisualElement target)
        {
            target.Clear();
            if (string.IsNullOrWhiteSpace(statlynPlayerId))
            {
                RenderEmpty(target);
                return;
            }

            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var result = new PlayerProfileQueryService(factory).Query(new PlayerProfileQuery { StatlynPlayerId = statlynPlayerId });
                    if (!result.Success)
                    {
                        target.Add(StatlynUiFactory.MakeCard("Player Profile", new[] { result.SafeMessage, "No fake player is shown." }));
                        return;
                    }

                    RenderReport(PlayerProfileReportViewModel.From(result), target);
                }
            }
            catch (Exception ex)
            {
                target.Add(StatlynUiFactory.MakeCard("Player Profile", new[] { "Could not load persisted profile safely.", ex.GetType().Name + ": " + ex.Message }));
            }
        }

        private static void BuildHeader(VisualElement main)
        {
            var header = new VisualElement();
            header.AddToClassList("header");
            main.Add(header);

            var headerBrand = new VisualElement();
            headerBrand.AddToClassList("header-brand");
            header.Add(headerBrand);
            var logo = StatlynUiFactory.MakeLogoImage(StatlynUiFactory.LightLogoResourceKey, "header-logo");
            if (logo != null)
            {
                headerBrand.Add(logo);
            }

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            headerBrand.Add(titleStack);
            var title = new Label("Player Profile");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("Persisted safe data only - no live FM26 data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Output-first report");
            status.AddToClassList("status-pill");
            header.Add(status);
        }

        private static void RenderEmpty(VisualElement target)
        {
            target.Clear();
            var cards = new VisualElement();
            cards.AddToClassList("dashboard-grid");
            target.Add(cards);
            cards.Add(StatlynUiFactory.MakeCard("No Profile Loaded", new[] { "Import a CSV in Data Sources or open a player from Recruitment Centre." }));
            cards.Add(StatlynUiFactory.MakeCard("Safety", new[] { "Persisted safe data only", "No raw blocked values", "No fake live FM26 data" }));
        }

        private static void RenderReport(PlayerProfileReportViewModel report, VisualElement target)
        {
            var identity = new VisualElement();
            identity.AddToClassList("dashboard-grid");
            target.Add(identity);
            identity.Add(StatlynUiFactory.MakeCard(report.PlayerName, new[]
            {
                report.Age + " | " + report.Nationality + " | " + report.PrimaryPosition,
                "Statlyn ID: " + report.StatlynPlayerId,
                "Position group: " + report.PositionGroup
            }));
            identity.Add(StatlynUiFactory.MakeCard("Source", new[]
            {
                report.SourceName,
                "Confidence: " + report.SourceConfidence,
                report.IsFixtureMode ? "Fixture/import mode" : "Persisted source",
                report.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data"
            }));
            identity.Add(StatlynUiFactory.MakeCard("Verdict", new[]
            {
                "Recommendation: " + report.Recommendation,
                "Role fit: " + report.RoleFit,
                "Confidence: " + report.Confidence,
                "Risk: " + report.Risk
            }));
            identity.Add(StatlynUiFactory.MakeCard("Role Output", new[]
            {
                "Role: " + report.RoleName,
                "Output fit: " + report.OutputFitLabel,
                "Tactical fit: " + report.TacticalFitDisplay
            }));

            target.Add(MakeMetricSection("Core Role Output", report.CoreOutputMetrics, "Output metrics missing"));
            target.Add(MakeMetricSection("Supporting Output", report.SupportingOutputMetrics, "No supporting output available yet"));
            target.Add(MakeMetricSection("Physical Output", report.PhysicalOutputMetrics, "No physical output available yet"));
            target.Add(StatlynUiFactory.MakeMessages("Missing Output", report.MissingOutputMetrics.Count == 0 ? new[] { "No core output missing." } : report.MissingOutputMetrics.ToArray()));
            target.Add(MakeDataQuality(report));
            target.Add(MakeAttributeSupport(report));
            target.Add(MakeEvidence(report));
            target.Add(MakeScoutActions(report));
            target.Add(StatlynUiFactory.MakeCard("Blocked Data Safe Notice", new[]
            {
                report.BlockedDataNotice.SafeMessage,
                "Count: " + report.BlockedDataNotice.Count.ToString(CultureInfo.InvariantCulture),
                "Categories: " + (report.BlockedDataNotice.Categories.Count == 0 ? "None" : string.Join(", ", report.BlockedDataNotice.Categories))
            }));
            target.Add(MakeVisualSections(report));
        }

        private static VisualElement MakeMetricSection(string title, System.Collections.Generic.IReadOnlyList<PlayerProfileMetricTileViewModel> metrics, string emptyText)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            if (metrics == null || metrics.Count == 0)
            {
                grid.Add(StatlynUiFactory.MakeCard(title, new[] { emptyText, "Missing output is not treated as zero." }));
                return grid;
            }

            foreach (var metric in metrics)
            {
                grid.Add(StatlynUiFactory.MakeCard(metric.Label, new[]
                {
                    metric.Value,
                    metric.Section,
                    "Source: " + metric.Source,
                    "Confidence: " + metric.Confidence,
                    "Sample: " + metric.Sample,
                    metric.VerificationLabel
                }));
            }

            return grid;
        }

        private static VisualElement MakeDataQuality(PlayerProfileReportViewModel report)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            foreach (var item in report.DataQualityCards)
            {
                grid.Add(StatlynUiFactory.MakeCard(item.Label, new[] { item.Value, item.Caption }));
            }

            foreach (var warning in report.KeyWarnings.Take(3))
            {
                grid.Add(StatlynUiFactory.MakeCard("Warning", new[] { warning }));
            }

            return grid;
        }

        private static VisualElement MakeAttributeSupport(PlayerProfileReportViewModel report)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            if (report.AttributeSupportCards.Count == 0)
            {
                grid.Add(StatlynUiFactory.MakeCard("Attribute Support", new[] { "No attribute support available.", "Attributes are support-only." }));
                return grid;
            }

            foreach (var item in report.AttributeSupportCards)
            {
                grid.Add(StatlynUiFactory.MakeCard(item.Label, new[] { item.Value, "Confidence: " + item.Confidence, item.Caption }));
            }

            return grid;
        }

        private static VisualElement MakeEvidence(PlayerProfileReportViewModel report)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            foreach (var item in report.EvidenceCards)
            {
                grid.Add(StatlynUiFactory.MakeCard(item.Title, new[] { item.Category, item.Body, "Source: " + item.Source, "Confidence: " + item.Confidence }));
            }

            return grid;
        }

        private static VisualElement MakeScoutActions(PlayerProfileReportViewModel report)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            foreach (var action in report.ScoutActionCards)
            {
                grid.Add(StatlynUiFactory.MakeCard(action.Title, new[] { action.Reason, action.Action }));
            }

            return grid;
        }

        private static VisualElement MakeVisualSections(PlayerProfileReportViewModel report)
        {
            var panel = new VisualElement();
            panel.AddToClassList("diagnostics-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Visual Sections"));
            foreach (var section in report.VisualSections)
            {
                panel.Add(StatlynUiFactory.MakeDiagnosticRow(section.Title, section.Summary));
                foreach (var row in section.Rows.Take(4))
                {
                    panel.Add(new Label(row));
                }
            }

            return panel;
        }

        private static string FindFirstImportedPlayerId(string databasePath)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var result = new RecruitmentCentreQueryService(factory).Query(new RecruitmentCentreQuery { SortBy = "DisplayName", SortDirection = "Ascending", Limit = 1 });
                    return result.Players.Count == 0 ? string.Empty : result.Players[0].StatlynPlayerId;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
