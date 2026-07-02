namespace Statlyn.Core
{
    public sealed class PlayerImageReference
    {
        public PlayerImageReference(string sourcePlayerId, string reference, bool canDisplay, string licenceStatus)
        {
            SourcePlayerId = sourcePlayerId ?? string.Empty;
            Reference = reference ?? string.Empty;
            CanDisplay = canDisplay;
            LicenceStatus = licenceStatus ?? string.Empty;
        }

        public string SourcePlayerId { get; }

        public string Reference { get; }

        public bool CanDisplay { get; }

        public string LicenceStatus { get; }
    }
}
