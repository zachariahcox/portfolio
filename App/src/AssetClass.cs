using System;
namespace PortfolioPicker.App
{
    public enum AssetClass
    {
        None,
        Stock,
        Bond
    }
    
    public static class AssetClasses
    {
        public static AssetClass[] ALL = Enum.GetValues(typeof(AssetClass)) as AssetClass[];
    }
}
