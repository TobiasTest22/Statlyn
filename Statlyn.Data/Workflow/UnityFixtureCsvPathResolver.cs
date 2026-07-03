using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Statlyn.Data.Workflow
{
    public sealed class UnityFixtureCsvPathResolver
    {
        private const string FixtureFileName = "players.sample.csv";

        public FixtureCsvPathResolutionResult Resolve(string applicationDataPath, string streamingAssetsPath)
        {
            var candidates = BuildCandidates(applicationDataPath, streamingAssetsPath).ToList();
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return new FixtureCsvPathResolutionResult(
                        true,
                        candidate,
                        candidates,
                        "Synthetic development fixture CSV found. It is not live FM26 data.");
                }
            }

            return new FixtureCsvPathResolutionResult(
                false,
                string.Empty,
                candidates,
                "Synthetic fixture CSV was not found. Run tools/copy-managed-to-unity.ps1 or enter a local CSV path manually.");
        }

        public IReadOnlyList<string> BuildCandidates(string applicationDataPath, string streamingAssetsPath)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(applicationDataPath))
            {
                var assetsPath = Path.GetFullPath(applicationDataPath);
                candidates.Add(Path.GetFullPath(Path.Combine(assetsPath, "..", "..", "Statlyn.Tests", "Fixtures", FixtureFileName)));
                candidates.Add(Path.GetFullPath(Path.Combine(assetsPath, "Fixtures", FixtureFileName)));
                candidates.Add(Path.GetFullPath(Path.Combine(assetsPath, "StreamingAssets", "Statlyn", "Fixtures", FixtureFileName)));
            }

            if (!string.IsNullOrWhiteSpace(streamingAssetsPath))
            {
                candidates.Add(Path.GetFullPath(Path.Combine(streamingAssetsPath, "Statlyn", "Fixtures", FixtureFileName)));
                candidates.Add(Path.GetFullPath(Path.Combine(streamingAssetsPath, "Fixtures", FixtureFileName)));
            }

            return candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
