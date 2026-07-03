using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Scouting
{
    public static class ScoutTextSanitizer
    {
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        private static readonly Regex HiddenAssignmentPattern = new Regex(
            @"\b(CurrentAbility|Current\s+Ability|PotentialAbility|Potential\s+Ability|HiddenPersonality|Hidden\s+Personality|InjuryProneness|Injury\s+Proneness|ImportantMatches|Important\s+Matches|Professionalism|Consistency|Pressure|Ambition|Loyalty|Adaptability|Temperament|CA|PA)\b\s*(?::|=|\s)\s*[-+]?\d{1,3}(\.\d+)?\b",
            Options);

        public static string Sanitize(string text)
        {
            return SanitizeWithMetadata(text).Text;
        }

        public static ScoutTextSanitizationResult SanitizeWithMetadata(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new ScoutTextSanitizationResult(string.Empty, false, new List<string>());
            }

            var trimmed = text.Trim();
            var wasRedacted = false;
            var sanitized = HiddenAssignmentPattern.Replace(trimmed, match =>
            {
                wasRedacted = true;
                return "[redacted hidden scouting value]";
            });
            var diagnosticSanitized = DiagnosticSanitizer.Sanitize(sanitized);
            wasRedacted = wasRedacted || !string.Equals(diagnosticSanitized, sanitized, StringComparison.Ordinal);
            sanitized = diagnosticSanitized;

            var warnings = wasRedacted
                ? new[] { "Hidden-value-looking numeric scouting text was redacted before storage." }
                : Array.Empty<string>();
            return new ScoutTextSanitizationResult(sanitized, wasRedacted, warnings);
        }

        public static bool ContainsHiddenAssignment(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return HiddenAssignmentPattern.IsMatch(text);
        }
    }

    public sealed class ScoutTextSanitizationResult
    {
        public ScoutTextSanitizationResult(string text, bool wasRedacted, IReadOnlyList<string> warnings)
        {
            Text = text ?? string.Empty;
            WasRedacted = wasRedacted;
            Warnings = warnings ?? new List<string>();
        }

        public string Text { get; }

        public bool WasRedacted { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
