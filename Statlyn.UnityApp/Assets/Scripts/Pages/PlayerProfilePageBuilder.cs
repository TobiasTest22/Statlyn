using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Profile;
using Statlyn.Data.Recruitment;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
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

                    RenderReport(databasePath, PlayerProfileReportViewModel.From(result), target);
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

        private static void RenderReport(string databasePath, PlayerProfileReportViewModel report, VisualElement target)
        {
            var visuals = StatlynVisualAnalyticsBuilder.Build(report);

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

            target.Add(MakeShortlistPanel(databasePath, report));
            target.Add(MakeScoutReportPanel(databasePath, report));
            target.Add(MakeScoreCards(visuals));
            target.Add(MakeRoleOutput(visuals.RoleOutput));

            foreach (var group in visuals.MetricGroups)
            {
                target.Add(StatlynMetricGroupComponent.Build(group));
            }

            target.Add(MakeDataQuality(visuals));
            target.Add(MakeMissingData(visuals));
            target.Add(MakeWarningList(visuals));
            target.Add(MakeEvidenceSection("Evidence", visuals.Evidence));
            target.Add(MakeEvidenceSection("Scout Actions", visuals.ScoutActions));
            target.Add(MakeAttributeSupport(visuals));
            target.Add(StatlynBlockedDataComponent.Build(visuals.BlockedData));
            target.Add(StatlynBenchmarkStatusComponent.Build(visuals.BenchmarkStatus));
        }

        private static VisualElement MakeScoreCards(StatlynVisualAnalyticsViewModel visuals)
        {
            var grid = new VisualElement();
            grid.AddToClassList("visual-score-grid");
            foreach (var score in visuals.ScoreCards)
            {
                grid.Add(StatlynScoreCardComponent.Build(score));
            }

            return grid;
        }

        private static VisualElement MakeRoleOutput(StatlynRoleOutputVisual visual)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Role / Output"));
            panel.Add(new Label("Role: " + visual.RoleName));
            panel.Add(new Label("Output fit: " + visual.OutputFitLabel));
            panel.Add(new Label("Tactical fit: " + visual.TacticalFitDisplay));
            foreach (var bar in visual.Bars)
            {
                panel.Add(StatlynHorizontalBarComponent.Build(bar));
            }

            return panel;
        }

        private static VisualElement MakeScoutReportPanel(string databasePath, PlayerProfileReportViewModel report)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Latest Scout Report"));
            panel.Add(new Label(LoadScoutReportLabel(databasePath, report.StatlynPlayerId)));
            panel.Add(new Label("Data and scout judgement can disagree; scout reports do not override outputs automatically."));

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");
            var create = new Button { text = "Create Scout Assignment" };
            create.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new ScoutDeskWorkflowService(factory).CreateAssignment(new CreateScoutAssignmentRequest
                        {
                            StatlynPlayerId = report.StatlynPlayerId,
                            RoleName = report.RoleName,
                            Priority = ShortlistPriority.Medium,
                            AssignmentTitle = "Scout " + report.RoleName
                        });
                        message.text = result.SafeMessage;
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not create scout assignment safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            panel.Add(create);
            panel.Add(message);
            return panel;
        }

        private static VisualElement MakeDataQuality(StatlynVisualAnalyticsViewModel visuals)
        {
            var grid = new VisualElement();
            grid.AddToClassList("visual-data-grid");
            foreach (var item in visuals.DataQuality)
            {
                grid.Add(StatlynDataQualityComponent.Build(item));
            }

            return grid;
        }

        private static VisualElement MakeMissingData(StatlynVisualAnalyticsViewModel visuals)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Missing Data"));
            foreach (var item in visuals.MissingData)
            {
                panel.Add(StatlynMissingDataComponent.Build(item));
            }

            return panel;
        }

        private static VisualElement MakeWarningList(StatlynVisualAnalyticsViewModel visuals)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Warnings"));
            if (visuals.Warnings.Count == 0)
            {
                panel.Add(new Label("No additional warnings."));
                return panel;
            }

            foreach (var warning in visuals.Warnings.Take(4))
            {
                panel.Add(StatlynWarningPanelComponent.Build(warning));
            }

            return panel;
        }

        private static VisualElement MakeAttributeSupport(StatlynVisualAnalyticsViewModel visuals)
        {
            var section = new VisualElement();
            section.AddToClassList("visual-section");
            section.Add(StatlynUiFactory.MakeSectionTitle("Attribute Support"));
            section.Add(new Label("Supporting evidence only; output metrics remain primary."));

            var grid = new VisualElement();
            grid.AddToClassList("visual-metric-grid");
            section.Add(grid);

            if (visuals.AttributeSupport.Count == 0)
            {
                grid.Add(StatlynMetricTileComponent.Build(new StatlynMetricTileVisual(
                    "Attribute Support",
                    "Unavailable",
                    "Attribute Support",
                    "Masked profile",
                    "n/a",
                    "No attributes available",
                    "Supporting evidence only",
                    false,
                    false)));
                return section;
            }

            foreach (var item in visuals.AttributeSupport)
            {
                grid.Add(StatlynMetricTileComponent.Build(item));
            }

            return section;
        }

        private static VisualElement MakeShortlistPanel(string databasePath, PlayerProfileReportViewModel report)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Shortlist"));
            panel.Add(new Label(LoadMembershipLabel(databasePath, report.StatlynPlayerId)));

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");
            var add = new Button { text = "Add to Main Recruitment List" };
            add.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new ShortlistWorkflowService(factory).AddToShortlist(new ShortlistAddPlayerRequest
                        {
                            ShortlistName = ShortlistWorkflowService.DefaultShortlistName,
                            CreateShortlistIfMissing = true,
                            StatlynPlayerId = report.StatlynPlayerId,
                            AddedReason = "Added from Player Profile safe report."
                        });
                        message.text = result.SafeMessage;
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not add to shortlist safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            panel.Add(add);
            panel.Add(message);
            return panel;
        }

        private static string LoadMembershipLabel(string databasePath, string statlynPlayerId)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var memberships = new ShortlistWorkflowService(factory).LoadMembershipsForPlayer(statlynPlayerId);
                    if (memberships.Count == 0)
                    {
                        return "Not shortlisted yet.";
                    }

                    return "Shortlisted: " + memberships[0].Status + " | Priority: " + memberships[0].Priority + " | Follow-up: " + memberships[0].FollowUpAction;
                }
            }
            catch
            {
                return "Shortlist status unavailable.";
            }
        }

        private static string LoadScoutReportLabel(string databasePath, string statlynPlayerId)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var summary = new ScoutDeskWorkflowService(factory).BuildLatestReportSummary(statlynPlayerId);
                    if (!summary.HasReport)
                    {
                        return summary.Summary;
                    }

                    return summary.Recommendation + " | Confidence: " + summary.Confidence + " | " + summary.ReportDate + " | " + summary.Summary;
                }
            }
            catch
            {
                return "Scout report unavailable.";
            }
        }

        private static VisualElement MakeEvidenceSection(string title, System.Collections.Generic.IReadOnlyList<StatlynEvidenceVisual> visuals)
        {
            var section = new VisualElement();
            section.AddToClassList("visual-section");
            section.Add(StatlynUiFactory.MakeSectionTitle(title));

            var grid = new VisualElement();
            grid.AddToClassList("visual-evidence-grid");
            section.Add(grid);

            foreach (var item in visuals)
            {
                grid.Add(StatlynEvidenceCardComponent.Build(item));
            }

            return section;
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
