using System;

namespace Statlyn.Data.Persistence
{
    public sealed class StoredPlayerRecord
    {
        public StoredPlayerRecord(long id, string statlynPlayerId, string displayName, string sourceName, int sourceConfidence, int dataCompleteness, DateTimeOffset lastUpdatedUtc)
        {
            Id = id;
            StatlynPlayerId = statlynPlayerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceConfidence = sourceConfidence;
            DataCompleteness = dataCompleteness;
            LastUpdatedUtc = lastUpdatedUtc;
        }

        public long Id { get; }

        public string StatlynPlayerId { get; }

        public string DisplayName { get; }

        public string SourceName { get; }

        public int SourceConfidence { get; }

        public int DataCompleteness { get; }

        public DateTimeOffset LastUpdatedUtc { get; }
    }
}
