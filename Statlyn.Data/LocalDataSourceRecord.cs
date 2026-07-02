using System;
using Statlyn.Core;

namespace Statlyn.Data
{
    public sealed class LocalDataSourceRecord
    {
        public LocalDataSourceRecord(string sourceName, ProviderType providerType, bool isLicensed, DateTimeOffset importedAtUtc)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            ProviderType = providerType;
            IsLicensed = isLicensed;
            ImportedAtUtc = importedAtUtc;
        }

        public string SourceName { get; }

        public ProviderType ProviderType { get; }

        public bool IsLicensed { get; }

        public DateTimeOffset ImportedAtUtc { get; }
    }
}
