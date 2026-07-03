using System;
using System.Collections.Generic;
using System.Linq;

namespace Statlyn.Data.Benchmarks
{
    public sealed class BenchmarkSeedService
    {
        private readonly BenchmarkRepository _repository;

        public BenchmarkSeedService(StatlynDbConnectionFactory connectionFactory)
        {
            _repository = new BenchmarkRepository(connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory)));
        }

        public BenchmarkSeedResult SeedDefaultDefinitions()
        {
            var definitions = CreateDefaultDefinitions();
            foreach (var definition in definitions)
            {
                _repository.SaveDefinition(definition);
            }

            var stored = _repository.LoadDefinitions(includeArchived: false);
            return new BenchmarkSeedResult(
                definitions.Count,
                stored.Count(item => definitions.Any(definition => string.Equals(definition.BenchmarkName, item.BenchmarkName, StringComparison.OrdinalIgnoreCase))),
                "Generic/import benchmark definitions are available. No benchmark results were seeded.");
        }

        public static IReadOnlyList<BenchmarkDefinition> CreateDefaultDefinitions()
        {
            var now = DateTimeOffset.UtcNow;
            return new[]
            {
                Definition("Wide Attacker Output Benchmark", BenchmarkScope.PositionGroup, "WingerWideForward", new[]
                {
                    "xA",
                    "xG",
                    "ProgressiveCarries",
                    "SuccessfulDribbles",
                    "KeyPasses",
                    "Crosses"
                }, now),
                Definition("Striker Output Benchmark", BenchmarkScope.PositionGroup, "StrikerForward", new[]
                {
                    "xG",
                    "Shots",
                    "Goals"
                }, now),
                Definition("Centre-Back Output Benchmark", BenchmarkScope.PositionGroup, "CentreBack", new[]
                {
                    "AerialDuelsWonPct",
                    "Clearances",
                    "Blocks",
                    "Interceptions",
                    "ProgressivePasses"
                }, now),
                Definition("Central Midfielder Output Benchmark", BenchmarkScope.PositionGroup, "CentralMidfield", new[]
                {
                    "ProgressivePasses",
                    "PassesIntoFinalThird",
                    "KeyPasses",
                    "Tackles",
                    "Interceptions"
                }, now),
                Definition("Goalkeeper Output Benchmark", BenchmarkScope.PositionGroup, "Goalkeeper", new[]
                {
                    "Saves",
                    "SavePercentage",
                    "GoalsPrevented",
                    "KeeperDistributionAccuracy"
                }, now),
                Definition("Physical Output Benchmark", BenchmarkScope.GlobalDataset, string.Empty, new[]
                {
                    "TopSpeed",
                    "SprintDistance",
                    "HighSpeedRunning",
                    "DistanceCovered"
                }, now, minimumMinutes: 0)
            };
        }

        private static BenchmarkDefinition Definition(
            string name,
            BenchmarkScope scope,
            string positionGroup,
            IReadOnlyList<string> metrics,
            DateTimeOffset now,
            int minimumSampleSize = 3,
            int minimumMinutes = 900)
        {
            return new BenchmarkDefinition(
                0,
                name,
                scope,
                string.Empty,
                positionGroup,
                string.Empty,
                string.Empty,
                string.Empty,
                metrics,
                minimumSampleSize,
                minimumMinutes,
                includeFixtureData: true,
                now,
                now,
                isArchived: false);
        }
    }

    public sealed class BenchmarkSeedResult
    {
        public BenchmarkSeedResult(int seededDefinitionCount, int activeSeedDefinitionCount, string safeMessage)
        {
            SeededDefinitionCount = seededDefinitionCount < 0 ? 0 : seededDefinitionCount;
            ActiveSeedDefinitionCount = activeSeedDefinitionCount < 0 ? 0 : activeSeedDefinitionCount;
            SafeMessage = safeMessage ?? string.Empty;
        }

        public int SeededDefinitionCount { get; }

        public int ActiveSeedDefinitionCount { get; }

        public string SafeMessage { get; }
    }
}
