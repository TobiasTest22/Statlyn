namespace Statlyn.DataProviders.Fm26
{
    public enum NativeConnectorAvailability
    {
        Available = 0,
        Unavailable = 1,
        UnsupportedPlatform = 2,
        MissingLibrary = 3,
        MissingExport = 4,
        BadImage = 5,
        Error = 6
    }
}
