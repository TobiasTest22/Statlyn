namespace Statlyn.DataProviders.Fm26
{
    public sealed class Fm26NativeConnectorOptions
    {
        public string LibraryName { get; set; } = "Statlyn.NativeConnector";

        public bool? ForceIsWindows { get; set; }
    }
}
