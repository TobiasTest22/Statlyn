namespace Statlyn.Core
{
    public sealed class PlayerStatSnapshot
    {
        public PlayerStatSnapshot(string sourcePlayerId, string statName, double value, int minutes, string sourceProvider)
        {
            SourcePlayerId = sourcePlayerId ?? string.Empty;
            StatName = statName ?? string.Empty;
            Value = value;
            Minutes = minutes < 0 ? 0 : minutes;
            SourceProvider = sourceProvider ?? string.Empty;
        }

        public string SourcePlayerId { get; }

        public string StatName { get; }

        public double Value { get; }

        public int Minutes { get; }

        public string SourceProvider { get; }
    }
}
