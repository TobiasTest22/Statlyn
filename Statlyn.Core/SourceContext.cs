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
            bool allowsPlayerImages,
            bool allowsNationalityFlags,
            bool usesBundledSafeFlagAssets,
            int sourceConfidence,
            string allowedUsage)
        {
            SourceName = sourceName ?? string.Empty;
            SourceProvider = sourceProvider ?? string.Empty;
            ProviderType = providerType;
            IsLicensed = isLicensed;
            AllowsPlayerImages = allowsPlayerImages;
            AllowsNationalityFlags = allowsNationalityFlags;
            UsesBundledSafeFlagAssets = usesBundledSafeFlagAssets;
            SourceConfidence = Clamp(sourceConfidence);
            AllowedUsage = allowedUsage ?? string.Empty;
        }

        public string SourceName { get; }

        public string SourceProvider { get; }

        public ProviderType ProviderType { get; }

        public bool IsLicensed { get; }

        public bool AllowsPlayerImages { get; }

        public bool AllowsNationalityFlags { get; }

        public bool UsesBundledSafeFlagAssets { get; }

        public int SourceConfidence { get; }

        public string AllowedUsage { get; }

        public static SourceContext ForProvider(string sourceProvider, ProviderType providerType, int sourceConfidence)
        {
            return new SourceContext(
                sourceProvider,
                sourceProvider,
                providerType,
                isLicensed: providerType == ProviderType.FM26LiveMemory,
                allowsPlayerImages: false,
                allowsNationalityFlags: false,
                usesBundledSafeFlagAssets: false,
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
