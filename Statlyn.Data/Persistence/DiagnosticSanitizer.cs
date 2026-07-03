using System.Text.RegularExpressions;

namespace Statlyn.Data.Persistence
{
    public static class DiagnosticSanitizer
    {
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        private static readonly Regex NamedAssignmentPattern = new Regex(
            @"\b(CurrentAbility|CA|PotentialAbility|PA|Professionalism|HiddenPersonality|InjuryProneness|Consistency|ImportantMatches|Pressure|Ambition|Loyalty|Adaptability|Temperament)\b\s*[:=]\s*[-+]?\d+(\.\d+)?",
            Options);

        private static readonly Regex NamedSpacePattern = new Regex(
            @"\b(CA|PA|CurrentAbility|PotentialAbility|Professionalism)\b\s+[-+]?\d+(\.\d+)?",
            Options);

        public static string Sanitize(string diagnostics)
        {
            if (string.IsNullOrWhiteSpace(diagnostics))
            {
                return string.Empty;
            }

            var sanitized = NamedAssignmentPattern.Replace(diagnostics, match => Redact(match.Value));
            sanitized = NamedSpacePattern.Replace(sanitized, match => Redact(match.Value));
            return sanitized;
        }

        private static string Redact(string value)
        {
            var separatorIndex = value.IndexOf(':');
            if (separatorIndex < 0)
            {
                separatorIndex = value.IndexOf('=');
            }

            if (separatorIndex >= 0)
            {
                return value.Substring(0, separatorIndex + 1) + " [redacted]";
            }

            var parts = value.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "[redacted]" : parts[0] + " [redacted]";
        }
    }
}
