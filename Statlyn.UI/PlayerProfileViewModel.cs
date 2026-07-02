using Statlyn.Core;

namespace Statlyn.UI
{
    public sealed class PlayerProfileViewModel
    {
        private PlayerProfileViewModel(MaskedPlayer player)
        {
            StatlynPlayerId = player.StatlynPlayerId;
            DisplayName = player.DisplayName;
            Confidence = player.Confidence;
            ScoutKnowledgePercentage = player.ScoutKnowledgePercentage;
        }

        public string StatlynPlayerId { get; }

        public string DisplayName { get; }

        public int Confidence { get; }

        public int ScoutKnowledgePercentage { get; }

        public static PlayerProfileViewModel FromMaskedPlayer(MaskedPlayer player)
        {
            var viewModel = new PlayerProfileViewModel(player);
            BindingPolicy.AssertBindable(viewModel);
            return viewModel;
        }
    }
}
