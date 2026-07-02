using System;

namespace Statlyn.Core
{
    public sealed class MatchSnapshot
    {
        public MatchSnapshot(string matchId, string competition, DateTimeOffset dateUtc)
        {
            MatchId = matchId ?? string.Empty;
            Competition = competition ?? string.Empty;
            DateUtc = dateUtc;
        }

        public string MatchId { get; }

        public string Competition { get; }

        public DateTimeOffset DateUtc { get; }
    }
}
