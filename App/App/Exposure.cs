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

        public AccountType[] Preferences => GetPreferences(this.Class, this.Location);


        /// <summary>
        /// Based on the target exposures and total money, compute target dollar-value per exposure type.
        ///  Strategy: 
        ///  * accounts prefer funds sponsored by their brokerage
        ///    * Helps avoid fees?
        ///
        ///  * roth accounts should prioritize stocks over bonds
        ///    * growth can be withdrawn tax-free, prioritize high-growth-potential products.
        ///
        ///  * regular brokerage accounts should prioritize international assets over domestic
        ///    * foreign income tax credit is deductible
        /// 
        ///  * 401k accounts should prioritize bonds and avoid international assets
        ///    * because growth is taxable, prioritize low-growth products
        /// 
        ///  * tax-advantaged accounts should be generally preferred over brokerage accounts
        //
        /// </summary>
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
                        AccountType.IRA,
                        AccountType.BROKERAGE,
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
                        AccountType.BROKERAGE, // not ideal, but you have to put something in the brokerage accounts
                        AccountType.IRA,
                        AccountType.ROTH, // this is the worst
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
