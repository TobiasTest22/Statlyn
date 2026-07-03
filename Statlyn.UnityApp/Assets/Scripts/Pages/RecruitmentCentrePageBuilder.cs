using System;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Recruitment;
using Statlyn.UI;
using Statlyn.UI.UnityBridge;
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

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            filters.Add(actions);
            var refresh = new Button { text = "Refresh" };
            actions.Add(refresh);

            var results = new VisualElement();
            results.AddToClassList("data-source-results");
            main.Add(results);
            var profile = new VisualElement();
            profile.AddToClassList("data-source-results");
            main.Add(profile);

            RenderRecruitmentCentre(databasePath, BuildQuery(search, source, position, minConfidence, minRoleFit), results, profile);
            refresh.clicked += () => RenderRecruitmentCentre(databasePath, BuildQuery(search, source, position, minConfidence, minRoleFit), results, profile);
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
            var status = new Label("Database: " + databasePath);
            status.AddToClassList("status-pill");
            header.Add(status);
        }

        private static RecruitmentCentreQuery BuildQuery(TextField search, TextField source, TextField position, TextField minConfidence, TextField minRoleFit)
        {
            return new RecruitmentCentreQuery
            {
                SearchText = search.value,
                SourceName = source.value,
                PrimaryPosition = position.value,
                MinimumConfidence = ParseInt(minConfidence.value),
                MinimumRoleFit = ParseInt(minRoleFit.value),
                SortBy = "RoleFit",
                SortDirection = "Descending",
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
            summary.Add(StatlynUiFactory.MakeCard("Players", new[] { viewModel.TotalCount.ToString(CultureInfo.InvariantCulture) + " matched", viewModel.Players.Count.ToString(CultureInfo.InvariantCulture) + " shown" }));
            summary.Add(StatlynUiFactory.MakeCard("Database", new[] { viewModel.Diagnostics.DatabasePath }));
            summary.Add(StatlynUiFactory.MakeCard("Safety", new[] { "Persisted safe data only", "No raw blocked values", "No live FM26 data" }));
            summary.Add(StatlynUiFactory.MakeCard("Sources", viewModel.Sources.Count == 0 ? new[] { "None" } : StatlynUiFactory.ToArray(viewModel.Sources)));

            if (viewModel.Players.Count == 0)
            {
                results.Add(StatlynUiFactory.MakeCard("Empty State", new[] { "No imported players yet. Go to Data Sources and import a local CSV." }));
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
            var card = new VisualElement();
            card.AddToClassList("glass-card");
            card.Add(StatlynUiFactory.MakeSectionTitle(row.Name));
            card.Add(new Label(row.Age + " | " + row.Nationality + " | " + row.Position));
            card.Add(new Label("Source: " + row.Source + " (" + row.SourceConfidence + " confidence)"));
            card.Add(new Label("Completeness: " + row.DataCompleteness));
            card.Add(new Label("Role fit: " + row.RoleFit + " | Confidence: " + row.Confidence));
            card.Add(new Label("Recommendation: " + row.Recommendation + " | Risk: " + row.Risk));
            card.Add(new Label("Output: " + (row.KeyOutputMetrics.Count == 0 ? "Missing core output" : string.Join(", ", row.KeyOutputMetrics))));
            card.Add(new Label("Blocked fields: " + row.BlockedFieldCount.ToString(CultureInfo.InvariantCulture) + " | Missing data: " + row.MissingDataCount.ToString(CultureInfo.InvariantCulture)));
            if (row.Warnings.Count > 0)
            {
                card.Add(new Label("Warning: " + row.Warnings[0]));
            }

            var open = new Button { text = "Open Profile" };
            open.clicked += () => RenderProfile(databasePath, row.StatlynPlayerId, profile);
            card.Add(open);
            return card;
        }

        private static void RenderProfile(string databasePath, string statlynPlayerId, VisualElement profile)
        {
            profile.Clear();
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var model = new RecruitmentCentreProfilePreviewService(factory).LoadProfile(statlynPlayerId);
                    if (model == null)
                    {
                        profile.Add(StatlynUiFactory.MakeCard("Profile Preview", new[] { "Persisted player could not be loaded safely." }));
                        return;
                    }

                    var unityModel = UnityProfileRenderModel.From(model);
                    profile.Add(StatlynUiFactory.MakeCard("Profile Preview", new[] { unityModel.PlayerName, unityModel.DetailLine, unityModel.SourceName, unityModel.IsFixtureMode ? "Fixture/import mode" : "Persisted source", unityModel.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data" }));
                    profile.Add(StatlynUiFactory.MakeMessages("Profile Evidence", new[] { unityModel.MissingDataMessage, unityModel.BlockedDataMessage }));
                }
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
    }
}
