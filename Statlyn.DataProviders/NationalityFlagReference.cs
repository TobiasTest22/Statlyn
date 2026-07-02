namespace Statlyn.DataProviders
{
    public sealed class NationalityFlagReference
    {
        public NationalityFlagReference(string nationality, string assetKey, bool canDisplay, bool isBundledSafeAsset)
        {
            Nationality = nationality ?? string.Empty;
            AssetKey = assetKey ?? string.Empty;
            CanDisplay = canDisplay;
            IsBundledSafeAsset = isBundledSafeAsset;
        }

        public string Nationality { get; }

        public string AssetKey { get; }

        public bool CanDisplay { get; }

        public bool IsBundledSafeAsset { get; }
    }
}
