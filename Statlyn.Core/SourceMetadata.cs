using System;

namespace Statlyn.Core
{
    public sealed class SourceMetadata
    {
        public SourceMetadata(
            string sourceName,
            ProviderType providerType,
            bool isLive,
            bool isLicensed,
            string licenceStatus,
            string allowedUsage,
            bool permitsImages,
            bool permitsFlags,
            DateTimeOffset importedAtUtc,
            int sourceConfidence)
            : this(
                sourceName,
                providerType,
                isLive,
                isLicensed,
                licenceStatus,
                allowedUsage,
                permitsPlayerImages: permitsImages,
                permitsProviderFlags: permitsFlags,
                usesBundledSafeFlagAssets: permitsFlags,
                permitsClubBadges: false,
                allowsExport: false,
                importedAtUtc,
                sourceConfidence)
        {
        }

        public SourceMetadata(
            string sourceName,
            ProviderType providerType,
            bool isLive,
            bool isLicensed,
            string licenceStatus,
            string allowedUsage,
            bool permitsPlayerImages,
            bool permitsProviderFlags,
            bool usesBundledSafeFlagAssets,
            bool permitsClubBadges,
            bool allowsExport,
            DateTimeOffset importedAtUtc,
            int sourceConfidence)
        {
            SourceName = sourceName ?? string.Empty;
            ProviderType = providerType;
            IsLive = isLive;
            IsLicensed = isLicensed;
            LicenceStatus = licenceStatus ?? string.Empty;
            AllowedUsage = allowedUsage ?? string.Empty;
            PermitsPlayerImages = permitsPlayerImages;
            PermitsProviderFlags = permitsProviderFlags;
            UsesBundledSafeFlagAssets = usesBundledSafeFlagAssets;
            PermitsClubBadges = permitsClubBadges;
            AllowsExport = allowsExport;
            ImportedAtUtc = importedAtUtc;
            SourceConfidence = Clamp(sourceConfidence);
        }

        public string SourceName { get; }

        public ProviderType ProviderType { get; }

        public bool IsLive { get; }

        public bool IsLicensed { get; }

        public string LicenceStatus { get; }

        public string AllowedUsage { get; }

        public bool PermitsPlayerImages { get; }

        public bool PermitsImages
        {
            get { return PermitsPlayerImages; }
        }

        public bool PermitsProviderFlags { get; }

        public bool PermitsFlags
        {
            get { return PermitsProviderFlags; }
        }

        public bool UsesBundledSafeFlagAssets { get; }

        public bool PermitsClubBadges { get; }

        public bool AllowsExport { get; }

        public DateTimeOffset ImportedAtUtc { get; }

        public int SourceConfidence { get; }

        public SourceContext ToSourceContext(string sourceProvider)
        {
            return new SourceContext(
                SourceName,
                sourceProvider,
                ProviderType,
                IsLicensed,
                PermitsPlayerImages,
                PermitsProviderFlags,
                UsesBundledSafeFlagAssets,
                PermitsClubBadges,
                AllowsExport,
                SourceConfidence,
                AllowedUsage);
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
