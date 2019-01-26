using System;

namespace PortfolioPicker.App
{
    internal class Exposure
    {
        public Exposure(
            AssetClass c,
            AssetLocation l,
            decimal v,
            AccountType[] types)
        {
            Class = c;
            Location = l;
            Target = v;
            AccountTypesPreference = types;
        }

        public AssetClass Class { get; }

        public AssetLocation Location { get; }

        public decimal Target { get; }

        public AccountType[] AccountTypesPreference { get; set; }

        public override string ToString()
        {
            return $"{Enum.GetName(typeof(AssetClass), Class)}, {Enum.GetName(typeof(AssetLocation), Location)}, target: {Target}";
        }
    }
}
