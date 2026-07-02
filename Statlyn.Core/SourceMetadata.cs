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
        {
            SourceName = sourceName ?? string.Empty;
            ProviderType = providerType;
            IsLive = isLive;
            IsLicensed = isLicensed;
            LicenceStatus = licenceStatus ?? string.Empty;
            AllowedUsage = allowedUsage ?? string.Empty;
            PermitsImages = permitsImages;
            PermitsFlags = permitsFlags;
            ImportedAtUtc = importedAtUtc;
            SourceConfidence = Clamp(sourceConfidence);
        }

        public string SourceName { get; }

        public ProviderType ProviderType { get; }

        public bool IsLive { get; }

        public bool IsLicensed { get; }

        public string LicenceStatus { get; }

        public string AllowedUsage { get; }

        public bool PermitsImages { get; }

        public bool PermitsFlags { get; }

        public DateTimeOffset ImportedAtUtc { get; }

        public int SourceConfidence { get; }

        public SourceContext ToSourceContext(string sourceProvider)
        {
            return new SourceContext(
                SourceName,
                sourceProvider,
                ProviderType,
                IsLicensed,
                PermitsImages,
                PermitsFlags,
                usesBundledSafeFlagAssets: PermitsFlags,
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
