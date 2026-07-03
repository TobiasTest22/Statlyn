using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Benchmarks;
using Statlyn.UI;
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
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            BuildHeader(main);
            main.Add(StatlynUiFactory.MakeCommandWarningBanner("Benchmark Guardrail", new[]
            {
                "Generic/import benchmark definitions only.",
                "No fake percentiles or official FM26 verification.",
                "Available, insufficient sample and no benchmark states stay textual."
            }));

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");

            var actions = new VisualElement();
            var seed = new Button { text = "Seed Benchmark Definitions" };
            var run = new Button { text = "Run Benchmark Definitions" };
            var refresh = new Button { text = "Refresh" };
            actions = StatlynUiFactory.MakeCommandActionButtonRow(seed, run, refresh);
            main.Add(actions);
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
            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                "Benchmarks",
                "Generic/import aggregate snapshots with honest sample states",
                "Aggregate snapshots only",
                CommandStatusCategory.Info));
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
                        list.Add(StatlynUiFactory.MakeCommandEmptyState("No benchmark definitions yet.", "Seed definitions to create generic/import benchmark templates.", "No fake results are shown."));
                        return;
                    }

                    var grid = new VisualElement();
                    grid.AddToClassList("dashboard-grid");
                    grid.AddToClassList("command-kpi-row");
                    list.Add(grid);
                    foreach (var definition in page.Definitions)
                    {
                        grid.Add(MakeDefinitionCard(definition));
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(StatlynUiFactory.MakeErrorCard("Benchmarks", "Could not load benchmarks safely.", ex.GetType().Name + ": " + ex.Message));
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

            var card = StatlynUiFactory.MakeCommandDataQualityPanel(definition.BenchmarkName, rows.ToArray(), definition.LatestRun == null ? CommandStatusCategory.Muted : ThemeTokens.BenchmarkStatus(definition.LatestRun.SafeMessage));
            return card;
        }
    }
}
