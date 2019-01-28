using System;

namespace PortfolioPicker.App
{
    internal class Exposure
    {
        public decimal Target { get; set; }

        public AssetClass Class { get; set; }

        public AssetLocation Location { get; set; }

        public AccountType[] AccountTypesPreference { get; set; }

        public override string ToString()
        {
            return $"{Enum.GetName(typeof(AssetClass), Class)}, {Enum.GetName(typeof(AssetLocation), Location)}, target: {Target}";
        }
    }
}
