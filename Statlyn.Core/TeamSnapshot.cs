namespace Statlyn.Core
{
    public sealed class TeamSnapshot
    {
        public TeamSnapshot(string sourceTeamId, string name, string nation, string league)
        {
            SourceTeamId = sourceTeamId ?? string.Empty;
            Name = name ?? string.Empty;
            Nation = nation ?? string.Empty;
            League = league ?? string.Empty;
        }

        public string SourceTeamId { get; }

        public string Name { get; }

        public string Nation { get; }

        public string League { get; }
    }
}
