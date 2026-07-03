namespace Statlyn.Data.Persistence
{
    public sealed class PlayerStatRecord
    {
        public PlayerStatRecord(long playerId, string fieldInstanceKey, string statName, double statValue, int minutes, bool sampleMinutesMissing, string minutesSource, string sourceName, int confidence)
        {
            PlayerId = playerId;
            FieldInstanceKey = fieldInstanceKey ?? string.Empty;
            StatName = statName ?? string.Empty;
            StatValue = statValue;
            Minutes = minutes;
            SampleMinutesMissing = sampleMinutesMissing;
            MinutesSource = minutesSource ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            Confidence = confidence;
        }

        public long PlayerId { get; }

        public string FieldInstanceKey { get; }

        public string StatName { get; }

        public double StatValue { get; }

        public int Minutes { get; }

        public bool SampleMinutesMissing { get; }

        public string MinutesSource { get; }

        public string SourceName { get; }

        public int Confidence { get; }
    }
}
