using System;
using System.Collections.Generic;

namespace Statlyn.Data.Shortlists
{
    public static class ShortlistDecisionHelper
    {
        public static ShortlistDecisionResult Suggest(
            string recommendation,
            int? confidence,
            int missingDataCount,
            int blockedFieldCount,
            int? roleFit,
            int? sourceConfidence)
        {
            var warnings = new List<string>();
            var reasonParts = new List<string>();
            var safeRecommendation = recommendation ?? string.Empty;
            var confidenceValue = confidence ?? 0;
            var roleFitValue = roleFit ?? 0;
            var sourceConfidenceValue = sourceConfidence ?? 0;

            if (blockedFieldCount > 0)
            {
                warnings.Add(blockedFieldCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " blocked field audit notice(s) exist; raw values are not used.");
            }

            if (sourceConfidence.HasValue && sourceConfidenceValue < 70)
            {
                reasonParts.Add("source confidence is low");
                return new ShortlistDecisionResult(
                    ShortlistStatus.ScoutFurther,
                    ShortlistPriority.Medium,
                    ShortlistFollowUpAction.ScoutAgain,
                    BuildReason(reasonParts, warnings),
                    warnings);
            }

            if (confidence.HasValue && confidenceValue < 55)
            {
                reasonParts.Add("profile confidence is low");
                return new ShortlistDecisionResult(
                    ShortlistStatus.ScoutFurther,
                    ShortlistPriority.Medium,
                    ShortlistFollowUpAction.ScoutAgain,
                    BuildReason(reasonParts, warnings),
                    warnings);
            }

            if (missingDataCount > 0)
            {
                reasonParts.Add("core output is missing");
                return new ShortlistDecisionResult(
                    ShortlistStatus.ScoutFurther,
                    ShortlistPriority.Medium,
                    ShortlistFollowUpAction.WatchMore,
                    BuildReason(reasonParts, warnings),
                    warnings);
            }

            if (roleFit.HasValue && confidence.HasValue && roleFitValue >= 88 && confidenceValue >= 80)
            {
                reasonParts.Add("role fit and confidence are both high");
                return new ShortlistDecisionResult(
                    ShortlistStatus.StrongTarget,
                    ShortlistPriority.High,
                    ShortlistFollowUpAction.CheckAvailability,
                    BuildReason(reasonParts, warnings),
                    warnings);
            }

            if (roleFit.HasValue && confidence.HasValue && roleFitValue >= 75 && confidenceValue >= 70)
            {
                reasonParts.Add("role fit and confidence are shortlist-ready");
                return new ShortlistDecisionResult(
                    ShortlistStatus.Shortlist,
                    ShortlistPriority.High,
                    ShortlistFollowUpAction.CompareAlternatives,
                    BuildReason(reasonParts, warnings),
                    warnings);
            }

            if (safeRecommendation.IndexOf("Avoid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                safeRecommendation.IndexOf("Unrealistic", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                reasonParts.Add("recommendation carries fit or realism risk");
                return new ShortlistDecisionResult(
                    ShortlistStatus.Watchlist,
                    ShortlistPriority.Low,
                    ShortlistFollowUpAction.ReviewRoleFit,
                    BuildReason(reasonParts, warnings),
                    warnings);
            }

            reasonParts.Add("profile is suitable for monitoring");
            return new ShortlistDecisionResult(
                ShortlistStatus.Watchlist,
                ShortlistPriority.Medium,
                ShortlistFollowUpAction.WatchMore,
                BuildReason(reasonParts, warnings),
                warnings);
        }

        private static string BuildReason(IReadOnlyList<string> parts, IReadOnlyList<string> warnings)
        {
            var reason = parts == null || parts.Count == 0
                ? "Safe shortlist workflow suggestion from persisted profile context."
                : "Safe shortlist workflow suggestion: " + string.Join(", ", parts) + ".";

            if (warnings != null && warnings.Count > 0)
            {
                reason += " " + string.Join(" ", warnings);
            }

            return reason;
        }
    }
}
