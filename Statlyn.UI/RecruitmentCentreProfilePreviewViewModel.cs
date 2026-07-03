using System.Collections.Generic;

namespace Statlyn.UI
{
    public sealed class RecruitmentCentreProfilePreviewViewModel
    {
        public RecruitmentCentreProfilePreviewViewModel(
            MaskedPlayerProfileViewModel maskedProfile,
            string playerName,
            string sourceName,
            string modeLabel,
            bool isFixtureMode,
            bool isLiveFm26Data,
            string roleName,
            string roleFit,
            string confidence,
            string risk,
            IReadOnlyList<string> outputMetrics,
            string missingDataWarning,
            string blockedDataSafeNotice)
        {
            MaskedProfile = maskedProfile;
            PlayerName = playerName ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            ModeLabel = modeLabel ?? string.Empty;
            IsFixtureMode = isFixtureMode;
            IsLiveFm26Data = isLiveFm26Data;
            RoleName = roleName ?? string.Empty;
            RoleFit = roleFit ?? string.Empty;
            Confidence = confidence ?? string.Empty;
            Risk = risk ?? string.Empty;
            OutputMetrics = outputMetrics ?? new List<string>();
            MissingDataWarning = missingDataWarning ?? string.Empty;
            BlockedDataSafeNotice = blockedDataSafeNotice ?? string.Empty;
        }

        public MaskedPlayerProfileViewModel MaskedProfile { get; }

        public string PlayerName { get; }

        public string SourceName { get; }

        public string ModeLabel { get; }

        public bool IsFixtureMode { get; }

        public bool IsLiveFm26Data { get; }

        public string RoleName { get; }

        public string RoleFit { get; }

        public string Confidence { get; }

        public string Risk { get; }

        public IReadOnlyList<string> OutputMetrics { get; }

        public string MissingDataWarning { get; }

        public string BlockedDataSafeNotice { get; }
    }
}
