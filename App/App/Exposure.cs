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

        public static AccountType[] GetPreferences(
            AssetClass c,
            AssetLocation l)
        {
            // generate type preferences
            if (c == AssetClass.Stock)
            {
                if (l == AssetLocation.Domestic)
                {
                    // stock, domestic
                    return new AccountType[] {
                        AccountType.ROTH,
                        AccountType.BROKERAGE,
                        AccountType.IRA
                    };
                }
                else 
                {
                    // stock, international
                    return new AccountType[] {
                        AccountType.BROKERAGE,
                        AccountType.ROTH,
                        AccountType.IRA
                    };
                }
            }
            else 
            {
                if (l == AssetLocation.Domestic)
                {
                    // bond, domestic
                    return new AccountType[] {
                        AccountType.IRA,
                        AccountType.BROKERAGE,
                        AccountType.ROTH,
                    };
                }
                else 
                {
                    // bond, international
                    return new AccountType[] {
                        AccountType.BROKERAGE,
                        AccountType.IRA,
                        AccountType.ROTH
                    };
                }
            }
        }
    }
}
