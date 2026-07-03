using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statlyn.Data.Benchmarks
{
    public sealed class BenchmarkWorkflowService
    {
        private readonly BenchmarkRepository _repository;
        private readonly BenchmarkCalculationService _calculator;
        private readonly BenchmarkSeedService _seedService;

        public BenchmarkWorkflowService(StatlynDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            _repository = new BenchmarkRepository(connectionFactory);
            _calculator = new BenchmarkCalculationService(connectionFactory);
            _seedService = new BenchmarkSeedService(connectionFactory);
        }

        public BenchmarkSeedResult SeedDefaultDefinitions()
        {
            return _seedService.SeedDefaultDefinitions();
        }

        public BenchmarksPageViewModel BuildPageViewModel()
        {
            var definitions = _repository.LoadDefinitions(includeArchived: false)
                .Select(BuildDefinitionCard)
                .ToList();
            return new BenchmarksPageViewModel(
                definitions.Count == 0
                    ? "No benchmark definitions yet."
                    : "Benchmarks use persisted safe data only. Generic/import metrics are not FM26-verified.",
                definitions);
        }

        public BenchmarkRunBatchResult RunAllActiveDefinitions()
        {
            var runs = new List<BenchmarkRunSummaryViewModel>();
            foreach (var definition in _repository.LoadDefinitions(includeArchived: false))
            {
                runs.Add(RunDefinition(definition.Id));
            }

            return new BenchmarkRunBatchResult(
                runs.Count,
                runs.Count == 0
                    ? "No benchmark definitions yet."
                    : "Benchmark definitions were run against persisted safe aggregate data.",
                runs);
        }

        public BenchmarkRunSummaryViewModel RunDefinition(long definitionId)
        {
            var definition = _repository.LoadDefinition(definitionId);
            if (definition == null || definition.IsArchived)
            {
                return new BenchmarkRunSummaryViewModel(0, definitionId, string.Empty, 0, 0, "No benchmark yet. Definition was not found.");
            }

            var calculation = _calculator.Calculate(definition);
            var run = _repository.SaveBenchmarkRun(new BenchmarkRunRecord(
                0,
                definition.Id,
                DateTimeOffset.UtcNow,
                calculation.ComparisonPlayerCount,
                calculation.Summary.Results.Count,
                calculation.Summary.SafeMessage));

            foreach (var result in calculation.Summary.Results)
            {
                _repository.SaveMetricSnapshot(_calculator.ToSnapshot(run.Id, result));
            }

            return ToRunSummary(run);
        }

        public BenchmarkPlayerSummary BuildPlayerBenchmarkSummary(string statlynPlayerId)
        {
            if (string.IsNullOrWhiteSpace(statlynPlayerId))
            {
                return new BenchmarkPlayerSummary(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new List<BenchmarkMetricResult>(),
                    BenchmarkStatus.NoBenchmark,
                    "No benchmark yet.",
                    new[] { "A Statlyn player id is required for a player benchmark." });
            }

            var definitions = _repository.LoadDefinitions(includeArchived: false);
            if (definitions.Count == 0)
            {
                return new BenchmarkPlayerSummary(
                    statlynPlayerId,
                    string.Empty,
                    string.Empty,
                    new List<BenchmarkMetricResult>(),
                    BenchmarkStatus.NoBenchmark,
                    "No benchmark yet.",
                    new[] { "Seed or create benchmark definitions before calculating player benchmarks." });
            }

            var calculations = definitions.Select(definition => _calculator.Calculate(definition, statlynPlayerId)).ToList();
            var selected = SelectBestCalculation(calculations);
            return selected == null
                ? new BenchmarkPlayerSummary(
                    statlynPlayerId,
                    string.Empty,
                    string.Empty,
                    new List<BenchmarkMetricResult>(),
                    BenchmarkStatus.NoBenchmark,
                    "No benchmark yet.",
                    new[] { "No valid comparison group exists for this player." })
                : selected.Summary;
        }

        public PlayerBenchmarkSummaryViewModel BuildPlayerBenchmarkSummaryViewModel(string statlynPlayerId)
        {
            return ToPlayerBenchmarkViewModel(BuildPlayerBenchmarkSummary(statlynPlayerId));
        }

        private BenchmarkDefinitionCardViewModel BuildDefinitionCard(BenchmarkDefinition definition)
        {
            var latestRun = _repository.LoadLatestRun(definition.Id);
            var snapshots = latestRun == null
                ? new List<BenchmarkMetricSnapshotViewModel>()
                : _repository.LoadSnapshotsForRun(latestRun.Id).Select(ToSnapshotViewModel).ToList();

            return new BenchmarkDefinitionCardViewModel(
                definition.Id,
                definition.BenchmarkName,
                definition.Scope.ToString(),
                string.IsNullOrWhiteSpace(definition.SourceName) ? "All imported sources" : definition.SourceName,
                string.IsNullOrWhiteSpace(definition.PositionGroup) ? "All position groups" : definition.PositionGroup,
                definition.MetricKeys,
                definition.MinimumSampleSize,
                definition.MinimumMinutes,
                "Generic/import benchmark - not FM26-verified",
                latestRun == null ? null : ToRunSummary(latestRun),
                snapshots);
        }

        private static BenchmarkCalculationResult? SelectBestCalculation(IReadOnlyList<BenchmarkCalculationResult> calculations)
        {
            return calculations
                .OrderBy(calculation => StatusRank(calculation.Summary.OverallStatus))
                .ThenByDescending(calculation => calculation.Summary.Results.Count(result => result.Status == BenchmarkStatus.Available))
                .ThenByDescending(calculation => calculation.Summary.Results.Sum(result => result.SampleSize))
                .FirstOrDefault();
        }

        private static int StatusRank(BenchmarkStatus status)
        {
            switch (status)
            {
                case BenchmarkStatus.Available:
                    return 0;
                case BenchmarkStatus.InsufficientSample:
                    return 1;
                case BenchmarkStatus.MissingMetric:
                    return 2;
                default:
                    return 3;
            }
        }

        private static BenchmarkRunSummaryViewModel ToRunSummary(BenchmarkRunRecord run)
        {
            return new BenchmarkRunSummaryViewModel(
                run.Id,
                run.BenchmarkDefinitionId,
                run.RanAtUtc.ToString("O", CultureInfo.InvariantCulture),
                run.PlayerCount,
                run.MetricCount,
                run.SafeMessage);
        }

        private static BenchmarkMetricSnapshotViewModel ToSnapshotViewModel(BenchmarkMetricSnapshot snapshot)
        {
            return new BenchmarkMetricSnapshotViewModel(
                snapshot.MetricKey,
                snapshot.FieldName,
                snapshot.MetricType.ToString(),
                snapshot.SampleSize,
                FormatNumber(snapshot.MedianValue),
                FormatNumber(snapshot.AverageValue),
                FormatNumber(snapshot.MinimumValue),
                FormatNumber(snapshot.MaximumValue),
                snapshot.SourceName,
                snapshot.ComparisonGroup,
                snapshot.IsGenericImportMetric && !snapshot.IsVerifiedFm26Metric
                    ? "Generic/import metric - not FM26-verified"
                    : "Not FM26-verified");
        }

        private static PlayerBenchmarkSummaryViewModel ToPlayerBenchmarkViewModel(BenchmarkPlayerSummary summary)
        {
            return new PlayerBenchmarkSummaryViewModel(
                summary.StatlynPlayerId,
                summary.BenchmarkName,
                summary.ComparisonGroup,
                summary.OverallStatus.ToString(),
                summary.SafeMessage,
                summary.Results.Select(ToPlayerMetricViewModel).ToList(),
                summary.Warnings);
        }

        private static PlayerBenchmarkMetricViewModel ToPlayerMetricViewModel(BenchmarkMetricResult result)
        {
            return new PlayerBenchmarkMetricViewModel(
                result.MetricKey,
                result.FieldName,
                result.MetricType.ToString(),
                FormatNumber(result.PlayerValue),
                FormatNumber(result.BenchmarkMedian),
                FormatNumber(result.BenchmarkAverage),
                result.Percentile.HasValue ? result.Percentile.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty,
                result.SampleSize,
                result.Status.ToString(),
                result.SourceName,
                result.ComparisonGroup,
                result.IsGenericImportMetric && !result.IsVerifiedFm26Metric
                    ? "Generic/import metric - not FM26-verified"
                    : "Not FM26-verified");
        }

        private static string FormatNumber(double? value)
        {
            if (!value.HasValue)
            {
                return "Unavailable";
            }

            return Math.Abs(value.Value - Math.Round(value.Value)) < 0.001
                ? ((int)Math.Round(value.Value)).ToString(CultureInfo.InvariantCulture)
                : value.Value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
