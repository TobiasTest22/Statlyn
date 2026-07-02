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

        public static PlayerRawSnapshot CreateExternalPlayer(
            bool isLicensed = true,
            bool permitsImages = false,
            bool permitsFlags = true,
            bool usesBundledFlags = false)
        {
            var player = new PlayerRawSnapshot("external-1001", "Synthetic external fixture", ProviderType.Csv)
            {
                DisplayName = "Synthetic Player",
                SourceContext = new SourceContext(
                    "Synthetic external fixture",
                    "CSV fixture",
                    ProviderType.Csv,
                    isLicensed,
                    permitsImages,
                    permitsFlags,
                    usesBundledFlags,
                    85,
                    "development fixture"),
                ScoutContext = new ScoutContext(false, 0, false)
            };

            player.Fields[PlayerFieldKey.DisplayName] = new RawFieldValue(PlayerFieldKey.DisplayName, "DisplayName", "Synthetic Player", FieldValueKind.Text, 90);
            player.Fields[PlayerFieldKey.Age] = new RawFieldValue(PlayerFieldKey.Age, "Age", 21, FieldValueKind.Number, 90);
            return player;
        }
    }
}
