using System;

namespace PortfolioPicker.App
{
    public enum AssetLocation
    {
        None,
        Domestic,
        International
    }
    
    public static class AssetLocations
    {
        public static AssetLocation[] ALL = Enum.GetValues(typeof(AssetLocation)) as AssetLocation[];
    }
}
