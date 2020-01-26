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
        public static double GetScoreWeight(AssetClass c, AssetLocation l, AccountType t)
        {
            if (c == AssetClass.Stock && l == AssetLocation.Domestic)
            {
                // stock, domestic
                switch (t)
                {
                    case AccountType.BROKERAGE: return 0; // really do not want this
                    case AccountType.IRA:       return 1; // fine
                    case AccountType.ROTH:      return 1; // fine
                    default: return 0;
                }
            }
            else if(c == AssetClass.Stock && l == AssetLocation.International)
            {     
                switch (t)
                {
                    case AccountType.BROKERAGE: return 1; // really want this
                    case AccountType.IRA:       return 0; // neither of these are great
                    case AccountType.ROTH:      return 0;
                    default: return 0;
                }
            }
            else if (c == AssetClass.Bond && l == AssetLocation.Domestic)
            {
                // bond, domestic
                switch (t)
                {
                    case AccountType.BROKERAGE: return 1; // you have to put something in the brokerage accounts
                    case AccountType.IRA:       return .25; // not ideal
                    case AccountType.ROTH:      return 0; // the worst
                    // default: return 0;
                }
            }
            else if (c == AssetClass.Bond && l == AssetLocation.International)
            {
                // bond, international
                switch (t){
                    case AccountType.BROKERAGE: return 1; // low growth + foreign income tax credit
                    case AccountType.IRA:       return 0; // neither of these are great
                    case AccountType.ROTH:      return 0;
                    default: return 0;
                }
            }

            // anything else gets no points
            return 0;
        }
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
