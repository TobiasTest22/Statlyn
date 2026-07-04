using System;
using System.Linq;

namespace Statlyn.DataProviders.Fm26.MemoryMaps
{
    public sealed class MemoryMapSelector
    {
        public MemoryMapSelectionResult Select(MemoryMapRegistryDiagnostic registry, Fm26ProcessDiagnostic process)
        {
            if (registry == null || registry.MapsFoundCount == 0)
            {
                return new MemoryMapSelectionResult
                {
                    SupportStatus = MemoryMapSupportStatus.MapMissing.ToString(),
                    SupportMessage = "No validated FM26 map metadata was found.",
                    NextActionSafeMessage = "Add validated FM26 map metadata before any future player snapshot milestone."
                };
            }

            var usableMaps = registry.Maps.Where(map => map.IsUsable && map.Manifest != null).ToList();
            if (usableMaps.Count == 0)
            {
                var status = registry.TemplateMapsCount > 0
                    ? MemoryMapSupportStatus.TemplateOnly.ToString()
                    : MemoryMapSupportStatus.Unvalidated.ToString();
                return new MemoryMapSelectionResult
                {
                    HasValidatedMap = registry.HasValidatedMap,
                    SupportStatus = status,
                    SupportMessage = registry.TemplateMapsCount > 0
                        ? "Only template FM26 map metadata is available. Templates are not usable."
                        : "No validated usable FM26 map metadata is available.",
                    NextActionSafeMessage = "Validate an FM26 map before any future player snapshot milestone."
                };
            }

            if (process == null || !process.IsDetected)
            {
                var selected = usableMaps[0];
                return BuildSelected(selected, MemoryMapSupportStatus.PlayerReadingNotImplemented.ToString(), "Validated FM26 map metadata is available, but no FM process is detected. Player reading is not implemented.");
            }

            var match = usableMaps.FirstOrDefault(map => MatchesProcess(map, process));
            if (match == null)
            {
                return new MemoryMapSelectionResult
                {
                    HasValidatedMap = true,
                    SupportStatus = MemoryMapSupportStatus.BuildMismatch.ToString(),
                    SupportMessage = "Validated FM26 map metadata exists, but it does not match the detected process build.",
                    NextActionSafeMessage = "Validate a map for the detected FM build before any future player snapshot milestone."
                };
            }

            return BuildSelected(match, MemoryMapSupportStatus.MapAvailable.ToString(), "Validated FM26 map metadata matches the detected build. Player reading is not implemented.");
        }

        private static MemoryMapSelectionResult BuildSelected(MemoryMapFileDiagnostic selected, string supportStatus, string message)
        {
            return new MemoryMapSelectionResult
            {
                HasValidatedMap = true,
                HasSelectedMap = true,
                SelectedMapId = selected.MapId,
                SelectedMapDisplayName = selected.DisplayName,
                SupportStatus = supportStatus,
                SupportMessage = message,
                NextActionSafeMessage = "Player reading remains disabled until a future safe snapshot milestone.",
                SelectedMap = selected
            };
        }

        private static bool MatchesProcess(MemoryMapFileDiagnostic map, Fm26ProcessDiagnostic process)
        {
            var version = process.ProductVersion ?? string.Empty;
            var fileVersion = process.FileVersion ?? string.Empty;
            var architecture = process.Architecture ?? string.Empty;

            var buildMatches = EqualsSafe(map.BuildNumber, version)
                || EqualsSafe(map.BuildNumber, fileVersion)
                || EqualsSafe(map.GameVersion, version)
                || EqualsSafe(map.GameVersion, fileVersion);
            var architectureMatches = string.IsNullOrWhiteSpace(map.Architecture)
                || string.IsNullOrWhiteSpace(architecture)
                || architecture.IndexOf(map.Architecture, StringComparison.OrdinalIgnoreCase) >= 0
                || map.Architecture.IndexOf(architecture, StringComparison.OrdinalIgnoreCase) >= 0;

            return buildMatches && architectureMatches;
        }

        private static bool EqualsSafe(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
