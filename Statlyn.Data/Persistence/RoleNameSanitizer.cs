using System;

namespace Statlyn.Data.Persistence
{
    public static class RoleNameSanitizer
    {
        private static readonly string[] HiddenTerms =
        {
            "CurrentAbility",
            "PotentialAbility",
            "Professionalism",
            "HiddenPersonality",
            "InjuryProneness",
            "Consistency",
            "ImportantMatches",
            "Pressure",
            "Ambition",
            "Loyalty",
            "Adaptability",
            "Temperament"
        };

        public static string SanitizeForStorage(string roleName)
        {
            return SanitizeForDisplay(roleName, "Unknown role");
        }

        public static string SanitizeForDisplay(string roleName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return string.IsNullOrWhiteSpace(fallback) ? "Unknown role" : fallback;
            }

            var sanitized = DiagnosticSanitizer.Sanitize(roleName.Trim());
            if (!string.Equals(sanitized, roleName.Trim(), StringComparison.Ordinal) ||
                ContainsHiddenTerm(sanitized))
            {
                return string.IsNullOrWhiteSpace(fallback) ? "Unknown role" : fallback;
            }

            return sanitized;
        }

        private static bool ContainsHiddenTerm(string value)
        {
            foreach (var term in HiddenTerms)
            {
                if (value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
