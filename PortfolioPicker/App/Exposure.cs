using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioPicker.App
{
    public class Exposure
    {
        public Exposure(
            AssetClass c,
            AssetLocation l,
            decimal v,
            AccountType[] types)
        {
            Class = c;
            Location = l;
            Value = v;
            AccountTypesPreference = types;
        }

        public AssetClass Class { get; }
        public AssetLocation Location { get; }
        public decimal Value { get; set; }
        public AccountType[] AccountTypesPreference { get; set; }
    }
}
