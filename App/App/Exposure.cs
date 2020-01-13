using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    /// <summary>
    /// Based on the target exposures and total money, compute target dollar-value per exposure type.
    ///  Strategy: 
    ///  * accounts prefer funds sponsored by their brokerage
    ///    * Helps avoid fees?
    ///
    ///  * roth accounts should prioritize stocks over bonds
    ///    * Growth is not taxable, prioritize high-growth potential products.
    ///
    ///  * taxable accounts should prioritize international assets over domestic
    ///    * foreign income tax credit
    /// 
    ///  * 401k accounts should prioritize bonds and avoid international assets
    ///    Because growth is taxable, prioritize low-growth products
    ///
    /// This basically works out to the following exposure-to-account type prioritization list:
    ///  dom stocks -> roth, tax, 401k
    ///  int stocks -> tax, roth, 401k
    ///  dom bonds  -> 401k, roth, tax
    ///  int bonds  -> tax, 401k, roth
    /// </summary>
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


    // public class ExposureRatio : Exposure
    // {
    //     /// <summary>
    //     /// final value ratio
    //     /// </summary>
    //     public double Ratio { get; set; }

    //     public ExposureRatio(
    //         AssetClass c,
    //         AssetLocation l,
    //         double r)
    //         : base(c, l)
    //     {
    //         if (double.IsNaN(r))
    //         {
    //             r = 0.0;
    //         }

    //         Ratio = Math.Max(0, r);
    //     }
    // }

    /// <summary>
    /// Add some functionality to lists of ExposureRatio
    /// </summary>
    public static class ExposureExtensions
    {
        public static double Percent(
            this IList<Exposure> exposures,
            AssetClass c)
        {
            return 100.0 * exposures.Where(x => x.Class == c).Sum(x => x.Value);
        }

        public static double Percent(
            this IList<Exposure> exposures,
            AssetLocation l)
        {
            return 100.0 * exposures.Where(x => x.Location == l).Sum(x => x.Value);
        }

        public static double Percent(
            this IList<Exposure> exposures,
            AssetClass c,
            AssetLocation l)
        {
            return 100.0 * exposures.Where(x => x.Class == c && x.Location == l).Sum(x => x.Value);
        }
    }
}
