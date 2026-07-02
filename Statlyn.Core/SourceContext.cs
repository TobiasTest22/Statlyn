using System;

namespace Statlyn.Core
{
    public sealed class SourceContext
    {
        public SourceContext(
            string sourceName,
            string sourceProvider,
            ProviderType providerType,
            bool isLicensed,
            bool permitsPlayerImages,
            bool permitsProviderFlags,
            bool usesBundledSafeFlagAssets,
            bool permitsClubBadges,
            bool allowsExport,
            int sourceConfidence,
            string allowedUsage)
        {
            SourceName = sourceName ?? string.Empty;
            SourceProvider = sourceProvider ?? string.Empty;
            ProviderType = providerType;
            IsLicensed = isLicensed;
            PermitsPlayerImages = permitsPlayerImages;
            PermitsProviderFlags = permitsProviderFlags;
            UsesBundledSafeFlagAssets = usesBundledSafeFlagAssets;
            PermitsClubBadges = permitsClubBadges;
            AllowsExport = allowsExport;
            SourceConfidence = Clamp(sourceConfidence);
            AllowedUsage = allowedUsage ?? string.Empty;
        }

        public SourceContext(
            string sourceName,
            string sourceProvider,
            ProviderType providerType,
            bool isLicensed,
            bool allowsPlayerImages,
            bool allowsNationalityFlags,
            bool usesBundledSafeFlagAssets,
            int sourceConfidence,
            string allowedUsage)
            : this(
                sourceName,
                sourceProvider,
                providerType,
                isLicensed,
                allowsPlayerImages,
                allowsNationalityFlags,
                usesBundledSafeFlagAssets,
                permitsClubBadges: false,
                allowsExport: false,
                sourceConfidence,
                allowedUsage)
        {
        }

        public string SourceName { get; }

        public string SourceProvider { get; }

        public ProviderType ProviderType { get; }

        public bool IsLicensed { get; }

        public bool PermitsPlayerImages { get; }

        public bool AllowsPlayerImages
        {
            get { return PermitsPlayerImages; }
        }

        public bool PermitsProviderFlags { get; }

        public bool AllowsNationalityFlags
        {
            get { return PermitsProviderFlags; }
        }

        public bool UsesBundledSafeFlagAssets { get; }

        public bool PermitsClubBadges { get; }

        public bool AllowsExport { get; }

        public int SourceConfidence { get; }

        public string AllowedUsage { get; }

        public static SourceContext ForProvider(string sourceProvider, ProviderType providerType, int sourceConfidence)
        {
            return new SourceContext(
                sourceProvider,
                sourceProvider,
                providerType,
                isLicensed: providerType == ProviderType.FM26LiveMemory,
                permitsPlayerImages: false,
                permitsProviderFlags: false,
                usesBundledSafeFlagAssets: false,
                permitsClubBadges: false,
                allowsExport: false,
                sourceConfidence: sourceConfidence,
                allowedUsage: providerType == ProviderType.FM26LiveMemory ? "local read-only process observation" : "unspecified");
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
