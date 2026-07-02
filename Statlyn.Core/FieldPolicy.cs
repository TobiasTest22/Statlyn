using System;

namespace Statlyn.Core
{
    public sealed class FieldPolicy
    {
        public FieldPolicy(
            PlayerFieldKey key,
            FieldVisibilityCategory visibilityCategory,
            bool canDisplay,
            bool canScore,
            bool canStore,
            bool requiresScoutReport,
            int minimumScoutKnowledge,
            bool requiresLicensedSource,
            bool isFm26HiddenValue,
            string missingReason)
        {
            Key = key;
            VisibilityCategory = visibilityCategory;
            CanDisplay = canDisplay;
            CanScore = canScore;
            CanStore = canStore;
            RequiresScoutReport = requiresScoutReport;
            MinimumScoutKnowledge = Clamp(minimumScoutKnowledge);
            RequiresLicensedSource = requiresLicensedSource;
            IsFm26HiddenValue = isFm26HiddenValue;
            MissingReason = missingReason ?? string.Empty;
        }

        public PlayerFieldKey Key { get; }

        public FieldVisibilityCategory VisibilityCategory { get; }

        public bool CanDisplay { get; }

        public bool CanScore { get; }

        public bool CanStore { get; }

        public bool RequiresScoutReport { get; }

        public int MinimumScoutKnowledge { get; }

        public bool RequiresLicensedSource { get; }

        public bool IsFm26HiddenValue { get; }

        public string MissingReason { get; }

        public static FieldPolicy Denied(PlayerFieldKey key, string reason)
        {
            return new FieldPolicy(
                key,
                FieldVisibilityCategory.NeverVisible,
                canDisplay: false,
                canScore: false,
                canStore: false,
                requiresScoutReport: false,
                minimumScoutKnowledge: 0,
                requiresLicensedSource: false,
                isFm26HiddenValue: false,
                missingReason: reason);
        }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }
    }
}
