using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Benchmarks;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class BenchmarksPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = Path.Combine(Application.persistentDataPath, "statlyn.db");
            BuildHeader(main);

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            main.Add(actions);
            var seed = new Button { text = "Seed Benchmark Definitions" };
            var run = new Button { text = "Run Benchmark Definitions" };
            var refresh = new Button { text = "Refresh" };
            actions.Add(seed);
            actions.Add(run);
            actions.Add(refresh);
            main.Add(message);

            var list = new VisualElement();
            list.AddToClassList("data-source-results");
            main.Add(list);
            RenderBenchmarks(databasePath, list, message);

            seed.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new BenchmarkWorkflowService(factory).SeedDefaultDefinitions();
                        message.text = result.SafeMessage + " Definitions: " + result.ActiveSeedDefinitionCount.ToString(CultureInfo.InvariantCulture);
                    }

                    RenderBenchmarks(databasePath, list, message);
                }
                catch (Exception ex)
                {
                    message.text = "Could not seed benchmarks safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            run.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new BenchmarkWorkflowService(factory).RunAllActiveDefinitions();
                        message.text = result.SafeMessage + " Runs: " + result.DefinitionsRun.ToString(CultureInfo.InvariantCulture);
                    }

                    RenderBenchmarks(databasePath, list, message);
                }
                catch (Exception ex)
                {
                    message.text = "Could not run benchmarks safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            refresh.clicked += () => RenderBenchmarks(databasePath, list, message);
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
            var title = new Label("Benchmarks");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("Generic/import benchmarks - no fake FM26 verification");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Aggregate snapshots only");
            status.AddToClassList("status-pill");
            header.Add(status);
        }

        private static void RenderBenchmarks(string databasePath, VisualElement list, Label message)
        {
            list.Clear();
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var page = new BenchmarkWorkflowService(factory).BuildPageViewModel();
                    message.text = string.IsNullOrWhiteSpace(message.text) ? page.SafeMessage : message.text;
                    if (page.Definitions.Count == 0)
                    {
                        list.Add(StatlynUiFactory.MakeCard("No benchmark definitions yet.", new[] { "Seed definitions to create generic/import benchmark templates.", "No fake results are shown." }));
                        return;
                    }

                    var grid = new VisualElement();
                    grid.AddToClassList("dashboard-grid");
                    list.Add(grid);
                    foreach (var definition in page.Definitions)
                    {
                        grid.Add(MakeDefinitionCard(definition));
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(StatlynUiFactory.MakeCard("Benchmarks", new[] { "Could not load benchmarks safely.", ex.GetType().Name + ": " + ex.Message }));
            }
        }

        private static VisualElement MakeDefinitionCard(BenchmarkDefinitionCardViewModel definition)
        {
            var rows = new[]
            {
                "Scope: " + definition.Scope,
                "Position group: " + definition.PositionGroup,
                "Source: " + definition.SourceName,
                "Metrics: " + string.Join(", ", definition.MetricKeys.Take(6).ToArray()),
                "Minimum sample: " + definition.MinimumSampleSize.ToString(CultureInfo.InvariantCulture),
                "Minimum minutes: " + definition.MinimumMinutes.ToString(CultureInfo.InvariantCulture),
                definition.VerificationLabel
            }.ToList();

            if (definition.LatestRun == null)
            {
                rows.Add("Latest run: none");
            }
            else
            {
                rows.Add("Latest run: " + definition.LatestRun.PlayerCount.ToString(CultureInfo.InvariantCulture) + " players | " + definition.LatestRun.MetricCount.ToString(CultureInfo.InvariantCulture) + " metrics");
                rows.Add(definition.LatestRun.SafeMessage);
            }

            foreach (var snapshot in definition.Snapshots.Take(4))
            {
                rows.Add(snapshot.MetricKey + " | n=" + snapshot.SampleSize.ToString(CultureInfo.InvariantCulture) + " | median " + snapshot.Median + " | avg " + snapshot.Average + " | min/max " + snapshot.Min + "/" + snapshot.Max);
                rows.Add(snapshot.ComparisonGroup + " | " + snapshot.VerificationLabel);
            }

            return StatlynUiFactory.MakeCard(definition.BenchmarkName, rows.ToArray());
        }
    }
}
