namespace Statlyn.DataProviders
{
    public sealed class ProviderCapabilities
    {
        public ProviderCapabilities(
            bool supportsPlayers,
            bool supportsTeams,
            bool supportsMatches,
            bool supportsPhysicalData,
            bool supportsEventData,
            bool supportsScoutingReports,
            bool supportsPlayerImages,
            bool supportsNationalityFlags)
        {
            SupportsPlayers = supportsPlayers;
            SupportsTeams = supportsTeams;
            SupportsMatches = supportsMatches;
            SupportsPhysicalData = supportsPhysicalData;
            SupportsEventData = supportsEventData;
            SupportsScoutingReports = supportsScoutingReports;
            SupportsPlayerImages = supportsPlayerImages;
            SupportsNationalityFlags = supportsNationalityFlags;
        }

        public bool SupportsPlayers { get; }

        public bool SupportsTeams { get; }

        public bool SupportsMatches { get; }

        public bool SupportsPhysicalData { get; }

        public bool SupportsEventData { get; }

        public bool SupportsScoutingReports { get; }

        public bool SupportsPlayerImages { get; }

        public bool SupportsNationalityFlags { get; }
    }
}
