using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Recruitment;
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
            var databasePath = Path.Combine(Application.persistentDataPath, "statlyn.db");
            BuildHeader(main, databasePath);

            var safety = new VisualElement();
            safety.AddToClassList("dashboard-grid");
            main.Add(safety);
            safety.Add(StatlynUiFactory.MakeCard("Recruitment Safety", new[] { "Persisted safe data only", "No raw provider snapshots", "No blocked raw values" }));
            safety.Add(StatlynUiFactory.MakeCard("FM26 Status", new[] { "No live FM26 data", "Unsupported until validated memory maps exist" }));
            safety.Add(StatlynUiFactory.MakeCard("Database Status", new[] { "Path: " + databasePath, "SQLite initialized on demand" }));

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
            var header = new VisualElement();
            header.AddToClassList("header");
            main.Add(header);
            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            header.Add(titleStack);
            var title = new Label("Recruitment Centre");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("Persisted safe data only - no live FM26 data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);
            var status = new Label("No live FM26 data");
            status.AddToClassList("status-pill");
            header.Add(status);
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
                cards.Add(StatlynUiFactory.MakeCard("Recruitment Centre", new[] { "Could not load persisted players." }));
                cards.Add(StatlynUiFactory.MakeCard("Runtime Error", new[] { ex.GetType().Name + ": " + ex.Message, "Run Data Sources runtime check if SQLite fails." }));
            }
        }

        private static void RenderViewModel(string databasePath, RecruitmentCentreViewModel viewModel, VisualElement results, VisualElement profile)
        {
            var summary = new VisualElement();
            summary.AddToClassList("dashboard-grid");
            results.Add(summary);
            summary.Add(StatlynUiFactory.MakeCard("Player Count", new[] { viewModel.TotalCount.ToString(CultureInfo.InvariantCulture) + " matched", viewModel.Players.Count.ToString(CultureInfo.InvariantCulture) + " shown" }));
            summary.Add(StatlynUiFactory.MakeCard("Database", new[] { "Path: " + viewModel.Diagnostics.DatabasePath, "Status: readable persisted store" }));
            summary.Add(StatlynUiFactory.MakeCard("Safety", new[] { "Persisted safe data only", "No raw blocked values", "No live FM26 data" }));
            summary.Add(StatlynUiFactory.MakeCard("Source List", viewModel.Sources.Count == 0 ? new[] { "None" } : StatlynUiFactory.ToArray(viewModel.Sources)));

            if (viewModel.Players.Count == 0)
            {
                results.Add(StatlynUiFactory.MakeCard("Empty State", new[] { "No imported players yet.", "Go to Data Sources and import a local CSV.", "No fake players are shown." }));
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
            card.Add(StatlynUiFactory.MakeSectionTitle(row.Name));
            card.Add(new Label(row.Age + " | " + row.Nationality + " | " + row.Position));
            card.Add(new Label("Source: " + row.Source + " (" + row.SourceConfidence + " confidence)"));
            card.Add(new Label("Role: " + row.RoleName));
            card.Add(new Label("Tactical fit: " + row.TacticalFit));
            card.Add(StatlynBadgeRowComponent.Build(visuals.Badges));
            card.Add(MakeMiniScore(visuals.RoleFitScore));
            card.Add(StatlynHorizontalBarComponent.Build(visuals.ConfidenceBar));
            card.Add(StatlynHorizontalBarComponent.Build(visuals.DataCompletenessBar));
            card.Add(MakeMiniRisk(visuals.RiskIndicator));

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
