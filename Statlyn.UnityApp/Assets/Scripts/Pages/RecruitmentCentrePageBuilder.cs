using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Recruitment;
using Statlyn.Data.Shortlists;
using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class RecruitmentCentrePageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            BuildHeader(main, databasePath);

            var safety = new VisualElement();
            safety.AddToClassList("dashboard-grid");
            safety.AddToClassList("command-kpi-row");
            main.Add(safety);
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("Recruitment Safety", "Safe local data", "No raw provider snapshots or blocked raw values", CommandStatusCategory.Info));
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("FM26 Status", "Unsupported", "No live FM26 data", CommandStatusCategory.Warning));
            safety.Add(StatlynUiFactory.MakeCommandKpiCard("Database", "Runtime main", databasePath, CommandStatusCategory.Info));

            var filters = new VisualElement();
            filters.AddToClassList("data-source-form");
            main.Add(filters);

            var search = new TextField("Search");
            filters.Add(search);
            var source = new TextField("Source");
            filters.Add(source);
            var position = new TextField("Position");
            filters.Add(position);
            var minConfidence = new TextField("Minimum confidence");
            filters.Add(minConfidence);
            var minRoleFit = new TextField("Minimum role fit");
            filters.Add(minRoleFit);
            var sort = new DropdownField("Sort", new List<string> { "Role fit", "Confidence", "Data completeness", "Source", "Position" }, 0);
            filters.Add(sort);

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            filters.Add(actions);
            var refresh = new Button { text = "Refresh" };
            var reset = new Button { text = "Reset Filters" };
            actions.Add(refresh);
            actions.Add(reset);

            var results = new VisualElement();
            results.AddToClassList("data-source-results");
            main.Add(results);
            var profile = new VisualElement();
            profile.AddToClassList("data-source-results");
            main.Add(profile);

            RenderRecruitmentCentre(databasePath, BuildQuery(search, source, position, minConfidence, minRoleFit, sort), results, profile);
            refresh.clicked += () => RenderRecruitmentCentre(databasePath, BuildQuery(search, source, position, minConfidence, minRoleFit, sort), results, profile);
            reset.clicked += () =>
            {
                search.value = string.Empty;
                source.value = string.Empty;
                position.value = string.Empty;
                minConfidence.value = string.Empty;
                minRoleFit.value = string.Empty;
                sort.value = "Role fit";
                RenderRecruitmentCentre(databasePath, BuildQuery(search, source, position, minConfidence, minRoleFit, sort), results, profile);
            };
        }

        private static void BuildHeader(VisualElement main, string databasePath)
        {
            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                "Recruitment Centre",
                "Search, filter and shortlist persisted safe players",
                "No live FM26 data",
                CommandStatusCategory.Info));
        }

        private static RecruitmentCentreQuery BuildQuery(TextField search, TextField source, TextField position, TextField minConfidence, TextField minRoleFit, DropdownField sort)
        {
            return new RecruitmentCentreQuery
            {
                SearchText = search.value,
                SourceName = source.value,
                PrimaryPosition = position.value,
                MinimumConfidence = ParseInt(minConfidence.value),
                MinimumRoleFit = ParseInt(minRoleFit.value),
                SortBy = SortKey(sort.value),
                SortDirection = SortDirection(sort.value),
                Limit = 100
            };
        }

        private static void RenderRecruitmentCentre(string databasePath, RecruitmentCentreQuery query, VisualElement results, VisualElement profile)
        {
            results.Clear();
            profile.Clear();
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var service = new RecruitmentCentreQueryService(factory);
                    var viewModel = RecruitmentCentreViewModel.From(service.Query(query), query, databasePath);
                    RenderViewModel(databasePath, viewModel, results, profile);
                }
            }
            catch (Exception ex)
            {
                var cards = new VisualElement();
                cards.AddToClassList("dashboard-grid");
                results.Add(cards);
                cards.Add(StatlynUiFactory.MakeErrorCard("Recruitment Centre", "Could not load persisted players safely."));
                cards.Add(StatlynUiFactory.MakeErrorCard("Runtime Error", ex.GetType().Name + ": " + ex.Message, "Run Data Sources runtime check if SQLite fails."));
            }
        }

        private static void RenderViewModel(string databasePath, RecruitmentCentreViewModel viewModel, VisualElement results, VisualElement profile)
        {
            var summary = new VisualElement();
            summary.AddToClassList("dashboard-grid");
            summary.AddToClassList("command-kpi-row");
            results.Add(summary);
            summary.Add(StatlynUiFactory.MakeCommandKpiCard("Matched Players", viewModel.TotalCount.ToString(CultureInfo.InvariantCulture), viewModel.Players.Count.ToString(CultureInfo.InvariantCulture) + " shown", viewModel.TotalCount == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Success));
            summary.Add(StatlynUiFactory.MakeCommandKpiCard("Database", "Readable", viewModel.Diagnostics.DatabasePath, CommandStatusCategory.Info));
            summary.Add(StatlynUiFactory.MakeCommandKpiCard("Safety", "Guarded", "Persisted safe data only; no raw blocked values", CommandStatusCategory.Warning));
            summary.Add(StatlynUiFactory.MakeCommandPanel("Source List", viewModel.Sources.Count == 0 ? new[] { "None" } : StatlynUiFactory.ToArray(viewModel.Sources)));

            if (viewModel.Players.Count == 0)
            {
                results.Add(StatlynUiFactory.MakeCommandEmptyState("Recruitment Centre", "No imported players yet.", "Go to Data Sources and import a local CSV.", "No fake players are shown."));
                return;
            }

            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            results.Add(grid);
            foreach (var row in viewModel.Players)
            {
                grid.Add(MakePlayerCard(databasePath, row, profile));
            }
        }

        private static VisualElement MakePlayerCard(string databasePath, RecruitmentCentrePlayerRowViewModel row, VisualElement profile)
        {
            var visuals = RecruitmentCentreMiniVisualBuilder.Build(row);
            var card = new VisualElement();
            card.AddToClassList("glass-card");
            card.AddToClassList("command-panel");
            card.Add(StatlynUiFactory.MakeSectionTitle(row.Name));
            card.Add(new Label(row.Age + " | " + row.Nationality + " | " + row.Position));
            card.Add(new Label("Source: " + row.Source + " (" + row.SourceConfidence + " confidence)"));
            card.Add(new Label("Role: " + row.RoleName));
            card.Add(new Label("Tactical fit: " + row.TacticalFit));
            card.Add(StatlynBadgeRowComponent.Build(visuals.Badges));
            var membership = LoadMembershipLabel(databasePath, row.StatlynPlayerId);
            if (!string.IsNullOrWhiteSpace(membership))
            {
                var badge = new Label(membership);
                badge.AddToClassList("visual-badge");
                card.Add(badge);
            }

            card.Add(MakeMiniScore(visuals.RoleFitScore));
            card.Add(StatlynHorizontalBarComponent.Build(visuals.ConfidenceBar));
            card.Add(StatlynHorizontalBarComponent.Build(visuals.DataCompletenessBar));
            card.Add(MakeMiniRisk(visuals.RiskIndicator));
            card.Add(new Label("Benchmark: " + visuals.BenchmarkStatus.SafeMessage + " | " + visuals.BenchmarkStatus.MetricKey + (visuals.BenchmarkStatus.SampleSize.HasValue ? " | n=" + visuals.BenchmarkStatus.SampleSize.Value.ToString(CultureInfo.InvariantCulture) : string.Empty)));

            var outputList = new VisualElement();
            outputList.AddToClassList("visual-badge-row");
            foreach (var metric in visuals.OutputMiniList)
            {
                var label = new Label(metric.Label + ": " + metric.Value);
                label.AddToClassList("visual-badge");
                outputList.Add(label);
            }

            card.Add(outputList);
            card.Add(new Label(visuals.NoLiveDataLabel));

            var shortlistMessage = new Label(string.Empty);
            shortlistMessage.AddToClassList("card-row");
            var addToShortlist = new Button { text = "Add to Main Recruitment List" };
            addToShortlist.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new ShortlistWorkflowService(factory).AddFromRecruitmentCentreRow(
                            new ShortlistAddPlayerRequest
                            {
                                ShortlistName = ShortlistWorkflowService.DefaultShortlistName,
                                CreateShortlistIfMissing = true,
                                Status = ShortlistStatus.Watchlist,
                                Priority = ShortlistPriority.Medium,
                                FollowUpAction = ShortlistFollowUpAction.WatchMore,
                                AddedReason = "Added from Recruitment Centre safe row."
                            },
                            row);
                        shortlistMessage.text = result.SafeMessage;
                    }
                }
                catch (Exception ex)
                {
                    shortlistMessage.text = "Could not add to shortlist safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };
            card.Add(addToShortlist);
            card.Add(shortlistMessage);

            var open = new Button { text = "Open Profile" };
            open.clicked += () => RenderProfile(databasePath, row.StatlynPlayerId, profile);
            card.Add(open);
            return card;
        }

        private static VisualElement MakeMiniScore(StatlynScoreCardVisual visual)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-mini-row");
            var label = new Label(visual.Title + ": " + visual.Value);
            label.AddToClassList("visual-title");
            panel.Add(label);
            panel.Add(new Label(visual.Caption));
            return panel;
        }

        private static VisualElement MakeMiniRisk(StatlynWarningVisual visual)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-mini-row");
            var label = new Label(visual.Message);
            label.AddToClassList("visual-warning-text");
            panel.Add(label);
            foreach (var row in visual.Rows)
            {
                panel.Add(new Label(row));
            }

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
                        return string.Empty;
                    }

                    return "Shortlisted: " + memberships[0].Status + " (" + memberships.Count.ToString(CultureInfo.InvariantCulture) + ")";
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void RenderProfile(string databasePath, string statlynPlayerId, VisualElement profile)
        {
            profile.Clear();
            try
            {
                PlayerProfilePageBuilder.RenderProfileReport(databasePath, statlynPlayerId, profile);
            }
            catch (Exception ex)
            {
                profile.Add(StatlynUiFactory.MakeCard("Profile Preview", new[] { "Could not open profile.", ex.GetType().Name + ": " + ex.Message }));
            }
        }

        private static int? ParseInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : (int?)null;
        }

        private static string SortKey(string label)
        {
            switch (label ?? string.Empty)
            {
                case "Confidence":
                    return "Confidence";
                case "Data completeness":
                    return "DataCompleteness";
                case "Source":
                    return "Source";
                case "Position":
                    return "Position";
                default:
                    return "RoleFit";
            }
        }

        private static string SortDirection(string label)
        {
            return string.Equals(label, "Source", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(label, "Position", StringComparison.OrdinalIgnoreCase)
                ? "Ascending"
                : "Descending";
        }
    }
}
