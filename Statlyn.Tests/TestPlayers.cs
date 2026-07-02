using Statlyn.Core;

namespace Statlyn.Tests
{
    internal static class TestPlayers
    {
        public static PlayerRawSnapshot CreateScoutedFm26Player()
        {
            var player = new PlayerRawSnapshot("fm26-1001", "FM26 test fixture", ProviderType.FM26LiveMemory)
            {
                DisplayName = "Fixture Forward",
                HasScoutReport = true,
                ScoutKnowledgePercentage = 62,
                SourceConfidence = 80,
                IsManagedClubPlayer = false
            };

            player.VisibleFacts["Age"] = "22";
            player.VisibleFacts["Nationality"] = "Romania";
            player.VisibleAttributes["Finishing"] = 14;
            player.VisibleAttributes["Pace"] = 13;
            player.VisibleAttributeNames.Add("Finishing");
            player.VisibleAttributeNames.Add("Pace");

            return player;
        }
    }
}
