namespace Statlyn.Core
{
    public sealed class ScoutContext
    {
        public ScoutContext(bool isManagedClubPlayer, int scoutKnowledgePercentage, bool hasScoutReport)
        {
            IsManagedClubPlayer = isManagedClubPlayer;
            ScoutKnowledgePercentage = Clamp(scoutKnowledgePercentage);
            HasScoutReport = hasScoutReport;
        }

        public bool IsManagedClubPlayer { get; }

        public int ScoutKnowledgePercentage { get; }

        public bool HasScoutReport { get; }

        public static ScoutContext Unknown
        {
            get { return new ScoutContext(false, 0, false); }
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
