namespace Statlyn.DataProviders.Fm26
{
    public enum Fm26DiagnosticSupportStatus
    {
        NotDetected = 0,
        ConnectorUnavailable = 1,
        UnsupportedPlatform = 2,
        AccessDenied = 3,
        MapMissing = 4,
        MapUnvalidated = 5,
        UnsupportedBuild = 6,
        DiagnosticsOnly = 7,
        ReadyForFutureMapValidation = 8
    }
}
