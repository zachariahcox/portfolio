using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    public class Exposure
    {
        public AssetClass Class { get; private set; }

        public AssetLocation Location { get; private set; }

        public double Value { get; set; }

        public Exposure(
            AssetClass c, 
            AssetLocation l,
            double value = 0.0)
        {
            Class = c;
            Location = l;

            if (double.IsNaN(value))
                value = 0.0;

            Value = value;
        }

        public override string ToString() => $"{Class}:{Location}:{Value}";
    }

    public class ExposureAccountTypePreference
    {
        public ExposureAccountTypePreference(
            AssetClass c, 
            AssetLocation l, 
            ICollection<AccountType> prefs)
        {
            Class = c;
            Location = l;
            Preferences = prefs;
        }
        public AssetClass Class { get; set; }

        public AssetLocation Location { get; set; }

        public ICollection<AccountType> Preferences { get; set;}
    }
}
